using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// PLACEHOLDER for Grand Jester Chortle until my teammate's Chortle prefab exists
/// (TEAMMATE_TODO #4). It's a big box that takes hits (mallet or coins, through IDamageable) and
/// is "defeated" at 0 health, so the whole boss flow (intro, fight, outro, rewards, End of Demo)
/// can be played and tested now. It also feeds the boss health bar (phase 2 below 2/3 health,
/// phase 3 below 1/3).
///
/// It doesn't attack. Replace it with ChortleFight on the real Chortle (see ChortleFight).
///
/// Setup: a big sprite child, a trigger Collider2D (what attacks hit), Enemy layer.
/// </summary>
public class PlaceholderBoss : BossHealthSource, IBossFight, IDamageable
{
    [SerializeField] private string displayName = "Chortle (placeholder)";
    [SerializeField, Min(1)] private int maxHealth = 12;
    [SerializeField] private SpriteRenderer sprite;

    public event Action Defeated;

    public bool IsDefeated { get; private set; }
    public int HealthLeft => _health;

    public override string BossName => displayName;
    public override float Health => _health;
    public override float MaxHealth => maxHealth;
    public override int Phase => _health > maxHealth * 2 / 3 ? 1 : _health > maxHealth / 3 ? 2 : 3;
    public override bool IsFighting => _fighting && !IsDefeated;

    private int _health;
    private bool _fighting;

    private void Awake() => _health = maxHealth;

    public void StartFight()
    {
        if (_fighting || IsDefeated)
            return;
        _fighting = true;
        RaiseChanged();
    }

    public void TakeDamage(DamageInfo info)
    {
        if (!IsFighting)
            return;
        _health = Mathf.Max(0, _health - Mathf.Max(1, info.Amount));
        if (sprite != null)
            StartCoroutine(Flash());
        if (_health == 0)
        {
            IsDefeated = true;
            Defeated?.Invoke();
        }
        RaiseChanged();
    }

    /// <summary>Used when Chortle was beaten before (loading a save): show him beaten, no fight.</summary>
    public void SetAlreadyDefeated()
    {
        IsDefeated = true;
        _health = 0;
        gameObject.SetActive(false);
    }

    private IEnumerator Flash()
    {
        Color original = sprite.color;
        sprite.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        sprite.color = original;
    }
}
