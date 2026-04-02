#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class GameViewFullscreen : EditorWindow
{
    [MenuItem("Tools/Game View Fullscreen %#F11")] // Ctrl+Shift+F11
    public static void ToggleGameViewFullscreen()
    {
        EditorWindow gameView = GetGameView();
        if (gameView != null)
        {
            gameView.maximized = !gameView.maximized;
            Debug.Log($"Game View maximized: {gameView.maximized}");
        }
    }
    
    private static EditorWindow GetGameView()
    {
        System.Type type = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        return EditorWindow.GetWindow(type);
    }
}
#endif
