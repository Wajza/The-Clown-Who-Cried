using UnityEngine;

/// <summary>
/// Keeps the game picture at exactly 16:9 on any screen: on a wider or taller window the main
/// camera draws in the middle and black bars fill the rest. So every room shows the same amount of
/// the world everywhere, and no room ever shows past the edge of its background art.
///
/// Starts by itself when the game starts (no setup). One small camera behind everything only
/// clears the bars to black. Overlay UI (HUD, menus) still uses the whole screen.
/// </summary>
public class AspectLock : MonoBehaviour
{
    private const float Target = 16f / 9f;

    private Camera _bars;
    private int _lastWidth, _lastHeight;
    private Camera _lastCamera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        var go = new GameObject("AspectLock");
        DontDestroyOnLoad(go);
        go.AddComponent<AspectLock>();
    }

    private void Awake()
    {
        _bars = gameObject.AddComponent<Camera>();
        _bars.clearFlags = CameraClearFlags.SolidColor;
        _bars.backgroundColor = Color.black;
        _bars.cullingMask = 0;       // Draws nothing, only clears.
        _bars.depth = -100f;          // Before every other camera.
        _bars.orthographic = true;
        _bars.useOcclusionCulling = false;
    }

    private void LateUpdate()
    {
        Camera main = Camera.main;
        if (main == null)
            return;
        if (main == _lastCamera && Screen.width == _lastWidth && Screen.height == _lastHeight)
            return;
        _lastCamera = main;
        _lastWidth = Screen.width;
        _lastHeight = Screen.height;

        float aspect = Screen.width / (float)Mathf.Max(1, Screen.height);
        Rect r = new Rect(0f, 0f, 1f, 1f);
        if (aspect > Target + 0.001f)
        {
            float w = Target / aspect; // Bars left and right.
            r = new Rect((1f - w) * 0.5f, 0f, w, 1f);
        }
        else if (aspect < Target - 0.001f)
        {
            float h = aspect / Target; // Bars top and bottom.
            r = new Rect(0f, (1f - h) * 0.5f, 1f, h);
        }
        main.rect = r;
    }
}
