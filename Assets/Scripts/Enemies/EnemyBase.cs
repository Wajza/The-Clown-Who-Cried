using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared base for every Wonderwick enemy. It handles:
///   - Health and taking damage (IDamageable), with a hit flash and knockback.
///   - Stuns (IStunnable), e.g. from Pip's Tear Burst.
///   - Hurting Pip on touch.
///   - Attack warnings: every attack must call Warn() first, which lasts at least 0.5 s.
///   - "Popping" at 0 health: fires Popped, maybe drops a pickup, then disables itself.
///   - Falling into a Pit trigger pops the enemy too (so a wave can never get stuck).
///
/// Each enemy type writes one coroutine, Behaviour(), that runs its AI as a simple loop.
/// A hit or a stun stops that coroutine and restarts it afterwards, so an interrupted attack
/// always starts over from its warning.
///
/// Setup on the prefab: Enemy layer, SpriteRenderer (on it or a child), a Collider2D, and
/// usually a Rigidbody2D (Dynamic, Freeze Rotation Z). No Rigidbody2D = no knockback.
/// </summary>
public abstract class EnemyBase : MonoBehaviour, IDamageable, IStunnable
{
    /// <summary>Shortest allowed warning before any attack (seconds).</summary>
    public const float MinWarningTime = 0.5f;

    /// <summary>A pickup this enemy might drop when it pops.</summary>
    [Serializable]
    public class Drop
    {
        [Tooltip("Pickup prefab, e.g. Cotton Candy or Confetti Popper.")]
        public Pickup prefab;
        [Range(0f, 1f), Tooltip("Chance to drop this (0.2 = 20%).")]
        public float chance = 0.2f;
    }

    // ------------------------------------------------------------------ Inspector fields

    [Header("Health")]
    [SerializeField, Min(1), Tooltip("Hits worth of health. The mallet does 3, a coin does 1.")]
    private int maxHealth = 2;

    [Header("Getting Hit")]
    [SerializeField, Min(0f), Tooltip("How strongly hits push this enemy. 1 = full, 0 = never moves.")]
    private float knockbackMultiplier = 1f;
    [SerializeField, Min(0f), Tooltip("Seconds the enemy pauses after being hit (it then restarts its behaviour).")]
    private float hitRecoverTime = 0.3f;
    [SerializeField, Min(0f), Tooltip("Seconds the hit flash lasts.")]
    private float hitFlashTime = 0.1f;

    [Header("Touch Damage")]
    [SerializeField, Min(0), Tooltip("Stars Pip loses when touching this enemy (Pip always loses 1 per hit).")]
    private int contactDamage = 1;
    [SerializeField, Tooltip("Can a stunned enemy still hurt Pip on touch?")]
    private bool contactDamageWhileStunned = false;
    [SerializeField, Tooltip("Pip's layer (Player). Used for touch damage and attack hitboxes.")]
    protected LayerMask playerLayer;
    [SerializeField, Tooltip("Ground layer, for checking if the enemy is standing on something.")]
    protected LayerMask groundLayer;

    [Header("Look")]
    [SerializeField, Tooltip("Sprite to flash and flip. Found automatically if empty.")]
    protected SpriteRenderer spriteRenderer;
    [SerializeField, Tooltip("Tick if the sprite art faces right.")]
    private bool spriteFacesRight = true;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.45f, 0.45f);
    [SerializeField] private Color warningColor = new Color(1f, 0.9f, 0.3f);
    [SerializeField] private Color stunnedColor = new Color(0.6f, 0.75f, 1f);
    [SerializeField, Tooltip("Optional child object (e.g. a '!' sign) shown during attack warnings.")]
    private GameObject warningSign;

    [Header("Popping")]
    [SerializeField, Tooltip("Optional effect spawned when popping (e.g. a confetti particle prefab).")]
    private GameObject popEffectPrefab;
    [SerializeField, Tooltip("Possible drops. At most one is dropped; chances are checked in order.")]
    private List<Drop> drops = new List<Drop>();
    [SerializeField, Tooltip("Tag of pit triggers. Falling into one pops the enemy (no drop).")]
    private string pitTag = "Pit";

    // ------------------------------------------------------------------ Public API

    public int Health { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsPopped { get; private set; }
    public bool IsStunned => Time.time < _stunnedUntil;

    /// <summary>Fired when the enemy pops (WaveSpawner listens to this).</summary>
    public event Action<EnemyBase> Popped;

    /// <summary>Fired when the enemy takes damage but survives. For hit sounds.</summary>
    public event Action<EnemyBase, DamageInfo> Damaged;

    /// <summary>Fired with the warning length when an attack warning starts. For warning sounds/animation.</summary>
    public event Action<float> AttackWarned;

    // ------------------------------------------------------------------ For subclasses

    /// <summary>The enemy's AI loop. Write it as a coroutine (see the Wonderwick enemies).</summary>
    protected abstract IEnumerator Behaviour();

    /// <summary>Override to block damage at times, e.g. while Jack is hidden in the box.</summary>
    protected virtual bool CanBeHit => true;

    /// <summary>Override to turn touch damage off at times.</summary>
    protected virtual bool ContactDamageActive => true;

    /// <summary>Called when the behaviour is stopped by a hit, a stun or popping. Clean up here.</summary>
    protected virtual void OnInterrupted() { }

    protected Rigidbody2D Body { get; private set; }
    protected Collider2D BodyCollider { get; private set; }

    /// <summary>Pip, or null if there is no player in the scene.</summary>
    protected PlayerController Player => PlayerController.Current;

    /// <summary>The middle of Pip's body (Pip's pivot is at her feet).</summary>
    protected Vector2 PlayerCenter => (Vector2)Player.transform.position + new Vector2(0f, 0.75f);

    protected bool PlayerExists => Player != null;

    protected float DistanceToPlayer => PlayerExists ? Vector2.Distance(transform.position, PlayerCenter) : float.PositiveInfinity;

    /// <summary>1 if Pip is to the right, -1 if to the left.</summary>
    protected int DirectionToPlayer => PlayerExists && Player.transform.position.x < transform.position.x ? -1 : 1;

    // ------------------------------------------------------------------ Private state

    private Coroutine _behaviourRoutine;
    private float _resumeAt;
    private float _stunnedUntil = float.NegativeInfinity;
    private float _hitFlashUntil = float.NegativeInfinity;
    private float _warningUntil = float.NegativeInfinity;
    private Color _baseColor = Color.white;
    private readonly List<Collider2D> _overlaps = new List<Collider2D>();

    // ------------------------------------------------------------------ Unity lifecycle

    protected virtual void Awake()
    {
        Body = GetComponent<Rigidbody2D>();
        BodyCollider = GetComponent<Collider2D>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
            _baseColor = spriteRenderer.color;
        if (Body != null)
            Body.freezeRotation = true;
    }

    protected virtual void OnEnable()
    {
        // Reset everything, so an enemy can also be re-used (pooled or respawned).
        Health = maxHealth;
        IsPopped = false;
        _stunnedUntil = _hitFlashUntil = _warningUntil = float.NegativeInfinity;
        _resumeAt = 0f;
        if (warningSign != null)
            warningSign.SetActive(false);
    }

    protected virtual void OnDisable()
    {
        StopBehaviour();
    }

    protected virtual void Update()
    {
        // (Re)start the behaviour once any hit/stun pause is over.
        if (!IsPopped && _behaviourRoutine == null && Time.time >= _resumeAt)
            _behaviourRoutine = StartCoroutine(RunBehaviour());

        UpdateTint();
    }

    protected virtual void FixedUpdate()
    {
        CheckContactDamage();
    }

    private IEnumerator RunBehaviour()
    {
        yield return Behaviour();
        _behaviourRoutine = null; // Only reached if a behaviour ever finishes on its own.
    }

    // ------------------------------------------------------------------ Damage (IDamageable)

    public void TakeDamage(DamageInfo info)
    {
        if (IsPopped || !CanBeHit)
            return;

        Health -= info.Amount;
        _hitFlashUntil = Time.time + hitFlashTime;

        if (Health <= 0)
        {
            Pop(allowDrop: true);
            return;
        }

        Damaged?.Invoke(this, info);

        // Knockback: replace the velocity so the push always feels the same.
        if (Body != null && Body.bodyType == RigidbodyType2D.Dynamic && knockbackMultiplier > 0f)
            Body.linearVelocity = info.Knockback * knockbackMultiplier;

        Interrupt(hitRecoverTime);
    }

    // ------------------------------------------------------------------ Stun (IStunnable)

    public void Stun(float duration)
    {
        if (IsPopped)
            return;

        _stunnedUntil = Mathf.Max(_stunnedUntil, Time.time + duration);
        if (Body != null && Body.bodyType == RigidbodyType2D.Dynamic)
            Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y); // Stop walking, keep falling.

        Interrupt(duration);
    }

    // ------------------------------------------------------------------ Interrupting

    /// <summary>Stops the current behaviour and restarts it after 'pause' seconds.</summary>
    private void Interrupt(float pause)
    {
        StopBehaviour();
        _resumeAt = Mathf.Max(_resumeAt, Time.time + pause);
    }

    private void StopBehaviour()
    {
        if (_behaviourRoutine != null)
        {
            StopCoroutine(_behaviourRoutine);
            _behaviourRoutine = null;
        }

        EndWarning();
        OnInterrupted();
    }

    // ------------------------------------------------------------------ Attack warnings

    /// <summary>
    /// Call before every attack: "yield return Warn(0.7f);". The enemy blinks, the optional
    /// warning sign shows, and AttackWarned fires. Never shorter than MinWarningTime (0.5 s).
    /// </summary>
    protected IEnumerator Warn(float seconds)
    {
        float duration = Mathf.Max(seconds, MinWarningTime);
        _warningUntil = Time.time + duration;
        if (warningSign != null)
            warningSign.SetActive(true);
        AttackWarned?.Invoke(duration);

        yield return new WaitForSeconds(duration);
        EndWarning();
    }

    private void EndWarning()
    {
        _warningUntil = float.NegativeInfinity;
        if (warningSign != null)
            warningSign.SetActive(false);
    }

    // ------------------------------------------------------------------ Hurting Pip

    private void CheckContactDamage()
    {
        if (IsPopped || contactDamage <= 0 || BodyCollider == null || !ContactDamageActive)
            return;
        if (IsStunned && !contactDamageWhileStunned)
            return;

        // Ask "which colliders on the Player layer overlap my body?" This works even when the
        // Player and Enemy layers don't physically collide (recommended: they pass through).
        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(playerLayer);
        BodyCollider.Overlap(filter, _overlaps);

        foreach (Collider2D col in _overlaps)
            DamagePlayer(col, contactDamage);
    }

    /// <summary>Damages Pip if she is inside the box. Use for attack hitboxes (e.g. Jack's upward hit).</summary>
    protected void HitPlayerInBox(Vector2 center, Vector2 size, int damage)
    {
        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(playerLayer);
        Physics2D.OverlapBox(center, size, 0f, filter, _overlaps);

        foreach (Collider2D col in _overlaps)
            DamagePlayer(col, damage);
    }

    private void DamagePlayer(Collider2D col, int damage)
    {
        PlayerHealth health = col.GetComponentInParent<PlayerHealth>();
        if (health != null) // PlayerHealth ignores the hit itself if Pip is invulnerable.
            health.TakeDamage(new DamageInfo(damage, Vector2.zero, col.ClosestPoint(transform.position), gameObject));
    }

    // ------------------------------------------------------------------ Popping

    private void Pop(bool allowDrop)
    {
        if (IsPopped)
            return;

        IsPopped = true;
        StopBehaviour();

        if (popEffectPrefab != null)
            Instantiate(popEffectPrefab, BodyCollider != null ? BodyCollider.bounds.center : transform.position, Quaternion.identity);

        if (allowDrop)
            TryDrop();

        Popped?.Invoke(this);
        gameObject.SetActive(false); // No death animation: it just pops.
    }

    private void TryDrop()
    {
        // One roll for all drops: e.g. Cotton Candy 20% and Popper 10% means
        // 0.0-0.2 = candy, 0.2-0.3 = popper, otherwise nothing.
        float roll = UnityEngine.Random.value;
        float cumulative = 0f;
        foreach (Drop drop in drops)
        {
            cumulative += drop.chance;
            if (roll < cumulative && drop.prefab != null)
            {
                Vector3 position = transform.position + new Vector3(0f, 0.5f, 0f);
                Instantiate(drop.prefab, position, Quaternion.identity);
                return;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!string.IsNullOrEmpty(pitTag) && other.CompareTag(pitTag))
            Pop(allowDrop: false);
    }

    // ------------------------------------------------------------------ Helpers for subclasses

    /// <summary>Turns the sprite to face left (-1) or right (1).</summary>
    protected void Face(int direction)
    {
        if (spriteRenderer != null && direction != 0)
            spriteRenderer.flipX = spriteFacesRight ? direction < 0 : direction > 0;
    }

    /// <summary>True if there is ground just under the enemy's collider.</summary>
    protected bool IsGrounded()
    {
        if (BodyCollider == null)
            return false;

        Bounds b = BodyCollider.bounds;
        Vector2 center = new Vector2(b.center.x, b.min.y);
        Vector2 size = new Vector2(b.size.x * 0.9f, 0.1f);
        return Physics2D.OverlapBox(center, size, 0f, groundLayer) != null;
    }

    /// <summary>Effective gravity on this enemy (tiles/s²).</summary>
    protected float Gravity => Mathf.Abs(Physics2D.gravity.y) * (Body != null ? Body.gravityScale : 1f);

    // ------------------------------------------------------------------ Tint

    private void UpdateTint()
    {
        if (spriteRenderer == null)
            return;

        // Priority: hit flash > attack warning blink > stunned > normal.
        Color color = _baseColor;
        if (Time.time < _hitFlashUntil)
            color = hitFlashColor;
        else if (Time.time < _warningUntil)
            color = Mathf.FloorToInt(Time.time / 0.08f) % 2 == 0 ? warningColor : _baseColor;
        else if (IsStunned)
            color = stunnedColor;

        spriteRenderer.color = color;
    }
}
