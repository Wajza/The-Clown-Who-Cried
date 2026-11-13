using System;
using UnityEngine;

/// <summary>Which "mode" the game is in. Only one at a time. GameManager owns the current value.</summary>
public enum GameState
{
    Title,     // On the title screen.
    Loading,   // A scene is loading behind a fade. Pip can't move.
    Playing,   // Normal gameplay.
    Paused,    // Pause menu open (Part 4).
    Dialogue,  // A conversation is on screen (Part 2).
    Cutscene,  // A cutscene is playing (Part 6).
}

/// <summary>
/// The event hub for MY systems (core, tasks, UI, dialogue, cutscenes, audio).
///
/// WHY IT EXISTS
/// Systems shouldn't need references to each other. Instead, one system "raises" an event here
/// and any number of others listen. Example: SceneLoader raises SceneLoaded, and GameManager,
/// PlayerSaveBridge and PlayerDeathHandler all react, without SceneLoader knowing they exist.
///
/// My teammate's scripts (player, combat, enemies, boss) have their own events on their own
/// components. I listen to those in small "bridge" scripts. This hub is only for my side.
///
/// HOW TO USE
///   Listen:   GameEvents.SceneLoaded += OnSceneLoaded;   (in OnEnable)
///   Stop:     GameEvents.SceneLoaded -= OnSceneLoaded;   (in OnDisable, always!)
///   Raise:    GameEvents.RaiseSceneLoaded("Tumbleton", "FromTrain");
///
/// Only the Raise methods can fire an event (C# 'event' blocks outside Invoke), which keeps
/// the code that fires each event easy to find.
///
/// Later parts add more events here (dialogue, tasks, cutscenes).
/// </summary>
public static class GameEvents
{
    // ------------------------------------------------------------------ Game state

    /// <summary>Fired with the new state whenever GameManager changes it.</summary>
    public static event Action<GameState> GameStateChanged;

    public static void RaiseGameStateChanged(GameState state) => GameStateChanged?.Invoke(state);

    // ------------------------------------------------------------------ Scenes

    /// <summary>Fired with the scene name just before a scene starts loading (screen is about to fade).</summary>
    public static event Action<string> SceneLoadStarted;

    /// <summary>
    /// Fired with (scene name, spawn point id) once a scene is loaded AND Pip is standing at her spawn point.
    /// The spawn id can be null (e.g. pressing Play in the Editor). Also fired for the very first scene.
    /// </summary>
    public static event Action<string, string> SceneLoaded;

    public static void RaiseSceneLoadStarted(string sceneName) => SceneLoadStarted?.Invoke(sceneName);
    public static void RaiseSceneLoaded(string sceneName, string spawnPointId) => SceneLoaded?.Invoke(sceneName, spawnPointId);

    // ------------------------------------------------------------------ Checkpoints, death and saving

    /// <summary>Fired with the checkpoint id when Pip touches a checkpoint for the first time.</summary>
    public static event Action<string> CheckpointReached;

    /// <summary>Fired after Pip has been revived and moved back to her respawn point.</summary>
    public static event Action PlayerRespawned;

    /// <summary>Fired after the save file has been written to disk (for a "Saved!" icon).</summary>
    public static event Action GameSaved;

    public static void RaiseCheckpointReached(string id) => CheckpointReached?.Invoke(id);
    public static void RaisePlayerRespawned() => PlayerRespawned?.Invoke();
    public static void RaiseGameSaved() => GameSaved?.Invoke();

    // ------------------------------------------------------------------ Reset

    /// <summary>
    /// Static events survive between Play sessions when "Domain Reload" is turned off in the Editor,
    /// so old listeners from the last session could still be attached. This clears them every time
    /// Play starts. (Same pattern as GameFeelSettings.)
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        GameStateChanged = null;
        SceneLoadStarted = null;
        SceneLoaded = null;
        CheckpointReached = null;
        PlayerRespawned = null;
        GameSaved = null;
    }
}
