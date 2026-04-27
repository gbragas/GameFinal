using UnityEngine;

/// <summary>
/// Ativa um sistema de confete ou objeto quando o jogador entra no gatilho.
/// </summary>
public class ConfettiTrigger : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("O objeto de confete que será ativado (ex: Confetti_directional_multicolor).")]
    [SerializeField] private GameObject confettiObject;

    [Tooltip("Tag do jogador para ativar o gatilho.")]
    [SerializeField] private string tagJogador = "Player";

    [Tooltip("Se verdadeiro, o confete só será ativado uma vez.")]
    [SerializeField] private bool apenasUmaVez = true;

    private bool jaAtivou = false;

    private void Awake()
    {
        // Garante que o confete comece desativado se o script for responsável por ele
        if (confettiObject != null)
        {
            confettiObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagJogador))
        {
            AtivarConfete();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(tagJogador))
        {
            AtivarConfete();
        }
    }

    public void AtivarConfete()
    {
        if (apenasUmaVez && jaAtivou) return;

        if (confettiObject != null)
        {
            Debug.Log($"[ConfettiTrigger] Ativando confete: {confettiObject.name}");
            confettiObject.SetActive(true);
            jaAtivou = true;

            // Se for um ParticleSystem, garante que ele toque
            ParticleSystem ps = confettiObject.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }
        }
        else
        {
            Debug.LogWarning("[ConfettiTrigger] Nenhum objeto de confete atribuído no Inspector!");
        }
    }
}
