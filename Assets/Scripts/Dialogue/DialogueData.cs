using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One conversation, written in the Inspector.
///
/// HOW A CONVERSATION PLAYS
///   1. Every line is shown in order (speaker + text, typed out letter by letter).
///   2. If there are Choices, they appear under the last line. The player picks one:
///        - its "Set Flag" is set (e.g. "agreed_to_help"),
///        - its "Next" conversation (if any) plays straight after.
///   3. "Set Flag When Finished" is set once this conversation's lines are done.
///
/// Flags are saved in GameManager.Save. NPCs (NpcTalk) use them to pick what to say next time,
/// and tasks/cutscenes use them in later parts.
///
/// Create one: right-click in the Project window > Create > Clown Who Cried > Dialogue > Dialogue.
///
/// TIP: TextMeshPro rich text works in lines, e.g. "That's <color=#7fb2ff>sad</color>!"
/// </summary>
[CreateAssetMenu(menuName = "Clown Who Cried/Dialogue/Dialogue", fileName = "Dialogue_")]
public class DialogueData : ScriptableObject
{
    /// <summary>One thing someone says.</summary>
    [Serializable]
    public class Line
    {
        [Tooltip("Who says it. Leave empty for narration (no name or portrait).")]
        public DialogueSpeaker speaker;

        [TextArea(2, 4), Tooltip("What they say. Keep it to about 3 short lines so it fits the box.")]
        public string text;
    }

    /// <summary>An answer the player can pick after the last line.</summary>
    [Serializable]
    public class Choice
    {
        [Tooltip("The answer shown on screen.")]
        public string text;

        [Tooltip("Optional flag set when this answer is picked.")]
        public string setFlag;

        [Tooltip("Optional conversation that plays straight after this answer.")]
        public DialogueData next;
    }

    [Tooltip("The lines, in order.")]
    public List<Line> lines = new List<Line>();

    [Tooltip("Optional answers shown after the last line. Leave empty for no choice.")]
    public List<Choice> choices = new List<Choice>();

    [Tooltip("Optional flag set when this conversation's lines are finished (e.g. 'met_mayor').")]
    public string setFlagWhenFinished;

    public bool HasChoices => choices != null && choices.Count > 0;
}
