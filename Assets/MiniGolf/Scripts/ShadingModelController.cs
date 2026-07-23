using UnityEngine;

// Modulo didatico da disciplina de Computacao Grafica.
// Troca ao vivo o MODELO DE SHADING aplicado a bola, para comparar como cada
// tecnica calcula a reflexao da luz:
//
//   Lambert       - so difuso (N . L). Fosco, sem brilho.
//   Phong         - especular por reflexao (R . V). Brilho classico.
//   Blinn-Phong   - especular por halfway (N . H). Padrao em tempo real.
//   PBR (Standard)- fisicamente correto (metallic/roughness) do Unity.
//
// Usa o shader "CG/ShadingModels" para os tres classicos e o "Standard" para PBR.
// Cria-se sozinho em runtime; nao precisa configurar nada na cena.
public class ShadingModelController : MonoBehaviour
{
    private static ShadingModelController instance;

    private enum ShadingMode { Off, Lambert, Phong, BlinnPhong, PBR }

    private static readonly string[] modeNames =
    {
        "Original (material do jogo)",
        "Lambert (difuso)",
        "Phong (R . V)",
        "Blinn-Phong (N . H)",
        "PBR (Standard)"
    };

    private static readonly string[] modeDescriptions =
    {
        "Material original da bola, sem alteracao. Base de comparacao.",
        "Difuso puro: I = N . L. Superficie fosca, sem reflexo especular. Modelo mais simples.",
        "Especular de Phong: reflete a luz na normal (R) e compara com a camera (R . V)^n. Brilho concentrado.",
        "Especular de Blinn-Phong: usa o vetor halfway H = norm(L + V), calcula (N . H)^n. Mais barato; padrao em tempo real.",
        "PBR do Unity (Standard): baseado em fisica com metallic e roughness. Reflexo depende do ambiente."
    };

    [SerializeField] private KeyCode cycleKey = KeyCode.C;

    private Renderer targetRenderer;
    private Material originalMaterial;
    private Material classicMaterial;   // shader CG/ShadingModels
    private Material pbrMaterial;        // shader Standard

    private ShadingMode currentMode = ShadingMode.Off;
    private float shininess = 32f;
    private float metallic = 0.5f;
    private float smoothness = 0.6f;
    private readonly Color demoColor = new Color(0.55f, 0.68f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapIfMissing()
    {
        if (Object.FindObjectOfType<ShadingModelController>() != null)
        {
            return;
        }

        GameObject go = new GameObject("CG_ShadingController");
        go.AddComponent<ShadingModelController>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(cycleKey))
        {
            int next = ((int)currentMode + 1) % modeNames.Length;
            ApplyMode((ShadingMode)next);
        }
    }

    // Chamado quando uma nova fase e gerada. A bola antiga foi destruida, entao
    // reprocuramos o renderer novo e reaplicamos o modelo de shading escolhido.
    public static void NotifyLevelSpawned()
    {
        if (instance != null)
        {
            instance.ReTargetAndReapply();
        }
    }

    private void ReTargetAndReapply()
    {
        targetRenderer = null;      // forca EnsureTarget a achar a bola nova
        originalMaterial = null;

        if (currentMode != ShadingMode.Off)
        {
            ApplyMode(currentMode);
        }
    }

    // Localiza o renderer da bola sob demanda (o objeto pode nao existir no Awake).
    private bool EnsureTarget()
    {
        if (targetRenderer != null)
        {
            return true;
        }

        BallControl ball = Object.FindObjectOfType<BallControl>();
        if (ball == null)
        {
            return false;
        }

        targetRenderer = ball.GetComponent<Renderer>();
        if (targetRenderer == null)
        {
            targetRenderer = ball.GetComponentInChildren<Renderer>();
        }

        if (targetRenderer == null)
        {
            return false;
        }

        originalMaterial = targetRenderer.sharedMaterial;
        return true;
    }

    private void EnsureMaterials()
    {
        if (classicMaterial == null)
        {
            Shader classic = Shader.Find("CG/ShadingModels");
            if (classic != null)
            {
                classicMaterial = new Material(classic);
                classicMaterial.SetColor("_Color", demoColor);
            }
        }

        if (pbrMaterial == null)
        {
            Shader standard = Shader.Find("Standard");
            if (standard != null)
            {
                pbrMaterial = new Material(standard);
                pbrMaterial.SetColor("_Color", demoColor);
            }
        }
    }

    private void ApplyMode(ShadingMode mode)
    {
        if (!EnsureTarget())
        {
            return;
        }

        EnsureMaterials();
        currentMode = mode;

        switch (mode)
        {
            case ShadingMode.Off:
                targetRenderer.material = originalMaterial;
                break;

            case ShadingMode.Lambert:
            case ShadingMode.Phong:
            case ShadingMode.BlinnPhong:
                if (classicMaterial != null)
                {
                    // _Mode: 0 Lambert, 1 Phong, 2 Blinn-Phong (bate com o shader).
                    float shaderMode = mode == ShadingMode.Lambert ? 0f
                                     : mode == ShadingMode.Phong ? 1f : 2f;
                    classicMaterial.SetFloat("_Mode", shaderMode);
                    classicMaterial.SetFloat("_Shininess", shininess);
                    targetRenderer.material = classicMaterial;
                }
                break;

            case ShadingMode.PBR:
                if (pbrMaterial != null)
                {
                    pbrMaterial.SetFloat("_Metallic", metallic);
                    pbrMaterial.SetFloat("_Glossiness", smoothness);
                    targetRenderer.material = pbrMaterial;
                }
                break;
        }
    }

    // Desenha o conteudo da aba "Shading" dentro do menu unificado (CGMenuHUD).
    public void DrawTabContent()
    {
        GUILayout.Label("C cicla o modelo de shading");
        GUILayout.Space(6f);

        if (targetRenderer == null && !EnsureTarget())
        {
            GUILayout.Label("Bola nao encontrada ainda. Inicie o nivel.");
            return;
        }

        for (int i = 0; i < modeNames.Length; i++)
        {
            bool isCurrent = (i == (int)currentMode);
            string label = (isCurrent ? "> " : "   ") + modeNames[i];

            GUI.enabled = !isCurrent;
            if (GUILayout.Button(label, GUILayout.Height(26f)))
            {
                ApplyMode((ShadingMode)i);
            }
            GUI.enabled = true;
        }

        GUILayout.Space(8f);

        // Sliders relevantes ao modelo ativo.
        if (currentMode == ShadingMode.Phong || currentMode == ShadingMode.BlinnPhong)
        {
            GUILayout.Label("Shininess (expoente especular): " + Mathf.RoundToInt(shininess));
            float newShin = GUILayout.HorizontalSlider(shininess, 1f, 128f);
            if (!Mathf.Approximately(newShin, shininess))
            {
                shininess = newShin;
                classicMaterial.SetFloat("_Shininess", shininess);
            }
            GUILayout.Label("Maior = brilho menor e mais concentrado (superficie mais polida).");
        }
        else if (currentMode == ShadingMode.PBR)
        {
            GUILayout.Label("Metallic: " + metallic.ToString("0.00"));
            float newMet = GUILayout.HorizontalSlider(metallic, 0f, 1f);
            if (!Mathf.Approximately(newMet, metallic))
            {
                metallic = newMet;
                pbrMaterial.SetFloat("_Metallic", metallic);
            }

            GUILayout.Label("Smoothness: " + smoothness.ToString("0.00"));
            float newSmooth = GUILayout.HorizontalSlider(smoothness, 0f, 1f);
            if (!Mathf.Approximately(newSmooth, smoothness))
            {
                smoothness = newSmooth;
                pbrMaterial.SetFloat("_Glossiness", smoothness);
            }
            GUILayout.Label("Metallic: quao metalico. Smoothness: quao polido (reflexo nitido).");
        }

        GUILayout.Space(6f);
        GUILayout.Label("<b>Explicacao:</b>");
        GUILayout.Label(modeDescriptions[(int)currentMode]);
    }
}
