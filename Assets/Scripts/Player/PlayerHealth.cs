using System;
using UnityEngine;

/// <summary>
/// Pip's health, counted in Smile Stars.
///
/// - Pip starts the game with startMaxStars stars (full). Star Fragments raise the maximum,
///   up to maxStarsCap.
/// - Every hit removes exactly 1 star (the DamageInfo amount is ignored on purpose).
/// - A Balloon Shield absorbs the next hit instead (no star lost, no knockback).
/// - After a hit Pip is knocked away from the attacker, flashes, and is invulnerable for a while.
/// - Hits are also ignored during the dash's invulnerability (PlayerController.IsInvulnerable).
/// - At 0 stars, OnDeath fires and player input is switched off. Call Revive() to bring Pip back.
///
/// Enemies damage Pip through the IDamageable interface, the same way Pip damages them.
/// Hazards (spikes, pits) go through TakeHazardDamage() instead; see PlayerHazards.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    // ------------------------------------------------------------------ Inspector fields

    [Header("Smile Stars")]
    [SerializeField, Min(1), Tooltip("Max stars at the start of the game. Pip starts full.")]
    private int startMaxStars = 5;
    [SerializeField, Min(1), Tooltip("Highest the max can ever go (with Star Fragment upgrades).")]
    private int maxStarsCap = 10;

    [Header("When Hit")]
    [SerializeField, Min(0f), Tooltip("Seconds of invulnerability after losing a star or popping a shield.")]
    private float invulnerabilityTime = 1f;
    [SerializeField, Min(0f), Tooltip("Sideways knockback speed, away from the attacker (tiles per second).")]
    private float knockbackHorizontal = 6f;
    [SerializeField, Min(0f), Tooltip("Upward knockback speed (tiles per second).")]
    private float knockbackVertical = 8f;
    [SerializeField, Min(0f), Tooltip("Seconds Pip can't move after being hit.")]
    private float hurtLockTime = 0.25f;

    [Header("Flashing")]
    [SerializeField, Min(0.01f), Tooltip("Seconds between flashes while invulnerable after a hit.")]
    private float flashInterval = 0.08f;
    [SerializeField, Range(0f, 1f), Tooltip("Sprite opacity during the 'off' part of a flash.")]
    private float flashAlpha = 0.3f;

    // ------------------------------------------------------------------ Public API

    public int CurrentStars { get; private set; }

    /// <summary>Current maximum: startMaxStars + Star Fragment bonus, never above maxStarsCap.</summary>
    public int MaxStars { get; private set; }

    public bool IsDead { get; private set; }

    /// <summary>True while a Balloon Shield is waiting to absorb a hit.</summary>
    public bool HasShield { get; private set; }

    /// <summary>True while hits are ignored: after-hit invulnerability or the dash's invulnerability.</summary>
    public bool IsInvulnerable => Time.time < _invulnerableUntil || _controller.IsInvulnerable;

    /// <summary>Fired with (current, max) whenever the star count or the max changes. For the UI.</summary>
    public event Action<int, int> StarsChanged;

    /// <summary>Fired each time Pip loses a star (from an enemy or a hazard). TearGauge listens to this.</summary>
    public event Action StarLost;

    /// <summary>Fired with true when Pip gains a Balloon Shield and false when it pops.</summary>
    public event Action<bool> ShieldChanged;

    /// <summary>Fired once when the stars reach 0. The save system should respawn Pip and call Revive().</summary>
    public event Action OnDeath;

    // ------------------------------------------------------------------ Private state

    private PlayerController _controller;
    private SpriteRenderer _sprite;
    private float _invulnerableUntil = float.NegativeInfinity;
    private bool _flashing;

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _sprite = GetComponentInChildren<SpriteRenderer>();
        MaxStars = Mathf.Min(startMaxStars, maxStarsCap);
        CurrentStars = MaxStars;
    }

    private void Start()
    {
        StarsChanged?.Invoke(CurrentStars, MaxStars); // Give the UI its starting value.
    }

    private void Update()
    {
        UpdateFlash();
    }

    // ------------------------------------------------------------------ Taking damage

    /// <summary>Called by enemies and enemy attacks. Ignored while invulnerable or dead.</summary>
    public void TakeDamage(DamageInfo info)
    {
        if (IsDead || IsInvulnerable)
            return;

        // A shield takes the whole hit: no star lost, no knockback, just a moment of safety.
        if (TryPopShield())
            return;

        LoseStar();
        if (IsDead)
            return;

        // Knock Pip away from whatever hit her.
        float direction = GetKnockbackDirection(info);
        _controller.ApplyKnockback(new Vector2(direction * knockbackHorizontal, knockbackVertical), hurtLockTime);
    }

    /// <summary>
    /// Called by PlayerHazards. Hazards always cost a star (or the shield), even during invulnerability,
    /// so the dash can't be used to cross spikes for free. PlayerHazards handles the respawn.
    /// </summary>
    public void TakeHazardDamage()
    {
        if (IsDead)
            return;

        if (TryPopShield())
            return;

        LoseStar();
    }

    private bool TryPopShield()
    {
        if (!HasShield)
            return false;

        HasShield = false;
        _invulnerableUntil = Time.time + invulnerabilityTime;
        ShieldChanged?.Invoke(false);
        return true;
    }

    private void LoseStar()
    {
        CurrentStars = Mathf.Max(CurrentStars - 1, 0);
        StarsChanged?.Invoke(CurrentStars, MaxStars);
        StarLost?.Invoke();

        if (CurrentStars == 0)
        {
            Die();
            return;
        }

        _invulnerableUntil = Time.time + invulnerabilityTime;
    }

    private float GetKnockbackDirection(DamageInfo info)
    {
        // Prefer the attacker's position; fall back to the hit point; then to "backwards".
        Vector2 from = info.Source != null ? (Vector2)info.Source.transform.position : info.HitPoint;
        float dx = transform.position.x - from.x;

        if (Mathf.Abs(dx) < 0.01f)
            return -_controller.FacingDirection;
        return Mathf.Sign(dx);
    }

    // ------------------------------------------------------------------ Death

    private void Die()
    {
        IsDead = true;
        _invulnerableUntil = float.NegativeInfinity;

        if (HasShield)
        {
            HasShield = false;
            ShieldChanged?.Invoke(false);
        }

        // Switch off all player input (move, jump, swing, throw, burst...). Pip just falls/stops.
        _controller.InputActions.FindActionMap("Player", true).Disable();
        OnDeath?.Invoke();
    }

    /// <summary>Brings Pip back with full stars and turns input back on. Move Pip first (e.g. Teleport).</summary>
    public void Revive()
    {
        IsDead = false;
        CurrentStars = MaxStars;
        _invulnerableUntil = Time.time + invulnerabilityTime; // A moment of safety after respawning.
        _controller.InputActions.FindActionMap("Player", true).Enable();
        StarsChanged?.Invoke(CurrentStars, MaxStars);
    }

    // ------------------------------------------------------------------ Pickups / upgrades

    /// <summary>Adds stars, up to MaxStars. Returns false (and does nothing) if dead or already full.</summary>
    public bool Heal(int amount)
    {
        if (IsDead || amount <= 0 || CurrentStars >= MaxStars)
            return false;

        CurrentStars = Mathf.Min(CurrentStars + amount, MaxStars);
        StarsChanged?.Invoke(CurrentStars, MaxStars);
        return true;
    }

    /// <summary>Gives a Balloon Shield. Returns false if Pip already has one (only one at a time).</summary>
    public bool GiveShield()
    {
        if (IsDead || HasShield)
            return false;

        HasShield = true;
        ShieldChanged?.Invoke(true);
        return true;
    }

    /// <summary>
    /// Sets the number of extra max stars from upgrades (e.g. Star Fragments).
    /// Newly gained max stars are filled, so an upgrade also heals. Used for loading too.
    /// </summary>
    public void SetBonusMaxStars(int bonus)
    {
        int newMax = Mathf.Clamp(startMaxStars + bonus, 1, maxStarsCap);
        if (newMax == MaxStars)
            return;

        int gained = newMax - MaxStars;
        MaxStars = newMax;
        if (!IsDead)
            CurrentStars = Mathf.Clamp(CurrentStars + Mathf.Max(gained, 0), 0, MaxStars);
        StarsChanged?.Invoke(CurrentStars, MaxStars);
    }

    // ------------------------------------------------------------------ Flashing

    private void UpdateFlash()
    {
        if (_sprite == null)
            return;

        // Only the after-hit invulnerability flashes (not the dash).
        bool shouldFlash = Time.time < _invulnerableUntil;
        if (shouldFlash)
        {
            // Alternates between visible and faded every flashInterval seconds.
            bool faded = Mathf.FloorToInt(Time.time / flashInterval) % 2 == 0;
            SetAlpha(faded ? flashAlpha : 1f);
        }
        else if (_flashing)
        {
            SetAlpha(1f); // Make sure Pip ends fully visible.
        }

        _flashing = shouldFlash;
    }

    private void SetAlpha(float alpha)
    {
        Color c = _sprite.color;
        c.a = alpha;
        _sprite.color = c;
    }
}
