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
/// Each part adds its events here in its own section (Part 2: dialogue, Part 3: tasks, Part 4: menus; later: cutscenes).
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

    // ------------------------------------------------------------------ Dialogue (Part 2)

    /// <summary>Fired with the conversation when the dialogue box opens.</summary>
    public static event Action<DialogueData> DialogueStarted;

    /// <summary>Fired with the SAME conversation (the one that was started) when the box closes.</summary>
    public static event Action<DialogueData> DialogueEnded;

    /// <summary>Fired with the NPC's id after a conversation with that NPC finishes. Tasks use this.</summary>
    public static event Action<string> NpcTalkedTo;

    public static void RaiseDialogueStarted(DialogueData dialogue) => DialogueStarted?.Invoke(dialogue);
    public static void RaiseDialogueEnded(DialogueData dialogue) => DialogueEnded?.Invoke(dialogue);
    public static void RaiseNpcTalkedTo(string npcId) => NpcTalkedTo?.Invoke(npcId);

    // ------------------------------------------------------------------ Tasks (Part 3)

    /// <summary>Fired with the task when TaskManager starts it.</summary>
    public static event Action<TaskData> TaskStarted;

    /// <summary>Fired with the task when one of its objectives is done but the task isn't finished yet.</summary>
    public static event Action<TaskData> TaskProgressed;

    /// <summary>Fired with the task when its last objective is done (its flags are already set).</summary>
    public static event Action<TaskData> TaskCompleted;

    public static void RaiseTaskStarted(TaskData task) => TaskStarted?.Invoke(task);
    public static void RaiseTaskProgressed(TaskData task) => TaskProgressed?.Invoke(task);
    public static void RaiseTaskCompleted(TaskData task) => TaskCompleted?.Invoke(task);

    // ------------------------------------------------------------------ Menus and journal (Part 4)

    /// <summary>Fired with true when the game pauses (pause menu or journal) and false when it resumes.</summary>
    public static event Action<bool> GamePausedChanged;

    /// <summary>Fired with the page when Pip finds a torn journal page.</summary>
    public static event Action<JournalPageData> JournalPageFound;

    public static void RaiseGamePausedChanged(bool paused) => GamePausedChanged?.Invoke(paused);
    public static void RaiseJournalPageFound(JournalPageData page) => JournalPageFound?.Invoke(page);

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
        DialogueStarted = null;
        DialogueEnded = null;
        NpcTalkedTo = null;
        TaskStarted = null;
        TaskProgressed = null;
        TaskCompleted = null;
        GamePausedChanged = null;
        JournalPageFound = null;
    }
}
