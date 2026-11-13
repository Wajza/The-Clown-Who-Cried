using UnityEngine;

/// <summary>
/// Decides WHICH interactable Pip would use, shows its prompt, and uses it when Interact (F)
/// is pressed.
///
/// Every frame:
///   1. If Pip can't act right now (locked, dead, not Playing), there is no target.
///   2. Otherwise the target is the closest object in Interactable.InRange that CanInteract.
///   3. Only the target's prompt is shown.
///   4. Pressing Interact calls the target's Interact().
///
/// Setup: on the GameSystems prefab. Nothing to assign.
/// </summary>
public class InteractionInput : MonoBehaviour
{
    // ------------------------------------------------------------------ Public API

    public static InteractionInput Instance { get; private set; }

    /// <summary>The interactable that Interact would use right now, or null.</summary>
    public Interactable Target { get; private set; }

    /// <summary>True when Pip is free to interact: playing, alive and not locked.</summary>
    public static bool PlayerCanInteract
    {
        get
        {
            PlayerController player = PlayerController.Current;
            if (player == null || PlayerInputLock.IsLocked)
                return false;

            GameManager gm = GameManager.Instance;
            if (gm != null && gm.State != GameState.Playing)
                return false;

            PlayerHealth health = player.GetComponent<PlayerHealth>();
            return health == null || !health.IsDead;
        }
    }

    /// <summary>Uses the current target. Returns false if there is none. (Tests call this too.)</summary>
    public bool TryInteract()
    {
        UpdateTarget(); // Make sure the target is up to date this frame.
        if (Target == null)
            return false;

        Interactable used = Target;
        SetTarget(null); // Hide the prompt; it comes back when Pip is free again.
        used.Interact();
        return true;
    }

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        UpdateTarget();

        if (Target != null && SystemInput.WasPressed(SystemInput.Interact))
            TryInteract();
    }

    // ------------------------------------------------------------------ Choosing the target

    private void UpdateTarget()
    {
        SetTarget(PlayerCanInteract ? FindClosest() : null);
    }

    private static Interactable FindClosest()
    {
        Vector2 pip = PlayerController.Current.transform.position;
        Interactable best = null;
        float bestDistance = float.MaxValue;

        foreach (Interactable candidate in Interactable.InRange)
        {
            if (candidate == null || !candidate.CanInteract)
                continue;

            float distance = Vector2.Distance(pip, candidate.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }
        return best;
    }

    private void SetTarget(Interactable newTarget)
    {
        if (newTarget == Target)
            return;

        // "!= null" (not "?.") because Unity objects destroyed with their scene only look
        // null to Unity's own == check.
        if (Target != null)
            Target.SetPromptVisible(false);

        Target = newTarget;

        if (Target != null)
            Target.SetPromptVisible(true);
    }
}
