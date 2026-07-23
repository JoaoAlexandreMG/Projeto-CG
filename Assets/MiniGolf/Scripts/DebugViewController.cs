using UnityEngine;

// Modulo didatico da disciplina de Computacao Grafica: MODOS DE DEPURACAO VISUAL.
// Mostra o que normalmente fica "escondido" no pipeline de renderizacao:
//
//   Wireframe - so as arestas da malha (a geometria por baixo do shading)
//   Normais   - vetor normal de cada face mapeado em cor (base do calculo N.L)
//   UVs       - coordenadas de textura (como a imagem e "colada" na malha)
//   Depth     - profundidade: distancia de cada pixel ate a camera
//
// Wireframe usa GL.wireframe (nos eventos da camera). Normais/UVs/Depth usam um
// shader de REPLACEMENT aplicado pela camera a todos os objetos.
// Cria-se sozinho em runtime; nao precisa configurar nada na cena.
public class DebugViewController : MonoBehaviour
{
    private static DebugViewController instance;

    private enum DebugMode { Off, Wireframe, Normals, UVs, Depth }

    private static readonly string[] modeNames =
    {
        "Desligado (jogo normal)",
        "Wireframe (arestas)",
        "Normais (cor = XYZ)",
        "UVs (R=U, G=V)",
        "Depth (profundidade)"
    };

    private static readonly string[] modeDescriptions =
    {
        "Renderizacao normal do jogo, sem depuracao.",
        "Mostra so as ARESTAS dos triangulos. Revela a malha (mesh) por baixo do material. Mais triangulos = superficie mais suave.",
        "Cada NORMAL vira cor: X->vermelho, Y->verde, Z->azul. A normal e a base da iluminacao (I = N . L). Faces viradas para lados diferentes tem cores diferentes.",
        "Coordenadas de textura (UV): U no vermelho, V no verde. Mostra como uma imagem 2D e mapeada sobre a malha 3D.",
        "PROFUNDIDADE: mais claro = mais perto da camera, mais escuro = mais longe. E o que fica no depth buffer, usado para decidir o que aparece na frente."
    };

    [SerializeField] private KeyCode cycleKey = KeyCode.V;
    [SerializeField] private float depthScale = 0.03f;

    private DebugMode currentMode = DebugMode.Off;
    private Shader debugShader;
    private Camera appliedCamera;      // camera onde o replacement esta ativo
    private bool wireframeActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapIfMissing()
    {
        if (Object.FindObjectOfType<DebugViewController>() != null)
        {
            return;
        }

        GameObject go = new GameObject("CG_DebugViewController");
        go.AddComponent<DebugViewController>();
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
        debugShader = Shader.Find("CG/DebugViews");
    }

    private void OnEnable()
    {
        Camera.onPreRender += HandlePreRender;
        Camera.onPostRender += HandlePostRender;
    }

    private void OnDisable()
    {
        Camera.onPreRender -= HandlePreRender;
        Camera.onPostRender -= HandlePostRender;
    }

    private void Update()
    {
        if (Input.GetKeyDown(cycleKey))
        {
            int next = ((int)currentMode + 1) % modeNames.Length;
            ApplyMode((DebugMode)next);
        }
    }

    // Reaplica na fase nova (a camera e recriada a cada nivel).
    public static void NotifyLevelSpawned()
    {
        if (instance != null)
        {
            instance.ApplyMode(instance.currentMode);
        }
    }

    private void ApplyMode(DebugMode mode)
    {
        currentMode = mode;
        wireframeActive = (mode == DebugMode.Wireframe);

        Camera cam = Camera.main;

        // Sempre limpa o replacement anterior antes de reconfigurar.
        if (appliedCamera != null)
        {
            appliedCamera.ResetReplacementShader();
            appliedCamera = null;
        }

        if (mode == DebugMode.Normals || mode == DebugMode.UVs || mode == DebugMode.Depth)
        {
            if (cam != null && debugShader != null)
            {
                int shaderMode = mode == DebugMode.Normals ? 1
                               : mode == DebugMode.UVs ? 2 : 3;
                Shader.SetGlobalInt("_CGDebugMode", shaderMode);
                Shader.SetGlobalFloat("_CGDepthScale", depthScale);
                cam.SetReplacementShader(debugShader, "");
                appliedCamera = cam;
            }
        }
    }

    // GL.wireframe so vale entre pre e post render da camera principal.
    private void HandlePreRender(Camera cam)
    {
        if (wireframeActive && cam == Camera.main)
        {
            GL.wireframe = true;
        }
    }

    private void HandlePostRender(Camera cam)
    {
        if (GL.wireframe)
        {
            GL.wireframe = false;
        }
    }

    // Desenha o conteudo da aba "Debug" dentro do menu unificado (CGMenuHUD).
    public void DrawTabContent()
    {
        GUILayout.Label("V cicla o modo de depuracao");
        GUILayout.Space(6f);

        for (int i = 0; i < modeNames.Length; i++)
        {
            bool isCurrent = (i == (int)currentMode);
            string label = (isCurrent ? "> " : "   ") + modeNames[i];

            GUI.enabled = !isCurrent;
            if (GUILayout.Button(label, GUILayout.Height(26f)))
            {
                ApplyMode((DebugMode)i);
            }
            GUI.enabled = true;
        }

        if (currentMode == DebugMode.Depth)
        {
            GUILayout.Space(6f);
            GUILayout.Label("Escala do depth: " + depthScale.ToString("0.000"));
            float newScale = GUILayout.HorizontalSlider(depthScale, 0.005f, 0.15f);
            if (!Mathf.Approximately(newScale, depthScale))
            {
                depthScale = newScale;
                Shader.SetGlobalFloat("_CGDepthScale", depthScale);
            }
        }

        GUILayout.Space(8f);
        GUILayout.Label("<b>Explicacao:</b>");
        GUILayout.Label(modeDescriptions[(int)currentMode]);
    }
}
