using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Representa uma única fala da narração.
/// Um áudio pode ter vários segmentos de texto — cada um aparece e some durante o mesmo clipe.
/// </summary>
[System.Serializable]
public class Fala
{
    [Tooltip("Áudio desta fala.")]
    public AudioClip dublagem;

    [Tooltip("Um ou mais textos para exibir durante este áudio.\n" +
             "O tempo do clipe é dividido igualmente entre os segmentos.\n" +
             "Cada segmento aparece, é digitado e some antes do próximo começar.")]
    [TextArea(2, 4)]
    public string[] segmentos;

    [Tooltip("TextMeshProUGUI onde esta fala será exibida. Se vazio, usa o TextMesh padrão.")]
    public TextMeshProUGUI textoAlvo;
}

/// <summary>
/// Sistema de narração com dublagem, legenda, efeito máquina de escrever e rastro de vento.
/// Os fantasmas de vento são gerados automaticamente em runtime — configure apenas 1 TextMesh por fala.
/// </summary>
public class Dialogo : MonoBehaviour
{
    [Header("Áudio")]
    [Tooltip("AudioSource que tocará as dublagens.")]
    [SerializeField] private AudioSource audioSource;

    [Header("Texto Padrão")]
    [Tooltip("Fallback: usado quando a Fala não especifica Texto Alvo.")]
    [SerializeField] private TextMeshProUGUI legendaTextoPadrao;

    [Header("Falas")]
    [SerializeField] private Fala[] falas;

    [Header("Efeito Máquina de Escrever")]
    [SerializeField] private bool usarEfeitoDigitacao = true;
    [SerializeField][Range(0f, 1f)] private float margemFinal = 0.1f;

    [Header("Fantasmas de Vento (Gerados Automaticamente)")]
    [Tooltip("Quantas cópias fantasma criar atrás de cada texto.")]
    [SerializeField][Range(0, 4)] private int quantidadeFantasmas = 2;

    [Tooltip("Alpha de cada fantasma. O índice corresponde à cópia (0 = mais próxima, etc.).")]
    [SerializeField] private float[] alphasFantasma = { 0.35f, 0.18f };

    [Tooltip("Deslocamento base (pixels) de cada fantasma em relação ao texto original.")]
    [SerializeField] private Vector2[] offsetsFantasma = { new Vector2(5f, -2f), new Vector2(10f, -4f) };

    [Tooltip("Velocidade da oscilação de vento.")]
    [SerializeField] private float ventoVelocidade = 1.5f;

    [Tooltip("Intensidade do deslocamento oscilatório em pixels.")]
    [SerializeField] private float ventoIntensidade = 6f;

    [Header("Configurações")]
    [SerializeField] private bool iniciarAutomaticamente = true;
    [SerializeField] private float delayInicial = 0.5f;
    [SerializeField] private float tempoExibicaoFinal = 0.3f;

    [Header("Eventos")]
    [Tooltip("Chamado quando toda a narração termina.")]
    [SerializeField] private UnityEvent aoFinalizarDialogo;

    /// <summary>Evento C# — assine via código: dialogo.OnDialogoFinalizado += MinhaFuncao;</summary>
    public event System.Action OnDialogoFinalizado;

    // Mapa: TextMesh original → lista de fantasmas gerados
    private Dictionary<TextMeshProUGUI, TextMeshProUGUI[]> fantasmasGerados
        = new Dictionary<TextMeshProUGUI, TextMeshProUGUI[]>();

    private bool podeTocar = true;
    private IEnumerator coroutineAtual;

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    private void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Esconde textos e gera fantasmas para todos os alvos únicos
        var alvosProcessados = new HashSet<TextMeshProUGUI>();

        EsconderTexto(legendaTextoPadrao);
        if (legendaTextoPadrao != null && !alvosProcessados.Contains(legendaTextoPadrao))
        {
            GerarFantasmas(legendaTextoPadrao);
            alvosProcessados.Add(legendaTextoPadrao);
        }

        if (falas != null)
        {
            foreach (var fala in falas)
            {
                var alvo = fala.textoAlvo != null ? fala.textoAlvo : legendaTextoPadrao;
                EsconderTexto(alvo);

                if (alvo != null && !alvosProcessados.Contains(alvo))
                {
                    GerarFantasmas(alvo);
                    alvosProcessados.Add(alvo);
                }
            }
        }

        if (iniciarAutomaticamente)
            StartCoroutine(IniciarComDelay());
    }

    // ── API de configuração ───────────────────────────────────────────────────

    /// <summary>
    /// Desabilita o início automático. Chame ANTES do Start() rodar
    /// (ou seja, logo após Instantiate, no mesmo frame).
    /// </summary>
    public void DesativarAutoStart()
    {
        iniciarAutomaticamente = false;
    }

    private IEnumerator IniciarComDelay()
    {
        yield return new WaitForSeconds(delayInicial);
        TocarDialogo();
    }

    // ── API pública ───────────────────────────────────────────────────────────

    public void TocarDialogo()
    {
        if (!podeTocar) return;

        if (falas == null || falas.Length == 0)
        {
            Debug.LogWarning("[Dialogo] Nenhuma fala configurada no Inspector.");
            return;
        }

        podeTocar = false;
        coroutineAtual = TocarFala();
        StartCoroutine(coroutineAtual);
    }

    public void PararDialogo()
    {
        if (coroutineAtual != null)
            StopCoroutine(coroutineAtual);

        if (audioSource != null)
            audioSource.Stop();

        // Esconde todos os textos e seus fantasmas
        EsconderComFantasmas(legendaTextoPadrao);
        if (falas != null)
            foreach (var fala in falas)
                EsconderComFantasmas(fala.textoAlvo);

        podeTocar = true;
    }

    // ── Lógica principal ──────────────────────────────────────────────────────

    private IEnumerator TocarFala()
    {
        for (int i = 0; i < falas.Length; i++)
        {
            Fala falaAtual = falas[i];
            TextMeshProUGUI textoAtivo = (falaAtual.textoAlvo != null)
                ? falaAtual.textoAlvo
                : legendaTextoPadrao;

            if (textoAtivo == null)
            {
                Debug.LogWarning($"[Dialogo] Fala {i}: nenhum TextMesh configurado.");
                continue;
            }

            // Toca o áudio UMA vez para todos os segmentos
            if (falaAtual.dublagem != null)
            {
                audioSource.clip = falaAtual.dublagem;
                audioSource.Play();
            }

            float duracaoTotal = (falaAtual.dublagem != null) ? falaAtual.dublagem.length : 1f;

            // Garante ao menos 1 segmento vazio se array não foi preenchido
            string[] segs = (falaAtual.segmentos != null && falaAtual.segmentos.Length > 0)
                ? falaAtual.segmentos
                : new string[] { string.Empty };

            // Tempo disponível para cada segmento (dividido igualmente)
            float duracaoPorSegmento = duracaoTotal / segs.Length;

            for (int s = 0; s < segs.Length; s++)
            {
                string texto = segs[s] ?? string.Empty;

                // Mostra o texto e os fantasmas
                textoAtivo.gameObject.SetActive(true);
                AtivarFantasmas(textoAtivo, string.Empty);

                if (usarEfeitoDigitacao && texto.Length > 0)
                {
                    float tempoUtil = Mathf.Max(duracaoPorSegmento - margemFinal, 0.1f);
                    float tempoPorLetra = tempoUtil / texto.Length;

                    yield return StartCoroutine(
                        DigitarComFantasmas(textoAtivo, texto, tempoPorLetra));

                    // Aguarda o resto do tempo deste segmento
                    float tempoRestante = duracaoPorSegmento - tempoUtil;
                    if (tempoRestante > 0f)
                        yield return new WaitForSeconds(tempoRestante);
                }
                else
                {
                    textoAtivo.text = texto;
                    SincronizarFantasmas(textoAtivo, texto);
                    yield return new WaitForSeconds(duracaoPorSegmento);
                }

                // Breve pausa com texto completo visível antes de sumir
                if (tempoExibicaoFinal > 0f && s < segs.Length - 1)
                    yield return new WaitForSeconds(tempoExibicaoFinal);

                // Esconde entre segmentos (exceto no último — ele some abaixo)
                if (s < segs.Length - 1)
                {
                    EsconderComFantasmas(textoAtivo);
                    yield return new WaitForSeconds(0.05f); // flash de transição
                }
            }

            // Pausa final após o último segmento
            if (tempoExibicaoFinal > 0f)
                yield return new WaitForSeconds(tempoExibicaoFinal);

            EsconderComFantasmas(textoAtivo);
            yield return new WaitForSeconds(0.1f);
        }

        // Dispara os eventos de conclusão
        aoFinalizarDialogo?.Invoke();
        OnDialogoFinalizado?.Invoke();

        podeTocar = true;
    }

    private IEnumerator DigitarComFantasmas(
        TextMeshProUGUI principal, string texto, float intervaloPorLetra)
    {
        principal.text = string.Empty;
        SincronizarFantasmas(principal, string.Empty);

        TextMeshProUGUI[] ghosts = ObterFantasmas(principal);
        int numGhosts = ghosts != null ? ghosts.Length : 0;

        for (int c = 0; c < texto.Length; c++)
        {
            principal.text = texto.Substring(0, c + 1);

            // Cada fantasma atrasa (f+1) letras — cria o rastro
            for (int f = 0; f < numGhosts; f++)
            {
                if (ghosts[f] == null) continue;
                int idx = Mathf.Max(0, c - (f + 1));
                ghosts[f].text = c >= (f + 1) ? texto.Substring(0, idx + 1) : string.Empty;
            }

            yield return new WaitForSeconds(intervaloPorLetra);
        }
    }

    // ── Geração de fantasmas ──────────────────────────────────────────────────

    /// <summary>
    /// Instancia cópias fantasma do TMP original como irmãos na hierarquia.
    /// </summary>
    private void GerarFantasmas(TextMeshProUGUI original)
    {
        if (original == null || quantidadeFantasmas <= 0) return;
        if (fantasmasGerados.ContainsKey(original)) return;

        var lista = new TextMeshProUGUI[quantidadeFantasmas];
        int indiceOriginal = original.transform.GetSiblingIndex();

        for (int f = 0; f < quantidadeFantasmas; f++)
        {
            // Clona o GameObject inteiro (preserva fonte, tamanho, material, etc.)
            GameObject clone = Instantiate(original.gameObject, original.transform.parent);
            clone.name = original.name + "_Fantasma" + f;

            // Coloca ANTES do original na hierarquia (renderiza por baixo)
            clone.transform.SetSiblingIndex(indiceOriginal);

            // Copia o RectTransform exatamente
            RectTransform rtOriginal = original.GetComponent<RectTransform>();
            RectTransform rtClone = clone.GetComponent<RectTransform>();
            rtClone.anchorMin = rtOriginal.anchorMin;
            rtClone.anchorMax = rtOriginal.anchorMax;
            rtClone.anchoredPosition = rtOriginal.anchoredPosition;
            rtClone.sizeDelta = rtOriginal.sizeDelta;
            rtClone.pivot = rtOriginal.pivot;

            // Ajusta alpha
            TextMeshProUGUI tmpClone = clone.GetComponent<TextMeshProUGUI>();
            float alpha = (f < alphasFantasma.Length) ? alphasFantasma[f] : 0.2f;
            Color cor = tmpClone.color;
            cor.a = alpha;
            tmpClone.color = cor;
            tmpClone.text = string.Empty;

            // Adiciona e configura o EfeitoVento
            EfeitoVento vento = clone.AddComponent<EfeitoVento>();
            vento.velocidade = ventoVelocidade;
            vento.intensidade = ventoIntensidade;
            vento.alphBase = alpha;
            vento.alphaVariacao = 0.08f;
            vento.alphaVelocidade = 2f;
            vento.sementeAleatoria = f * 3.7f;
            vento.deslocamentoBase = (f < offsetsFantasma.Length)
                ? offsetsFantasma[f]
                : new Vector2((f + 1) * 5f, (f + 1) * -2f);

            // Inicializa a posição base ANTES de ativar o EfeitoVento
            vento.InicializarPosicao();

            clone.SetActive(false);
            lista[f] = tmpClone;
        }

        fantasmasGerados[original] = lista;
    }

    // ── Helpers de exibição ───────────────────────────────────────────────────

    private void AtivarFantasmas(TextMeshProUGUI original, string texto)
    {
        TextMeshProUGUI[] ghosts = ObterFantasmas(original);
        if (ghosts == null) return;
        foreach (var g in ghosts)
        {
            if (g == null) continue;
            g.text = texto;
            g.gameObject.SetActive(true);
        }
    }

    private void SincronizarFantasmas(TextMeshProUGUI original, string texto)
    {
        TextMeshProUGUI[] ghosts = ObterFantasmas(original);
        if (ghosts == null) return;
        foreach (var g in ghosts)
            if (g != null) g.text = texto;
    }

    private void EsconderComFantasmas(TextMeshProUGUI original)
    {
        EsconderTexto(original);
        TextMeshProUGUI[] ghosts = ObterFantasmas(original);
        if (ghosts == null) return;
        foreach (var g in ghosts)
            EsconderTexto(g);
    }

    private TextMeshProUGUI[] ObterFantasmas(TextMeshProUGUI original)
    {
        if (original == null) return null;
        fantasmasGerados.TryGetValue(original, out var ghosts);
        return ghosts;
    }

    private void EsconderTexto(TextMeshProUGUI alvo)
    {
        if (alvo == null) return;
        alvo.text = string.Empty;
        alvo.gameObject.SetActive(false);
    }
}
