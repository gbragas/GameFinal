using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Singleton persistente que gerencia o estado global do jogo:
///   • Total de itens coletados (0–6, somados entre todos os mapas)
///   • Itens coletados no mapa atual (0–3)
///   • Troca do modelo visual do Player (Player-v0 → Player-v6)
///   • HUD "Colete os itens (X/3)"
/// 
/// ▸ Coloque num GameObject vazio na PRIMEIRA cena que carrega (ex.: Menu).
/// ▸ Arraste os 7 prefabs (Player-v0 … Player-v6) no Inspector.
/// ▸ O script sobrevive entre cenas (DontDestroyOnLoad).
/// </summary>
public class GameManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ── Configuração ─────────────────────────────────────────────────────────
    [Header("Prefabs do Player (v0 a v6)")]
    [Tooltip("Arraste os 7 prefabs na ordem: Player-v0, Player-v1, … Player-v6.")]
    public GameObject[] playerPrefabs = new GameObject[7];

    [Header("Itens por Mapa")]
    [Tooltip("Quantos itens existem em cada mapa.")]
    public int itensPorMapa = 3;

    // ── Estado ────────────────────────────────────────────────────────────────
    /// <summary>Total absoluto de itens coletados em toda a partida (0–6).</summary>
    public int TotalColetados { get; private set; } = 0;

    /// <summary>Itens coletados no mapa ATUAL (reseta ao trocar de cena).</summary>
    public int ColetadosNoMapa { get; private set; } = 0;

    // ── Referência ao player vivo na cena ────────────────────────────────────
    private GameObject playerAtual;

    // ── Evento para a HUD se inscrever ───────────────────────────────────────
    public event System.Action<int, int> OnItemColetado;   // (coletadosNoMapa, itensPorMapa)
    public event System.Action OnMapaCompleto;             // todos os 3 itens do mapa

    // ─────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Troca de cena
    // ─────────────────────────────────────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reseta a contagem do mapa atual
        ColetadosNoMapa = 0;

        // Tenta encontrar o player existente na cena
        playerAtual = GameObject.FindWithTag("Player");

        if (playerAtual != null)
        {
            // Substitui pelo prefab correto (versão = TotalColetados)
            SubstituirPlayer();
        }

        // Notifica a HUD nova para atualizar
        OnItemColetado?.Invoke(ColetadosNoMapa, itensPorMapa);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Coletar item  (chamado pelo CollectableController)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Registra a coleta de um item. Incrementa contadores e troca o prefab
    /// visual do jogador.
    /// </summary>
    public void RegistrarColeta()
    {
        ColetadosNoMapa++;
        TotalColetados = Mathf.Clamp(TotalColetados + 1, 0, playerPrefabs.Length - 1);

        Debug.Log($"[GameManager] Item coletado! Mapa: {ColetadosNoMapa}/{itensPorMapa} | Total: {TotalColetados}");

        // Troca o modelo do player
        SubstituirPlayer();

        // Notifica a HUD
        OnItemColetado?.Invoke(ColetadosNoMapa, itensPorMapa);

        // Verifica se completou o mapa
        if (ColetadosNoMapa >= itensPorMapa)
        {
            Debug.Log("[GameManager] Mapa completo! Todos os itens foram coletados.");
            OnMapaCompleto?.Invoke();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Troca de prefab do Player
    // ─────────────────────────────────────────────────────────────────────────

    private void SubstituirPlayer()
    {
        int versao = Mathf.Clamp(TotalColetados, 0, playerPrefabs.Length - 1);

        if (playerPrefabs[versao] == null)
        {
            Debug.LogWarning($"[GameManager] Prefab Player-v{versao} não está atribuído!");
            return;
        }

        // Encontra o player atual na cena
        playerAtual = GameObject.FindWithTag("Player");
        if (playerAtual == null)
        {
            Debug.LogWarning("[GameManager] Nenhum objeto com tag 'Player' encontrado na cena.");
            return;
        }

        // Guarda posição, rotação e dados relevantes
        Vector3 posicao = playerAtual.transform.position;
        Quaternion rotacao = playerAtual.transform.rotation;
        Transform parentOriginal = playerAtual.transform.parent;

        // Instancia o novo player
        GameObject novoPlayer = Instantiate(playerPrefabs[versao], posicao, rotacao, parentOriginal);
        novoPlayer.name = $"Player-v{versao}";
        novoPlayer.tag = "Player";

        // Transfere a referência da câmera (se o PlayerMovement usar uma)
        PlayerMovement movimentoAntigo = playerAtual.GetComponent<PlayerMovement>();
        PlayerMovement movimentoNovo = novoPlayer.GetComponent<PlayerMovement>();

        if (movimentoAntigo != null && movimentoNovo != null)
        {
            movimentoNovo.cameraTransform = movimentoAntigo.cameraTransform;
        }

        // Se houver câmera Cinemachine/Follow, atualiza o target
        AtualizarCameraTarget(novoPlayer.transform);

        // Destroi o player antigo
        Destroy(playerAtual);
        playerAtual = novoPlayer;

        Debug.Log($"[GameManager] Player trocado para v{versao}");
    }

    /// <summary>
    /// Tenta atualizar câmeras que seguem o player (Cinemachine ou script custom).
    /// </summary>
    private void AtualizarCameraTarget(Transform novoTarget)
    {
        // Procura Cinemachine Virtual Camera (se existir)
        var vCam = FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vCam != null)
        {
            vCam.Follow = novoTarget;
            vCam.LookAt = novoTarget;
            Debug.Log("[GameManager] Cinemachine atualizado para novo player.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Reset (para menu / reiniciar jogo)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reseta todo o progresso. Chame ao voltar ao menu principal.
    /// </summary>
    public void ResetarProgresso()
    {
        TotalColetados = 0;
        ColetadosNoMapa = 0;
        Debug.Log("[GameManager] Progresso resetado.");
    }
}
