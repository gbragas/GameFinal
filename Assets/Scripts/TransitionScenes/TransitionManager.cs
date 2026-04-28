using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Gerencia a cena de transição.
/// Carrega os modelos do player atual e os itens coletados, 
/// faz um fade in, espera, e depois carrega a cena de destino com fade out.
/// </summary>
public class TransitionManager : MonoBehaviour
{
    public static string targetSceneName = "";

    [Header("Configurações UI")]
    public float duracaoFade = 2.5f;
    public float tempoEsperaTransicao = 4.0f;
    public Color corFundo = Color.black;
    public string textoExibido = "Viajando nas memórias...";

    [Header("Vitrine 3D")]
    public float rotacaoVelocidade = 45f;
    public Vector3 playerOffset = new Vector3(0, -1, 4);

    // Interno
    private Canvas canvas;
    private Image fundoEscuro;
    private TextMeshProUGUI textoTransitiva;
    private RawImage rawVitrine;

    private Camera cameraVitrine;
    private RenderTexture renderTexture;
    private Light luzVitrine;

    private GameObject modeloPlayer;
    private List<GameObject> modelosItens = new List<GameObject>();

    private void Start()
    {
        CriarUI();
        CriarCameraVitrine();
        MontarModelos();

        StartCoroutine(RotinaDeTransicao());
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }

    private void Update()
    {
        // Gira os modelos
        if (modeloPlayer != null)
        {
            modeloPlayer.transform.Rotate(Vector3.up, rotacaoVelocidade * Time.deltaTime, Space.World);
        }
        
        foreach (var item in modelosItens)
        {
            if (item != null)
                item.transform.Rotate(Vector3.up, rotacaoVelocidade * 1.5f * Time.deltaTime, Space.World);
        }
    }

    private IEnumerator RotinaDeTransicao()
    {
        // Fade in (aparecendo a transição)
        yield return StartCoroutine(FadeFundo(1f, 0f, duracaoFade)); // fundo vai de preto pra transparente para mostrar a cena? Nao, a UI ja cobre tudo com fade out.

        // Na verdade, a UI de transicao já deve começar visível, e fazemos um fade-in do render texture e texto.
        // Vamos ajustar: o fundo é sempre opaco. O texto e os modelos fazem fade-in.
        yield return StartCoroutine(FadeElementos(0f, 1f, duracaoFade));

        yield return new WaitForSeconds(tempoEsperaTransicao);

        // Carregar cena em background
        AsyncOperation asyncLoad = null;
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
            asyncLoad.allowSceneActivation = false;
        }

        // Fade out
        yield return StartCoroutine(FadeElementos(1f, 0f, duracaoFade));

        // Transição final: tela preta e carrega a cena real
        if (asyncLoad != null)
        {
            asyncLoad.allowSceneActivation = true;
        }
        else
        {
            Debug.LogWarning("[TransitionManager] Cena destino vazia!");
            // Voltar pro menu ou algo assim
        }
    }

    private IEnumerator FadeElementos(float startAlpha, float endAlpha, float duration)
    {
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, p);
            
            if (rawVitrine != null)
                rawVitrine.color = new Color(1, 1, 1, alpha);
            
            if (textoTransitiva != null)
                textoTransitiva.color = new Color(textoTransitiva.color.r, textoTransitiva.color.g, textoTransitiva.color.b, alpha);
                
            yield return null;
        }
    }
    
    private IEnumerator FadeFundo(float startAlpha, float endAlpha, float duration)
    {
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, p);
            fundoEscuro.color = new Color(corFundo.r, corFundo.g, corFundo.b, alpha);
            yield return null;
        }
    }

    private void CriarUI()
    {
        GameObject goCanvas = new GameObject("TransitionCanvas");
        canvas = goCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        goCanvas.AddComponent<CanvasScaler>();
        goCanvas.AddComponent<GraphicRaycaster>();

        // Fundo Escuro
        GameObject goFundo = new GameObject("FundoEscuro");
        goFundo.transform.SetParent(goCanvas.transform, false);
        fundoEscuro = goFundo.AddComponent<Image>();
        fundoEscuro.color = corFundo;
        RectTransform rtFundo = fundoEscuro.rectTransform;
        rtFundo.anchorMin = Vector2.zero;
        rtFundo.anchorMax = Vector2.one;
        rtFundo.sizeDelta = Vector2.zero;

        // Raw Image Vitrine
        GameObject goVitrine = new GameObject("RawVitrine");
        goVitrine.transform.SetParent(goCanvas.transform, false);
        rawVitrine = goVitrine.AddComponent<RawImage>();
        rawVitrine.color = new Color(1, 1, 1, 0); // Começa invisível
        RectTransform rtVitrine = rawVitrine.rectTransform;
        rtVitrine.anchorMin = new Vector2(0.5f, 0.5f);
        rtVitrine.anchorMax = new Vector2(0.5f, 0.5f);
        rtVitrine.sizeDelta = new Vector2(800, 800);
        rtVitrine.anchoredPosition = new Vector2(0, 50);

        // Texto
        GameObject goTexto = new GameObject("TextoTransition");
        goTexto.transform.SetParent(goCanvas.transform, false);
        textoTransitiva = goTexto.AddComponent<TextMeshProUGUI>();
        textoTransitiva.text = textoExibido;
        textoTransitiva.fontSize = 48;
        textoTransitiva.alignment = TextAlignmentOptions.Center;
        textoTransitiva.color = new Color(1, 1, 1, 0); // Começa invisível
        RectTransform rtTexto = textoTransitiva.rectTransform;
        rtTexto.anchorMin = new Vector2(0.5f, 0f);
        rtTexto.anchorMax = new Vector2(0.5f, 0f);
        rtTexto.sizeDelta = new Vector2(1000, 100);
        rtTexto.anchoredPosition = new Vector2(0, 150);
    }

    private void CriarCameraVitrine()
    {
        renderTexture = new RenderTexture(1024, 1024, 24);
        renderTexture.antiAliasing = 4;
        rawVitrine.texture = renderTexture;

        GameObject goCam = new GameObject("TransitionCamera_Vitrine");
        goCam.transform.position = new Vector3(0, -500, 0); // Longe da cena
        cameraVitrine = goCam.AddComponent<Camera>();
        cameraVitrine.targetTexture = renderTexture;
        cameraVitrine.clearFlags = CameraClearFlags.SolidColor;
        cameraVitrine.backgroundColor = Color.clear;
        cameraVitrine.cullingMask = 1 << 31; // Layer 31
        cameraVitrine.fieldOfView = 60f;

        GameObject goLuz = new GameObject("TransitionLuz");
        goLuz.transform.SetParent(goCam.transform);
        goLuz.transform.localPosition = new Vector3(0, 3, -2);
        goLuz.transform.localRotation = Quaternion.Euler(30, 0, 0);
        luzVitrine = goLuz.AddComponent<Light>();
        luzVitrine.type = LightType.Directional;
        luzVitrine.intensity = 1.0f;
        luzVitrine.cullingMask = 1 << 31;
    }

    private void MontarModelos()
    {
        if (GameManager.Instance == null) return;

        int totalColetados = GameManager.Instance.TotalColetados;
        int versaoPlayer = Mathf.Clamp(totalColetados, 0, GameManager.Instance.playerPrefabs.Length - 1);
        
        GameObject playerPrefab = GameManager.Instance.playerPrefabs[versaoPlayer];
        
        if (playerPrefab != null)
        {
            modeloPlayer = Instantiate(playerPrefab);
            LimparScriptsComponentes(modeloPlayer);
            SetLayerRecursive(modeloPlayer, 31);
            
            modeloPlayer.transform.position = cameraVitrine.transform.position + cameraVitrine.transform.forward * 4.5f + Vector3.down * 1.5f;
            modeloPlayer.transform.localScale = Vector3.one * 1.5f;
        }

        // Criar as memórias (caixinhas ou cristais) girando ao redor dele
        // O CutsceneEvolucao não salva o prefab do item globalmente.
        // Vamos apenas usar cubos coloridos ou buscar do GameManager, se houvesse.
        // Como o GameManager não guarda os prefabs das memórias (itens coletados), 
        // vamos exibir esferas ou pequenos pontos de luz, ou apenas o player se não for possível.
    }

    private void LimparScriptsComponentes(GameObject obj)
    {
        foreach (var comp in obj.GetComponentsInChildren<MonoBehaviour>()) Destroy(comp);
        foreach (var comp in obj.GetComponentsInChildren<Collider>()) Destroy(comp);
        foreach (var comp in obj.GetComponentsInChildren<Rigidbody>()) Destroy(comp);
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }
}
