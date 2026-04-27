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
    [Header("Sistema de Diálogo")]
    [Tooltip("Prefab do Dialog que contém o componente Dialogo.")]
    public GameObject dialogPrefab;

    [Header("Configuração das Memórias")]
    [Tooltip("Lista de memórias. Cada uma contém sua imagem, legenda e sequência de áudio/texto.")]
    public ConfiguracaoMemoria[] memorias = new ConfiguracaoMemoria[6];

    [Header("Tempos")]
    public float duracaoFade = 0.6f;
    public float tempoAntesDoDialogo = 1.0f;
    public float tempoDepoisDoDialogo = 0.8f;

    [System.Serializable]
    public class ConfiguracaoMemoria
    {
        public string nomeIdentificador = "Memória #";
        public Sprite imagem;
        [TextArea(2, 3)]
        public string legendaFixa;
        
        [Header("Narração (Sistema de Diálogo)")]
        public Fala[] falas;
    }

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
    private TextMeshProUGUI textoLegenda;

    // Render do modelo 3D
    private Camera cameraVitrine;
    private RenderTexture renderTexture;
    private GameObject modeloVitrineAtual;
    private Light luzVitrine;

    // Render dos Players (Antigo e Novo)
    private RawImage rawPlayerAntigo;
    private RawImage rawPlayerNovo;
    private Camera cameraPlayerAntigo;
    private RenderTexture renderPlayerAntigo;
    private GameObject modeloPlayerAntigo;
    private Light luzPlayerAntigo;
    private Camera cameraPlayerNovo;
    private RenderTexture renderPlayerNovo;
    private GameObject modeloPlayerNovo;
    private Light luzPlayerNovo;

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
        
        // Cria as câmeras para renderizar os players
        CriarCameraPlayer(ref cameraPlayerAntigo, ref renderPlayerAntigo, ref luzPlayerAntigo, rawPlayerAntigo, new Vector3(0f, -200f, 0f));
        CriarCameraPlayer(ref cameraPlayerNovo, ref renderPlayerNovo, ref luzPlayerNovo, rawPlayerNovo, new Vector3(0f, -300f, 0f));
        
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
        if (renderPlayerAntigo != null) { renderPlayerAntigo.Release(); Destroy(renderPlayerAntigo); }
        if (renderPlayerNovo != null) { renderPlayerNovo.Release(); Destroy(renderPlayerNovo); }

        if (cameraVitrine != null) Destroy(cameraVitrine.gameObject);
        if (cameraPlayerAntigo != null) Destroy(cameraPlayerAntigo.gameObject);
        if (cameraPlayerNovo != null) Destroy(cameraPlayerNovo.gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // API Pública
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Inicia a cutscene de memória.
    /// </summary>
    /// <param name="indiceMemoria">Qual memória (0–5) foi coletada.</param>
    /// <param name="collectablePrefab">O GameObject 3D do collectable (para exibir na vitrine).</param>
    /// <param name="playerAntigoPrefab">Prefab do player antes da evolução.</param>
    /// <param name="playerNovoPrefab">Prefab do player após a evolução.</param>
    /// <param name="onTerminar">Callback quando a cutscene acabar.</param>
    public void Iniciar(int indiceMemoria, GameObject collectablePrefab, GameObject playerAntigoPrefab, GameObject playerNovoPrefab, System.Action onTerminar)
    {
        if (cutsceneAtiva) return;
        StartCoroutine(ExecutarCutscene(indiceMemoria, collectablePrefab, playerAntigoPrefab, playerNovoPrefab, onTerminar));
    }

    // Sobrecarga para compatibilidade (sem collectable 3D)
    public void Iniciar(int versaoAnterior, int versaoNova, System.Action onTerminar)
    {
        if (cutsceneAtiva) return;
        StartCoroutine(ExecutarCutscene(versaoAnterior, null, null, null, onTerminar));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Coroutine principal
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator ExecutarCutscene(int indiceMemoria, GameObject collectablePrefab, GameObject playerAntigoPrefab, GameObject playerNovoPrefab, System.Action onTerminar)
    {
        cutsceneAtiva = true;

        // ★ Salva e libera o cursor (impede mouse de controlar a câmera)
        cursorLockAnterior = Cursor.lockState;
        cursorVisibleAnterior = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ── Pega os dados da memória atual ──
        ConfiguracaoMemoria dados = (indiceMemoria >= 0 && indiceMemoria < memorias.Length) 
            ? memorias[indiceMemoria] 
            : null;

        // ── Configura imagem de lembrança ──
        if (dados != null && dados.imagem != null)
        {
            imagemMemoria.sprite = dados.imagem;
            imagemMemoria.gameObject.SetActive(true);
        }
        else
        {
            imagemMemoria.gameObject.SetActive(false);
        }

        // ── Configura legenda ──
        if (dados != null && !string.IsNullOrEmpty(dados.legendaFixa))
        {
            textoLegenda.text = dados.legendaFixa;
            textoLegenda.gameObject.SetActive(true);
        }
        else
        {
            textoLegenda.gameObject.SetActive(false);
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

        // ── Configura os Players Antigo e Novo ──
        if (playerAntigoPrefab != null)
        {
            modeloPlayerAntigo = MontarVitrinePlayer(playerAntigoPrefab, cameraPlayerAntigo, luzPlayerAntigo);
            rawPlayerAntigo.gameObject.SetActive(true);
            rawPlayerAntigo.color = new Color(1f, 1f, 1f, 1f); // começa 100% opaco
            rawPlayerAntigo.transform.localScale = Vector3.zero; // para o pop-in animado
        }
        else
        {
            rawPlayerAntigo.gameObject.SetActive(false);
        }

        if (playerNovoPrefab != null)
        {
            modeloPlayerNovo = MontarVitrinePlayer(playerNovoPrefab, cameraPlayerNovo, luzPlayerNovo);
            rawPlayerNovo.gameObject.SetActive(true);
            rawPlayerNovo.color = new Color(1f, 1f, 1f, 0f); // começa invisível (fade in depois)
            rawPlayerNovo.transform.localScale = Vector3.one;
        }
        else
        {
            rawPlayerNovo.gameObject.SetActive(false);
        }

        // ── Título ──
        textoTitulo.text = $"Memória #{indiceMemoria + 1}";

        // Ativa o painel
        painelRaiz.SetActive(true);
        canvasGroup.alpha = 0f;

        // Esconde elementos para animação de entrada
        imagemMemoria.transform.localScale = Vector3.zero;
        textoLegenda.transform.localScale = Vector3.zero;
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
            yield return AnimarEscala(imagemMemoria.transform, Vector3.zero, Vector3.one * 1.05f, 0.3f);
            yield return AnimarEscala(imagemMemoria.transform, Vector3.one * 1.05f, Vector3.one, 0.15f);
            
            if (textoLegenda.gameObject.activeSelf)
                yield return AnimarEscala(textoLegenda.transform, Vector3.zero, Vector3.one, 0.25f);
        }

        // ═══ FASE 5: Fade-in do Player antigo e crossfade pro novo ═══
        if (rawPlayerAntigo.gameObject.activeSelf)
        {
            // Pop-in do player antigo
            yield return AnimarEscala(rawPlayerAntigo.transform, Vector3.zero, Vector3.one, 0.3f);
            
            // Crossfade suave
            float t = 0f;
            float tempoFade = 1.2f;
            while (t < tempoFade)
            {
                t += Time.unscaledDeltaTime;
                float p = t / tempoFade;
                rawPlayerAntigo.color = new Color(1f, 1f, 1f, 1f - p);
                if (rawPlayerNovo.gameObject.activeSelf)
                    rawPlayerNovo.color = new Color(1f, 1f, 1f, p);
                yield return null;
            }
            rawPlayerAntigo.color = new Color(1f, 1f, 1f, 0f);
            if (rawPlayerNovo.gameObject.activeSelf)
                rawPlayerNovo.color = new Color(1f, 1f, 1f, 1f);
        }

        // ═══ FASE 5: Espera antes do diálogo ═══
        yield return new WaitForSecondsRealtime(tempoAntesDoDialogo);

        // ═══ FASE 6: Instancia e toca o Diálogo ═══
        if (dialogPrefab != null)
        {
            dialogoTerminou = false;

            // Instancia o prefab do diálogo dentro do Canvas
            dialogoInstanciado = Instantiate(dialogPrefab, painelRaiz.transform);
            
            // Garante que fique na frente de todos os outros elementos do painel
            dialogoInstanciado.transform.SetAsLastSibling();

            // Se o prefab tiver um Canvas próprio, vamos garantir que ele não conflite
            Canvas c = dialogoInstanciado.GetComponent<Canvas>();
            if (c != null)
            {
                c.overrideSorting = true;
                c.sortingOrder = 999; // Valor alto para ficar na frente
            }

            dialogoComponente = dialogoInstanciado.GetComponentInChildren<Dialogo>();

            if (dialogoComponente != null)
            {
                // Desativa auto-start ANTES do Start() rodar (mesmo frame)
                dialogoComponente.DesativarAutoStart();

                // ★ INJETA AS FALAS DA MEMÓRIA ATUAL ★
                if (dados != null && dados.falas != null && dados.falas.Length > 0)
                {
                    dialogoComponente.ConfigurarFalas(dados.falas);
                }

                // Quando o diálogo acabar, marca a flag
                dialogoComponente.OnDialogoFinalizado += () => dialogoTerminou = true;
                
                Debug.Log($"[CutsceneEvolucao] Iniciando diálogo: {dialogoInstanciado.name}");
                dialogoComponente.TocarDialogo();

                // Espera o diálogo terminar
                while (!dialogoTerminou)
                {
                    yield return null;
                }
                Debug.Log("[CutsceneEvolucao] Diálogo finalizado.");
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
        if (modeloPlayerAntigo != null) Destroy(modeloPlayerAntigo);
        if (modeloPlayerNovo != null) Destroy(modeloPlayerNovo);
        if (cameraPlayerAntigo != null) cameraPlayerAntigo.enabled = false;
        if (cameraPlayerNovo != null) cameraPlayerNovo.enabled = false;
        if (luzPlayerAntigo != null) luzPlayerAntigo.enabled = false;
        if (luzPlayerNovo != null) luzPlayerNovo.enabled = false;

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
        luzVitrine.intensity = 0.1f; // Reduzido de 1.5 para 0.8
        luzVitrine.color = new Color(1f, 0.98f, 0.95f); // Tom levemente quente
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
    // Sistema de Vitrine 3D para os Players (Animação de Transição)
    // ─────────────────────────────────────────────────────────────────────────

    private void CriarCameraPlayer(ref Camera cam, ref RenderTexture rt, ref Light luz, RawImage rawTarget, Vector3 basePos)
    {
        rt = new RenderTexture(512, 512, 24);
        rt.antiAliasing = 4;
        rawTarget.texture = rt;

        GameObject goCam = new GameObject("CutscenePlayer_Camera");
        goCam.transform.position = basePos;
        cam = goCam.AddComponent<Camera>();
        cam.targetTexture = rt;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.clear;
        cam.cullingMask = 1 << 31; // layer 31
        cam.fieldOfView = 50f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 50f;
        cam.enabled = false;

        GameObject goLuz = new GameObject("CutscenePlayer_Luz");
        goLuz.transform.SetParent(goCam.transform);
        goLuz.transform.localPosition = new Vector3(0f, 3f, -2f);
        goLuz.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
        luz = goLuz.AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.intensity = 0.1f; // Reduzido de 1.2 para 0.7
        luz.color = new Color(1f, 0.98f, 0.95f);
        luz.cullingMask = 1 << 31;
        luz.enabled = false;
    }

    private GameObject MontarVitrinePlayer(GameObject prefab, Camera cam, Light luz)
    {
        if (prefab == null) return null;
        GameObject modelo = Instantiate(prefab);
        modelo.name = "CutsceneVitrine_Player";

        // Remove scripts de gameplay
        foreach (var script in modelo.GetComponentsInChildren<MonoBehaviour>())
            Destroy(script);
        foreach (var col in modelo.GetComponentsInChildren<Collider>())
            Destroy(col);
        foreach (var rb in modelo.GetComponentsInChildren<Rigidbody>())
            Destroy(rb);

        SetLayerRecursive(modelo, 31);

        // Posiciona na frente da câmera (olhando para ela)
        modelo.transform.localScale = Vector3.one * 2.2f; // Leve redução para caber melhor no frame quadrado
        modelo.transform.position = cam.transform.position + cam.transform.forward * 6.5f; // Um pouco mais longe
        modelo.transform.position -= new Vector3(0f, 2.0f, 0f); // Centralizado verticalmente
        modelo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        cam.enabled = true;
        luz.enabled = true;

        return modelo;
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
        rtMemoria.sizeDelta = new Vector2(500f, 360f); // Reduzi levemente de 550x400
        rtMemoria.anchoredPosition = new Vector2(250f, -20f); // Desci de 60 para -20

        // ── Legenda fixa embaixo da imagem ──
        GameObject goLegenda = CriarElemento("LegendaMemoria", painelRaiz.transform);
        textoLegenda = goLegenda.AddComponent<TextMeshProUGUI>();
        textoLegenda.fontSize = 24;
        textoLegenda.color = Color.white;
        textoLegenda.alignment = TextAlignmentOptions.Center;
        textoLegenda.fontStyle = FontStyles.Italic;
        textoLegenda.enableWordWrapping = true;
        RectTransform rtLegenda = goLegenda.GetComponent<RectTransform>();
        rtLegenda.anchorMin = new Vector2(0.5f, 0.5f);
        rtLegenda.anchorMax = new Vector2(0.5f, 0.5f);
        rtLegenda.sizeDelta = new Vector2(550f, 100f);
        rtLegenda.anchoredPosition = new Vector2(250f, -260f); // Desci para acompanhar a imagem

        // Borda decorativa na imagem de memória
        Outline borda = goMemoria.AddComponent<Outline>();
        borda.effectColor = new Color(1f, 0.84f, 0f, 0.6f);
        borda.effectDistance = new Vector2(3f, -3f);

        // ── Container para os Players (RenderTexture) ──
        // (PlayerNovo por trás, PlayerAntigo na frente, mas ancorados no mesmo lugar)
        GameObject goPlayerNovo = CriarElemento("PlayerNovo", painelRaiz.transform);
        rawPlayerNovo = goPlayerNovo.AddComponent<RawImage>();
        rawPlayerNovo.color = new Color(1f, 1f, 1f, 0f);
        RectTransform rtPlayerNovo = goPlayerNovo.GetComponent<RectTransform>();
        rtPlayerNovo.anchorMin = new Vector2(0.5f, 0.5f);
        rtPlayerNovo.anchorMax = new Vector2(0.5f, 0.5f);
        rtPlayerNovo.sizeDelta = new Vector2(500f, 500f); // Voltou a ser quadrado para não esticar (mesmo aspect do RenderTexture)
        // Lado esquerdo
        rtPlayerNovo.anchoredPosition = new Vector2(-380f, -20f);

        GameObject goPlayerAntigo = CriarElemento("PlayerAntigo", painelRaiz.transform);
        rawPlayerAntigo = goPlayerAntigo.AddComponent<RawImage>();
        rawPlayerAntigo.color = new Color(1f, 1f, 1f, 0f);
        RectTransform rtPlayerAntigo = goPlayerAntigo.GetComponent<RectTransform>();
        rtPlayerAntigo.anchorMin = new Vector2(0.5f, 0.5f);
        rtPlayerAntigo.anchorMax = new Vector2(0.5f, 0.5f);
        rtPlayerAntigo.sizeDelta = new Vector2(500f, 500f);
        rtPlayerAntigo.anchoredPosition = new Vector2(-380f, -20f);
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
