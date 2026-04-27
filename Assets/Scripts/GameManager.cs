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
    /// Registra a coleta de um item (memória). Incrementa contadores e inicia
    /// a cutscene de memória. A troca de prefab só acontece quando a cutscene terminar.
    /// </summary>
    /// <param name="collectablePrefab">Prefab do collectable para exibir na cutscene.</param>
    public void RegistrarColeta(GameObject collectablePrefab = null)
    {
        int versaoAnterior = TotalColetados;

        ColetadosNoMapa++;
        TotalColetados = Mathf.Clamp(TotalColetados + 1, 0, playerPrefabs.Length - 1);

        int versaoNova = TotalColetados;
        int indiceMemoria = versaoAnterior; // memória 0 = primeira coletada, etc.

        Debug.Log($"[GameManager] Memória coletada! Mapa: {ColetadosNoMapa}/{itensPorMapa} | Total: {TotalColetados}");

        // Notifica a HUD
        OnItemColetado?.Invoke(ColetadosNoMapa, itensPorMapa);

        // Tenta iniciar a cutscene — se não existir, troca direto
        var cutscene = CutsceneEvolucao.Instance;
        if (cutscene != null && versaoAnterior != versaoNova)
        {
            // Congela o player durante a cutscene
            CongelarPlayer(true);

            cutscene.Iniciar(indiceMemoria, collectablePrefab, () =>
            {
                // Callback: cutscene terminou → troca o prefab e libera
                SubstituirPlayer();
                CongelarPlayer(false);

                // Verifica se completou o mapa
                if (ColetadosNoMapa >= itensPorMapa)
                {
                    Debug.Log("[GameManager] Mapa completo! Todas as memórias foram recuperadas.");
                    OnMapaCompleto?.Invoke();
                }
            });
        }
        else
        {
            // Sem cutscene → troca instantânea (fallback)
            SubstituirPlayer();

            if (ColetadosNoMapa >= itensPorMapa)
            {
                Debug.Log("[GameManager] Mapa completo! Todas as memórias foram recuperadas.");
                OnMapaCompleto?.Invoke();
            }
        }
    }

    /// <summary>
    /// Congela/descongela TODA interação do jogador durante a cutscene:
    ///   • PlayerMovement (para de andar)
    ///   • PlayerInputHandler (para de processar teclas)
    ///   • PlayerInput (Input System — bloqueia teclado/mouse/gamepad)
    ///   • CinemachineInputAxisController (para de orbitar a câmera)
    ///   • Animator (congela animação)
    /// </summary>
    private void CongelarPlayer(bool congelar)
    {
        playerAtual = GameObject.FindWithTag("Player");
        if (playerAtual == null) return;

        // ── 1. Movimento ──
        var movement = playerAtual.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.SetMovementEnabled(!congelar);
            if (congelar)
            {
                movement.SetMoveInput(Vector2.zero);
                movement.SetSprinting(false);
            }
        }

        // ── 2. Input Handler custom ──
        var inputHandler = playerAtual.GetComponent<PlayerInputHandler>();
        if (inputHandler != null)
        {
            inputHandler.enabled = !congelar;
        }

        // ── 3. PlayerInput do Input System (bloqueia TUDO: teclado, mouse, gamepad) ──
        var playerInput = playerAtual.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = !congelar;
        }

        // ── 4. Animator (congela a pose) ──
        var animator = playerAtual.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.speed = congelar ? 0f : 1f;
        }

        // ── 5. Cinemachine Input (para de orbitar/rotacionar a câmera) ──
        DesativarInputCinemachine(congelar);
    }

    /// <summary>
    /// Desativa/ativa o processamento de input do Cinemachine
    /// para impedir que o jogador orbite a câmera durante cutscenes.
    /// </summary>
    private void DesativarInputCinemachine(bool desativar)
    {
        // CinemachineInputAxisController é o componente que lê mouse/gamepad
        // para orbitar a câmera no Cinemachine 3.x
        var inputControllers = FindObjectsByType<Unity.Cinemachine.CinemachineInputAxisController>(
            FindObjectsSortMode.None);
        foreach (var ctrl in inputControllers)
        {
            ctrl.enabled = !desativar;
        }

        // Também tenta desativar qualquer InputAxisController legado
        var vCam = FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vCam != null)
        {
            var axisInput = vCam.GetComponent<Unity.Cinemachine.CinemachineInputAxisController>();
            if (axisInput != null)
            {
                axisInput.enabled = !desativar;
            }
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

        // Guarda posição, rotação e parent do player
        Vector3 posicao = playerAtual.transform.position;
        Quaternion rotacao = playerAtual.transform.rotation;
        Transform parentOriginal = playerAtual.transform.parent;

        // Desativa e destrói o player antigo
        playerAtual.SetActive(false);
        Destroy(playerAtual);

        // Instancia o novo player (já vem com a câmera própria dele configurada no prefab)
        GameObject novoPlayer = Instantiate(playerPrefabs[versao], posicao, rotacao, parentOriginal);
        novoPlayer.name = $"Player-v{versao}";
        novoPlayer.tag = "Player";

        playerAtual = novoPlayer;

        Debug.Log($"[GameManager] Player trocado para v{versao}. O novo prefab gerenciará sua própria câmera.");
    }

    /// <summary>
    /// Mantido vazio por compatibilidade, pois os prefabs do player agora gerenciam suas próprias câmeras.
    /// </summary>
    private void AtualizarCameraTarget(Transform novoTarget)
    {
        // Não faz nada — a câmera já está embutida no prefab do player.
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
