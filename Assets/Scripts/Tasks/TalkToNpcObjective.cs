using System;
using UnityEngine;

/// <summary>
/// Objective: "talk to this NPC". Done when a conversation with the NPC whose
/// NpcTalk "Npc Id" matches finishes (GameEvents.NpcTalkedTo, raised by NpcTalk).
///
/// It counts ANY finished conversation with that NPC. To make the NPC say something
/// task-specific, give their NpcTalk a Conditional Dialogue with Required Flag
/// "task_[taskId]_active" (TaskManager sets that flag while the task runs).
///
/// Create one: Create > Clown Who Cried > Tasks > Objective - Talk To NPC.
/// </summary>
[CreateAssetMenu(menuName = "Clown Who Cried/Tasks/Objective - Talk To NPC", fileName = "Objective_Talk_")]
public class TalkToNpcObjective : TaskObjective
{
    [Tooltip("The NPC's 'Npc Id' from their NpcTalk component, e.g. 'baker'.")]
    public string npcId;

    public override Action Watch(ObjectiveRun run)
    {
        // A small local method that remembers 'run'. It's added to the event now and
        // removed again by the action we return.
        void OnNpcTalkedTo(string talkedTo)
        {
            if (talkedTo == npcId)
                run.Complete();
        }

        GameEvents.NpcTalkedTo += OnNpcTalkedTo;
        return () => GameEvents.NpcTalkedTo -= OnNpcTalkedTo;
    }
}
