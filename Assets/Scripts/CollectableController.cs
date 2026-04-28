using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CollectableController : MonoBehaviour
{
    public float rotationSpeed = 100f;
    public float hoverSpeed = 2f;
    public float hoverHeight = 0.5f;

    [Header("Identificação da Memória")]
    [Tooltip("Prefab original deste collectable (arraste o mesmo prefab aqui). " +
             "Usado para exibir o modelo 3D na cutscene de memória.")]
    public GameObject collectablePrefab;

    [Header("Eventos de Proximidade")]
    public UnityEvent OnPlayerEnter;
    public UnityEvent OnPlayerExit;

	[Header("Efeitos sonoros")]
	public List<AudioClip> soundFx;
	private AudioSource audioSource;

    private Vector3 startPosition;
    private bool jaColetado = false; // Impede coleta dupla

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
		audioSource = GetComponent<AudioSource>();
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
            Collect(); // Coleta automaticamente ao tocar
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
		AudioClip clip = soundFx[Random.Range(0, soundFx.Count)];

        audioSource.clip = clip;
        audioSource.volume = Random.Range(0.2f, 0.5f);
        audioSource.pitch = Random.Range(0.8f, 1.2f);
        audioSource.Play();

        if (jaColetado) return; // Já foi coletado, ignora chamadas duplicadas
        jaColetado = true;

        // Registra a coleta no GameManager, passando o prefab para a cutscene
        if (GameManager.Instance != null)
        {
            // Usa o prefab configurado, ou o próprio GameObject como fallback
            GameObject prefabParaCutscene = collectablePrefab != null ? collectablePrefab : gameObject;
            GameManager.Instance.RegistrarColeta(prefabParaCutscene);
        }

        if (transform.parent != null)
        {
            Destroy(transform.parent.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
