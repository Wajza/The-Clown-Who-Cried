using UnityEngine;

/// <summary>
/// Starts a task by itself, without an NPC: either as soon as the level is loaded (e.g. P1
/// "Wake Up" when Pip wakes up in her house), or when Pip walks into this object's trigger.
/// Nothing happens if the task is already running or finished, or if Required Flag isn't set.
///
/// Setup: an empty GameObject with this script. For "On Enter", also add a trigger Collider2D.
/// </summary>
public class TaskStarter : MonoBehaviour
{
    public enum When { OnLevelLoaded, OnEnter }

    [SerializeField] private TaskData task;
    [SerializeField] private When when = When.OnLevelLoaded;
    [SerializeField, Tooltip("Optional: only start once this flag is set. Empty = always.")]
    private string requiredFlag;

    private void OnEnable() => GameEvents.SceneLoaded += OnSceneLoaded;
    private void OnDisable() => GameEvents.SceneLoaded -= OnSceneLoaded;

    // Waits for SceneLoaded (not Start): the save and TaskManager are only ready by then
    // when you press Play in the Editor.
    private void OnSceneLoaded(string sceneName, string spawnPointId)
    {
        if (when == When.OnLevelLoaded)
            TryStart();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (when == When.OnEnter && other.GetComponentInParent<PlayerController>() != null)
            TryStart();
    }

    private void TryStart()
    {
        TaskManager manager = TaskManager.Instance;
        GameManager gm = GameManager.Instance;
        if (task == null || manager == null || gm == null)
            return;
        if (!string.IsNullOrEmpty(requiredFlag) && !gm.Save.HasFlag(requiredFlag))
            return;
        manager.StartTask(task); // Ignored if running or finished.
    }
}
