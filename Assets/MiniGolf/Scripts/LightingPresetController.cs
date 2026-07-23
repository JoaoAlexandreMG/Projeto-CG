using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class LightingPresetController : MonoBehaviour
{
    private static LightingPresetController instance;

    [System.Serializable]
    public class LightingPreset
    {
        public string presetName = "Preset";
        [TextArea(2, 4)] public string description = "";
        public Color directionalColor = Color.white;
        [Range(0f, 2.5f)] public float directionalIntensity = 1f;
        public Vector3 directionalEuler = new Vector3(50f, -30f, 0f);

        // Sombras do key light (sol). Shadow map = textura de profundidade vista pela luz.
        public LightShadows directionalShadows = LightShadows.Soft;   // None / Hard / Soft
        [Range(0f, 1f)] public float shadowStrength = 1f;             // 1 = preta, 0 = invisível
        public LightShadowResolution shadowResolution = LightShadowResolution.FromQualitySettings;

        public AmbientMode ambientMode = AmbientMode.Trilight;
        [Range(0f, 2f)] public float ambientIntensity = 1f;
        public Color ambientSkyColor = new Color(0.3f, 0.35f, 0.4f);
        public Color ambientEquatorColor = new Color(0.2f, 0.2f, 0.2f);
        public Color ambientGroundColor = new Color(0.1f, 0.1f, 0.1f);

        public bool enableFog = false;
        public Color fogColor = new Color(0.7f, 0.75f, 0.8f);
        [Range(0.001f, 0.1f)] public float fogDensity = 0.015f;

        public bool useFillLight = false;
        public Color fillColor = new Color(0.45f, 0.5f, 0.65f);
        [Range(0f, 1.5f)] public float fillIntensity = 0.35f;
        public Vector3 fillEuler = new Vector3(20f, 140f, 0f);

        // Point light: luz tipo lâmpada. Tem posição, emite para todos os lados,
        // enfraquece com a distância (atenuação controlada por 'range').
        public bool usePointLight = false;
        public Color pointColor = new Color(1f, 0.85f, 0.6f);
        [Range(0f, 8f)] public float pointIntensity = 2f;
        [Range(1f, 30f)] public float pointRange = 10f;
        public Vector3 pointPosition = new Vector3(0f, 3f, 0f);

        // Spot light: holofote. Combina posição (point) + direção (directional) + cone.
        // 'spotAngle' controla a largura do cone; 'range' a atenuação ao longo do feixe.
        public bool useSpotLight = false;
        public Color spotColor = new Color(1f, 0.95f, 0.85f);
        [Range(0f, 12f)] public float spotIntensity = 5f;
        [Range(1f, 40f)] public float spotRange = 18f;
        [Range(5f, 120f)] public float spotAngle = 40f;
        public Vector3 spotPosition = new Vector3(0f, 8f, 0f);
        public Vector3 spotEuler = new Vector3(90f, 0f, 0f);
    }

    [Header("References")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private Light fillLight;
    [SerializeField] private Light pointLight;
    [SerializeField] private Light spotLight;

    [Header("Keyboard")]
    [SerializeField] private KeyCode beforeAfterKey = KeyCode.B;
    [SerializeField] private KeyCode nextPresetKey = KeyCode.N;

    [Header("Presets")]
    [SerializeField] private int defaultPresetIndex = 0;
    [SerializeField] private LightingPreset[] presets = new LightingPreset[]
    {
        new LightingPreset
        {
            presetName = "Baseline (Flat)",
            description = "Ambiente FLAT: uma cor unica ilumina tudo igual. Sem profundidade, tudo parece chapado. Base de comparacao.",
            directionalColor = Color.white,
            directionalIntensity = 0.9f,
            directionalEuler = new Vector3(50f, -30f, 0f),
            ambientMode = AmbientMode.Flat,
            ambientIntensity = 1.2f,
            ambientSkyColor = new Color(0.35f, 0.35f, 0.35f),
            ambientEquatorColor = new Color(0.35f, 0.35f, 0.35f),
            ambientGroundColor = new Color(0.35f, 0.35f, 0.35f),
            enableFog = false,
            useFillLight = false
        },
        new LightingPreset
        {
            presetName = "Depth (Directional + Trilight)",
            description = "Key light (sol) + ambiente TRILIGHT (ceu/equador/chao). Aparece volume e sombra suave. A base 'de verdade'.",
            directionalColor = new Color(1f, 0.96f, 0.88f),
            directionalIntensity = 1.25f,
            directionalEuler = new Vector3(42f, -52f, 0f),
            ambientMode = AmbientMode.Trilight,
            ambientIntensity = 1f,
            ambientSkyColor = new Color(0.45f, 0.56f, 0.7f),
            ambientEquatorColor = new Color(0.26f, 0.28f, 0.32f),
            ambientGroundColor = new Color(0.13f, 0.12f, 0.11f),
            enableFog = false,
            useFillLight = false
        },
        new LightingPreset
        {
            presetName = "Dramatic (Key + Fill + Fog)",
            description = "Iluminacao de 3 pontos: KEY forte + FILL fraco no lado oposto (suaviza sombra) + FOG. Tecnica de cinema.",
            directionalColor = new Color(1f, 0.9f, 0.78f),
            directionalIntensity = 1.45f,
            directionalEuler = new Vector3(22f, -72f, 0f),
            ambientMode = AmbientMode.Trilight,
            ambientIntensity = 0.75f,
            ambientSkyColor = new Color(0.22f, 0.25f, 0.32f),
            ambientEquatorColor = new Color(0.12f, 0.12f, 0.14f),
            ambientGroundColor = new Color(0.05f, 0.05f, 0.05f),
            enableFog = true,
            fogColor = new Color(0.5f, 0.55f, 0.62f),
            fogDensity = 0.02f,
            useFillLight = true,
            fillColor = new Color(0.45f, 0.56f, 0.75f),
            fillIntensity = 0.35f,
            fillEuler = new Vector3(18f, 135f, 0f)
        },
        new LightingPreset
        {
            presetName = "Sunset (Warm/Cool Contrast)",
            description = "Contraste de temperatura: luz QUENTE (laranja) do sol vs ambiente FRIO (azul). Cria clima de por-do-sol.",
            directionalColor = new Color(1f, 0.72f, 0.48f),
            directionalIntensity = 1.55f,
            directionalEuler = new Vector3(14f, -95f, 0f),
            ambientMode = AmbientMode.Trilight,
            ambientIntensity = 0.8f,
            ambientSkyColor = new Color(0.2f, 0.26f, 0.45f),
            ambientEquatorColor = new Color(0.2f, 0.16f, 0.14f),
            ambientGroundColor = new Color(0.08f, 0.06f, 0.05f),
            enableFog = true,
            fogColor = new Color(0.72f, 0.52f, 0.44f),
            fogDensity = 0.012f,
            useFillLight = true,
            fillColor = new Color(0.42f, 0.5f, 0.75f),
            fillIntensity = 0.28f,
            fillEuler = new Vector3(30f, 110f, 0f)
        },
        new LightingPreset
        {
            // DEMO point light: cena noturna escura + uma "lâmpada" quente.
            // Objetivo didático: ver a atenuação (perto brilha, longe apaga).
            presetName = "Night (Point Light Lamp)",
            description = "POINT LIGHT: luz tipo lampada com posicao. Emite pra todo lado e ENFRAQUECE com a distancia (atenuacao). Perto brilha, longe apaga.",
            directionalColor = new Color(0.4f, 0.45f, 0.6f),
            directionalIntensity = 0.15f,
            directionalEuler = new Vector3(60f, -20f, 0f),
            ambientMode = AmbientMode.Flat,
            ambientIntensity = 0.3f,
            ambientSkyColor = new Color(0.06f, 0.07f, 0.12f),
            ambientEquatorColor = new Color(0.06f, 0.07f, 0.12f),
            ambientGroundColor = new Color(0.06f, 0.07f, 0.12f),
            enableFog = true,
            fogColor = new Color(0.05f, 0.06f, 0.1f),
            fogDensity = 0.03f,
            useFillLight = false,
            usePointLight = true,
            pointColor = new Color(1f, 0.8f, 0.5f),
            pointIntensity = 3.5f,
            pointRange = 12f,
            pointPosition = new Vector3(0f, 3.5f, 0f)
        },
        new LightingPreset
        {
            // DEMO spot light: palco escuro + holofote apontando pra baixo no centro.
            // Objetivo didático: ver o CONE (círculo de luz) e mexer no ângulo.
            presetName = "Stage (Spot Light)",
            description = "SPOT LIGHT: holofote = posicao + direcao + CONE. spotAngle controla a largura do feixe. Junta point e directional.",
            directionalColor = new Color(0.35f, 0.4f, 0.55f),
            directionalIntensity = 0.1f,
            directionalEuler = new Vector3(60f, -20f, 0f),
            ambientMode = AmbientMode.Flat,
            ambientIntensity = 0.25f,
            ambientSkyColor = new Color(0.05f, 0.06f, 0.1f),
            ambientEquatorColor = new Color(0.05f, 0.06f, 0.1f),
            ambientGroundColor = new Color(0.05f, 0.06f, 0.1f),
            enableFog = false,
            useFillLight = false,
            usePointLight = false,
            useSpotLight = true,
            spotColor = new Color(1f, 0.97f, 0.9f),
            spotIntensity = 6f,
            spotRange = 20f,
            spotAngle = 45f,
            spotPosition = new Vector3(0f, 9f, 0f),
            spotEuler = new Vector3(90f, 0f, 0f)
        },
        new LightingPreset
        {
            // DEMO sombra DURA: sol baixo (sombras longas) + Hard shadows + resolução baixa.
            // Compare com preset 2 (Depth), que usa Soft. Troque directionalShadows
            // entre Hard/Soft/None no Inspector para ver a diferença ao vivo.
            presetName = "Shadow Study (Hard, Low Res)",
            description = "SOMBRA DURA + resolucao baixa. Borda serrilhada e comprida (sol baixo). Compare com preset 2 (Soft) pra ver a diferenca.",
            directionalColor = new Color(1f, 0.95f, 0.85f),
            directionalIntensity = 1.4f,
            directionalEuler = new Vector3(18f, -40f, 0f),   // sol baixo = sombra comprida
            directionalShadows = LightShadows.Hard,
            shadowStrength = 1f,
            shadowResolution = LightShadowResolution.Low,
            ambientMode = AmbientMode.Trilight,
            ambientIntensity = 0.7f,
            ambientSkyColor = new Color(0.4f, 0.5f, 0.65f),
            ambientEquatorColor = new Color(0.22f, 0.24f, 0.28f),
            ambientGroundColor = new Color(0.1f, 0.1f, 0.1f),
            enableFog = false,
            useFillLight = false,
            usePointLight = false,
            useSpotLight = false
        }
    };

    private LightingPreset runtimeBeforeState;
    private int currentPresetIndex;
    private bool showingBefore;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapIfMissing()
    {
        LightingPresetController existing = Object.FindObjectOfType<LightingPresetController>();
        if (existing != null)
        {
            return;
        }

        GameObject bootstrapObject = new GameObject("CG_LightingController");
        bootstrapObject.AddComponent<LightingPresetController>();
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

        RefreshSceneReferences();

        if (presets == null || presets.Length == 0)
        {
            return;
        }

        currentPresetIndex = Mathf.Clamp(defaultPresetIndex, 0, presets.Length - 1);
        runtimeBeforeState = CaptureCurrentState("Before");
        ApplyPreset(currentPresetIndex);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ApplyPresetFromKey(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ApplyPresetFromKey(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ApplyPresetFromKey(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ApplyPresetFromKey(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) ApplyPresetFromKey(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) ApplyPresetFromKey(5);
        if (Input.GetKeyDown(KeyCode.Alpha7)) ApplyPresetFromKey(6);

        if (Input.GetKeyDown(nextPresetKey))
        {
            int next = (currentPresetIndex + 1) % presets.Length;
            ApplyPreset(next);
        }

        if (Input.GetKeyDown(beforeAfterKey))
        {
            ToggleBeforeAfter();
        }
    }

    // Desenha o conteudo da aba "Iluminacao" dentro do menu unificado (CGMenuHUD).
    // Nao abre area propria: o HUD ja fez BeginArea.
    public void DrawTabContent()
    {
        if (presets == null || presets.Length == 0)
        {
            GUILayout.Label("Nenhum preset configurado.");
            return;
        }

        GUILayout.Label("Teclas: 1-" + presets.Length + " escolher | N proximo | B antes/depois");
        GUILayout.Space(6f);

        for (int i = 0; i < presets.Length; i++)
        {
            bool isCurrent = (i == currentPresetIndex && !showingBefore);
            string label = (isCurrent ? "> " : "   ") + (i + 1) + ". " + presets[i].presetName;

            GUI.enabled = !isCurrent;
            if (GUILayout.Button(label, GUILayout.Height(26f)))
            {
                ApplyPreset(i);
            }
            GUI.enabled = true;
        }

        GUILayout.Space(8f);

        // Explicacao do preset ativo.
        string desc = showingBefore
            ? "ANTES: estado original da cena (toggle com B)."
            : presets[currentPresetIndex].description;

        GUILayout.Label("<b>Explicacao:</b>");
        GUILayout.Label(string.IsNullOrEmpty(desc) ? "(sem descricao)" : desc);
    }

    // Chamado quando uma nova fase e gerada (bola/luz recriadas em outra cena).
    // Reacquire as referencias e reaplica o preset atual para nao "resetar".
    public static void NotifyLevelSpawned()
    {
        if (instance != null)
        {
            instance.ReapplyForCurrentScene();
        }
    }

    private void ReapplyForCurrentScene()
    {
        // Referencias antigas (sol/fill/point/spot) podem ter sido destruidas na
        // troca de cena; RefreshSceneReferences reacquire ou recria o que faltar.
        RefreshSceneReferences();

        if (presets == null || presets.Length == 0 || showingBefore)
        {
            return;
        }

        currentPresetIndex = Mathf.Clamp(currentPresetIndex, 0, presets.Length - 1);
        ApplyLighting(presets[currentPresetIndex]);
    }

    private void ApplyPresetFromKey(int presetIndex)
    {
        if (presetIndex < presets.Length)
        {
            ApplyPreset(presetIndex);
        }
    }

    public void ApplyPreset(int presetIndex)
    {
        if (presets == null || presets.Length == 0)
        {
            return;
        }

        currentPresetIndex = Mathf.Clamp(presetIndex, 0, presets.Length - 1);
        showingBefore = false;
        ApplyLighting(presets[currentPresetIndex]);
    }

    public void ToggleBeforeAfter()
    {
        showingBefore = !showingBefore;
        if (showingBefore)
        {
            ApplyLighting(runtimeBeforeState);
        }
        else
        {
            ApplyLighting(presets[currentPresetIndex]);
        }
    }

    private void ApplyLighting(LightingPreset preset)
    {
        if (directionalLight != null)
        {
            directionalLight.color = preset.directionalColor;
            directionalLight.intensity = preset.directionalIntensity;
            directionalLight.transform.rotation = Quaternion.Euler(preset.directionalEuler);
            directionalLight.shadows = preset.directionalShadows;
            directionalLight.shadowStrength = preset.shadowStrength;
            directionalLight.shadowResolution = preset.shadowResolution;
        }

        RenderSettings.ambientMode = preset.ambientMode;
        RenderSettings.ambientIntensity = preset.ambientIntensity;
        RenderSettings.ambientSkyColor = preset.ambientSkyColor;
        RenderSettings.ambientEquatorColor = preset.ambientEquatorColor;
        RenderSettings.ambientGroundColor = preset.ambientGroundColor;

        RenderSettings.fog = preset.enableFog;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = preset.fogColor;
        RenderSettings.fogDensity = preset.fogDensity;

        if (fillLight != null)
        {
            fillLight.gameObject.SetActive(preset.useFillLight);
            fillLight.color = preset.fillColor;
            fillLight.intensity = preset.fillIntensity;
            fillLight.transform.rotation = Quaternion.Euler(preset.fillEuler);
        }

        if (pointLight != null)
        {
            pointLight.gameObject.SetActive(preset.usePointLight);
            pointLight.color = preset.pointColor;
            pointLight.intensity = preset.pointIntensity;
            pointLight.range = preset.pointRange;
            // Point light usa POSIÇÃO, não rotação (emite para todos os lados).
            pointLight.transform.position = preset.pointPosition;
        }

        if (spotLight != null)
        {
            spotLight.gameObject.SetActive(preset.useSpotLight);
            spotLight.color = preset.spotColor;
            spotLight.intensity = preset.spotIntensity;
            spotLight.range = preset.spotRange;
            spotLight.spotAngle = preset.spotAngle;
            // Spot usa POSIÇÃO + ROTAÇÃO: de onde sai e para onde o cone aponta.
            spotLight.transform.position = preset.spotPosition;
            spotLight.transform.rotation = Quaternion.Euler(preset.spotEuler);
        }
    }

    private LightingPreset CaptureCurrentState(string name)
    {
        LightingPreset snapshot = new LightingPreset();
        snapshot.presetName = name;

        if (directionalLight != null)
        {
            snapshot.directionalColor = directionalLight.color;
            snapshot.directionalIntensity = directionalLight.intensity;
            snapshot.directionalEuler = directionalLight.transform.eulerAngles;
            snapshot.directionalShadows = directionalLight.shadows;
            snapshot.shadowStrength = directionalLight.shadowStrength;
            snapshot.shadowResolution = directionalLight.shadowResolution;
        }

        snapshot.ambientMode = RenderSettings.ambientMode;
        snapshot.ambientIntensity = RenderSettings.ambientIntensity;
        snapshot.ambientSkyColor = RenderSettings.ambientSkyColor;
        snapshot.ambientEquatorColor = RenderSettings.ambientEquatorColor;
        snapshot.ambientGroundColor = RenderSettings.ambientGroundColor;
        snapshot.enableFog = RenderSettings.fog;
        snapshot.fogColor = RenderSettings.fogColor;
        snapshot.fogDensity = RenderSettings.fogDensity;

        if (fillLight != null)
        {
            snapshot.useFillLight = fillLight.gameObject.activeSelf;
            snapshot.fillColor = fillLight.color;
            snapshot.fillIntensity = fillLight.intensity;
            snapshot.fillEuler = fillLight.transform.eulerAngles;
        }

        if (pointLight != null)
        {
            snapshot.usePointLight = pointLight.gameObject.activeSelf;
            snapshot.pointColor = pointLight.color;
            snapshot.pointIntensity = pointLight.intensity;
            snapshot.pointRange = pointLight.range;
            snapshot.pointPosition = pointLight.transform.position;
        }

        if (spotLight != null)
        {
            snapshot.useSpotLight = spotLight.gameObject.activeSelf;
            snapshot.spotColor = spotLight.color;
            snapshot.spotIntensity = spotLight.intensity;
            snapshot.spotRange = spotLight.range;
            snapshot.spotAngle = spotLight.spotAngle;
            snapshot.spotPosition = spotLight.transform.position;
            snapshot.spotEuler = spotLight.transform.eulerAngles;
        }

        return snapshot;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshSceneReferences();
        runtimeBeforeState = CaptureCurrentState("Before");

        if (showingBefore)
        {
            ApplyLighting(runtimeBeforeState);
            return;
        }

        if (presets != null && presets.Length > 0)
        {
            currentPresetIndex = Mathf.Clamp(currentPresetIndex, 0, presets.Length - 1);
            ApplyLighting(presets[currentPresetIndex]);
        }
    }

    private void RefreshSceneReferences()
    {
        if (directionalLight == null)
        {
            directionalLight = RenderSettings.sun;
        }

        EnsureFillLight();
        EnsurePointLight();
        EnsureSpotLight();
    }

    private void EnsureFillLight()
    {
        if (fillLight != null)
        {
            return;
        }

        GameObject existing = GameObject.Find("CG_FillLight");
        if (existing != null)
        {
            fillLight = existing.GetComponent<Light>();
        }

        if (fillLight != null)
        {
            return;
        }

        GameObject fillObject = new GameObject("CG_FillLight");
        fillLight = fillObject.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.shadows = LightShadows.None;
        fillObject.SetActive(false);
    }

    private void EnsurePointLight()
    {
        if (pointLight != null)
        {
            return;
        }

        GameObject existing = GameObject.Find("CG_PointLight");
        if (existing != null)
        {
            pointLight = existing.GetComponent<Light>();
        }

        if (pointLight != null)
        {
            return;
        }

        GameObject pointObject = new GameObject("CG_PointLight");
        pointLight = pointObject.AddComponent<Light>();
        pointLight.type = LightType.Point;
        // Point light pode projetar sombra suave; deixamos ligada para o efeito de "poste".
        pointLight.shadows = LightShadows.Soft;
        pointObject.SetActive(false);
    }

    private void EnsureSpotLight()
    {
        if (spotLight != null)
        {
            return;
        }

        GameObject existing = GameObject.Find("CG_SpotLight");
        if (existing != null)
        {
            spotLight = existing.GetComponent<Light>();
        }

        if (spotLight != null)
        {
            return;
        }

        GameObject spotObject = new GameObject("CG_SpotLight");
        spotLight = spotObject.AddComponent<Light>();
        spotLight.type = LightType.Spot;
        spotLight.shadows = LightShadows.Soft;
        spotObject.SetActive(false);
    }
}