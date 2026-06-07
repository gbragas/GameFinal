using System;
using UnityEngine;
using Unity.Services.LevelPlay;

/// <summary>
/// Gerencia os anúncios do jogo via Unity LevelPlay.
/// - Interstitial: mostrado ao trocar de mapa.
/// - Rewarded: mostrado para reviver no local da morte.
///
/// O AdsManager se cria sozinho (Bootstrap) e persiste entre cenas.
/// Não precisa colocá-lo manualmente em nenhuma cena.
/// </summary>
public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    // ====================================================================
    // TODO: cole aqui os valores do dashboard LevelPlay.
    //  - AppKey: a "App Key" do app (uma para Android, outra para iOS).
    //  - Os Ad Unit IDs já estão preenchidos para Android.
    //  - Preencha os do iOS quando criar o app iOS no dashboard.
    // ====================================================================
#if UNITY_IOS
    private const string AppKey               = "26a226665";
    private const string InterstitialAdUnitId = "pic0gd8rr9bdjvsg"; // Interstitial_MapChange (iOS)
    private const string RewardedAdUnitId     = "fmv2mk1zon6yvi2w"; // Rewarded_Revive (iOS)
#else // UNITY_ANDROID e Editor
    private const string AppKey               = "26a21f355";
    private const string InterstitialAdUnitId = "voexlygw0186c77c"; // Interstitial_MapChange (Android)
    private const string RewardedAdUnitId     = "d7a2o40fzhndvohb"; // Rewarded_Revive (Android)
#endif

    [Tooltip("Mostra o interstitial a cada N trocas de mapa (1 = toda troca). " +
             "2 ou 3 é recomendado para não irritar o jogador.")]
    [SerializeField] private int interstitialACadaXTrocas = 2;

    private LevelPlayInterstitialAd interstitial;
    private LevelPlayRewardedAd rewarded;
    private bool initialized;
    private int trocasDeMapa;

    // Callbacks pendentes
    private Action interstitialAoFechar;
    private Action rewardedAoGanhar;
    private Action rewardedAoFalhar;
    private bool recompensaGanha;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("AdsManager (Auto)");
        go.AddComponent<AdsManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitAds();
    }

    private void InitAds()
    {
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed  += OnInitFailed;
        LevelPlay.Init(AppKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        initialized = true;

        // ---- Interstitial (troca de mapa) ----
        interstitial = new LevelPlayInterstitialAd(InterstitialAdUnitId);
        interstitial.OnAdClosed += _ => FinalizarInterstitial();
        interstitial.OnAdDisplayFailed += (_, __) => FinalizarInterstitial();
        interstitial.LoadAd();

        // ---- Rewarded (reviver) ----
        rewarded = new LevelPlayRewardedAd(RewardedAdUnitId);
        rewarded.OnAdRewarded += (_, __) => recompensaGanha = true;
        rewarded.OnAdClosed += _ => FinalizarRewarded();
        rewarded.OnAdDisplayFailed += (_, __) => FinalizarRewarded(forcarFalha: true);
        rewarded.LoadAd();

        Debug.Log("[AdsManager] LevelPlay inicializado.");
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogWarning($"[AdsManager] Falha ao inicializar LevelPlay: {error}");
    }

    // ===================== INTERSTITIAL (troca de mapa) =====================

    /// <summary>
    /// Mostra o interstitial respeitando a frequência configurada e chama
    /// <paramref name="aoFechar"/> quando o anúncio fecha (ou imediatamente
    /// se não houver anúncio para mostrar). Sempre chama o callback — seguro
    /// para usar antes de carregar a próxima cena.
    /// </summary>
    public void MostrarInterstitialTrocaDeMapa(Action aoFechar)
    {
        trocasDeMapa++;

        bool naFrequencia = interstitialACadaXTrocas <= 1
                            || trocasDeMapa % interstitialACadaXTrocas == 0;

        if (initialized && naFrequencia && interstitial != null && interstitial.IsAdReady())
        {
            interstitialAoFechar = aoFechar;
            interstitial.ShowAd();
        }
        else
        {
            aoFechar?.Invoke();
        }
    }

    private void FinalizarInterstitial()
    {
        var cb = interstitialAoFechar;
        interstitialAoFechar = null;
        interstitial?.LoadAd(); // pré-carrega o próximo
        cb?.Invoke();
    }

    // ===================== REWARDED (reviver) =====================

    /// <summary>True se há um anúncio rewarded pronto para reviver.</summary>
    public bool RewardedDisponivel()
        => initialized && rewarded != null && rewarded.IsAdReady();

    /// <summary>
    /// Mostra o vídeo rewarded. Chama <paramref name="aoGanharRecompensa"/>
    /// somente se o jogador assistir até o fim; caso contrário (ou sem anúncio
    /// disponível) chama <paramref name="aoFalhar"/>.
    /// </summary>
    public void MostrarRewardedReviver(Action aoGanharRecompensa, Action aoFalhar)
    {
        if (RewardedDisponivel())
        {
            recompensaGanha = false;
            rewardedAoGanhar = aoGanharRecompensa;
            rewardedAoFalhar = aoFalhar;
            rewarded.ShowAd();
        }
        else
        {
            aoFalhar?.Invoke();
        }
    }

    private void FinalizarRewarded(bool forcarFalha = false)
    {
        var ganhou = recompensaGanha && !forcarFalha;
        var cbGanhar = rewardedAoGanhar;
        var cbFalhar = rewardedAoFalhar;

        rewardedAoGanhar = null;
        rewardedAoFalhar = null;
        recompensaGanha = false;

        rewarded?.LoadAd(); // pré-carrega o próximo

        if (ganhou) cbGanhar?.Invoke();
        else        cbFalhar?.Invoke();
    }
}
