using UnityEngine;
using TMPro;

/// <summary>
/// Adicione este script às cópias fantasma geradas em runtime pelo Dialogo.cs.
/// Oscila a posição com Perlin Noise e pulsa o alpha para simular vento.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class EfeitoVento : MonoBehaviour
{
    [Header("Oscilação de Posição")]
    public float velocidade = 1.5f;
    public float intensidade = 6f;
    public Vector2 deslocamentoBase = new Vector2(4f, -2f);

    [Header("Alpha")]
    [Range(0f, 1f)] public float alphBase = 0.35f;
    [Range(0f, 0.5f)] public float alphaVariacao = 0.08f;
    public float alphaVelocidade = 2f;

    [Header("Perlin Seed")]
    public float sementeAleatoria = 0f;

    private TextMeshProUGUI tmp;
    private Vector3 posicaoOriginal;

    private void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// Chamado pelo Dialogo.cs logo após o posicionamento do fantasma.
    /// </summary>
    public void InicializarPosicao()
    {
        posicaoOriginal = transform.localPosition;
    }

    private void Update()
    {
        float t = Time.time * velocidade + sementeAleatoria;
        float dx = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * intensidade;
        float dy = (Mathf.PerlinNoise(0f, t + 10f) - 0.5f) * 2f * intensidade;

        transform.localPosition = posicaoOriginal
            + new Vector3(deslocamentoBase.x + dx, deslocamentoBase.y + dy, 0f);

        float alphaAtual = alphBase
            + Mathf.Sin(Time.time * alphaVelocidade + sementeAleatoria) * alphaVariacao;

        Color cor = tmp.color;
        cor.a = Mathf.Clamp01(alphaAtual);
        tmp.color = cor;
    }
}
