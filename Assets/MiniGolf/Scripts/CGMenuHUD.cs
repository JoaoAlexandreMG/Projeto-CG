using UnityEngine;

// Menu unificado dos modulos didaticos de Computacao Grafica.
// Reune todas as ferramentas em UM painel com ABAS, em vez de um menu (e uma
// tecla) para cada. Cada aba delega o desenho ao controller responsavel.
//
// Controles: TAB liga/desliga o painel; clique nas abas para trocar de secao.
// Cria-se sozinho em runtime; nao precisa configurar nada na cena.
public class CGMenuHUD : MonoBehaviour
{
    private static CGMenuHUD instance;

    private enum Tab { Lighting, Shading, Debug }
    private static readonly string[] tabNames = { "Iluminacao", "Shading", "Debug" };

    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] private bool visible = true;

    private Tab currentTab = Tab.Lighting;

    private LightingPresetController lighting;
    private ShadingModelController shading;
    private DebugViewController debug;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapIfMissing()
    {
        if (Object.FindObjectOfType<CGMenuHUD>() != null)
        {
            return;
        }

        GameObject go = new GameObject("CG_MenuHUD");
        go.AddComponent<CGMenuHUD>();
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
        if (Input.GetKeyDown(toggleKey))
        {
            visible = !visible;
        }
    }

    private void EnsureControllers()
    {
        if (lighting == null)
        {
            lighting = Object.FindObjectOfType<LightingPresetController>();
        }

        if (shading == null)
        {
            shading = Object.FindObjectOfType<ShadingModelController>();
        }

        if (debug == null)
        {
            debug = Object.FindObjectOfType<DebugViewController>();
        }
    }

    private void OnGUI()
    {
        if (!visible)
        {
            // Mesmo escondido, mostra uma dica curta de como reabrir.
            GUI.Label(new Rect(12f, 12f, 300f, 24f), "TAB: abrir menu de Computacao Grafica");
            CGMenuGuard.RegisterPanel("hud", new Rect(0, 0, 0, 0));
            return;
        }

        GUI.skin.label.richText = true;
        GUI.skin.label.wordWrap = true;

        EnsureControllers();

        const float panelWidth = 360f;
        const float panelHeight = 470f;
        Rect panelRect = new Rect(12f, 12f, panelWidth, panelHeight);

        // Registra a area para o InputManager nao mover camera/bola ao clicar aqui.
        CGMenuGuard.RegisterPanel("hud", panelRect);
        GUILayout.BeginArea(panelRect, GUI.skin.box);

        GUILayout.Label("<b>COMPUTACAO GRAFICA — Ferramentas</b>");
        GUILayout.Label("TAB esconde | Scroll = zoom da camera");
        GUILayout.Space(4f);

        // Barra de abas.
        int selected = GUILayout.Toolbar((int)currentTab, tabNames, GUILayout.Height(28f));
        currentTab = (Tab)selected;
        GUILayout.Space(8f);

        switch (currentTab)
        {
            case Tab.Lighting:
                if (lighting != null)
                {
                    lighting.DrawTabContent();
                }
                else
                {
                    GUILayout.Label("Controller de iluminacao nao encontrado.");
                }
                break;

            case Tab.Shading:
                if (shading != null)
                {
                    shading.DrawTabContent();
                }
                else
                {
                    GUILayout.Label("Controller de shading nao encontrado.");
                }
                break;

            case Tab.Debug:
                if (debug != null)
                {
                    debug.DrawTabContent();
                }
                else
                {
                    GUILayout.Label("Controller de debug nao encontrado.");
                }
                break;
        }

        GUILayout.EndArea();
    }
}
