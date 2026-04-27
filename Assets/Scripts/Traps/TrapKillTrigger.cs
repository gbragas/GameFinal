using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TrapKillTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool oneKillPerTouch = true;

    private bool hasKilledOnCurrentTouch;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryKill(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Cobre casos em que o player entra já sobreposto ao trigger.
        TryKill(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        hasKilledOnCurrentTouch = false;
    }

    private void TryKill(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (oneKillPerTouch && hasKilledOnCurrentTouch)
        {
            return;
        }

        var movement = other.GetComponentInParent<PlayerMovement>();
        if (movement == null)
        {
            movement = other.GetComponent<PlayerMovement>();
        }

        if (movement == null)
        {
            return;
        }

        movement.KillPlayer();
        hasKilledOnCurrentTouch = true;
    }
}
