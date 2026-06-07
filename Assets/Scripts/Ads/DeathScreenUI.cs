using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Tela de morte criada por código (sem precisar de prefab).
/// Mostra um anel de contagem regressiva (recuando) com o número no centro,
/// um botão "Reviver" (com ícone de vídeo, só aparece se houver rewarded pronto)
/// e abaixo um botão "Reiniciar mapa".
/// Se o tempo acabar sem escolha, reinicia automaticamente.
/// </summary>
public class DeathScreenUI : MonoBehaviour
{
    private const float TempoTotal = 10f;

    private Action onRevive;
    private Action onRestart;

    private Button btnRevive;
    private Button btnRestart;
    private TextMeshProUGUI status;

    private Image anelTimer;
    private TextMeshProUGUI contadorTexto;

    private float tempoRestante = TempoTotal;
    private bool contando = true;
    private bool finalizado;

    /// <summary>Cria e mostra a tela de morte.</summary>
    public static DeathScreenUI Mostrar(Action onRevive, Action onRestart)
    {
        var go = new GameObject("DeathScreenUI");
        var ui = go.AddComponent<DeathScreenUI>();
        ui.onRevive = onRevive;
        ui.onRestart = onRestart;
        ui.Build();
        return ui;
    }

    private void Build()
    {
        GarantirEventSystem();

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        gameObject.AddComponent<GraphicRaycaster>();

        // Fundo escuro semi-transparente (bloqueia cliques no que está atrás).
        var bg = NovoImage(transform, new Color(0f, 0f, 0f, 0.8f), "FundoEscuro");
        Esticar(bg.rectTransform);

        // Título
        var titulo = NovoTexto(transform, "Você morreu", 90, FontStyles.Bold);
        Centralizar(titulo.rectTransform, new Vector2(0, 360), new Vector2(1400, 200));

        // ── Anel de contagem regressiva ──
        // Trilho (anel cinza de fundo)
        var trilho = NovoImage(transform, new Color(1f, 1f, 1f, 0.12f), "AnelTrilho");
        trilho.sprite = GerarAnelSprite();
        trilho.type = Image.Type.Simple;
        Centralizar(trilho.rectTransform, new Vector2(0, 120), new Vector2(240, 240));

        // Anel que recua (preenchimento radial)
        anelTimer = NovoImage(transform, new Color(0.95f, 0.85f, 0.2f, 1f), "AnelTimer");
        anelTimer.sprite = GerarAnelSprite();
        anelTimer.type = Image.Type.Filled;
        anelTimer.fillMethod = Image.FillMethod.Radial360;
        anelTimer.fillOrigin = (int)Image.Origin360.Top;
        anelTimer.fillClockwise = true;
        anelTimer.fillAmount = 1f;
        Centralizar(anelTimer.rectTransform, new Vector2(0, 120), new Vector2(240, 240));

        // Número no centro do anel
        contadorTexto = NovoTexto(transform, "10", 96, FontStyles.Bold);
        Centralizar(contadorTexto.rectTransform, new Vector2(0, 120), new Vector2(240, 240));

        bool rewardedOk = AdsManager.Instance != null && AdsManager.Instance.RewardedDisponivel();

        // ── Botão Reviver (com ícone de vídeo) ──
        if (rewardedOk)
        {
            btnRevive = NovoBotaoComIcone(transform, "Reviver", new Vector2(0, -130),
                                          new Color(0.16f, 0.62f, 0.26f), GerarVideoSprite());
            btnRevive.onClick.AddListener(AoClicarReviver);
        }

        // ── Botão Reiniciar mapa ──
        btnRestart = NovoBotao(transform, "Voltar ao início", new Vector2(0, rewardedOk ? -290 : -150),
                               new Color(0.34f, 0.34f, 0.38f));
        btnRestart.onClick.AddListener(() => Decidir(reviver: false));

        // Texto de status (escondido até precisar)
        status = NovoTexto(transform, "", 38, FontStyles.Normal);
        Centralizar(status.rectTransform, new Vector2(0, -410), new Vector2(1500, 100));
    }

    private void Update()
    {
        if (!contando || finalizado) return;

        tempoRestante -= Time.unscaledDeltaTime;
        if (tempoRestante < 0f) tempoRestante = 0f;

        if (anelTimer != null) anelTimer.fillAmount = tempoRestante / TempoTotal;
        if (contadorTexto != null) contadorTexto.text = Mathf.CeilToInt(tempoRestante).ToString();

        if (tempoRestante <= 0f)
        {
            // Tempo esgotado: reinicia o mapa automaticamente.
            Decidir(reviver: false);
        }
    }

    private void AoClicarReviver()
    {
        if (finalizado) return;

        // Pausa o timer e bloqueia novos cliques enquanto o vídeo carrega/abre.
        contando = false;
        if (btnRevive != null) btnRevive.interactable = false;
        if (btnRestart != null) btnRestart.interactable = false;
        if (status != null) status.text = "Carregando vídeo...";

        AdsManager.Instance.MostrarRewardedReviver(
            aoGanharRecompensa: () => Decidir(reviver: true),
            aoFalhar: () =>
            {
                // Não assistiu até o fim: volta a contar e reabilita as opções.
                if (finalizado) return;
                contando = true;
                if (btnRevive != null) btnRevive.interactable = true;
                if (btnRestart != null) btnRestart.interactable = true;
                if (status != null) status.text = "Anúncio não concluído.";
            });
    }

    private void Decidir(bool reviver)
    {
        if (finalizado) return;
        finalizado = true;
        contando = false;

        if (reviver) onRevive?.Invoke();
        else onRestart?.Invoke();

        Destroy(gameObject);
    }

    // ───────────────────── helpers de UI ─────────────────────

    private static void GarantirEventSystem()
    {
        if (EventSystem.current != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    private static Image NovoImage(Transform parent, Color cor, string nome = "Image")
    {
        var go = new GameObject(nome);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = cor;
        return img;
    }

    private static void Esticar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private static void Centralizar(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static TextMeshProUGUI NovoTexto(Transform parent, string texto, float tamanho, FontStyles estilo)
    {
        var go = new GameObject("Texto");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = tamanho;
        tmp.fontStyle = estilo;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return tmp;
    }

    private static Button NovoBotao(Transform parent, string texto, Vector2 pos, Color cor)
    {
        var go = new GameObject("Botao");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = cor;
        var btn = go.AddComponent<Button>();

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(820, 130);

        var label = NovoTexto(go.transform, texto, 46, FontStyles.Bold);
        Esticar(label.rectTransform);

        return btn;
    }

    private static Button NovoBotaoComIcone(Transform parent, string texto, Vector2 pos, Color cor, Sprite icone)
    {
        var btn = NovoBotao(parent, texto, pos, cor);

        // Ícone à esquerda do botão.
        var go = new GameObject("Icone");
        go.transform.SetParent(btn.transform, false);
        var img = go.AddComponent<Image>();
        img.sprite = icone;
        img.color = Color.white;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(80, 0);
        rt.sizeDelta = new Vector2(70, 70);

        return btn;
    }

    // ───────────────────── ícones gerados por código ─────────────────────

    /// <summary>Anel (donut) branco para o timer/trilho.</summary>
    private static Sprite GerarAnelSprite(int size = 128, float espessura = 0.16f)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Vector2 c = new Vector2(size / 2f, size / 2f);
        float outer = size / 2f - 2f;
        float inner = outer * (1f - espessura * 2f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                // Anti-aliasing suave nas bordas do anel.
                float aOut = Mathf.Clamp01(outer - d);
                float aIn = Mathf.Clamp01(d - inner);
                float a = Mathf.Min(aOut, aIn);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    /// <summary>Ícone simples de "vídeo": triângulo de play apontando para a direita.</summary>
    private static Sprite GerarVideoSprite(int size = 64)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int i = 0; i < size * size; i++) tex.SetPixel(i % size, i / size, new Color(1, 1, 1, 0));

        float x0 = 0.28f * size;   // borda esquerda do triângulo
        float x1 = 0.80f * size;   // ponta (direita)
        float yMid = 0.5f * size;
        float meiaAltura = 0.30f * size;

        for (int y = 0; y < size; y++)
        {
            float frac = 1f - Mathf.Abs(y - yMid) / meiaAltura;
            if (frac <= 0f) continue;
            float xDir = x0 + (x1 - x0) * frac;
            for (int x = 0; x < size; x++)
            {
                if (x >= x0 && x <= xDir)
                    tex.SetPixel(x, y, Color.white);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
