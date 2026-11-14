using UnityEngine;

/// <summary>
/// A box of punchlines for W1 "Punchline Delivery". Press Interact (F) next to it to pick it up;
/// Pip carries it above her head until she walks into a DeliveryStall.
///
/// WHILE CARRYING
///   - Pip can't swing, throw coins or dash (PlayerAbilities.Block), only walk and jump.
///   - Only one box at a time (Carried is the box in Pip's hands, or null).
///
/// THE BOX GOES BACK TO ITS STARTING SPOT when:
///   - the task fails (time ran out) or starts again,
///   - Pip respawns after falling/dying while carrying it,
///   - the scene changes while carrying it.
///
/// Setup: a GameObject with a box sprite child and a trigger Collider2D as the pick-up range
/// (about 2 x 2 tiles), this script, and the W1 task in "Only While Task Active".
/// </summary>
public class CarryableBox : Interactable
{
    [SerializeField, Tooltip("The box can only be picked up while this task runs.")]
    private TaskData onlyWhileTaskActive;

    [SerializeField, Tooltip("Where the box sits relative to Pip's feet while carried.")]
    private Vector2 carryOffset = new Vector2(0f, 1.6f);

    // ------------------------------------------------------------------ Public API

    /// <summary>The box Pip is carrying right now, or null.</summary>
    public static CarryableBox Carried { get; private set; }

    public bool IsCarried => Carried == this;

    public override bool CanInteract =>
        Carried == null && gameObject.activeInHierarchy &&
        (onlyWhileTaskActive == null || (TaskManager.Instance != null && TaskManager.Instance.IsActive(onlyWhileTaskActive)));

    /// <summary>Pick up.</summary>
    public override void Interact()
    {
        PlayerController pip = PlayerController.Current;
        if (pip == null || !CanInteract)
            return;

        Carried = this;
        _range.enabled = false; // Not "in range" of itself while carried.
        transform.SetParent(pip.transform, worldPositionStays: false);
        transform.localPosition = carryOffset;
        PlayerAbilities.Block(this, Ability.Mallet, Ability.Coins, Ability.Dash);
    }

    /// <summary>Called by a DeliveryStall: the box is handed over and disappears.</summary>
    public void Deliver()
    {
        Release();
        gameObject.SetActive(false);
    }

    /// <summary>Back to where it started (and visible again).</summary>
    public void ReturnHome()
    {
        Release();
        gameObject.SetActive(true);
        _range.enabled = true;
    }

    // ------------------------------------------------------------------ Private state

    private Transform _homeParent;
    private Vector3 _homePosition;
    private Collider2D _range;

    // ------------------------------------------------------------------ Unity lifecycle

    protected override void Awake()
    {
        base.Awake();
        _range = GetComponent<Collider2D>();
        _homeParent = transform.parent;
        _homePosition = transform.position;

        // These events must work even while the box is switched off (delivered), so they're
        // subscribed here for the box's whole life instead of in OnEnable.
        GameEvents.TaskStarted += OnTaskReset;
        GameEvents.TaskFailed += OnTaskReset;
        GameEvents.PlayerRespawned += OnPlayerRespawned;
        GameEvents.SceneLoadStarted += OnSceneLoadStarted;
    }

    private void OnDestroy()
    {
        GameEvents.TaskStarted -= OnTaskReset;
        GameEvents.TaskFailed -= OnTaskReset;
        GameEvents.PlayerRespawned -= OnPlayerRespawned;
        GameEvents.SceneLoadStarted -= OnSceneLoadStarted;
        if (IsCarried)
            Release();
    }

    // ------------------------------------------------------------------ Event handlers

    private void OnTaskReset(TaskData task)
    {
        if (task == onlyWhileTaskActive)
            ReturnHome();
    }

    private void OnPlayerRespawned()
    {
        if (IsCarried)
            ReturnHome();
    }

    // Leaving the level with a box: drop the block, the box stays behind (scene unloads).
    private void OnSceneLoadStarted(string sceneName)
    {
        if (IsCarried)
            Release();
    }

    private void Release()
    {
        if (IsCarried)
        {
            Carried = null;
            PlayerAbilities.Unblock(this);
        }
        if (transform.parent != _homeParent)
            transform.SetParent(_homeParent, worldPositionStays: true);
        transform.position = _homePosition;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Carried = null;
}
