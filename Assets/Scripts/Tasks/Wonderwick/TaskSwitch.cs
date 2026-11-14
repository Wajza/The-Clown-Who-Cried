using UnityEngine;

/// <summary>
/// A lever/switch for W3 "Runaway Coaster". Pip flips it by pressing Interact (F) next to it OR
/// by hitting it (mallet or coin: my teammate's attacks hit anything IDamageable). It can only be
/// flipped once; it sends the task signal (e.g. "w3_switch") and remembers that it's on in the
/// save (flag "switch_[Save Id]"), so it stays on after a reload.
///
/// Setup: a switch sprite (child "Sprite"), a trigger Collider2D (the F range, also what attacks
/// hit), this script, on the Enemy layer (the layer Pip's coins and mallet hit).
/// </summary>
public class TaskSwitch : Interactable, IDamageable
{
    [SerializeField] private string signalKey = "w3_switch";
    [SerializeField, Tooltip("Unique in the whole game, e.g. 'coaster_switch_1'.")]
    private string saveId;
    [SerializeField, Tooltip("Optional: only works while this task runs.")]
    private TaskData onlyWhileTaskActive;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Color offColor = new Color(0.8f, 0.3f, 0.3f);
    [SerializeField] private Color onColor = new Color(0.4f, 0.9f, 0.4f);

    public bool IsOn => GameManager.Instance != null && GameManager.Instance.Save.HasFlag(Flag);

    private string Flag => $"switch_{saveId}";

    private bool TaskAllows =>
        onlyWhileTaskActive == null || (TaskManager.Instance != null && TaskManager.Instance.IsActive(onlyWhileTaskActive));

    public override bool CanInteract => !IsOn && TaskAllows;

    public override void Interact() => Flip();

    public void TakeDamage(DamageInfo info) => Flip();

    private void Flip()
    {
        if (IsOn || !TaskAllows)
            return;
        GameManager.Instance.Save.SetFlag(Flag);
        ShowState();
        GameEvents.RaiseTaskSignal(signalKey);
    }

    private void Start() => ShowState();

    private void ShowState()
    {
        if (sprite != null)
            sprite.color = IsOn ? onColor : offColor;
    }
}
