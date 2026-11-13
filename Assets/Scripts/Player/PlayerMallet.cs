using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Which way a mallet swing goes.</summary>
public enum SwingDirection { Forward, Up, Down }

/// <summary>
/// Pip's Squeak Mallet attack.
///
/// A swing has three phases:
///   Windup   – short delay before the hit (time for the animation to wind up).
///   Active   – the hitbox is live; each target can be hit once per swing.
///   Recovery – the swing finishes; Pip can't swing again until it ends.
///
/// Direction is chosen when the swing starts, from the Move input:
///   holding up = Up swing, holding down while in the air = Down swing, otherwise Forward.
/// A Down swing that hits an enemy (a target on a Pogo Layer) or anything tagged "Pogo"
/// bounces Pip upward.
///
/// Hit detection uses Physics2D.OverlapBox, so the hitboxes are just numbers (offset + size)
/// and are drawn as gizmos in the Scene view when Pip is selected.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerMallet : MonoBehaviour
{
    /// <summary>A box relative to Pip's pivot (the feet). Forward offsets are for facing right.</summary>
    [Serializable]
    private struct Hitbox
    {
        [Tooltip("Center of the box relative to Pip's feet (tiles). X is mirrored when facing left.")]
        public Vector2 offset;
        [Tooltip("Width and height of the box (tiles).")]
        public Vector2 size;

        public Hitbox(Vector2 offset, Vector2 size)
        {
            this.offset = offset;
            this.size = size;
        }
    }

    // ------------------------------------------------------------------ Inspector fields

    [Header("Damage")]
    [SerializeField, Min(0), Tooltip("Damage per hit.")]
    private int damage = 3;
    [SerializeField, Min(0f), Tooltip("Knockback speed given to enemies (tiles per second).")]
    private float knockbackForce = 8f;
    [SerializeField, Tooltip("Layers the mallet can hit (enemies, breakables, hazards). Don't include Pip's layer.")]
    private LayerMask hitLayers;

    [Header("Timing")]
    [SerializeField, Min(0f), Tooltip("Seconds from pressing Swing until the hitbox turns on.")]
    private float windupTime = 0.05f;
    [SerializeField, Min(0.01f), Tooltip("Seconds the hitbox stays on.")]
    private float activeTime = 0.1f;
    [SerializeField, Min(0f), Tooltip("Seconds after the hitbox turns off before Pip can swing again.")]
    private float recoveryTime = 0.15f;
    [SerializeField, Min(0f), Tooltip("Seconds a Swing press is remembered while the previous swing finishes.")]
    private float swingBufferTime = 0.1f;
    [SerializeField, Range(0.1f, 0.9f), Tooltip("How far up/down the stick must be held to aim an up/down swing.")]
    private float aimThreshold = 0.5f;

    [Header("Hitboxes")]
    [SerializeField] private Hitbox forwardHitbox = new Hitbox(new Vector2(1.0f, 0.75f), new Vector2(1.5f, 1.0f));
    [SerializeField] private Hitbox upHitbox = new Hitbox(new Vector2(0f, 2.0f), new Vector2(1.25f, 1.25f));
    [SerializeField] private Hitbox downHitbox = new Hitbox(new Vector2(0f, -0.5f), new Vector2(1.25f, 1.25f));

    [Header("Pogo")]
    [SerializeField, Min(0.01f), Tooltip("Height of the pogo bounce in tiles.")]
    private float pogoHeight = 2f;
    [SerializeField, Tooltip("Damageable targets on these layers can be pogoed off (e.g. Enemy). Pots on other layers can't.")]
    private LayerMask pogoLayers;
    [SerializeField, Tooltip("Colliders with any of these tags can always be pogoed off. Each tag must exist in Tags and Layers.")]
    private string[] pogoTags = { "Pogo", "Spikes" };

    // ------------------------------------------------------------------ Public API

    /// <summary>True from the start of a swing until its recovery ends.</summary>
    public bool IsSwinging { get; private set; }

    /// <summary>Direction of the current (or last) swing.</summary>
    public SwingDirection CurrentDirection { get; private set; }

    /// <summary>Fired when a swing starts. Use it to play the right swing animation.</summary>
    public event Action<SwingDirection> SwingStarted;

    /// <summary>Fired for every target the mallet damages (hit sparks, squeak sound, hit-stop).</summary>
    public event Action<IDamageable, DamageInfo> HitLanded;

    /// <summary>Fired when a down-swing bounces Pip upward.</summary>
    public event Action Pogoed;

    // ------------------------------------------------------------------ Private state

    private PlayerController _controller;
    private InputAction _moveAction;
    private InputAction _swingAction;

    private float _lastSwingPressTime = float.NegativeInfinity;
    private float _swingStartTime;
    private int _swingFacing = 1;         // Facing locked at swing start, so turning mid-swing doesn't move the hitbox.
    private bool _pogoUsedThisSwing;

    // Reused every swing to avoid creating garbage.
    private readonly HashSet<IDamageable> _hitThisSwing = new HashSet<IDamageable>();
    private readonly List<Collider2D> _overlaps = new List<Collider2D>();
    private ContactFilter2D _filter;

    private float SwingTotalTime => windupTime + activeTime + recoveryTime;

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        // Use the same Input Actions asset as the PlayerController (it enables the map).
        InputActionMap map = _controller.InputActions.FindActionMap("Player", true);
        _moveAction = map.FindAction("Move", true);
        _swingAction = map.FindAction("Swing", true);
        _swingAction.performed += OnSwingPressed;

        _controller.DashingChanged += OnDashingChanged;
    }

    private void OnDisable()
    {
        if (_swingAction != null)
            _swingAction.performed -= OnSwingPressed;
        _controller.DashingChanged -= OnDashingChanged;
    }

    private void FixedUpdate()
    {
        // Rebuilt each step so changing Hit Layers in the Inspector works during Play mode.
        _filter = new ContactFilter2D { useTriggers = true };
        _filter.SetLayerMask(hitLayers);

        if (!IsSwinging)
        {
            bool swingBuffered = Time.time - _lastSwingPressTime <= swingBufferTime;
            if (swingBuffered && !_controller.IsDashing)
                StartSwing();
            return;
        }

        float elapsed = Time.time - _swingStartTime;

        if (elapsed >= windupTime && elapsed < windupTime + activeTime)
            CheckHits();

        if (elapsed >= SwingTotalTime)
            IsSwinging = false;
    }

    // ------------------------------------------------------------------ Input / events

    private void OnSwingPressed(InputAction.CallbackContext ctx)
    {
        _lastSwingPressTime = Time.time; // Handled (and buffered) in FixedUpdate.
    }

    private void OnDashingChanged(bool dashing)
    {
        // Dashing cancels a swing, so a dash always feels instant.
        if (dashing)
            IsSwinging = false;
    }

    // ------------------------------------------------------------------ Swing

    private void StartSwing()
    {
        _lastSwingPressTime = float.NegativeInfinity; // Use up the press.

        CurrentDirection = ChooseDirection();
        _swingFacing = _controller.FacingDirection;
        _swingStartTime = Time.time;
        _pogoUsedThisSwing = false;
        _hitThisSwing.Clear();
        IsSwinging = true;

        SwingStarted?.Invoke(CurrentDirection);
    }

    private SwingDirection ChooseDirection()
    {
        float aimY = _moveAction.ReadValue<Vector2>().y;

        if (aimY > aimThreshold)
            return SwingDirection.Up;

        // Like most pogo games, the down swing only exists in the air.
        if (aimY < -aimThreshold && !_controller.IsGrounded)
            return SwingDirection.Down;

        return SwingDirection.Forward;
    }

    private void CheckHits()
    {
        GetHitboxWorld(CurrentDirection, _swingFacing, out Vector2 center, out Vector2 size);
        Physics2D.OverlapBox(center, size, 0f, _filter, _overlaps);

        foreach (Collider2D col in _overlaps)
        {
            if (col.transform.IsChildOf(transform))
                continue; // Never hit Pip.

            // Damage: each IDamageable is hit at most once per swing,
            // even if it has several colliders or stays in the box for several steps.
            IDamageable target = col.GetComponentInParent<IDamageable>();
            if (target != null && _hitThisSwing.Add(target))
            {
                var info = new DamageInfo(damage, GetKnockback(), col.ClosestPoint(center), gameObject);
                target.TakeDamage(info);
                HitLanded?.Invoke(target, info);
            }

            if (CurrentDirection == SwingDirection.Down && !_pogoUsedThisSwing && CanPogoOff(col, target))
            {
                _pogoUsedThisSwing = true;
                _controller.Bounce(pogoHeight);
                Pogoed?.Invoke();
            }
        }
    }

    private bool CanPogoOff(Collider2D col, IDamageable target)
    {
        foreach (string tag in pogoTags)
        {
            if (!string.IsNullOrEmpty(tag) && col.CompareTag(tag))
                return true;
        }

        // Damageable things on a pogo layer (enemies) can be bounced on; pots usually can't.
        bool onPogoLayer = (pogoLayers.value & (1 << col.gameObject.layer)) != 0;
        return target != null && onPogoLayer;
    }

    private Vector2 GetKnockback()
    {
        // Push targets away from Pip in the swing direction.
        Vector2 direction = CurrentDirection switch
        {
            SwingDirection.Up => Vector2.up,
            SwingDirection.Down => Vector2.down,
            _ => new Vector2(_swingFacing, 0f),
        };
        return direction * knockbackForce;
    }

    /// <summary>Converts a hitbox's local offset into a world-space center, mirrored for facing.</summary>
    private void GetHitboxWorld(SwingDirection direction, int facing, out Vector2 center, out Vector2 size)
    {
        Hitbox box = direction switch
        {
            SwingDirection.Up => upHitbox,
            SwingDirection.Down => downHitbox,
            _ => forwardHitbox,
        };

        Vector2 offset = new Vector2(box.offset.x * facing, box.offset.y);
        center = (Vector2)transform.position + offset;
        size = box.size;
    }

    // ------------------------------------------------------------------ Editor helpers

    private void OnDrawGizmosSelected()
    {
        // Draws all three hitboxes. The active one is red; the others are faint.
        int facing = Application.isPlaying && _controller != null ? _controller.FacingDirection : 1;

        foreach (SwingDirection dir in (SwingDirection[])Enum.GetValues(typeof(SwingDirection)))
        {
            bool active = Application.isPlaying && IsSwinging && dir == CurrentDirection;
            Gizmos.color = active ? Color.red : new Color(1f, 0.5f, 0f, 0.35f);
            GetHitboxWorld(dir, active ? _swingFacing : facing, out Vector2 center, out Vector2 size);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
