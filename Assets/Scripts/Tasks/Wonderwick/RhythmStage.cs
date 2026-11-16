using UnityEngine;

/// <summary>
/// The audition stage in W4: press Interact (F) on it to start the rhythm mini-game, but only
/// while the W4 task is running and it isn't already playing.
///
/// Setup: a stage sprite, a trigger Collider2D as the "use" range, this script, the
/// RhythmMinigame in the scene, and the W4 task.
/// </summary>
public class RhythmStage : Interactable
{
    [SerializeField] private RhythmMinigame minigame;
    [SerializeField] private TaskData task;

    public override bool CanInteract =>
        minigame != null && !minigame.IsPlaying &&
        TaskManager.Instance != null && TaskManager.Instance.IsActive(task);

    public override void Interact() => minigame.Begin();
}
