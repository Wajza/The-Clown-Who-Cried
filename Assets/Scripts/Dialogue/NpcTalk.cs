using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes a character talk when Pip presses Interact (F) next to them.
///
/// WHICH CONVERSATION?
/// The NPC can say different things depending on story flags (from GameManager.Save):
///   - "Conditional Dialogues" are checked top to bottom. The first one whose conditions match
///     is used. Put the most specific ones at the top.
///       Required Flag:   only if this flag IS set   (empty = no requirement)
///       Blocked By Flag: only if this flag is NOT set (empty = no restriction)
///   - If none match, "Default Dialogue" is used.
///
/// Example for the Mayor:
///   1. Required "tumbleton_tasks_done"  -> "Thank you, Pip! The train is waiting."
///   2. Required "met_mayor"             -> "Have you helped the townsfolk yet?"
///   Default                             -> first meeting (its Set Flag When Finished = "met_mayor")
///
/// When a conversation finishes, GameEvents.NpcTalkedTo(npcId) is raised. "Talk to an NPC"
/// tasks (Part 3) listen for that.
///
/// Setup: a GameObject with the NPC sprite, a trigger Collider2D for the talk range (e.g. a box
/// 3 tiles wide), this script, and optionally a child "!" bubble as the Prompt.
/// </summary>
public class NpcTalk : Interactable
{
    /// <summary>A conversation plus the flags that decide when it's used.</summary>
    [Serializable]
    public class ConditionalDialogue
    {
        [Tooltip("Use this only if this flag IS set. Empty = no requirement.")]
        public string requiredFlag;

        [Tooltip("Use this only if this flag is NOT set. Empty = no restriction.")]
        public string blockedByFlag;

        public DialogueData dialogue;
    }

    // ------------------------------------------------------------------ Inspector fields

    [SerializeField, Tooltip("Unique name for this character, used by tasks, e.g. 'mayor'.")]
    private string npcId;

    [SerializeField, Tooltip("Said when no conditional dialogue matches.")]
    private DialogueData defaultDialogue;

    [SerializeField, Tooltip("Checked top to bottom; the first match wins.")]
    private List<ConditionalDialogue> conditionalDialogues = new List<ConditionalDialogue>();

    [Header("Facing")]
    [SerializeField, Tooltip("Optional: sprite flipped to face Pip when the conversation starts.")]
    private SpriteRenderer spriteToFlip;
    [SerializeField, Tooltip("Tick if the NPC art faces right.")]
    private bool spriteFacesRight = true;

    // ------------------------------------------------------------------ Public API

    public string NpcId => npcId;

    /// <summary>Only talkable when there's something to say and no conversation is running.</summary>
    public override bool CanInteract =>
        PickDialogue() != null && DialogueRunner.Instance != null && !DialogueRunner.Instance.IsRunning;

    /// <summary>The conversation this NPC would use right now (see the class comment).</summary>
    public DialogueData PickDialogue()
    {
        SaveData save = GameManager.Instance != null ? GameManager.Instance.Save : null;

        foreach (ConditionalDialogue option in conditionalDialogues)
        {
            if (option.dialogue == null)
                continue;
            if (!string.IsNullOrEmpty(option.requiredFlag) && (save == null || !save.HasFlag(option.requiredFlag)))
                continue;
            if (!string.IsNullOrEmpty(option.blockedByFlag) && save != null && save.HasFlag(option.blockedByFlag))
                continue;
            return option.dialogue;
        }
        return defaultDialogue;
    }

    public override void Interact()
    {
        DialogueData dialogue = PickDialogue();
        if (dialogue == null || DialogueRunner.Instance == null)
            return;

        FacePip();
        DialogueRunner.Instance.StartDialogue(dialogue, onFinished: () => GameEvents.RaiseNpcTalkedTo(npcId));
    }

    // ------------------------------------------------------------------ Helpers

    private void FacePip()
    {
        PlayerController pip = PlayerController.Current;
        if (spriteToFlip == null || pip == null)
            return;

        bool pipIsToTheRight = pip.transform.position.x > transform.position.x;
        // Flip when the art faces the wrong way for where Pip is standing.
        spriteToFlip.flipX = pipIsToTheRight != spriteFacesRight;
    }
}
