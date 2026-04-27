using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{

    public InputAction interagir;
    public float interactRange = 2f;

    // private InteracaoPlayer interacao;
    private PlayerMovement movement;
    // private PlayerInteraction interaction;

    // public PlayerCombat combat;

    private void Awake()
    {
        // interacao = GetComponent<InteracaoPlayer>();
        movement = GetComponent<PlayerMovement>();
        // interaction = GetComponent<PlayerInteraction>();
        // combat = GetComponent<PlayerCombat>();
    }

    private void OnEnable()
    {
        interagir.Enable();
        interagir.performed += OnInteract;
    }

    private void OnDisable()
    {
        interagir.performed -= OnInteract;
        interagir.Disable();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if(context.performed)
        {
            // if(interaction != null) interaction.TryInteract();
            
            // Busca objetos interativos (Collectables) perto do jogador
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactRange);
            foreach (var hitCollider in hitColliders)
            {
                CollectableController collectable = hitCollider.GetComponent<CollectableController>();
                if (collectable != null)
                {
                    collectable.Collect();
                    break; // interage apenas com o primeiro que encontrar
                }
            }
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if(movement != null) movement.SetMoveInput(context.ReadValue<Vector2>());
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if(movement != null)
        {
            if(context.performed) movement.SetSprinting(true);
            else if(context.canceled) movement.SetSprinting(false);
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if(movement != null && context.performed) movement.Jump();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        // if(combat == null) return;
        // if(context.performed) combat.LightAttack();
    }

    public void OnHeavyAttack(InputAction.CallbackContext context)
    {
        // if(combat == null) return;
        // if(context.performed) combat.HeavyAttack();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
