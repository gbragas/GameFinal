using UnityEngine;
using UnityEngine.Events;

public class CollectableController : MonoBehaviour
{
    public float rotationSpeed = 100f;
    public float hoverSpeed = 2f;
    public float hoverHeight = 0.5f;

    [Header("Eventos de Proximidade")]
    public UnityEvent OnPlayerEnter;
    public UnityEvent OnPlayerExit;

    private Vector3 startPosition;

    void Awake()
    {
        // Remove IMEDIATAMENTE qualquer Rigidbody para que a gravidade
        // nunca tenha chance de agir (DestroyImmediate age na hora).
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) DestroyImmediate(rb);

        foreach (Rigidbody childRb in GetComponentsInChildren<Rigidbody>())
        {
            DestroyImmediate(childRb);
        }

        // Transforma TODOS os Colliders em Trigger para que o player
        // possa atravessar a bolsa sem travar.
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.isTrigger = true;
        }
    }

    void Start()
    {
        // Salva a posição onde a bolsa foi colocada na cena
        startPosition = transform.position;
    }

    void Update()
    {
        // === ROTAÇÃO: gira suavemente no eixo Y do mundo ===
        transform.rotation *= Quaternion.AngleAxis(rotationSpeed * Time.deltaTime, Vector3.up);

        // === FLUTUAÇÃO: sobe e desce no eixo Y ===
        float newY = startPosition.y + Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OnPlayerEnter.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OnPlayerExit.Invoke();
        }
    }

    public void Collect()
    {
        Destroy(gameObject);
    }
}
