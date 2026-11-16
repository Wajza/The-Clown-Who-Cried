using UnityEngine;

/// <summary>
/// Lets an NPC hand out a task. Put it next to the NPC's NpcTalk.
///
/// HOW IT WORKS
/// When a conversation with this NPC finishes (GameEvents.NpcTalkedTo with their Npc Id),
/// the task starts, as long as:
///   - it isn't running or finished already, and
///   - "Required Flag" is set (if you filled it in).
///
/// Required Flag lets the player say yes or no. Example for the baker:
///   Default Dialogue "Dialogue_Baker_Ask" ends with the choices
///       "I'll help!"  (Set Flag: accepted_bakery)      "Not now."  (no flag)
///   TaskGiver: Task = Task_Bakery, Required Flag = accepted_bakery
/// Pick "Not now" and nothing happens; talk again and pick "I'll help!" and the task starts.
///
/// Use flags in the NPC's Conditional Dialogues so they say the right thing afterwards:
///   Required "task_bakery_done"    -> "Thank you so much!"
///   Required "task_bakery_active"  -> "Did you find out why my cake won't rise?"
///   Default                        -> the asking conversation above
///
/// NOTE: the conversation that GIVES a task never counts towards that task's own "talk to"
/// objectives, even if it's the same NPC. The task starts while that conversation's
/// NpcTalkedTo event is being sent, and C# only sends an event to listeners that were
/// already attached when it was raised. So "talk to the baker" needs a NEW talk with the baker.
/// </summary>
[RequireComponent(typeof(NpcTalk))]
public class TaskGiver : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector fields

    [SerializeField, Tooltip("The task this NPC gives.")]
    private TaskData task;

    [SerializeField, Tooltip("Optional: only give the task if this flag is set (e.g. set by the 'Yes' answer). Empty = after any conversation.")]
    private string requiredFlag;

    // ------------------------------------------------------------------ Public API

    public TaskData Task => task;

    /// <summary>True if talking to this NPC right now would start the task (ignoring the flag).</summary>
    public bool HasTaskToGive =>
        task != null && TaskManager.Instance != null &&
        !TaskManager.Instance.IsActive(task) && !TaskManager.IsCompleted(task);

    // ------------------------------------------------------------------ Private state

    private NpcTalk _npc;

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake() => _npc = GetComponent<NpcTalk>();

    private void OnEnable() => GameEvents.NpcTalkedTo += OnNpcTalkedTo;
    private void OnDisable() => GameEvents.NpcTalkedTo -= OnNpcTalkedTo;

    // ------------------------------------------------------------------ Event handlers

    private void OnNpcTalkedTo(string npcId)
    {
        if (npcId != _npc.NpcId || !HasTaskToGive)
            return;

        if (!string.IsNullOrEmpty(requiredFlag) && !GameManager.Instance.Save.HasFlag(requiredFlag))
            return; // The player said no (or hasn't been asked yet).

        TaskManager.Instance.StartTask(task);
    }
}
