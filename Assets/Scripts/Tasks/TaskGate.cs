using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Something that stays closed until certain tasks are finished, e.g. the barrier in front of
/// Tumbleton's train station that opens once all 4 town tasks are done.
///
/// HOW IT WORKS
/// The gate is "open" when every task in Required Tasks is finished. It checks when the scene
/// starts and every time a task is completed (GameEvents.TaskCompleted). It just switches
/// GameObjects on and off:
///   - Active While Closed: e.g. the barrier sprite + its solid collider, a "Closed" sign.
///   - Active When Open:    e.g. an open-gate sprite, the LevelExit to the train (Part 5).
///
/// Because "finished" comes from the save file, a gate that was opened stays open after
/// leaving the scene, dying, or loading a save.
///
/// Setup: an empty GameObject with this script. Put the barrier (sprite + BoxCollider2D, NOT a
/// trigger) as a child, and drag it into Active While Closed. Drag the tasks into Required Tasks.
/// </summary>
public class TaskGate : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector fields

    [SerializeField, Tooltip("All of these must be finished for the gate to open.")]
    private List<TaskData> requiredTasks = new List<TaskData>();

    [SerializeField, Tooltip("Switched ON while closed, OFF once open (the barrier).")]
    private GameObject[] activeWhileClosed = new GameObject[0];

    [SerializeField, Tooltip("Switched OFF while closed, ON once open.")]
    private GameObject[] activeWhenOpen = new GameObject[0];

    // ------------------------------------------------------------------ Public API

    public bool IsOpen { get; private set; }

    /// <summary>How many of the required tasks are finished (for a "2 / 4" sign, say).</summary>
    public int CompletedCount
    {
        get
        {
            int count = 0;
            foreach (TaskData task in requiredTasks)
                if (TaskManager.IsCompleted(task))
                    count++;
            return count;
        }
    }

    public int RequiredCount => requiredTasks.Count;

    // ------------------------------------------------------------------ Unity lifecycle

    private void OnEnable()
    {
        GameEvents.TaskCompleted += OnTaskCompleted;
        GameEvents.SceneLoaded += OnSceneLoaded;
        Refresh();
    }

    private void OnDisable()
    {
        GameEvents.TaskCompleted -= OnTaskCompleted;
        GameEvents.SceneLoaded -= OnSceneLoaded;
    }

    // ------------------------------------------------------------------ Opening

    private void OnTaskCompleted(TaskData task) => Refresh();

    // Checked again once the scene is fully loaded: when you press Play in the Editor, the
    // GameManager (and the save) only exist after this scene's objects have woken up.
    private void OnSceneLoaded(string sceneName, string spawnPointId) => Refresh();

    /// <summary>Checks the tasks and shows the open or closed objects.</summary>
    public void Refresh()
    {
        IsOpen = CompletedCount == requiredTasks.Count;

        foreach (GameObject go in activeWhileClosed)
            if (go != null)
                go.SetActive(!IsOpen);
        foreach (GameObject go in activeWhenOpen)
            if (go != null)
                go.SetActive(IsOpen);
    }
}
