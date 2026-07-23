using System.Collections.Generic;
using UnityEngine;

// Utilitario dos menus didaticos (iluminacao e shading).
// Os menus IMGUI registram aqui a area que ocupam na tela. O InputManager
// consulta este guard para NAO mover a camera / bola quando o clique acontece
// em cima de um menu (ex.: arrastar um slider).
public static class CGMenuGuard
{
    // Retangulos em espaco de GUI (origem no canto superior esquerdo, Y para baixo).
    private static readonly Dictionary<string, Rect> panelRects = new Dictionary<string, Rect>();

    public static void RegisterPanel(string id, Rect guiRect)
    {
        panelRects[id] = guiRect;
    }

    // True se o ponteiro estiver sobre algum menu registrado.
    public static bool PointerIsOverMenu()
    {
        // Input.mousePosition tem origem no canto inferior esquerdo (Y para cima).
        // Os rects de GUI usam Y para baixo, entao convertemos a coordenada.
        Vector2 guiPoint = new Vector2(
            Input.mousePosition.x,
            Screen.height - Input.mousePosition.y);

        foreach (Rect rect in panelRects.Values)
        {
            if (rect.Contains(guiPoint))
            {
                return true;
            }
        }

        return false;
    }
}
