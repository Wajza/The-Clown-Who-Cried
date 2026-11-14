using System;
using UnityEngine;

/// <summary>
/// One step of a task, e.g. "Talk to the baker" or "Pop the balloons (3/8)". It is "abstract":
/// you never create a TaskObjective itself, only a kind built on it:
///   TalkToNpcObjective   done when a conversation with an NPC finishes
///   SignalObjective      done when a named task signal happened N times (Part 6)
///
/// HOW AN OBJECTIVE WORKS
/// TaskManager calls Watch(run) when Pip reaches this step. The objective starts listening for
/// whatever it needs (a GameEvents event) and tells the run what happened:
///   run.AddProgress()   one more (for counting objectives; saved, shown as "3/8")
///   run.Complete()      the step is done
/// Watch returns a "stop watching" action that TaskManager calls when the step is over.
///
/// The objective asset itself stores NO progress (no "isDone" or "count" field). Assets are shared
/// and Unity can keep their values between Play sessions in the Editor, so progress lives in the
/// save file instead (TaskManager handles that; run.Progress is the saved count).
///
/// ADDING A NEW KIND LATER (e.g. "collect 3 balloons")
///   1. Make a class built on TaskObjective with a [CreateAssetMenu] attribute.
///   2. Add its settings as public fields.
///   3. In Watch, subscribe to the event, call run.AddProgress / run.Complete, and return the
///      unsubscribe. Optionally override Describe to show progress.
/// TaskManager, TaskData and the tracker don't need to change.
/// </summary>
public abstract class TaskObjective : ScriptableObject
{
    [TextArea(1, 2), Tooltip("Shown in the task tracker, e.g. 'Talk to the baker'.")]
    public string description;

    /// <summary>Start listening; report through 'run'. Return an action that stops listening.</summary>
    public abstract Action Watch(ObjectiveRun run);

    /// <summary>Text for the tracker and Journal. 'progress' is the saved count (0 if not counting).</summary>
    public virtual string Describe(int progress) => description;
}

/// <summary>
/// One objective being worked on right now. TaskManager makes one per step and gives it to
/// TaskObjective.Watch; the objective reports progress and completion through it.
/// </summary>
public sealed class ObjectiveRun
{
    private readonly Action<int> _onProgress;
    private readonly Action _onComplete;

    /// <summary>The count so far (comes back from the save file after Continue).</summary>
    public int Progress { get; private set; }

    public bool IsComplete { get; private set; }

    public ObjectiveRun(int progress, Action<int> onProgress, Action onComplete)
    {
        Progress = progress;
        _onProgress = onProgress;
        _onComplete = onComplete;
    }

    /// <summary>One more (or 'amount' more). Ignored once complete.</summary>
    public void AddProgress(int amount = 1)
    {
        if (IsComplete)
            return;
        Progress += amount;
        _onProgress?.Invoke(Progress);
    }

    /// <summary>The step is done. Only the first call counts.</summary>
    public void Complete()
    {
        if (IsComplete)
            return;
        IsComplete = true;
        _onComplete?.Invoke();
    }
}
