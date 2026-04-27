using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HammerImpulseTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool oneImpulsePerTouch = true;

    private bool hasImpulseedOnCurrentTouch;

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
        TryImpulse(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Cobre casos em que o player entra já sobreposto ao trigger.
        TryImpulse(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        hasImpulseedOnCurrentTouch = false;
    }

    private void TryImpulse(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (oneImpulsePerTouch && hasImpulseedOnCurrentTouch)
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

        Vector3 closestPoint = other.ClosestPoint(transform.position);
        Vector3 direction = closestPoint - transform.position;
        
        movement.Push(direction);

        hasImpulseedOnCurrentTouch = true;
    }
}
