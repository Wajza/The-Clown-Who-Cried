using UnityEngine;

/// <summary>
/// Something Pip can look at or use with Interact (F): a mirror, a sign, a poster...
/// It plays a conversation (usually Pip thinking out loud, or narration) and, when that's
/// finished, sends a task signal (e.g. "p1_mirror") that a SignalObjective can count.
///
/// Use the conversation's "Set Flag When Finished" if a door or NPC should react afterwards
/// (e.g. the front door opens once "p1_looked_in_mirror" is set).
///
/// Setup: like an NPC: a sprite, a trigger Collider2D as the "use" range, this script, and
/// optionally a child prompt.
/// </summary>
public class Inspectable : Interactable
{
    [SerializeField, Tooltip("What Pip thinks/sees when she looks at it.")]
    private DialogueData dialogue;

    [SerializeField, Tooltip("Optional task signal sent after the conversation, e.g. 'p1_mirror'.")]
    private string signalKey;

    public override bool CanInteract =>
        dialogue != null && DialogueRunner.Instance != null && !DialogueRunner.Instance.IsRunning;

    public override void Interact()
    {
        DialogueRunner.Instance.StartDialogue(dialogue, onFinished: () =>
        {
            if (!string.IsNullOrEmpty(signalKey))
                GameEvents.RaiseTaskSignal(signalKey);
        });
    }
}
