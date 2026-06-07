using UnityEngine;
using System.Collections;

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
    private Transform spawnPoint;
    private Quaternion initialRotation;
    [SerializeField] private float pushForce = 1000f;
    private bool canRotate = true;
    private bool isInSafeZone = false;

    // Última posição segura (em chão firme) — usada para reviver sem cair na água/vazio.
    private Vector3 lastSafePosition;
    private Quaternion lastSafeRotation;
    private bool temPosicaoSegura = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    private void Awake()
    {
        initialRotation = transform.rotation;
        GameObject spawnObj = GameObject.FindGameObjectWithTag("SpawnPoint");

        if (spawnObj != null)
        {
            spawnPoint = spawnObj.transform;
        }
        else
        {
            spawnPoint = null;
            Debug.LogWarning("SpawnPoint não encontrado na cena!");
        }
        rb = GetComponent<Rigidbody>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Inicializa a posição segura com a posição inicial (ou o spawn).
        lastSafePosition = spawnPoint != null ? spawnPoint.position : transform.position;
        lastSafeRotation = initialRotation;
        temPosicaoSegura = true;

        // Adiciona automaticamente o gerador de UI mobile
        if (GetComponent<MobileControlsGenerator>() == null)
        {
            gameObject.AddComponent<MobileControlsGenerator>();
        }
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
        AtualizarPosicaoSegura();
    }

    /// <summary>
    /// Memoriza a última posição em chão firme (parado/andando, sem estar morrendo
    /// nem caindo). Serve como ponto de revive seguro quando o player morre na água/vazio.
    /// </summary>
    private void AtualizarPosicaoSegura()
    {
        if (isDying) return;

        // Só considera seguro se estiver no chão e não estiver caindo/subindo rápido.
        if (IsGrounded() && Mathf.Abs(rb.linearVelocity.y) < 1.5f)
        {
            lastSafePosition = rb.position;
            lastSafeRotation = rb.rotation;
            temPosicaoSegura = true;
        }
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
        if (!canRotate) return;

        Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        forward.y = 0f;
        forward.Normalize();

        if (moveInput.sqrMagnitude < 0.01f)
            return;

        Vector3 direction = forward * Mathf.Max(0, moveInput.y)
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

    private bool isDying = false;

    public void KillPlayer()
    {
        // Evita iniciar várias rotinas de morte (e várias telas de morte sobrepostas)
        // se o player for atingido de novo enquanto já está morrendo.
        if (isDying) return;
        StartCoroutine(KillPlayerRoutine());
    }

    private IEnumerator KillPlayerRoutine()
    {
        isDying = true;

        // Guarda onde o jogador morreu (para a opção de reviver no local).
        Vector3 deathPosition = rb.position;
        Quaternion deathRotation = rb.rotation;

        animator.SetBool("isDead", true);

        canRotate = false;
        SetMovementEnabled(false);

        var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;

        var inputControllers = FindObjectsByType<Unity.Cinemachine.CinemachineInputAxisController>(FindObjectsSortMode.None);
        foreach (var ctrl in inputControllers)
        {
            ctrl.enabled = false;
        }

        // Deixa a animação de morte tocar antes de mostrar a tela.
        yield return new WaitForSeconds(2f);

        // Mostra a tela de morte e espera a decisão do jogador.
        bool decidiu = false;
        bool reviverNoLocal = false;
        DeathScreenUI.Mostrar(
            onRevive: () => { reviverNoLocal = true; decidiu = true; },
            onRestart: () => { reviverNoLocal = false; decidiu = true; }
        );
        yield return new WaitUntil(() => decidiu);

        // Reabilita os controles.
        SetMovementEnabled(true);
        if (playerInput != null) playerInput.enabled = true;
        foreach (var ctrl in inputControllers)
        {
            if (ctrl != null) ctrl.enabled = true;
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (reviverNoLocal)
        {
            // Revive na última posição SEGURA (chão firme). Assim, se o player morreu
            // caindo na água ou no vazio, ele volta para um lugar seguro em vez de
            // renascer na água. Para mortes em terra firme, é praticamente o mesmo lugar.
            if (temPosicaoSegura)
            {
                rb.position = lastSafePosition + Vector3.up * 0.5f;
                rb.rotation = lastSafeRotation;
            }
            else
            {
                rb.position = deathPosition + Vector3.up * 0.5f;
                rb.rotation = deathRotation;
            }
        }
        else if (spawnPoint != null)
        {
            // Voltar ao início da fase (comportamento antigo).
            rb.position = spawnPoint.position;
            rb.rotation = initialRotation;
        }

        canRotate = true;
        SetMovementEnabled(true);
        animator.SetBool("isDead", false);

        var playerSound = GetComponent<PlayerSound>();
        if (playerSound != null)
        {
            playerSound.PlaySpawn();
        }

        // Pequena janela de invulnerabilidade ao reviver, para não morrer
        // instantaneamente caso reviva em cima da armadilha/mob que o matou.
        yield return new WaitForSeconds(reviverNoLocal ? 1.0f : 0.2f);
        isDying = false;
    }

    public void Push(Vector3 direction)
    {
        // remove velocidade atual (opcional, deixa o knockback mais consistente)
        rb.linearVelocity = Vector3.zero;

        // aplica força
        rb.AddForce(direction.normalized * pushForce, ForceMode.Impulse);
    }

    /// <summary>
    /// Chamado quando o player entra em uma safe zone
    /// </summary>
    public void EnterSafeZone()
    {
        isInSafeZone = true;
        Debug.Log("Player entrou na Safe Zone!");
    }

    /// <summary>
    /// Chamado quando o player sai de uma safe zone
    /// </summary>
    public void ExitSafeZone()
    {
        isInSafeZone = false;
        Debug.Log("Player saiu da Safe Zone!");
    }

    /// <summary>
    /// Verifica se o player está em uma safe zone
    /// </summary>
    public bool IsInSafeZone()
    {
        return isInSafeZone;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateAnimator();
    }
}
