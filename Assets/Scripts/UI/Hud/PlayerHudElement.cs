using UnityEngine;

/// <summary>
/// Base class for HUD pieces that show something about Pip (stars, tears, coins...).
///
/// THE PROBLEM IT SOLVES
/// The HUD lives in the GameSystems prefab and survives scene changes, but every level has its
/// own Pip. So each HUD piece must stop listening to the old Pip's events and start listening
/// to the new one's.
///
/// HOW
/// Every frame it compares PlayerController.Current with the Pip it's linked to. When they
/// differ, it calls Unbind() (old Pip) and Bind() (new Pip, or none on the title screen).
/// Bind should subscribe to my teammate's events AND read the current values straight away,
/// because their scripts announce their starting values before the HUD is linked.
///
/// A piece hides its "Content" object while there's no Pip, or when Bind says so (e.g. Pip has
/// no Blue Sight component).
/// </summary>
public abstract class PlayerHudElement : MonoBehaviour
{
    [SerializeField, Tooltip("What to show/hide. Usually the visible child holding the pictures.")]
    private GameObject content;

    /// <summary>True while the piece is showing its contents.</summary>
    public bool ContentVisible => content != null && content.activeSelf;

    /// <summary>The Pip this piece is linked to (null = none).</summary>
    protected PlayerController Pip { get; private set; }

    private bool _bound;

    /// <summary>Subscribe to Pip's events and show the current values. Return false to hide this piece.</summary>
    protected abstract bool Bind(PlayerController pip);

    /// <summary>Unsubscribe from everything Bind subscribed to.</summary>
    protected abstract void Unbind();

    protected virtual void OnEnable() => Relink();

    protected virtual void Update()
    {
        if (PlayerController.Current != Pip)
            Relink();
    }

    protected virtual void OnDisable()
    {
        if (_bound)
            Unbind();
        _bound = false;
        Pip = null;
    }

    /// <summary>Unlink and link again (e.g. when what decides "show or hide" changed).</summary>
    protected void Relink()
    {
        if (_bound)
            Unbind();

        Pip = PlayerController.Current;
        bool show = Pip != null && Bind(Pip);
        _bound = Pip != null;

        if (content != null)
            content.SetActive(show);
    }
}
