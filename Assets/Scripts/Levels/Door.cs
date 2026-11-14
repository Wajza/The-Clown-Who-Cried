using UnityEngine;

/// <summary>
/// A door (house, shop, station, the Laugh Tower...) that Pip goes through by pressing
/// Interact (F) next to it. It uses the same "talk range + prompt" system as NPCs (Interactable).
///
///   Requirement met     -> the Destination loads.
///   Requirement not met -> the Locked Dialogue plays (e.g. "It's locked.").
///
/// Setup: a GameObject with the door sprite, a trigger BoxCollider2D around the doorway, this
/// script, and optionally a child prompt (e.g. an arrow) assigned as Prompt.
/// </summary>
public class Door : Interactable
{
    [SerializeField] private ExitDestination destination = new ExitDestination();
    [SerializeField] private ExitRequirement requirement = new ExitRequirement();

    public ExitDestination Destination => destination;
    public ExitRequirement Requirement => requirement;

    /// <summary>A locked door can still be "used", so the player hears why it's locked.</summary>
    public override bool CanInteract =>
        requirement.IsMet || requirement.lockedDialogue != null;

    public override void Interact()
    {
        if (requirement.IsMet)
            destination.Go();
        else
            requirement.SayLocked();
    }
}
