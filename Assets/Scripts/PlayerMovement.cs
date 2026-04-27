using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public float jumpForce = 6.5f;
    public float walkSpeed = 2.5f;
    public float runSpeed = 5f;
    public float rotationSpeed = 1f;
    public float rotationSpeedWalking = 2f;
    public float rotationSpeedRunning = 3f;
    public Transform cameraTransform;
    public Animator animator;
    public float animationSmooth = 0.08f;
    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isSprinting;
    private float currentAnimX, currentAnimY;
    private float animXVelocity, animYVelocity;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    public void SetSprinting(bool sprinting)
    {
        isSprinting = sprinting;
    }
    public void Jump()
    {
        if (IsGrounded())
        {
            animator.SetTrigger("isJumping");
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private bool IsGrounded()
    {
        float distance = 0.3f; // aumenta isso
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        return Physics.Raycast(origin, Vector3.down, distance);
    }

    private void FixedUpdate()
    {
        MovePlayer();
        RotatePlayer();
    }

    private void MovePlayer()
    {
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * moveInput.y + right * moveInput.x;
        float currentSpeed = isSprinting ? runSpeed : walkSpeed;
        Vector3 velocity = new Vector3(
            move.x * currentSpeed, rb.linearVelocity.y, move.z * currentSpeed
        );
        rb.linearVelocity = velocity;
    }

    private void RotatePlayer()
    {
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        forward.y = 0f;
        forward.Normalize();

        // 👉 só rotaciona se tiver input relevante
        if (moveInput.sqrMagnitude < 0.01f)
            return;

        // 👉 direção baseada na câmera + input lateral
        Vector3 direction = forward * Mathf.Max(0, moveInput.y) // ignora andar pra trás
                        + cameraTransform.right * moveInput.x;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        bool isMovingBackward = moveInput.y < -0.1f;
        bool isWalking = moveInput.sqrMagnitude > 1f;
        float currentRotationSpeed = isWalking && !isMovingBackward ? rotationSpeedWalking : rotationSpeed;

        Quaternion newRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            currentRotationSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(newRotation);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        Vector2 animInput = Vector2.zero;

        if (moveInput.sqrMagnitude > 0.001f)
        {
           float locomotionAmount = isSprinting ? 1f : 0.5f;

           animInput.x = Mathf.Abs(moveInput.x) > 0.01f ? Mathf.Sign(moveInput.x) * locomotionAmount : 0f;
           animInput.y = Mathf.Abs(moveInput.y) > 0.01f ? Mathf.Sign(moveInput.y) * locomotionAmount : 0f;
        }

        currentAnimX = Mathf.SmoothDamp(
            currentAnimX, animInput.x, ref animXVelocity, animationSmooth
        );
        currentAnimY = Mathf.SmoothDamp(
            currentAnimY, animInput.y, ref animYVelocity, animationSmooth
        );

        animator.SetFloat("Horizontal", currentAnimX);
        animator.SetFloat("Vertical", currentAnimY);
    }

    public void SetMovementEnabled(bool enabled)
    {
        if (enabled)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
        else
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateAnimator();
    }
}
