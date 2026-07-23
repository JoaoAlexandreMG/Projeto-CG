using UnityEngine;

// Zoom da camera com a roda do mouse, para inspecionar de perto o material /
// textura da bola nos modulos didaticos. Ajusta o Field of View da camera
// principal (efeito de "aproximar" sem mover a camera do trilho de orbita).
// Cria-se sozinho em runtime.
public class CGCameraZoom : MonoBehaviour
{
    private static CGCameraZoom instance;

    [SerializeField] private float minFov = 15f;   // mais perto (mais zoom)
    [SerializeField] private float maxFov = 60f;    // mais longe (visao ampla)
    [SerializeField] private float zoomSpeed = 8f;

    private Camera cam;
    private float defaultFov;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapIfMissing()
    {
        if (Object.FindObjectOfType<CGCameraZoom>() != null)
        {
            return;
        }

        GameObject go = new GameObject("CG_CameraZoom");
        go.AddComponent<CGCameraZoom>();
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
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                return;
            }
            defaultFov = cam.fieldOfView;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (!Mathf.Approximately(scroll, 0f))
        {
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - scroll * zoomSpeed, minFov, maxFov);
        }

        // Botao do meio do mouse reseta o zoom.
        if (Input.GetMouseButtonDown(2))
        {
            cam.fieldOfView = defaultFov;
        }
    }
}
