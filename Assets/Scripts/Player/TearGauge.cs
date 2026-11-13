using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Pip's Tear Gauge (0 to 100) and the Tear Burst special.
///
/// Filling: +20 when Pip loses a star (PlayerHealth.StarLost), +5 per mallet hit (PlayerMallet.HitLanded).
/// When full, the Tear Burst action releases a ring around Pip that:
///   - stuns every IStunnable in range for stunDuration seconds,
///   - deals bossDamage to anything tagged "Boss",
///   - then empties the gauge.
/// The ring's effect is instant; TearBurstReleased tells the VFX how big to draw it.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class TearGauge : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector fields

    [Header("Gauge")]
    [SerializeField, Min(1), Tooltip("Gauge value needed for a Tear Burst.")]
    private int maxValue = 100;
    [SerializeField, Min(0), Tooltip("Gauge gained when Pip loses a star.")]
    private int gainOnStarLost = 20;
    [SerializeField, Min(0), Tooltip("Gauge gained per mallet hit.")]
    private int gainOnMalletHit = 5;

    [Header("Tear Burst")]
    [SerializeField, Min(0.1f), Tooltip("Radius of the burst ring (tiles).")]
    private float burstRadius = 6f;
    [SerializeField, Min(0f), Tooltip("Seconds enemies stay stunned.")]
    private float stunDuration = 2f;
    [SerializeField, Min(0), Tooltip("Damage dealt to objects tagged Boss.")]
    private int bossDamage = 12;
    [SerializeField, Tooltip("Tag that marks the boss. Must exist in Tags and Layers.")]
    private string bossTag = "Boss";
    [SerializeField, Tooltip("Layers the burst affects (Enemy). Don't include Pip's layer.")]
    private LayerMask targetLayers;
    [SerializeField, Tooltip("Ring center relative to Pip's feet (tiles).")]
    private Vector2 burstOffset = new Vector2(0f, 0.75f);

    // ------------------------------------------------------------------ Public API

    public int Value { get; private set; }
    public int MaxValue => maxValue;
    public bool IsFull => Value >= maxValue;

    /// <summary>Fired with (value, max) whenever the gauge changes. For the UI.</summary>
    public event Action<int, int> GaugeChanged;

    /// <summary>Fired with (center, radius) when a Tear Burst goes off. For the ring effect and sound.</summary>
    public event Action<Vector2, float> TearBurstReleased;

    // ------------------------------------------------------------------ Private state

    private PlayerController _controller;
    private PlayerHealth _health;   // Optional: gauge still works without it.
    private PlayerMallet _mallet;   // Optional.
    private InputAction _burstAction;

    private readonly List<Collider2D> _overlaps = new List<Collider2D>();
    private readonly HashSet<IStunnable> _stunned = new HashSet<IStunnable>();
    private readonly HashSet<IDamageable> _damaged = new HashSet<IDamageable>();

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _health = GetComponent<PlayerHealth>();
        _mallet = GetComponent<PlayerMallet>();
    }

    private void Start()
    {
        GaugeChanged?.Invoke(Value, maxValue); // Give the UI its starting value.
    }

    private void OnEnable()
    {
        _burstAction = _controller.InputActions.FindActionMap("Player", true).FindAction("TearBurst", true);
        _burstAction.performed += OnBurstPressed;

        if (_health != null) _health.StarLost += OnStarLost;
        if (_mallet != null) _mallet.HitLanded += OnMalletHit;
    }

    private void OnDisable()
    {
        if (_burstAction != null) _burstAction.performed -= OnBurstPressed;
        if (_health != null) _health.StarLost -= OnStarLost;
        if (_mallet != null) _mallet.HitLanded -= OnMalletHit;
    }

    // ------------------------------------------------------------------ Filling

    private void OnStarLost() => Add(gainOnStarLost);
    private void OnMalletHit(IDamageable target, DamageInfo info) => Add(gainOnMalletHit);

    /// <summary>Adds to the gauge (clamped to 0..max). Public so pickups or other moves can fill it too.</summary>
    public void Add(int amount)
    {
        int newValue = Mathf.Clamp(Value + amount, 0, maxValue);
        if (newValue == Value)
            return;

        Value = newValue;
        GaugeChanged?.Invoke(Value, maxValue);
    }

    // ------------------------------------------------------------------ Tear Burst

    private void OnBurstPressed(InputAction.CallbackContext ctx)
    {
        if (!IsFull)
            return; // Not enough tears yet.

        ReleaseBurst();
    }

    private void ReleaseBurst()
    {
        Vector2 center = (Vector2)transform.position + burstOffset;

        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(targetLayers);
        Physics2D.OverlapCircle(center, burstRadius, filter, _overlaps);

        // An enemy can have several colliders; stun/damage each one only once.
        _stunned.Clear();
        _damaged.Clear();
        foreach (Collider2D col in _overlaps)
        {
            if (col.transform.IsChildOf(transform))
                continue; // Never affect Pip.

            var stunnable = col.GetComponentInParent<IStunnable>();
            if (stunnable != null && _stunned.Add(stunnable))
                stunnable.Stun(stunDuration);

            var damageable = col.GetComponentInParent<IDamageable>();
            if (damageable != null && IsBoss(col, damageable) && _damaged.Add(damageable))
            {
                var info = new DamageInfo(bossDamage, Vector2.zero, col.ClosestPoint(center), gameObject, DamageType.TearBurst);
                damageable.TakeDamage(info);
            }
        }

        // Empty the gauge.
        Value = 0;
        GaugeChanged?.Invoke(Value, maxValue);
        TearBurstReleased?.Invoke(center, burstRadius);
    }

    private bool IsBoss(Collider2D col, IDamageable damageable)
    {
        // The tag may be on the collider's object or on the object holding the damage script.
        return col.CompareTag(bossTag) || ((Component)damageable).CompareTag(bossTag);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.6f, 1f);
        Gizmos.DrawWireSphere((Vector2)transform.position + burstOffset, burstRadius);
    }
}
