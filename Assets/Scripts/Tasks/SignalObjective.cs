using System;
using UnityEngine;

/// <summary>
/// Objective: "this named thing happened N times". It counts GameEvents.TaskSignal(key).
///
/// The world objects only raise the signal; this objective does the counting:
///   AreaTrigger        raises its key when Pip walks in        ("p1_outside", "p2_crossed")
///   Inspectable        raises its key after Pip looks at it    ("p1_mirror")
///   BreakableTarget    raises its key when it breaks           ("p3_balloon" x 8, "p4_pot" x 5)
///
/// The count is saved (so "3/8" survives Continue) and shown in the tracker when Required Count
/// is more than 1: "Pop the balloons (3/8)".
///
/// Signals only count while this step is the current one. A balloon popped before the task
/// started doesn't count; that's why task targets can be set to ignore hits until then.
///
/// Create one: Create > Clown Who Cried > Tasks > Objective - Signal.
/// </summary>
[CreateAssetMenu(menuName = "Clown Who Cried/Tasks/Objective - Signal", fileName = "Objective_Signal_")]
public class SignalObjective : TaskObjective
{
    [Tooltip("The signal to count, e.g. 'p3_balloon'. Must match the world object's Signal Key exactly.")]
    public string signalKey;

    [Min(1), Tooltip("How many times it must happen.")]
    public int requiredCount = 1;

    [Min(0f), Tooltip("Seconds allowed for this step (0 = no limit). Running out fails the task (e.g. W1's deliveries).")]
    public float timeLimit;

    public override float TimeLimit => timeLimit;

    public override Action Watch(ObjectiveRun run)
    {
        void OnSignal(string key)
        {
            if (key != signalKey)
                return;
            run.AddProgress();
            if (run.Progress >= requiredCount)
                run.Complete();
        }

        GameEvents.TaskSignal += OnSignal;

        // Already enough from before (e.g. the save had 8/8 when the game quit): done now.
        // TaskManager copes with completing while Watch runs.
        if (run.Progress >= requiredCount)
            run.Complete();

        return () => GameEvents.TaskSignal -= OnSignal;
    }

    public override string Describe(int progress) =>
        requiredCount > 1 ? $"{description} ({Mathf.Min(progress, requiredCount)}/{requiredCount})" : description;
}
