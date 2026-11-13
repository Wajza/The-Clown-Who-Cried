using System;
using UnityEngine;

/// <summary>
/// One step of a task, e.g. "Talk to the baker". It is "abstract": you never create a
/// TaskObjective itself, only a kind built on it. Today there is one kind, TalkToNpcObjective.
///
/// HOW AN OBJECTIVE WORKS
/// TaskManager calls Watch(onDone) when Pip reaches this step. The objective starts listening
/// for whatever it needs (a GameEvents event) and calls onDone when it happens. Watch returns
/// a "stop watching" action that TaskManager calls when the step is over or the game resets.
///
/// The objective asset itself stores NO progress (no "isDone" field). Assets are shared and
/// Unity can keep their values between Play sessions in the Editor, so progress lives in the
/// save file instead (TaskManager handles that).
///
/// ADDING A NEW KIND LATER (e.g. "collect 3 balloons")
///   1. Make a class built on TaskObjective with a [CreateAssetMenu] attribute.
///   2. Add its settings as public fields.
///   3. In Watch, subscribe to the event, call onDone when it's met, and return the unsubscribe.
/// TaskManager, TaskData and the tracker don't need to change.
/// </summary>
public abstract class TaskObjective : ScriptableObject
{
    [TextArea(1, 2), Tooltip("Shown in the task tracker, e.g. 'Talk to the baker'.")]
    public string description;

    /// <summary>
    /// Start listening. Call onDone once the objective is met. Return an action that stops
    /// listening (TaskManager always calls it). Don't call onDone from inside Watch itself.
    /// </summary>
    public abstract Action Watch(Action onDone);
}
