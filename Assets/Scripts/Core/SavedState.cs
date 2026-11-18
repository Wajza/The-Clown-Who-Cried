using UnityEngine;

/// <summary>
/// Lets level objects REMEMBER what happened to them, so leaving a scene and coming back
/// (or quitting and pressing Continue) never shows a reset level.
///
/// It stores simple on/off facts as flags in the save (GameManager.Save), the same way
/// BreakableTarget ("broken_...") and TaskSwitch ("switch_...") already do.
///
///   SavedState.Get(key) / Set(key) / Clear(key)       a fact that lasts for good
///   SavedState.TaskKey(task, id)                       a fact that belongs to one ATTEMPT at a task:
///                                                      it's wiped when that task starts again or
///                                                      fails (e.g. W1's served stalls and delivered
///                                                      boxes go back when time runs out)
///   SavedState.ObjectId(component)                     a default id: scene name + object name
///
/// Used by: DeliveryStall, CarryableBox (W1), WaveSpawner (W3 waves), Pickup and EnemyBase
/// (when given a Save Id). Added by the teammate (player/combat side).
/// </summary>
public static class SavedState
{
    private const string TaskPrefix = "taskstate_";

    /// <summary>Is this fact remembered? (False when there's no game running, e.g. a bare test scene.)</summary>
    public static bool Get(string key)
    {
        GameManager gm = GameManager.Instance;
        return gm != null && !string.IsNullOrEmpty(key) && gm.Save.HasFlag(key);
    }

    /// <summary>Remembers a fact. It's written to disk with the next save (checkpoint, door, task step...).</summary>
    public static void Set(string key)
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && !string.IsNullOrEmpty(key))
            gm.Save.SetFlag(key);
    }

    public static void Clear(string key)
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && !string.IsNullOrEmpty(key))
            gm.Save.ClearFlag(key);
    }

    /// <summary>A key that only lasts for the current attempt at 'task'.</summary>
    public static string TaskKey(TaskData task, string id) =>
        task == null ? id : $"{TaskPrefix}{task.id}_{id}";

    /// <summary>A default id for a placed object: "SceneName/ObjectName". Keep object names unique per scene.</summary>
    public static string ObjectId(Component component) =>
        $"{component.gameObject.scene.name}/{component.gameObject.name}";

    // ------------------------------------------------------------------ Task attempts

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        // AfterSceneLoad runs after GameEvents clears its events, so these stay hooked up.
        GameEvents.TaskStarted -= ForgetAttempt;
        GameEvents.TaskFailed -= ForgetAttempt;
        GameEvents.TaskStarted += ForgetAttempt;
        GameEvents.TaskFailed += ForgetAttempt;
    }

    /// <summary>A task (re)started or failed: forget everything about its previous attempt.</summary>
    private static void ForgetAttempt(TaskData task)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || task == null)
            return;
        string prefix = $"{TaskPrefix}{task.id}_";
        gm.Save.flags.RemoveAll(flag => flag.StartsWith(prefix));
    }
}
