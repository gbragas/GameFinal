using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Gerencia troca de cenas com suporte a delay, colisão 2D/3D e eventos.
/// Coloque num GameObject com Collider (Is Trigger = true) OU chame os métodos por código/botão.
/// </summary>
public class TrocarDeCena : MonoBehaviour
{
    [Header("Cena Destino")]
    [Tooltip("Nome da cena a carregar (deve estar adicionada em File > Build Settings).")]
    [SerializeField] private string nomeDaCena;

    [Tooltip("Usar índice de build em vez do nome. -1 = usar o nome acima.")]
    [SerializeField] private int indiceDeCena = -1;

    [Header("Filtro")]
    [Tooltip("Tag do objeto que pode ativar a troca de cena pelo Trigger.")]
    [SerializeField] private string tagJogador = "Player";

    [Header("Comportamento")]
    [Tooltip("Tempo de espera (segundos) antes de trocar a cena. 0 = instantâneo.")]
    [SerializeField] private float delayAntesDeTrocar = 0f;

    [Tooltip("Se verdadeiro, só troca uma vez (evita chamadas duplicadas).")]
    [SerializeField] private bool apenasUmaVez = true;

    [Header("Eventos")]
    [Tooltip("Chamado quando a troca de cena é iniciada (antes do delay).")]
    [SerializeField] private UnityEvent aoIniciarTroca;

    [Tooltip("Chamado imediatamente antes de carregar a cena (após o delay).")]
    [SerializeField] private UnityEvent aoCarregarCena;

    private bool foiAtivado = false;

    // ── Colisões ──────────────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider outro)
    {
        if (outro.CompareTag(tagJogador))
            IniciarTroca();
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (outro.CompareTag(tagJogador))
            IniciarTroca();
    }

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>
    /// Inicia a troca de cena. Chame por botão UI, AnimationEvent ou outro script.
    /// </summary>
    public void IniciarTroca()
    {
        if (apenasUmaVez && foiAtivado) return;
        foiAtivado = true;

        aoIniciarTroca?.Invoke();

        if (delayAntesDeTrocar > 0f)
            StartCoroutine(TrocarComDelay());
        else
            ExecutarTroca();
    }

    /// <summary>
    /// Troca para uma cena específica pelo nome, ignorando o campo configurado.
    /// </summary>
    public void TrocarPara(string nome)
    {
        StartCoroutine(TrocarComDelayNome(nome));
    }

    /// <summary>
    /// Recarrega a cena atual.
    /// </summary>
    public void RecarregarCenaAtual()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Reseta o estado para permitir trocar novamente (quando apenasUmaVez = true).
    /// </summary>
    public void Resetar()
    {
        foiAtivado = false;
    }

    // ── Lógica interna ────────────────────────────────────────────────────────

    private IEnumerator TrocarComDelay()
    {
        yield return new WaitForSeconds(delayAntesDeTrocar);
        ExecutarTroca();
    }

    private IEnumerator TrocarComDelayNome(string nome)
    {
        if (delayAntesDeTrocar > 0f)
            yield return new WaitForSeconds(delayAntesDeTrocar);

        aoCarregarCena?.Invoke();
        SceneManager.LoadScene(nome);
    }

    private void ExecutarTroca()
    {
        aoCarregarCena?.Invoke();

        if (indiceDeCena >= 0)
        {
            Debug.Log($"[TrocarDeCena] Carregando cena índice {indiceDeCena}");
            SceneManager.LoadScene(indiceDeCena);
        }
        else if (!string.IsNullOrEmpty(nomeDaCena))
        {
            Debug.Log($"[TrocarDeCena] Carregando cena \"{nomeDaCena}\"");
            SceneManager.LoadScene(nomeDaCena);
        }
        else
        {
            Debug.LogWarning("[TrocarDeCena] Nenhuma cena configurada (nome ou índice).");
        }
    }

    // ── Gizmo visual no Editor ────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.25f);

        // Tenta usar o tamanho do Collider, senão usa uma esfera padrão
        var box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
            Gizmos.DrawWireCube(box.center, box.size);
            return;
        }

        var box2d = GetComponent<BoxCollider2D>();
        if (box2d != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box2d.offset, box2d.size);
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
            Gizmos.DrawWireCube(box2d.offset, box2d.size);
            return;
        }

        // Fallback: esfera
        Gizmos.DrawSphere(transform.position, 1f);
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}
