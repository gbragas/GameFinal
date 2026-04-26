using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tipos de gatilho disponíveis para iniciar o diálogo.
/// </summary>
public enum TipoGatilho
{
    /// <summary>Iniciado por código externo ou botão via UnityEvent.</summary>
    Manual,

    /// <summary>Jogador entra em uma zona (Collider com Is Trigger ativado).</summary>
    ZonaDeEntrada,

    /// <summary>Jogador entra na zona E pressiona uma tecla para interagir.</summary>
    Interagir,

    /// <summary>Pressionar uma tecla em qualquer lugar da cena.</summary>
    PressionarTecla,

    /// <summary>Jogador fica dentro de um raio de distância do objeto.</summary>
    Distancia,
}

/// <summary>
/// Coloque este script em qualquer GameObject para controlar QUANDO o diálogo é disparado.
/// Referencia um componente Dialogo e chama TocarDialogo() conforme o gatilho configurado.
///
/// Modos disponíveis:
///   Manual        → Chame TriggerDialogo() por código ou botão UI.
///   ZonaDeEntrada → Adicione um Collider (Is Trigger = true) neste GameObject.
///   Interagir     → Entra na zona + pressiona a tecla de interação.
///   PressionarTecla → Pressiona tecla a qualquer momento.
///   Distancia     → Jogador entra no raio configurado.
/// </summary>
public class DialogoTrigger : MonoBehaviour
{
    [Header("Referência ao Diálogo")]
    [Tooltip("Componente Dialogo que será disparado. Pode estar em qualquer GameObject da cena.")]
    [SerializeField] private Dialogo dialogo;

    [Header("Tipo de Gatilho")]
    [SerializeField] private TipoGatilho tipoGatilho = TipoGatilho.ZonaDeEntrada;

    [Header("Filtro de Jogador")]
    [Tooltip("Tag do jogador para filtrar colisões e distância.")]
    [SerializeField] private string tagJogador = "Player";

    [Header("Configurações — Tecla (PressionarTecla / Interagir)")]
    [Tooltip("Tecla usada para disparar ou interagir.")]
    [SerializeField] private KeyCode teclaDiálogo = KeyCode.E;

    [Header("Configurações — Distância")]
    [Tooltip("Distância máxima (unidades) para disparar o diálogo automaticamente.")]
    [SerializeField] private float raioDistancia = 3f;
    [Tooltip("Referência ao Transform do jogador (preenchida automaticamente se GameObject tiver a tag).")]
    [SerializeField] private Transform transformJogador;

    [Header("Opções")]
    [Tooltip("Se verdadeiro, o diálogo só pode ser disparado uma vez.")]
    [SerializeField] private bool dispararApenasUmaVez = true;

    [Tooltip("Exibe um ícone/dica na tela enquanto o jogador está na zona de interação.")]
    [SerializeField] private GameObject iconeInteragir;

    [Header("Eventos (Opcionais)")]
    [Tooltip("Chamado quando o diálogo é disparado.")]
    [SerializeField] private UnityEvent aoDispararDialogo;

    [Tooltip("Chamado quando toda a narração termina.")]
    [SerializeField] private UnityEvent aoFinalizarDialogo;

    // ── Estado interno ────────────────────────────────────────────────────────

    private bool foiDisparado = false;
    private bool jogadorNaZona = false;

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    private void Start()
    {
        // Tenta encontrar o jogador automaticamente se não foi configurado
        if (transformJogador == null)
        {
            GameObject jogador = GameObject.FindWithTag(tagJogador);
            if (jogador != null)
                transformJogador = jogador.transform;
        }

        // Esconde o ícone de interação no início
        if (iconeInteragir != null)
            iconeInteragir.SetActive(false);

        // Subscreve ao evento de conclusão do Diálogo
        if (dialogo != null)
            dialogo.OnDialogoFinalizado += () => aoFinalizarDialogo?.Invoke();
    }

    private void Update()
    {
        switch (tipoGatilho)
        {
            case TipoGatilho.PressionarTecla:
                if (Input.GetKeyDown(teclaDiálogo))
                    TentarDisparar();
                break;

            case TipoGatilho.Interagir:
                if (jogadorNaZona && Input.GetKeyDown(teclaDiálogo))
                    TentarDisparar();
                break;

            case TipoGatilho.Distancia:
                if (transformJogador != null)
                {
                    float dist = Vector3.Distance(transform.position, transformJogador.position);
                    if (dist <= raioDistancia)
                        TentarDisparar();
                }
                break;
        }
    }

    // ── Colisões ──────────────────────────────────────────────────────────────

    // Suporte 3D
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(tagJogador)) return;
        jogadorNaZona = true;
        MostrarIcone(true);

        if (tipoGatilho == TipoGatilho.ZonaDeEntrada)
            TentarDisparar();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(tagJogador)) return;
        jogadorNaZona = false;
        MostrarIcone(false);
    }

    // Suporte 2D
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(tagJogador)) return;
        jogadorNaZona = true;
        MostrarIcone(true);

        if (tipoGatilho == TipoGatilho.ZonaDeEntrada)
            TentarDisparar();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(tagJogador)) return;
        jogadorNaZona = false;
        MostrarIcone(false);
    }

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>
    /// Dispara o diálogo manualmente. Use em botões UI ou chamadas de outros scripts.
    /// Funciona com qualquer TipoGatilho, inclusive Manual.
    /// </summary>
    public void TriggerDialogo()
    {
        TentarDisparar();
    }

    /// <summary>
    /// Reseta o estado para permitir disparar novamente (mesmo com dispararApenasUmaVez = true).
    /// </summary>
    public void Resetar()
    {
        foiDisparado = false;
    }

    // ── Lógica interna ────────────────────────────────────────────────────────

    private void TentarDisparar()
    {
        if (dispararApenasUmaVez && foiDisparado) return;
        if (dialogo == null)
        {
            Debug.LogWarning("[DialogoTrigger] Nenhum Dialogo referenciado no Inspector.");
            return;
        }

        foiDisparado = true;
        MostrarIcone(false);

        dialogo.TocarDialogo();
        aoDispararDialogo?.Invoke();
    }

    private void MostrarIcone(bool mostrar)
    {
        if (iconeInteragir == null) return;
        // Só mostra o ícone no modo Interagir (aguarda input do jogador)
        if (tipoGatilho == TipoGatilho.Interagir)
            iconeInteragir.SetActive(mostrar);
    }

    // ── Debug visual ──────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (tipoGatilho != TipoGatilho.Distancia) return;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, raioDistancia);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, raioDistancia);
    }
}
