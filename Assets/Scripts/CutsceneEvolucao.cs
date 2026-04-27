using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Cutscene de Memória — exibida quando o player coleta um item (memória).
///
/// Fluxo:
///   1. Tela escurece (fade in)
///   2. O modelo 3D do collectable coletado aparece girando numa RenderTexture
///   3. Uma imagem de "lembrança" (sprite) aparece ao lado
///   4. O sistema de Diálogo (prefab Dialog) toca a narração
///   5. Quando a narração termina → fade out → troca prefab → volta ao jogo
///
/// ▸ Coloque este script num GameObject filho do Canvas de cada mapa.
/// ▸ Toda a UI é gerada via código — não precisa montar nada manualmente.
/// </summary>
public class CutsceneEvolucao : MonoBehaviour
{
    // ── Configuração ─────────────────────────────────────────────────────────
    [Header("Diálogo (Prefab)")]
    [Tooltip("Prefab do Dialog que contém o componente Dialogo. " +
             "Será instanciado dentro do Canvas durante a cutscene.")]
    public GameObject dialogPrefab;

    [Header("Memórias — Imagens de Lembrança (uma por item, 0–5)")]
    [Tooltip("Sprite exibido durante a cutscene de cada memória. " +
             "Índice 0 = 1ª memória coletada, índice 5 = última.")]
    public Sprite[] imagensMemoria = new Sprite[6];

    [Header("Tempos")]
    public float duracaoFade = 0.6f;
    public float tempoAntesDoDialogo = 1.0f;
    public float tempoDepoisDoDialogo = 0.8f;

    [Header("Modelo 3D do Collectable")]
    [Tooltip("Velocidade de rotação do collectable na cutscene.")]
    public float rotacaoVitrine = 60f;

    [Header("Cores")]
    public Color corFundo = new Color(0f, 0f, 0f, 0.93f);
    public Color corTituloMemoria = new Color(1f, 0.84f, 0f);

    // ── Referências internas (criadas automaticamente) ───────────────────────
    private Canvas canvas;
    private GameObject painelRaiz;
    private CanvasGroup canvasGroup;
    private Image fundoEscuro;
    private Image imagemMemoria;
    private RawImage rawImageModelo3D;
    private TextMeshProUGUI textoTitulo;

    // Render do modelo 3D
    private Camera cameraVitrine;
    private RenderTexture renderTexture;
    private GameObject modeloVitrineAtual;
    private Light luzVitrine;

    // Diálogo instanciado
    private GameObject dialogoInstanciado;
    private Dialogo dialogoComponente;
    private bool dialogoTerminou;

    private bool cutsceneAtiva = false;

    // Estado do cursor salvo antes da cutscene
    private CursorLockMode cursorLockAnterior;
    private bool cursorVisibleAnterior;

    // ── Singleton por cena ───────────────────────────────────────────────────
    public static CutsceneEvolucao Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        canvas = GetComponentInParent<Canvas>();
        CriarUI();
        CriarCameraVitrine();
        painelRaiz.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        LimparVitrine();
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        if (cameraVitrine != null)
            Destroy(cameraVitrine.gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // API Pública
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Inicia a cutscene de memória.
    /// </summary>
    /// <param name="indiceMemoria">Qual memória (0–5) foi coletada.</param>
    /// <param name="collectablePrefab">O GameObject 3D do collectable (para exibir na vitrine).</param>
    /// <param name="onTerminar">Callback quando a cutscene acabar.</param>
    public void Iniciar(int indiceMemoria, GameObject collectablePrefab, System.Action onTerminar)
    {
        if (cutsceneAtiva) return;
        StartCoroutine(ExecutarCutscene(indiceMemoria, collectablePrefab, onTerminar));
    }

    // Sobrecarga para compatibilidade (sem collectable 3D)
    public void Iniciar(int versaoAnterior, int versaoNova, System.Action onTerminar)
    {
        if (cutsceneAtiva) return;
        StartCoroutine(ExecutarCutscene(versaoAnterior, null, onTerminar));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Coroutine principal
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator ExecutarCutscene(int indiceMemoria, GameObject collectablePrefab, System.Action onTerminar)
    {
        cutsceneAtiva = true;

        // ★ Salva e libera o cursor (impede mouse de controlar a câmera)
        cursorLockAnterior = Cursor.lockState;
        cursorVisibleAnterior = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ── Configura imagem de lembrança ──
        if (indiceMemoria >= 0 && indiceMemoria < imagensMemoria.Length && imagensMemoria[indiceMemoria] != null)
        {
            imagemMemoria.sprite = imagensMemoria[indiceMemoria];
            imagemMemoria.gameObject.SetActive(true);
        }
        else
        {
            imagemMemoria.gameObject.SetActive(false);
        }

        // ── Configura modelo 3D do collectable ──
        if (collectablePrefab != null)
        {
            MontarVitrine(collectablePrefab);
            rawImageModelo3D.gameObject.SetActive(true);
        }
        else
        {
            rawImageModelo3D.gameObject.SetActive(false);
        }

        // ── Título ──
        textoTitulo.text = $"Memória #{indiceMemoria + 1}";

        // Ativa o painel
        painelRaiz.SetActive(true);
        canvasGroup.alpha = 0f;

        // Esconde elementos para animação de entrada
        imagemMemoria.transform.localScale = Vector3.zero;
        rawImageModelo3D.transform.localScale = Vector3.zero;
        textoTitulo.transform.localScale = Vector3.zero;

        // ═══ FASE 1: FADE IN ═══
        yield return FadeCanvasGroup(0f, 1f, duracaoFade);

        // ═══ FASE 2: Título aparece ═══
        yield return AnimarEscala(textoTitulo.transform, Vector3.zero, Vector3.one * 1.15f, 0.25f);
        yield return AnimarEscala(textoTitulo.transform, Vector3.one * 1.15f, Vector3.one, 0.1f);

        // ═══ FASE 3: Modelo 3D do collectable aparece (girando) ═══
        if (rawImageModelo3D.gameObject.activeSelf)
        {
            yield return AnimarEscala(rawImageModelo3D.transform, Vector3.zero, Vector3.one, 0.35f);
        }
        yield return new WaitForSecondsRealtime(0.3f);

        // ═══ FASE 4: Imagem de memória aparece ═══
        if (imagemMemoria.gameObject.activeSelf)
        {
            yield return AnimarEscala(imagemMemoria.transform, Vector3.zero, Vector3.one * 1.1f, 0.3f);
            yield return AnimarEscala(imagemMemoria.transform, Vector3.one * 1.1f, Vector3.one, 0.15f);
        }

        // ═══ FASE 5: Espera antes do diálogo ═══
        yield return new WaitForSecondsRealtime(tempoAntesDoDialogo);

        // ═══ FASE 6: Instancia e toca o Diálogo ═══
        if (dialogPrefab != null)
        {
            dialogoTerminou = false;

            // Instancia o prefab do diálogo dentro do Canvas
            dialogoInstanciado = Instantiate(dialogPrefab, painelRaiz.transform);
            dialogoComponente = dialogoInstanciado.GetComponentInChildren<Dialogo>();

            if (dialogoComponente != null)
            {
                // Desativa auto-start ANTES do Start() rodar (mesmo frame)
                dialogoComponente.DesativarAutoStart();

                // Quando o diálogo acabar, marca a flag
                dialogoComponente.OnDialogoFinalizado += () => dialogoTerminou = true;
                dialogoComponente.TocarDialogo();

                // Espera o diálogo terminar
                while (!dialogoTerminou)
                {
                    yield return null;
                }
            }
            else
            {
                Debug.LogWarning("[CutsceneEvolucao] Prefab de Dialog não contém componente Dialogo!");
                yield return new WaitForSecondsRealtime(2f);
            }

            // Limpa o diálogo instanciado
            if (dialogoInstanciado != null)
                Destroy(dialogoInstanciado);
        }
        else
        {
            // Sem diálogo — espera um tempo fixo
            yield return new WaitForSecondsRealtime(3f);
        }

        // ═══ FASE 7: Espera após diálogo ═══
        yield return new WaitForSecondsRealtime(tempoDepoisDoDialogo);

        // ═══ FASE 8: FADE OUT ═══
        yield return FadeCanvasGroup(1f, 0f, duracaoFade);

        // Limpa
        LimparVitrine();
        painelRaiz.SetActive(false);
        cutsceneAtiva = false;

        // ★ Restaura o cursor ao estado anterior (locked para gameplay)
        Cursor.lockState = cursorLockAnterior;
        Cursor.visible = cursorVisibleAnterior;

        // Callback — GameManager troca o prefab agora
        onTerminar?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Sistema de Vitrine 3D (renderiza o collectable numa RenderTexture)
    // ─────────────────────────────────────────────────────────────────────────

    private void CriarCameraVitrine()
    {
        // RenderTexture
        renderTexture = new RenderTexture(512, 512, 24);
        renderTexture.antiAliasing = 4;
        rawImageModelo3D.texture = renderTexture;

        // Camera invisível dedicada à vitrine
        GameObject goCam = new GameObject("CutsceneVitrine_Camera");
        goCam.transform.position = new Vector3(0f, -100f, 0f); // bem longe da cena
        cameraVitrine = goCam.AddComponent<Camera>();
        cameraVitrine.targetTexture = renderTexture;
        cameraVitrine.clearFlags = CameraClearFlags.SolidColor;
        cameraVitrine.backgroundColor = Color.clear;
        cameraVitrine.cullingMask = 1 << 31; // layer 31 exclusiva
        cameraVitrine.fieldOfView = 30f;
        cameraVitrine.nearClipPlane = 0.1f;
        cameraVitrine.farClipPlane = 50f;
        cameraVitrine.enabled = false; // desligada até precisar

        // Luz da vitrine
        GameObject goLuz = new GameObject("CutsceneVitrine_Luz");
        goLuz.transform.SetParent(goCam.transform);
        goLuz.transform.localPosition = new Vector3(0f, 3f, -2f);
        goLuz.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
        luzVitrine = goLuz.AddComponent<Light>();
        luzVitrine.type = LightType.Directional;
        luzVitrine.intensity = 1.5f;
        luzVitrine.cullingMask = 1 << 31;
        luzVitrine.enabled = false;
    }

    private void MontarVitrine(GameObject collectablePrefab)
    {
        LimparVitrine();

        // Instancia o modelo
        modeloVitrineAtual = Instantiate(collectablePrefab);
        modeloVitrineAtual.name = "CutsceneVitrine_Modelo";

        // Remove scripts de gameplay (CollectableController, colliders, rigidbodies)
        foreach (var script in modeloVitrineAtual.GetComponentsInChildren<MonoBehaviour>())
            Destroy(script);
        foreach (var col in modeloVitrineAtual.GetComponentsInChildren<Collider>())
            Destroy(col);
        foreach (var rb in modeloVitrineAtual.GetComponentsInChildren<Rigidbody>())
            Destroy(rb);

        // Coloca na layer 31 (exclusiva da câmera vitrine)
        SetLayerRecursive(modeloVitrineAtual, 31);

        // Posiciona na frente da câmera vitrine
        modeloVitrineAtual.transform.position = cameraVitrine.transform.position + cameraVitrine.transform.forward * 3f;
        modeloVitrineAtual.transform.rotation = Quaternion.identity;

        // Liga câmera e luz
        cameraVitrine.enabled = true;
        luzVitrine.enabled = true;

        // Inicia rotação
        StartCoroutine(RotacionarVitrine());
    }

    private IEnumerator RotacionarVitrine()
    {
        while (modeloVitrineAtual != null)
        {
            modeloVitrineAtual.transform.Rotate(Vector3.up, rotacaoVitrine * Time.unscaledDeltaTime, Space.World);
            yield return null;
        }
    }

    private void LimparVitrine()
    {
        if (modeloVitrineAtual != null)
        {
            Destroy(modeloVitrineAtual);
            modeloVitrineAtual = null;
        }
        if (cameraVitrine != null)
            cameraVitrine.enabled = false;
        if (luzVitrine != null)
            luzVitrine.enabled = false;
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers de animação (realtime — funciona com timeScale = 0)
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator FadeCanvasGroup(float de, float para, float duracao)
    {
        float t = 0f;
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(de, para, t / duracao);
            yield return null;
        }
        canvasGroup.alpha = para;
    }

    private IEnumerator AnimarEscala(Transform alvo, Vector3 de, Vector3 para, float duracao)
    {
        float t = 0f;
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            float ease = 1f - Mathf.Pow(1f - (t / duracao), 3f);
            alvo.localScale = Vector3.Lerp(de, para, ease);
            yield return null;
        }
        alvo.localScale = para;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Criação da UI via código
    // ─────────────────────────────────────────────────────────────────────────

    private void CriarUI()
    {
        // ── Painel raiz ──
        painelRaiz = new GameObject("CutsceneMemoria_Painel");
        painelRaiz.transform.SetParent(transform, false);

        RectTransform rtRaiz = painelRaiz.AddComponent<RectTransform>();
        rtRaiz.anchorMin = Vector2.zero;
        rtRaiz.anchorMax = Vector2.one;
        rtRaiz.offsetMin = Vector2.zero;
        rtRaiz.offsetMax = Vector2.zero;

        canvasGroup = painelRaiz.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true;

        // ── Fundo escuro ──
        GameObject goFundo = CriarElemento("Fundo", painelRaiz.transform);
        fundoEscuro = goFundo.AddComponent<Image>();
        fundoEscuro.color = corFundo;
        ExpandirAnchors(goFundo.GetComponent<RectTransform>());

        // ── Título "Memória #X" ──
        GameObject goTitulo = CriarElemento("TituloMemoria", painelRaiz.transform);
        textoTitulo = goTitulo.AddComponent<TextMeshProUGUI>();
        textoTitulo.text = "Memória";
        textoTitulo.fontSize = 52;
        textoTitulo.color = corTituloMemoria;
        textoTitulo.alignment = TextAlignmentOptions.Center;
        textoTitulo.fontStyle = FontStyles.Bold;
        textoTitulo.enableWordWrapping = false;
        RectTransform rtTitulo = goTitulo.GetComponent<RectTransform>();
        rtTitulo.anchorMin = new Vector2(0.5f, 1f);
        rtTitulo.anchorMax = new Vector2(0.5f, 1f);
        rtTitulo.sizeDelta = new Vector2(600f, 80f);
        rtTitulo.anchoredPosition = new Vector2(0f, -80f);

        // ── Container para modelo 3D + imagem de memória ──
        // Layout: [Modelo 3D]   [Imagem de Memória]
        //          (esquerda)      (direita)

        // Modelo 3D do collectable (RawImage com RenderTexture)
        GameObject goModelo = CriarElemento("Modelo3D", painelRaiz.transform);
        rawImageModelo3D = goModelo.AddComponent<RawImage>();
        rawImageModelo3D.color = Color.white;
        RectTransform rtModelo = goModelo.GetComponent<RectTransform>();
        rtModelo.anchorMin = new Vector2(0.5f, 0.5f);
        rtModelo.anchorMax = new Vector2(0.5f, 0.5f);
        rtModelo.sizeDelta = new Vector2(280f, 280f);
        rtModelo.anchoredPosition = new Vector2(-180f, 20f);

        // Imagem de memória/lembrança (sprite)
        GameObject goMemoria = CriarElemento("ImagemMemoria", painelRaiz.transform);
        imagemMemoria = goMemoria.AddComponent<Image>();
        imagemMemoria.color = Color.white;
        imagemMemoria.preserveAspect = true;
        RectTransform rtMemoria = goMemoria.GetComponent<RectTransform>();
        rtMemoria.anchorMin = new Vector2(0.5f, 0.5f);
        rtMemoria.anchorMax = new Vector2(0.5f, 0.5f);
        rtMemoria.sizeDelta = new Vector2(300f, 220f);
        rtMemoria.anchoredPosition = new Vector2(180f, 20f);

        // Borda decorativa na imagem de memória
        Outline borda = goMemoria.AddComponent<Outline>();
        borda.effectColor = new Color(1f, 0.84f, 0f, 0.6f);
        borda.effectDistance = new Vector2(3f, -3f);
    }

    private GameObject CriarElemento(string nome, Transform parent)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private void ExpandirAnchors(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
