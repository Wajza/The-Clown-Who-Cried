using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The "is this way open yet?" rule shared by LevelExit and Door. Filled in on the Inspector:
///   Required Flag    a save flag that must be set (empty = none), e.g. "p1_mirror".
///   Required Tasks   tasks that must be finished (empty = none).
///   Locked Dialogue  said when Pip tries to go while it's still locked (optional), e.g.
///                    "I should look in the mirror first."
/// </summary>
[Serializable]
public class ExitRequirement
{
    [Tooltip("Only open once this flag is set. Empty = no flag needed.")]
    public string requiredFlag;

    [Tooltip("Only open once all these tasks are finished.")]
    public List<TaskData> requiredTasks = new List<TaskData>();

    [Tooltip("Optional: what Pip hears/thinks when it's locked.")]
    public DialogueData lockedDialogue;

    public bool IsMet
    {
        get
        {
            SaveData save = GameManager.Instance != null ? GameManager.Instance.Save : null;
            if (!string.IsNullOrEmpty(requiredFlag) && (save == null || !save.HasFlag(requiredFlag)))
                return false;
            foreach (TaskData task in requiredTasks)
                if (task != null && !TaskManager.IsCompleted(task))
                    return false;
            return true;
        }
    }

    /// <summary>Plays the locked dialogue (if any). Returns true if it started.</summary>
    public bool SayLocked() =>
        lockedDialogue != null && DialogueRunner.Instance != null && DialogueRunner.Instance.StartDialogue(lockedDialogue);
}

/// <summary>Where an exit leads: a scene and the spawn point to arrive at.</summary>
[Serializable]
public class ExitDestination
{
    [Tooltip("Scene to load (must be in the Build Profile scene list), e.g. 'Tumbleton'.")]
    public string sceneName;

    [Tooltip("SpawnPoint id in that scene to arrive at, e.g. 'FromHouse'. Empty = its default spawn point.")]
    public string spawnPointId;

    /// <summary>Loads the scene. Returns false if a load is already running or no scene is set.</summary>
    public bool Go()
    {
        if (string.IsNullOrEmpty(sceneName) || SceneLoader.Instance == null || SceneLoader.Instance.IsLoading)
            return false;
        SceneLoader.Instance.LoadScene(sceneName, spawnPointId);
        return true;
    }
}
