using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Builds the entire game at runtime — camera, event system, canvas and the
/// GameController — so any scene (even a brand-new empty one) can run the
/// game by simply pressing Play.
/// </summary>
public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindFirstObjectByType<GameController>() != null) return;

        // camera (screen-space-overlay UI doesn't need one, but an empty
        // scene without a camera logs warnings and renders black behind UI)
        if (Camera.main == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = GameConfig.S.backgroundColor;
            cam.orthographic = true;
            camGo.AddComponent<AudioListener>();
        }

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // web3 bridge — must exist (and keep its exact name) before the
        // controller builds any wallet UI; JS finds it via SendMessage
        var web3Go = new GameObject("Web3Bridge");
        web3Go.AddComponent<Web3Bridge>();

        var controllerGo = new GameObject("GameController");
        var controller = controllerGo.AddComponent<GameController>();
        controller.Setup(canvas);
    }
}
