using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Playables;

/// <summary>
/// Responsável por exibir um título ao iniciar um nível (ex: "Memórias boas")
/// com um efeito de fade in e fade out no Canvas, com suporte opcional a Timeline Cutscenes.
/// </summary>
public class LevelIntro : MonoBehaviour
{
    [Header("Configuração do Título")]
    [Tooltip("O texto que será exibido (ex: Memórias boas, Memórias ruins)")]
    public string titulo = "Memórias boas";
    
    [Tooltip("Tempo de exibição do texto na tela.")]
    public float tempoExibicao = 3.5f;
    
    [Tooltip("Velocidade do fade in/out.")]
    public float duracaoFade = 2.5f;

    [Header("Estilo do Texto")]
    public int tamanhoFonte = 80;
    public Color corTexto = Color.white;

    [Header("Cutscene Inicial (Timeline)")]
    [Tooltip("Arraste o PlayableDirector da sua Timeline aqui. Se configurado, a fase só começará quando a Timeline terminar.")]
    public PlayableDirector timelineCutscene;

    private Canvas canvas;
    private TextMeshProUGUI textoUI;
    private Coroutine rotinaPrincipal;

    private void Start()
    {
        CriarUI();
        rotinaPrincipal = StartCoroutine(RotinaApresentacao());
    }

    private void OnDisable()
    {
        // CRÍTICO: Garantir que o player seja descongelado mesmo se algo der errado
        if (timelineCutscene != null && timelineCutscene.isActiveAndEnabled)
        {
            Debug.Log("[LevelIntro.OnDisable] Script desativado com timeline ainda ativa — parando timeline e descongelando player.");
            timelineCutscene.Stop();

            // Fallback: Desativar câmeras da timeline se o script for desativado abruptamente
            var vCams = timelineCutscene.GetComponentsInChildren<Unity.Cinemachine.CinemachineCamera>(true);
            foreach (var cam in vCams)
            {
                cam.Priority = 0;
                cam.enabled = false;
            }
            var normCams = timelineCutscene.GetComponentsInChildren<Camera>(true);
            foreach (var cam in normCams)
            {
                cam.enabled = false;
            }
        }
        CongelarPlayer(false);
        if (canvas != null)
        {
            Destroy(canvas.gameObject);
        }
    }

    private void CriarUI()
    {
        // Criação de Canvas isolado para intro
        GameObject goCanvas = new GameObject("LevelIntroCanvas");
        canvas = goCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 998; // Atrás da tela de transição se houver
        goCanvas.AddComponent<CanvasScaler>();
        
        GameObject goTexto = new GameObject("TextoTitulo");
        goTexto.transform.SetParent(goCanvas.transform, false);
        textoUI = goTexto.AddComponent<TextMeshProUGUI>();
        textoUI.text = titulo;
        textoUI.fontSize = tamanhoFonte;
        textoUI.alignment = TextAlignmentOptions.Center;
        textoUI.color = new Color(corTexto.r, corTexto.g, corTexto.b, 0); // Começa invisível
        
        RectTransform rtTexto = textoUI.rectTransform;
        rtTexto.anchorMin = new Vector2(0.5f, 0.5f);
        rtTexto.anchorMax = new Vector2(0.5f, 0.5f);
        rtTexto.sizeDelta = new Vector2(1200, 200);
        rtTexto.anchoredPosition = Vector2.zero; // Centro da tela
    }

    private IEnumerator RotinaApresentacao()
    {
        // Trava o player se houver cutscene
        if (timelineCutscene != null)
        {
            CongelarPlayer(true);
        }

        // Fade in do Título
        yield return StartCoroutine(FadeTexto(0f, 1f, duracaoFade));

        // Tocar a Timeline, se houver
        if (timelineCutscene != null)
        {
            Debug.Log("[LevelIntro] Iniciando Timeline...");
            timelineCutscene.Play();
            
            // Espera até que a Timeline não esteja mais no estado 'Playing' COM TIMEOUT
            float timelineTimeout = 6f; // máximo 30 segundos
            float timelineStartTime = Time.time;
            while (timelineCutscene.state == PlayState.Playing && 
                   (Time.time - timelineStartTime) < timelineTimeout)
            {
                yield return null;
            }
            
            if (Time.time - timelineStartTime >= timelineTimeout)
            {
                Debug.LogWarning("[LevelIntro] Timeline demorou demais! Parando forçadamente.");
                timelineCutscene.Stop();
            }
            else
            {
                Debug.Log("[LevelIntro] Timeline finalizada normalmente.");
            }
            
            // CORREÇÃO DUPLA DE FALLBACK:
            // 1. Não desativamos o GameObject da timeline (o que pode quebrar algo no projeto do amigo)
            // 2. Zeramos a prioridade e desativamos todas as CinemachineCameras filhas da timeline.
            // 3. Desativamos também Câmeras normais filhas da timeline.
            // Isso devolve o controle limpo para a câmera do player.
            var vCams = timelineCutscene.GetComponentsInChildren<Unity.Cinemachine.CinemachineCamera>(true);
            foreach (var cam in vCams)
            {
                cam.Priority = 0;
                cam.enabled = false;
            }

            var normCams = timelineCutscene.GetComponentsInChildren<Camera>(true);
            foreach (var cam in normCams)
            {
                cam.enabled = false;
            }
        }
        else
        {
            // Wait normal se não tiver cutscene
            yield return new WaitForSeconds(tempoExibicao);
        }

        // Fade out do Título
        yield return StartCoroutine(FadeTexto(1f, 0f, duracaoFade));

        // Libera o player (sempre, não importa se houve timeline)
        Debug.Log("[LevelIntro] Descongelando player...");
        CongelarPlayer(false);

        // Limpar da memoria
        if (canvas != null)
        {
            Destroy(canvas.gameObject);
        }
        
        Debug.Log("[LevelIntro] Apresentação completa!");
        rotinaPrincipal = null;
    }

    private IEnumerator FadeTexto(float startAlpha, float endAlpha, float duration)
    {
        if (textoUI == null) yield break;

        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, p);
            textoUI.color = new Color(corTexto.r, corTexto.g, corTexto.b, alpha);
            yield return null;
        }
    }

    private void CongelarPlayer(bool congelar)
    {
        GameObject playerAtual = GameObject.FindWithTag("Player");
        if (playerAtual == null) return;

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

        var inputHandler = playerAtual.GetComponent<PlayerInputHandler>();
        if (inputHandler != null)
        {
            inputHandler.enabled = !congelar;
        }

        var playerInput = playerAtual.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = !congelar;
        }

        var animator = playerAtual.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.speed = congelar ? 0f : 1f;
        }

        var inputControllers = FindObjectsByType<Unity.Cinemachine.CinemachineInputAxisController>(FindObjectsSortMode.None);
        foreach (var ctrl in inputControllers)
        {
            ctrl.enabled = !congelar;
        }
    }
}
