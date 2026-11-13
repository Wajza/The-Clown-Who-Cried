using System.Collections;
using UnityEngine;

/// <summary>
/// An automatic check of Part 3 (Tasks). It lives in the TaskTest scene:
///   - Mayor  (TaskGiver): asks Pip to run an errand. Answer "Sure!" to get the task.
///   - "The Mayor's Errand": 1. talk to the baker   2. report back to the mayor
///   - "Say Hi": talk to the kid (started directly by the test, like a cutscene would).
///   - A red wall (TaskGate) that opens once both tasks are done.
///
/// Press Play in TaskTest and watch Pip do the errand, including a "Continue" from the save
/// file halfway through. Results go to the Console and Logs/Part3TestResults.txt.
///
/// NPCs and the gate are looked up by name each time, because loading a save rebuilds the
/// scene (the old objects are gone). The task assets don't change, so they're linked directly.
/// </summary>
public class TaskSystemTest : SystemTestBase
{
    [SerializeField] private TaskData errand;
    [SerializeField] private TaskData kidTask;
    [SerializeField] private DialogueData mayorThanks;

    protected override string ResultsFileName => "Part3TestResults.txt";
    protected override string LogTag => "Part3Test";

    private int _started, _progressed, _completed;
    private TaskData _lastStarted, _lastProgressed, _lastCompleted;
    private bool _sceneLoaded;

    // ------------------------------------------------------------------ Events the test watches

    private void OnEnable()
    {
        GameEvents.TaskStarted += OnStarted;
        GameEvents.TaskProgressed += OnProgressed;
        GameEvents.TaskCompleted += OnCompleted;
        GameEvents.SceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        GameEvents.TaskStarted -= OnStarted;
        GameEvents.TaskProgressed -= OnProgressed;
        GameEvents.TaskCompleted -= OnCompleted;
        GameEvents.SceneLoaded -= OnSceneLoaded;
    }

    private void OnStarted(TaskData t) { _started++; _lastStarted = t; }
    private void OnProgressed(TaskData t) { _progressed++; _lastProgressed = t; }
    private void OnCompleted(TaskData t) { _completed++; _lastCompleted = t; }
    private void OnSceneLoaded(string scene, string spawn) => _sceneLoaded = true;

    private static TaskManager Tasks => TaskManager.Instance;
    private static SaveData Save => GameManager.Instance.Save;

    // ------------------------------------------------------------------ Test flow

    protected override IEnumerator RunTest()
    {
        // 0. Links must be set, or "null == null" comparisons would pass by accident.
        bool linked = errand != null && kidTask != null && mayorThanks != null;
        Check("Test driver references are assigned", linked);
        if (!linked)
            yield break;

        // 1. Starting state.
        yield return WaitUntil(() => GameManager.Instance != null && _sceneLoaded, 5f, "first SceneLoaded");
        yield return null;

        TaskTrackerUI tracker = FindFirstObjectByType<TaskTrackerUI>();
        Check("TaskManager and TaskTrackerUI exist", Tasks != null && tracker != null);
        Check("Tasks are found by id (loaded from Resources/Tasks)",
            Tasks.FindTask("test_errand") == errand && Tasks.FindTask("test_kid") == kidTask);
        Check("No tasks running at start", Tasks.ActiveTasks.Count == 0);
        Check("Tracker hidden with no tasks", !tracker.TrackerVisible);
        CheckGate("Gate closed at start", open: false, completed: 0);

        // 2. Talking to the baker before having the task does nothing.
        yield return Talk("baker");
        Check("Baker before the task: errand not started", !Tasks.IsActive(errand) && _started == 0);

        // 3. Mayor asks; answer "Not now." (the 2nd answer): no task.
        Check("Mayor has a task to give", Npc("mayor").GetComponent<TaskGiver>().HasTaskToGive);
        yield return Talk("mayor", answer: 1);
        Check("Saying no doesn't start the task", !Tasks.IsActive(errand) && _started == 0);

        // 4. Ask again and answer "Sure!": the task starts.
        yield return Talk("mayor", answer: 0);
        Check("Saying yes starts the task", Tasks.IsActive(errand) && Tasks.GetStep(errand) == 0);
        Check("TaskStarted raised once with the errand", _started == 1 && _lastStarted == errand);
        Check("Flag task_test_errand_active set", Save.HasFlag("task_test_errand_active"));
        Check("Mayor has no task to give any more", !Npc("mayor").GetComponent<TaskGiver>().HasTaskToGive);
        Check("Current objective is 'Talk to the baker'", Tasks.GetCurrentObjective(errand)?.description == "Talk to the baker");
        SaveData onDisk = SaveSystem.Read();
        Check("Started task written to disk at step 0", DiskStep(onDisk, "test_errand") == 0);
        Check("Tracker shows the task and objective",
            tracker.TrackerVisible && tracker.TrackerText.Contains("The Mayor's Errand") && tracker.TrackerText.Contains("Talk to the baker"));
        Check("Banner says 'New task'", tracker.BannerShowing && tracker.BannerText == "New task: The Mayor's Errand");

        // 5. Talking to the mayor now gives the "waiting" line, and doesn't skip the baker step.
        NpcTalk mayor = Npc("mayor");
        yield return MovePipTo(mayor.transform.position.x);
        InteractionInput.Instance.TryInteract();
        yield return null;
        Check("Tracker hidden during a conversation", Runner.IsRunning && !tracker.TrackerVisible);
        Check("Mayor uses the 'waiting' dialogue", UI().SpeakerName == "Mayor (test)" && Runner.CurrentDialogue != mayorThanks);
        yield return FinishConversation();
        Check("Tracker back after the conversation", tracker.TrackerVisible);
        Check("Mayor too early: still on step 0", Tasks.GetStep(errand) == 0 && _progressed == 0);

        // 6. A different NPC doesn't count either.
        yield return Talk("kid");
        Check("Kid doesn't count for the errand", Tasks.GetStep(errand) == 0);

        // 7. The baker: step 1 done.
        yield return Talk("baker");
        Check("Baker moves the errand to step 1", Tasks.GetStep(errand) == 1);
        Check("TaskProgressed raised", _progressed == 1 && _lastProgressed == errand);
        Check("Tracker shows the next objective", tracker.TrackerText.Contains("Report back to the mayor"));
        Check("Step 1 written to disk", DiskStep(SaveSystem.Read(), "test_errand") == 1);

        // 8. Continue from the save file: the task must come back at step 1, still working.
        _sceneLoaded = false;
        Check("ContinueGame finds the save", GameManager.Instance.ContinueGame());
        yield return WaitUntil(() => _sceneLoaded && !SceneLoader.Instance.IsLoading, 10f, "Continue");
        yield return null;
        Check("After Continue: errand running again at step 1", Tasks.IsActive(errand) && Tasks.GetStep(errand) == 1);
        Check("After Continue: tracker shows it", tracker.TrackerVisible && tracker.TrackerText.Contains("Report back to the mayor"));
        CheckGate("After Continue: gate still closed", open: false, completed: 0);

        // 9. Report back: the errand is finished.
        yield return Talk("mayor");
        Check("Errand finished", !Tasks.IsActive(errand) && TaskManager.IsCompleted(errand));
        Check("TaskCompleted raised once", _completed == 1 && _lastCompleted == errand);
        Check("Moved from active to completed in the save",
            Save.activeTasks.Count == 0 && Save.completedTaskIds.Contains("test_errand"));
        Check("Flags: active removed, done + reward set",
            !Save.HasFlag("task_test_errand_active") && Save.HasFlag("task_test_errand_done") && Save.HasFlag("test_errand_reward"));
        onDisk = SaveSystem.Read();
        Check("Completion written to disk", onDisk != null && onDisk.completedTaskIds.Contains("test_errand") && DiskStep(onDisk, "test_errand") < 0);
        Check("Mayor now says thanks", Npc("mayor").PickDialogue() == mayorThanks);
        Check("A finished task can't start again", !Tasks.StartTask(errand));
        Check("Tracker hidden: no tasks left", !tracker.TrackerVisible);
        yield return WaitUntil(() => tracker.BannerText == "Task complete: The Mayor's Errand", 10f, "'Task complete' banner");
        CheckGate("One of two tasks done: gate still closed", open: false, completed: 1);

        // 10. Start a task directly (as a cutscene would), then finish it: the gate opens.
        Check("StartTask works", Tasks.StartTask(kidTask));
        Check("Starting a running task again is refused", !Tasks.StartTask(kidTask));
        Check("Tracker shows 'Say Hi'", tracker.TrackerVisible && tracker.TrackerText.Contains("Say hi to the kid"));
        yield return Talk("kid");
        Check("Kid task finished", TaskManager.IsCompleted(kidTask) && _completed == 2);
        CheckGate("Both tasks done: gate open", open: true, completed: 2);

        // 11. Reload the scene: the gate opens straight away (the save remembers).
        _sceneLoaded = false;
        SceneLoader.Instance.LoadScene(SceneLoader.CurrentSceneName);
        yield return WaitUntil(() => _sceneLoaded && !SceneLoader.Instance.IsLoading, 10f, "scene reload");
        CheckGate("After reloading: gate already open", open: true, completed: 2);
    }

    // ------------------------------------------------------------------ Steps

    /// <summary>Walk to the NPC, talk, pick an answer if asked (0 = first), and finish.</summary>
    private IEnumerator Talk(string npcId, int answer = 0)
    {
        NpcTalk npc = Npc(npcId);
        yield return MovePipTo(npc.transform.position.x);
        if (InteractionInput.Instance.Target != npc || !InteractionInput.Instance.TryInteract())
        {
            Fail($"Could not start talking to '{npcId}'");
            yield break;
        }

        yield return AdvanceToChoices();
        for (int i = 0; i < answer && Runner.IsWaitingForChoice; i++)
        {
            Runner.RequestChoiceMove(+1);
            yield return null;
        }
        yield return FinishConversation(); // Advance picks the highlighted answer.
    }

    private void CheckGate(string label, bool open, int completed)
    {
        TaskGate gate = FindFirstObjectByType<TaskGate>();
        GameObject wall = gate != null ? gate.transform.Find("Wall").gameObject : null;
        bool ok = gate != null && gate.IsOpen == open && wall.activeSelf == !open &&
                  gate.CompletedCount == completed && gate.RequiredCount == 2;
        Check($"{label} ({(gate != null ? gate.CompletedCount : -1)}/2 done)", ok);
    }

    private static NpcTalk Npc(string id)
    {
        foreach (NpcTalk npc in FindObjectsByType<NpcTalk>())
            if (npc.NpcId == id)
                return npc;
        return null;
    }

    private static DialogueUI UI() => DialogueRunner.Instance.UI;

    /// <summary>The saved step of a task in a save read from disk, or -1 if it isn't active there.</summary>
    private static int DiskStep(SaveData save, string taskId)
    {
        if (save == null)
            return -1;
        foreach (TaskProgress p in save.activeTasks)
            if (p.taskId == taskId)
                return p.step;
        return -1;
    }
}
