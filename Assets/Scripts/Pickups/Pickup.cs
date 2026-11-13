using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base class for everything Pip can pick up. It handles what all pickups share:
///   - bobbing gently up and down,
///   - noticing when Pip touches the trigger collider,
///   - firing the pickup events (for sound and effects),
///   - disappearing once collected.
///
/// Each pickup type only writes TryApply(): what the pickup does, and whether it can be
/// collected right now (e.g. Cotton Candy stays in the world while Pip's stars are full).
///
/// Setup: a Collider2D with "Is Trigger" on, plus a child SpriteRenderer assigned as Visual,
/// so only the sprite bobs and the collider stays still.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public abstract class Pickup : MonoBehaviour
{
    [Header("Bobbing")]
    [SerializeField, Tooltip("What bobs (usually the sprite child). Leave empty to bob the whole object.")]
    private Transform visual;
    [SerializeField, Min(0f), Tooltip("How far up and down it bobs (tiles). 0.125 = 2 pixels.")]
    private float bobHeight = 0.125f;
    [SerializeField, Min(0f), Tooltip("Bobs per second.")]
    private float bobSpeed = 0.75f;

    [Header("Feedback")]
    [SerializeField, Tooltip("Runs when collected. Hook up particles or a one-shot sound here in the Inspector.")]
    private UnityEvent onCollected;

    /// <summary>
    /// Fired when ANY pickup is collected, with the pickup that was collected.
    /// One audio/effects manager can listen here instead of hooking up every pickup.
    /// </summary>
    public static event Action<Pickup> AnyCollected;

    private Vector3 _visualStart;
    private float _bobOffset;
    private bool _collected;

    // ------------------------------------------------------------------ For subclasses

    /// <summary>
    /// Applies the pickup's effect to Pip. Return true if it was used up,
    /// or false to leave it in the world (e.g. the player doesn't need it right now).
    /// </summary>
    protected abstract bool TryApply(PlayerController player);

    // ------------------------------------------------------------------ Unity lifecycle

    protected virtual void Reset()
    {
        // Runs when the component is first added: make the collider a trigger automatically.
        GetComponent<Collider2D>().isTrigger = true;
    }

    protected virtual void Awake()
    {
        if (visual == null)
            visual = transform;

        _visualStart = visual.localPosition;
        _bobOffset = UnityEngine.Random.value * 10f; // So neighbouring pickups don't bob in sync.
    }

    protected virtual void Update()
    {
        // Smooth up-and-down motion with a sine wave.
        float wave = Mathf.Sin((Time.time + _bobOffset) * bobSpeed * 2f * Mathf.PI);
        visual.localPosition = _visualStart + Vector3.up * (wave * bobHeight);
    }

    // Enter: the normal case. Stay: Pip was standing on a pickup she couldn't use yet
    // (e.g. full health) and now can, without having to step off and back on.
    private void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
    private void OnTriggerStay2D(Collider2D other) => TryCollect(other);

    // ------------------------------------------------------------------ Collecting

    private void TryCollect(Collider2D other)
    {
        if (_collected)
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || !TryApply(player))
            return;

        _collected = true;
        onCollected?.Invoke();
        AnyCollected?.Invoke(this);

        // Hide rather than Destroy: cheaper, and level/save code can re-enable it if needed.
        // Note: an AudioSource on this object would be cut off. Play sounds from a manager
        // (AnyCollected) or with AudioSource.PlayClipAtPoint.
        gameObject.SetActive(false);
    }
}
