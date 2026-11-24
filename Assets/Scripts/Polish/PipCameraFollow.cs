using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The camera always follows Pip, wherever she goes, in every level:
///   - no camera limits (the level / room bounds and CameraZones no longer stop the camera),
///   - no pixel-perfect camera (it was made for the old 16-pixel placeholder art: with the real art it
///     overrode the zoom and made the camera jitter),
///   - a tight, smooth follow: Pip stays in the middle of the screen, a little below centre.
///
/// Runs by itself on every scene load (no setup). Camera zoom stays whatever the level's PipCamera
/// says (7 in every level).
/// </summary>
public static class PipCameraFollow
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Apply();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply();

    private static void Apply()
    {
        foreach (Behaviour b in Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string type = b.GetType().Name;
            if (type == "PixelPerfectCamera" || type == "CinemachinePixelPerfect")
                b.enabled = false;
        }

        // Never hold the camera back from Pip, except in a fixed-camera room (the boss arena).
        bool fixedRoom = Object.FindAnyObjectByType<FixedCameraRoom>() != null ||
                         Object.FindAnyObjectByType<KeepCameraInBounds>() != null;
        foreach (var confiner in Object.FindObjectsByType<CinemachineConfiner2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            confiner.enabled = fixedRoom;

        foreach (var composer in Object.FindObjectsByType<CinemachinePositionComposer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            composer.Damping = new Vector3(0.12f, 0.2f, 0f); // Smooth, but never far behind.
            var comp = composer.Composition;
            comp.ScreenPosition = Vector2.zero;
            comp.DeadZone.Enabled = false;
            comp.HardLimits.Enabled = true; // However fast she moves, Pip never leaves the middle of the screen.
            comp.HardLimits.Size = new Vector2(0.3f, 0.4f);
            comp.HardLimits.Offset = Vector2.zero;
            composer.Composition = comp;
            var look = composer.Lookahead;
            look.Enabled = false;
            composer.Lookahead = look;
        }
    }
}
