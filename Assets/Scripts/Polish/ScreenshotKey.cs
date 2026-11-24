#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Editor-only helper for the checkpoint evidence: press F12 while playing to save a screenshot
/// of the Game view to docs/phase2/images (next to the Assets folder). Each file is named after
/// the scene and the time, e.g. "ChortleArena_20261125_143012.png".
///
/// Starts by itself in Play Mode (no setup). Not included in builds.
/// </summary>
public class ScreenshotKey : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        var go = new GameObject("ScreenshotKey (editor only)");
        go.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(go);
        go.AddComponent<ScreenshotKey>();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.f12Key.wasPressedThisFrame)
            return;

        string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "docs", "phase2", "images");
        Directory.CreateDirectory(folder);
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string file = Path.Combine(folder, $"{scene}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        ScreenCapture.CaptureScreenshot(file);
        Debug.Log($"Screenshot saved: {file}");
    }
}
#endif
