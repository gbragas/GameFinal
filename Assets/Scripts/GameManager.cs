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
    // ── Singleton (auto-cria se não existir) ─────────────────────────────────
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Tenta encontrar um que já exista na cena
                _instance = FindAnyObjectByType<GameManager>();

                // Se ainda não existe, cria automaticamente
                if (_instance == null)
                {
                    Debug.Log("[GameManager] Nenhuma instância encontrada — criando automaticamente.");
                    GameObject go = new GameObject("GameManager (Auto)");
                    _instance = go.AddComponent<GameManager>();
                    // O Awake() cuida do DontDestroyOnLoad
                }
            }
            return _instance;
        }
    }

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
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Carrega prefabs automaticamente se nenhum foi atribuído no Inspector
        CarregarPrefabsSeNecessario();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// Se o array de prefabs está vazio (ex.: criado automaticamente, não pelo Inspector),
    /// tenta carregar de Resources/PlayerPrefabs/.
    /// </summary>
    private void CarregarPrefabsSeNecessario()
    {
        // Verifica se pelo menos o primeiro prefab está preenchido
        bool algumPreenchido = false;
        for (int i = 0; i < playerPrefabs.Length; i++)
        {
            if (playerPrefabs[i] != null) { algumPreenchido = true; break; }
        }

        if (algumPreenchido) return; // já está configurado pelo Inspector

        Debug.Log("[GameManager] Prefabs não configurados — tentando carregar de Resources/PlayerPrefabs/...");

        for (int i = 0; i < playerPrefabs.Length; i++)
        {
            playerPrefabs[i] = Resources.Load<GameObject>($"PlayerPrefabs/Player-v{i}");
            if (playerPrefabs[i] != null)
                Debug.Log($"[GameManager] Carregado: Player-v{i}");
            else
                Debug.LogWarning($"[GameManager] NÃO encontrado: Resources/PlayerPrefabs/Player-v{i}");
        }
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

        // Se o player atual JÁ é a versão correta, não troca
        if (playerAtual.name.Contains($"v{versao}"))
        {
            Debug.Log($"[GameManager] Player já é v{versao}, sem necessidade de trocar.");
            return;
        }

        // Guarda posição, rotação e dados relevantes
        Vector3 posicao = playerAtual.transform.position;
        Quaternion rotacao = playerAtual.transform.rotation;
        Transform parentOriginal = playerAtual.transform.parent;

        // Transfere a referência da câmera ANTES de destruir
        PlayerMovement movimentoAntigo = playerAtual.GetComponent<PlayerMovement>();
        Transform cameraRef = movimentoAntigo != null ? movimentoAntigo.cameraTransform : null;

        // ★ DESATIVA o player antigo IMEDIATAMENTE para que FindWithTag
        //   não o encontre de novo (Destroy é deferido ao fim do frame)
        playerAtual.SetActive(false);
        Destroy(playerAtual);

        // Instancia o novo player
        GameObject novoPlayer = Instantiate(playerPrefabs[versao], posicao, rotacao, parentOriginal);
        novoPlayer.name = $"Player-v{versao}";
        novoPlayer.tag = "Player";

        // Transfere a referência da câmera
        PlayerMovement movimentoNovo = novoPlayer.GetComponent<PlayerMovement>();
        if (cameraRef != null && movimentoNovo != null)
        {
            movimentoNovo.cameraTransform = cameraRef;
        }

        // Se houver câmera Cinemachine/Follow, atualiza o target
        AtualizarCameraTarget(novoPlayer.transform);

        playerAtual = novoPlayer;

        Debug.Log($"[GameManager] Player trocado para v{versao}");
    }

    /// <summary>
    /// Tenta atualizar câmeras que seguem o player (Cinemachine ou script custom).
    /// Força snap instantâneo para evitar deslocamento de câmera na troca.
    /// </summary>
    private void AtualizarCameraTarget(Transform novoTarget)
    {
        // Procura Cinemachine Virtual Camera (se existir)
        var vCam = FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vCam != null)
        {
            vCam.Follow = novoTarget;
            vCam.LookAt = novoTarget;

            // ★ Força a câmera a snapar INSTANTANEAMENTE para o novo target
            //   sem fazer transição suave (que causava o deslocamento)
            vCam.ForceCameraPosition(vCam.transform.position, vCam.transform.rotation);

            // Também procura o CinemachineBrain e força update imediato
            var brain = FindAnyObjectByType<Unity.Cinemachine.CinemachineBrain>();
            if (brain != null)
            {
                brain.ManualUpdate();
            }

            Debug.Log("[GameManager] Cinemachine atualizado e snapado para novo player.");
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
