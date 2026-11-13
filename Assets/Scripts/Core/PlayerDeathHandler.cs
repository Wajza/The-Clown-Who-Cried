using System.Collections;
using UnityEngine;

/// <summary>
/// Brings Pip back after she runs out of Smile Stars.
///
/// My teammate's PlayerHealth fires OnDeath at 0 stars, switches off Pip's input, and leaves the
/// rest to "the save system" (see their comment on OnDeath). This script is that part:
///
///   1. Wait a moment so the player sees what happened.
///   2. Fade to black.
///   3. Teleport Pip to her respawn point: the SpawnPoint/Checkpoint saved in GameManager.Save,
///      or, if there isn't one, the place she entered the scene.
///   4. Call PlayerHealth.Revive() (full stars, input back on).
///   5. Fade back in, then give control back.
///
/// Setup: on the GameSystems prefab. It finds the new Pip by itself after every scene load.
/// </summary>
public class PlayerDeathHandler : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector fields

    [SerializeField, Min(0f), Tooltip("Seconds to wait after dying before the screen fades.")]
    private float delayBeforeFade = 1f;

    [SerializeField, Min(0f), Tooltip("Seconds to fade to black.")]
    private float fadeOutTime = 0.5f;

    [SerializeField, Min(0f), Tooltip("Seconds the screen stays black after moving Pip (lets the camera catch up).")]
    private float blackHoldTime = 0.3f;

    [SerializeField, Min(0f), Tooltip("Seconds to fade back in.")]
    private float fadeInTime = 0.5f;

    // ------------------------------------------------------------------ Private state

    private PlayerHealth _health;        // The current scene's Pip.
    private Vector2 _sceneEntryPosition; // Fallback respawn spot when no spawn point is saved.
    private bool _respawning;

    // ------------------------------------------------------------------ Unity lifecycle

    private void OnEnable() => GameEvents.SceneLoaded += OnSceneLoaded;

    private void OnDisable()
    {
        GameEvents.SceneLoaded -= OnSceneLoaded;
        Unhook();
    }

    // ------------------------------------------------------------------ Event handlers

    private void OnSceneLoaded(string sceneName, string spawnPointId)
    {
        // If a scene change happened in the middle of a respawn, cancel it cleanly.
        if (_respawning)
        {
            StopAllCoroutines();
            _respawning = false;
            PlayerInputLock.Unlock(this);
        }

        Unhook();

        PlayerController player = PlayerController.Current;
        if (player == null)
            return; // No Pip in this scene.

        // SceneLoader has already put Pip at her spawn point before raising SceneLoaded.
        _sceneEntryPosition = player.transform.position;

        _health = player.GetComponent<PlayerHealth>();
        if (_health != null)
            _health.OnDeath += OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        if (!_respawning)
            StartCoroutine(RespawnRoutine());
    }

    // ------------------------------------------------------------------ Respawning

    private IEnumerator RespawnRoutine()
    {
        _respawning = true;
        PlayerInputLock.Lock(this);

        // Real-time waits, so this still works if a hit pause slowed time at the moment of death.
        yield return new WaitForSecondsRealtime(delayBeforeFade);

        ScreenFader fader = ScreenFader.Instance;
        if (fader != null)
            yield return fader.FadeOut(fadeOutTime);

        PlayerController player = PlayerController.Current;
        if (player != null && _health != null)
        {
            player.Teleport(GetRespawnPosition(), 0f);
            _health.Revive();           // Full stars. This also switches the "Player" map back on...
            PlayerInputLock.Refresh();  // ...so re-apply our lock until the fade-in is finished.
            GameEvents.RaisePlayerRespawned();
        }

        yield return new WaitForSecondsRealtime(blackHoldTime);

        if (fader != null)
            yield return fader.FadeIn(fadeInTime);

        PlayerInputLock.Unlock(this);
        _respawning = false;
    }

    /// <summary>The saved checkpoint/spawn point if it's in this scene, otherwise where Pip came in.</summary>
    private Vector2 GetRespawnPosition()
    {
        GameManager gm = GameManager.Instance;
        SpawnPoint point = gm != null ? SpawnPoint.Find(gm.Save.spawnPointId) : null;
        return point != null ? point.Position : _sceneEntryPosition;
    }

    private void Unhook()
    {
        if (_health != null)
            _health.OnDeath -= OnPlayerDied;
        _health = null;
    }
}
