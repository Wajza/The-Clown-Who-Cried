using System.Collections;
using UnityEngine;

/// <summary>
/// A REAL mirror in W2 "Crooked Mirror Maze": press Interact (F) and Pip steps through it and
/// comes out of its partner mirror (the screen fades out and back in).
///
/// Fake mirrors are not this script: they're an Inspectable ("It's just a painting...") with a
/// crack that only shows in Blue Sight (my teammate's BlueSightOnly on a child sprite).
///
/// Setup: a mirror sprite, a trigger Collider2D as the "use" range, this script, and the
/// partner mirror in "Exit". Usually pair them both ways (A -> B and B -> A).
/// Pip arrives standing at the partner mirror's position (its pivot should be on the floor).
/// </summary>
public class MirrorTeleporter : Interactable
{
    [SerializeField, Tooltip("The mirror Pip comes out of.")]
    private MirrorTeleporter exit;

    [SerializeField, Min(0f)] private float fadeSeconds = 0.25f;

    public MirrorTeleporter Exit => exit;

    /// <summary>True from pressing F until Pip is out of the other side.</summary>
    public bool IsTravelling { get; private set; }

    public override bool CanInteract => exit != null && !IsTravelling;

    public override void Interact()
    {
        if (CanInteract)
            StartCoroutine(Travel());
    }

    private IEnumerator Travel()
    {
        IsTravelling = true;
        PlayerInputLock.Lock(this);

        ScreenFader fader = ScreenFader.Instance;
        if (fader != null)
            yield return fader.FadeOut(fadeSeconds);

        PlayerController pip = PlayerController.Current;
        if (pip != null)
            pip.Teleport(exit.transform.position, 0f); // My teammate's method.

        if (fader != null)
            yield return fader.FadeIn(fadeSeconds);

        PlayerInputLock.Unlock(this);
        IsTravelling = false;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (IsTravelling) // Scene changed mid-travel: never leave Pip locked.
        {
            PlayerInputLock.Unlock(this);
            IsTravelling = false;
        }
    }
}
