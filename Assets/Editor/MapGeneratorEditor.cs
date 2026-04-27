using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MapGeneratorEditor : EditorWindow
{
    private int mapLength = 15;
    private float minJumpGap = 3f;
    private float maxJumpGap = 6f;

    // Segment direction system
    private int segmentLength = 4;
    private float maxSegmentAngle = 55f;
    private bool allowBacktrack = false;
    private float ySpringStrength = 0.4f;
    private float maxYOffset = 12f;

    private List<GameObject> chunkTemplates = new List<GameObject>();
    private Vector2 scrollPos;

    [MenuItem("Tools/Gerador de Mapa Avançado")]
    public static void ShowWindow()
    {
        GetWindow<MapGeneratorEditor>("Gerador de Mapa");
    }

    private void OnGUI()
    {
        GUILayout.Label("Gerador de Níveis Modular (Chunks)", EditorStyles.boldLabel);

        mapLength = EditorGUILayout.IntField("Quantidade de Estruturas", mapLength);
        minJumpGap = EditorGUILayout.FloatField("Pulo Mínimo (Eixo X)", minJumpGap);
        maxJumpGap = EditorGUILayout.FloatField("Pulo Máximo (Eixo X)", maxJumpGap);

        EditorGUILayout.Space();
        GUILayout.Label("Forma do Caminho", EditorStyles.boldLabel);

        segmentLength = EditorGUILayout.IntSlider("Chunks por Segmento", segmentLength, 2, 8);
        maxSegmentAngle = EditorGUILayout.Slider("Ângulo Máximo de Curva (°)", maxSegmentAngle, 10f, 80f);
        allowBacktrack = EditorGUILayout.Toggle("Permitir Retrocesso em X", allowBacktrack);
        ySpringStrength = EditorGUILayout.Slider("Força de Retorno ao Centro Y", ySpringStrength, 0f, 1f);
        maxYOffset = EditorGUILayout.FloatField("Desvio Máximo em Y", maxYOffset);

        EditorGUILayout.Space();
        GUILayout.Label("Sua Biblioteca de Estruturas", EditorStyles.boldLabel);

        if (GUILayout.Button("Buscar Estruturas na Cena (Desafio 1, Ilha, etc)"))
        {
            FindChunksInScene();
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
        for (int i = 0; i < chunkTemplates.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            chunkTemplates[i] = (GameObject)EditorGUILayout.ObjectField(chunkTemplates[i], typeof(GameObject), true);
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                chunkTemplates.RemoveAt(i);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Adicionar Estrutura Manualmente"))
        {
            chunkTemplates.Add(null);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Gerar Novo Mapa Modular", GUILayout.Height(30)))
        {
            GenerateComplexMap();
        }

        if (GUILayout.Button("Limpar Mapa Gerado"))
        {
            ClearMap();
        }

        EditorGUILayout.HelpBox(
            "DICA: 'Chunks por Segmento' controla de quantos em quantos chunks o caminho muda de direção. " +
            "'Ângulo Máximo' define o quão bruscas são as curvas. " +
            "Com poucos chunks distintos o mapa fica repetitivo — crie mais blocos 'Desafio N'!",
            MessageType.Info);
    }

    private void FindChunksInScene()
    {
        chunkTemplates.Clear();
        string[] commonNames = new string[] { "Desafio 1", "Desafio 2", "Desafio 3", "Desafio 4", "Desafio 5", "Ilha", "Ponte", "Subida", "Inicio", "Final" };

        foreach (string n in commonNames)
        {
            GameObject obj = GameObject.Find(n);
            if (obj != null && !chunkTemplates.Contains(obj))
                chunkTemplates.Add(obj);
        }

        if (chunkTemplates.Count == 0)
            Debug.LogWarning("Nenhuma estrutura padrão encontrada. Certifique-se de que existem objetos como 'Desafio 1' na cena.");
        else
            Debug.Log(chunkTemplates.Count + " estruturas encontradas e adicionadas!");
    }

    private void GenerateComplexMap()
    {
        ClearMap();

        chunkTemplates.RemoveAll(item => item == null);

        if (chunkTemplates.Count == 0)
        {
            Debug.LogError("A lista de estruturas está vazia.");
            return;
        }

        GameObject container = new GameObject("GeneratedMap_Modular");

        Vector2 currentPos = Vector2.zero;
        Vector2 currentDirection = Vector2.right;
        GameObject lastTemplate = null;

        for (int i = 0; i < mapLength; i++)
        {
            // Change direction at the start of each segment
            if (i % segmentLength == 0)
            {
                float angle = Random.Range(-maxSegmentAngle, maxSegmentAngle);
                currentDirection = RotateVector(currentDirection, angle).normalized;

                if (!allowBacktrack && currentDirection.x < 0.2f)
                    currentDirection.x = 0.2f;

                currentDirection.Normalize();
            }

            // Spring-back: nudge direction toward Y=0 when drifting too far
            if (Mathf.Abs(currentPos.y) > maxYOffset)
            {
                currentDirection.y = Mathf.Lerp(currentDirection.y,
                    -Mathf.Sign(currentPos.y) * ySpringStrength, 0.5f);
                currentDirection.Normalize();
            }

            // Pick a template, avoid repeating the same chunk twice in a row
            GameObject template = null;
            int attempts = 0;
            do
            {
                template = chunkTemplates[Random.Range(0, chunkTemplates.Count)];
                attempts++;
            } while (template == lastTemplate && chunkTemplates.Count > 1 && attempts < 10);
            lastTemplate = template;

            GameObject chunk = Instantiate(template);
            chunk.name = template.name + "_Part_" + i;
            chunk.transform.SetParent(container.transform);
            chunk.SetActive(true);

            // Move to origin to measure visual bounds
            chunk.transform.position = Vector3.zero;

            Vector2 entryPoint, exitPoint;
            GetChunkEntryAndExit(chunk, out entryPoint, out exitPoint);

            // Align the chunk's entry to currentPos
            float offsetX = currentPos.x - entryPoint.x;
            float offsetY = currentPos.y - entryPoint.y;
            chunk.transform.position += new Vector3(offsetX, offsetY, 0);

            // Recalculate exit after move
            GetChunkEntryAndExit(chunk, out entryPoint, out exitPoint);

            // Advance along the current direction
            float gap = Random.Range(minJumpGap, maxJumpGap);
            currentPos = exitPoint + currentDirection * gap;
        }

        Debug.Log("Mapa gerado com " + mapLength + " chunks e " + Mathf.CeilToInt((float)mapLength / segmentLength) + " segmentos de direção.");

        foreach (GameObject t in chunkTemplates)
            t.SetActive(false);
    }

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private void GetChunkEntryAndExit(GameObject chunk, out Vector2 entry, out Vector2 exit)
    {
        Renderer[] renderers = chunk.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            entry = chunk.transform.position;
            exit = chunk.transform.position;
            return;
        }

        float minX = float.MaxValue;
        float maxX = float.MinValue;

        List<Renderer> validRenderers = new List<Renderer>();

        foreach (Renderer r in renderers)
        {
            string n = r.gameObject.name.ToLower();
            if (n.Contains("tree") || n.Contains("cloud") || n.Contains("bg") || n.Contains("particle"))
                continue;

            validRenderers.Add(r);
            if (r.bounds.min.x < minX) minX = r.bounds.min.x;
            if (r.bounds.max.x > maxX) maxX = r.bounds.max.x;
        }

        if (validRenderers.Count == 0)
        {
            foreach (Renderer r in renderers)
            {
                validRenderers.Add(r);
                if (r.bounds.min.x < minX) minX = r.bounds.min.x;
                if (r.bounds.max.x > maxX) maxX = r.bounds.max.x;
            }
        }

        float entryY = 0f;
        float exitY = 0f;
        float minXDist = float.MaxValue;
        float maxXDist = float.MaxValue;

        foreach (Renderer r in validRenderers)
        {
            float distToStart = Mathf.Abs(r.bounds.min.x - minX);
            if (distToStart < minXDist)
            {
                minXDist = distToStart;
                entryY = r.bounds.max.y;
            }

            float distToEnd = Mathf.Abs(r.bounds.max.x - maxX);
            if (distToEnd < maxXDist)
            {
                maxXDist = distToEnd;
                exitY = r.bounds.max.y;
            }
        }

        entry = new Vector2(minX, entryY);
        exit = new Vector2(maxX, exitY);
    }

    private void ClearMap()
    {
        GameObject existing = GameObject.Find("GeneratedMap_Modular");
        if (existing != null)
            DestroyImmediate(existing);
    }
}
