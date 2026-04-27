using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Exibe na tela "Colete os itens (X/3)" usando TextMeshPro.
/// 
/// ▸ Coloque este script num GameObject dentro do Canvas de cada mapa.
/// ▸ Arraste o componente TextMeshProUGUI no campo "textoItens" no Inspector.
/// </summary>
public class CollectableHUD : MonoBehaviour
{
    [Header("Referência UI")]
    [Tooltip("TextMeshProUGUI que exibirá a contagem de itens.")]
    public TextMeshProUGUI textoItens;

    [Header("Mensagem de Conclusão")]
    [Tooltip("Texto exibido ao completar todos os itens do mapa.")]
    public string mensagemCompleto = "Mapa completo!";

    [Tooltip("Tempo (segundos) que a mensagem de conclusão fica na tela.")]
    public float tempoMensagemCompleto = 3f;

    [Header("Efeito Visual")]
    [Tooltip("Cor normal do texto.")]
    public Color corNormal = Color.white;
    [Tooltip("Cor do texto ao coletar um item (flash).")]
    public Color corColeta = new Color(0.2f, 1f, 0.4f); // verde
    [Tooltip("Cor do texto quando o mapa é completado.")]
    public Color corCompleto = new Color(1f, 0.84f, 0f); // dourado
    [Tooltip("Duração do flash ao coletar.")]
    public float duracaoFlash = 0.4f;

    private Coroutine flashCoroutine;

    private void Start()
    {
        // Inscreve nos eventos do GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnItemColetado += AtualizarTexto;
            GameManager.Instance.OnMapaCompleto += MostrarMapaCompleto;

            // Atualiza imediatamente
            AtualizarTexto(GameManager.Instance.ColetadosNoMapa, GameManager.Instance.itensPorMapa);
        }
        else
        {
            Debug.LogWarning("[CollectableHUD] GameManager não encontrado! O texto não será atualizado.");
            if (textoItens != null)
                textoItens.text = "Colete os itens (0/3)";
        }
    }

    private void OnDestroy()
    {
        // Desinscreve para evitar erros ao destruir
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnItemColetado -= AtualizarTexto;
            GameManager.Instance.OnMapaCompleto -= MostrarMapaCompleto;
        }
    }

    private void AtualizarTexto(int coletados, int total)
    {
        if (textoItens == null) return;

        textoItens.text = $"Colete os itens ({coletados}/{total})";

        // Flash verde ao coletar (só se não for a chamada inicial com 0)
        if (coletados > 0)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashCor(corColeta));
        }
        else
        {
            textoItens.color = corNormal;
        }
    }

    private void MostrarMapaCompleto()
    {
        if (textoItens == null) return;

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);

        textoItens.text = mensagemCompleto;
        textoItens.color = corCompleto;

        // Opcional: scale punch
        StartCoroutine(PulseTexto());
    }

    private IEnumerator FlashCor(Color corFlash)
    {
        textoItens.color = corFlash;

        float t = 0f;
        while (t < duracaoFlash)
        {
            t += Time.deltaTime;
            textoItens.color = Color.Lerp(corFlash, corNormal, t / duracaoFlash);
            yield return null;
        }

        textoItens.color = corNormal;
        flashCoroutine = null;
    }

    private IEnumerator PulseTexto()
    {
        Vector3 escalaOriginal = textoItens.transform.localScale;
        Vector3 escalaGrande = escalaOriginal * 1.3f;

        float t = 0f;
        float duracao = 0.3f;

        // Aumenta
        while (t < duracao)
        {
            t += Time.deltaTime;
            textoItens.transform.localScale = Vector3.Lerp(escalaOriginal, escalaGrande, t / duracao);
            yield return null;
        }

        t = 0f;
        // Volta
        while (t < duracao)
        {
            t += Time.deltaTime;
            textoItens.transform.localScale = Vector3.Lerp(escalaGrande, escalaOriginal, t / duracao);
            yield return null;
        }

        textoItens.transform.localScale = escalaOriginal;
    }
}
