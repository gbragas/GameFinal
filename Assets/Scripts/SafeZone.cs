using UnityEngine;

public class SafeZone : MonoBehaviour
{
    public static SafeZone Instance { get; private set; }
    
    private bool playerInSafeZone = false;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInSafeZone = true;
            playerMovement = other.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.EnterSafeZone();
            }
        }
        
        // Mobs não podem entrar na safe zone - empurra para fora
        if (other.CompareTag("Mob"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 direction = (other.transform.position - transform.position).normalized;
                rb.linearVelocity = direction * 5f;
                rb.AddForce(direction * 1000f, ForceMode.Impulse);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Mantém mobs fora da safe zone empurrando continuamente
        if (other.CompareTag("Mob"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 direction = (other.transform.position - transform.position).normalized;
                rb.linearVelocity = new Vector3(direction.x * 5f, rb.linearVelocity.y, direction.z * 5f);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInSafeZone = false;
            if (playerMovement != null)
            {
                playerMovement.ExitSafeZone();
            }
        }
    }

    /// <summary>
    /// Verifica se o player está dentro da safe zone
    /// </summary>
    public bool IsPlayerInSafeZone()
    {
        return playerInSafeZone;
    }
}
