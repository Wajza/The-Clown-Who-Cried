using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps track of which tasks are running, which step each is on, and which are finished.
///
/// THE LIFE OF A TASK
///   1. StartTask(task)          (called by TaskGiver, a cutscene, or a test)
///        - added to Save.activeTasks at step 0, flag task_[id]_active set
///        - GameEvents.TaskStarted raised, game saved
///        - starts watching objective 0 (e.g. "talk to the baker")
///   2. Each time the current objective is met:
///        - step + 1 in the save; if there are more objectives, watch the next one and
///          raise GameEvents.TaskProgressed (game saved)
///   3. After the last objective:
///        - moved to Save.completedTaskIds, task_[id]_active removed, task_[id]_done and
///          "Set Flag When Complete" set
///        - GameEvents.TaskCompleted raised, game saved
///
/// THE SAVE FILE IS THE TRUTH
/// Progress is stored only in GameManager.Save. When the save is swapped (New Game or
/// Continue), TaskManager notices on the next SceneLoaded (or the next time something asks
/// it) and rebuilds its watchers from the save. That's "SyncWithSave" below.
///
/// To turn task ids from the save back into TaskData, every task asset in
/// Assets/Resources/Tasks is loaded once at start.
///
/// Setup: on the GameSystems prefab (the builder adds it).
/// </summary>
public class TaskManager : MonoBehaviour
{
    private const string TasksResourcesFolder = "Tasks"; // = Assets/Resources/Tasks

    // ------------------------------------------------------------------ Inspector fields

    [SerializeField, Tooltip("Write the save file whenever a task starts, moves on a step, or finishes.")]
    private bool saveOnChange = true;

    // ------------------------------------------------------------------ Public API

    public static TaskManager Instance { get; private set; }

    /// <summary>Running tasks, in the order they were started.</summary>
    public IReadOnlyList<TaskData> ActiveTasks
    {
        get
        {
            SyncWithSave();
            return _activeOrder;
        }
    }

    public bool IsActive(TaskData task)
    {
        SyncWithSave();
        return task != null && _active.ContainsKey(task.id);
    }

    /// <summary>Finished? (Read straight from the save, so it works even before any sync.)</summary>
    public static bool IsCompleted(TaskData task)
    {
        SaveData save = CurrentSave;
        return task != null && save != null && save.completedTaskIds.Contains(task.id);
    }

    /// <summary>Which objective a running task is on (0 = first), or -1 if it isn't running.</summary>
    public int GetStep(TaskData task) =>
        IsActive(task) ? _active[task.id].progress.step : -1;

    /// <summary>The objective Pip is working on for this task, or null if it isn't running.</summary>
    public TaskObjective GetCurrentObjective(TaskData task)
    {
        int step = GetStep(task);
        return step >= 0 && step < task.objectives.Count ? task.objectives[step] : null;
    }

    /// <summary>
    /// The current objective as the player should read it, with progress if it counts,
    /// e.g. "Pop the balloons (3/8)". Empty if the task isn't running.
    /// </summary>
    public string GetObjectiveText(TaskData task)
    {
        TaskObjective objective = GetCurrentObjective(task);
        return objective != null ? objective.Describe(_active[task.id].progress.count) : "";
    }

    /// <summary>Every task TaskManager knows (all of Resources/Tasks). Used by the Journal's Task Board.</summary>
    public IEnumerable<TaskData> AllTasks
    {
        get
        {
            LoadAllTasks();
            return _tasksById.Values;
        }
    }

    /// <summary>The task with this id, or null. Knows every task in Resources/Tasks.</summary>
    public TaskData FindTask(string id)
    {
        LoadAllTasks();
        return id != null && _tasksById.TryGetValue(id, out TaskData task) ? task : null;
    }

    /// <summary>Starts a task. Returns false if it's already running or already finished.</summary>
    public bool StartTask(TaskData task)
    {
        SaveData save = CurrentSave;
        if (task == null || save == null)
            return false;
        if (IsActive(task) || IsCompleted(task))
            return false;

        LoadAllTasks();
        _tasksById[task.id] = task; // In case it lives outside Resources/Tasks.

        var progress = new TaskProgress { taskId = task.id, step = 0 };
        save.activeTasks.Add(progress);
        save.SetFlag(task.ActiveFlag);
        AddRunning(task, progress);
        UnlockAbilities(task.unlockOnStart); // e.g. P4 hands Pip the mallet.

        GameEvents.RaiseTaskStarted(task);

        if (task.objectives.Count == 0)
        {
            Debug.LogWarning($"TaskManager: task '{task.id}' has no objectives, so it's finished at once.", task);
            Complete(task);
            return true;
        }

        WatchCurrentObjective(task);
        SaveIfWanted();
        return true;
    }

    /// <summary>
    /// Finishes a running task straight away, skipping its remaining objectives
    /// (for cutscenes or debugging). Does nothing if the task isn't running.
    /// </summary>
    public void CompleteTask(TaskData task)
    {
        if (IsActive(task))
            Complete(task);
    }

    // ------------------------------------------------------------------ Private state

    /// <summary>A running task: its save entry and how to stop watching its current objective.</summary>
    private class RunningTask
    {
        public TaskData task;
        public TaskProgress progress; // The same object that's in Save.activeTasks.
        public Action stopWatching;
    }

    private readonly Dictionary<string, RunningTask> _active = new Dictionary<string, RunningTask>();
    private readonly List<TaskData> _activeOrder = new List<TaskData>();
    private Dictionary<string, TaskData> _tasksById;
    private SaveData _syncedSave; // The save object our watchers were built from.

    private static SaveData CurrentSave => GameManager.Instance != null ? GameManager.Instance.Save : null;

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        StopAllWatching();
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable() => GameEvents.SceneLoaded += OnSceneLoaded;
    private void OnDisable() => GameEvents.SceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(string sceneName, string spawnPointId) => SyncWithSave();

    // ------------------------------------------------------------------ Watching objectives

    private void WatchCurrentObjective(TaskData task)
    {
        RunningTask running = _active[task.id];
        int step = running.progress.step;
        TaskObjective objective = task.objectives[step];

        if (objective == null)
        {
            Debug.LogWarning($"TaskManager: task '{task.id}' has an empty objective slot (step {step + 1}); skipping it.", task);
            OnObjectiveDone(task, step);
            return;
        }

        // The run reports progress (saved count) and completion back to us.
        var run = new ObjectiveRun(running.progress.count,
            onProgress: count => OnObjectiveProgress(task, step, count),
            onComplete: () => OnObjectiveDone(task, step));
        Action stop = objective.Watch(run);

        // Normally we keep the "stop" for later. But if the objective was met during Watch
        // (and the task already moved on), stop listening right away.
        if (IsActive(task) && _active[task.id].progress.step == step)
            running.stopWatching = stop;
        else
            stop?.Invoke();
    }

    /// <summary>Called by a counting objective: store the new count, update the tracker, save.</summary>
    private void OnObjectiveProgress(TaskData task, int step, int count)
    {
        if (!_active.TryGetValue(task.id, out RunningTask running) || running.progress.step != step)
            return;

        running.progress.count = count;
        GameEvents.RaiseTaskProgressed(task);
        SaveIfWanted();
    }

    /// <summary>Called by an objective when it's met.</summary>
    private void OnObjectiveDone(TaskData task, int step)
    {
        // Ignore late calls from an old step or a task that's no longer running.
        if (!_active.TryGetValue(task.id, out RunningTask running) || running.progress.step != step)
            return;

        StopWatching(running);
        running.progress.step++;
        running.progress.count = 0; // The next objective counts from zero.

        if (running.progress.step >= task.objectives.Count)
        {
            Complete(task);
            return;
        }

        WatchCurrentObjective(task);
        GameEvents.RaiseTaskProgressed(task);
        SaveIfWanted();
    }

    private void Complete(TaskData task)
    {
        SaveData save = CurrentSave;
        RunningTask running = _active[task.id];
        StopWatching(running);
        _active.Remove(task.id);
        _activeOrder.Remove(task);

        save.activeTasks.Remove(running.progress);
        if (!save.completedTaskIds.Contains(task.id))
            save.completedTaskIds.Add(task.id);
        save.ClearFlag(task.ActiveFlag);
        save.SetFlag(task.DoneFlag);
        save.SetFlag(task.setFlagWhenComplete);
        UnlockAbilities(task.unlockOnComplete); // e.g. W3 rewards Juggle Shot.

        GameEvents.RaiseTaskCompleted(task);
        SaveIfWanted();
    }

    private static void UnlockAbilities(List<Ability> abilities)
    {
        if (abilities == null)
            return;
        foreach (Ability ability in abilities)
            PlayerAbilities.Unlock(ability);
    }

    private void AddRunning(TaskData task, TaskProgress progress)
    {
        _active[task.id] = new RunningTask { task = task, progress = progress };
        _activeOrder.Add(task);
    }

    private static void StopWatching(RunningTask running)
    {
        running.stopWatching?.Invoke();
        running.stopWatching = null;
    }

    private void StopAllWatching()
    {
        foreach (RunningTask running in _active.Values)
            StopWatching(running);
        _active.Clear();
        _activeOrder.Clear();
    }

    // ------------------------------------------------------------------ Save file

    /// <summary>
    /// If the save data object has changed (New Game, Continue), throw away the old watchers
    /// and rebuild them from the save: every saved task resumes at its saved step.
    /// </summary>
    private void SyncWithSave()
    {
        SaveData save = CurrentSave;
        if (save == _syncedSave)
            return;

        StopAllWatching();
        _syncedSave = save;
        if (save == null)
            return;

        foreach (TaskProgress progress in save.activeTasks.ToArray())
        {
            TaskData task = FindTask(progress.taskId);
            if (task == null)
            {
                Debug.LogWarning($"TaskManager: the save has task '{progress.taskId}', but no task asset with that id is in Resources/Tasks.");
                continue;
            }
            if (_active.ContainsKey(task.id))
                continue; // Duplicate entry in the save.

            AddRunning(task, progress);
            if (progress.step >= task.objectives.Count)
                Complete(task); // Saved right at the end (e.g. objectives removed since).
            else
                WatchCurrentObjective(task);
        }
    }

    private void SaveIfWanted()
    {
        if (saveOnChange && GameManager.Instance != null)
            GameManager.Instance.SaveGame();
    }

    private void LoadAllTasks()
    {
        if (_tasksById != null)
            return;

        _tasksById = new Dictionary<string, TaskData>();
        foreach (TaskData task in Resources.LoadAll<TaskData>(TasksResourcesFolder))
        {
            if (_tasksById.TryGetValue(task.id, out TaskData other))
                Debug.LogError($"TaskManager: '{task.name}' and '{other.name}' both use the id '{task.id}'. Ids must be unique.", task);
            else
                _tasksById[task.id] = task;
        }
    }

    // ------------------------------------------------------------------ Reset

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;
}
