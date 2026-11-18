using UnityEngine;

/// <summary>
/// Starts one task as soon as another one is finished, e.g. P4 "Reflex Check" -> P5 "Doctor's
/// Visit". Also catches up when a level loads (a save where After is done but Next never started).
///
/// Setup: an empty GameObject in the level with this script, After and Next filled in.
/// Added by the teammate (for the doctor's office).
/// </summary>
public class TaskChain : MonoBehaviour
{
    [SerializeField, Tooltip("When this task is finished...")]
    private TaskData after;
    [SerializeField, Tooltip("...start this one.")]
    private TaskData next;

    private void OnEnable()
    {
        GameEvents.TaskCompleted += OnTaskCompleted;
        GameEvents.SceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        GameEvents.TaskCompleted -= OnTaskCompleted;
        GameEvents.SceneLoaded -= OnSceneLoaded;
    }

    private void OnTaskCompleted(TaskData task)
    {
        if (task == after)
            TryStart();
    }

    private void OnSceneLoaded(string scene, string spawnPointId) => TryStart();

    private void TryStart()
    {
        TaskManager manager = TaskManager.Instance;
        if (manager == null || after == null || next == null)
            return;
        if (TaskManager.IsCompleted(after))
            manager.StartTask(next); // Ignored if it's running or already finished.
    }
}
