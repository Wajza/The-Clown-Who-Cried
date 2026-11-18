using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Makes sure EXACTLY ONE AudioListener is active in every scene, so the game is never silent
/// ("There are no audio listeners in the scene") and never has two.
///
/// Every scene's Main Camera already gets an AudioListener from the Combat setup tool. This is the
/// safety net for scenes made later or rebuilt by a setup tool (e.g. the test scenes): after each
/// scene load it adds one to the main camera if none is active, and switches off extras.
/// Added by the teammate (player/combat side).
/// </summary>
public static class AudioListenerGuard
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Ensure(); // The scene Play started in.
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Ensure();

    public static void Ensure()
    {
        AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude);
        int enabledCount = 0;
        foreach (AudioListener listener in listeners)
        {
            if (!listener.enabled)
                continue;
            enabledCount++;
            if (enabledCount > 1)
                listener.enabled = false; // Only one may listen.
        }
        if (enabledCount > 0)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            cam = Object.FindAnyObjectByType<Camera>();
        if (cam != null)
            cam.gameObject.AddComponent<AudioListener>();
    }
}
