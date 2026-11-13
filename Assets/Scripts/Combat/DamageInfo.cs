using UnityEngine;

/// <summary>What kind of attack dealt the damage. Lets targets react differently (e.g. the boss mask).</summary>
public enum DamageType
{
    Normal,     // Mallet, coins, enemy attacks, ...
    TearBurst,  // Pip's Tear Burst special.
}

/// <summary>
/// Everything a target needs to know about one hit.
/// A struct (value type) so creating one per hit costs nothing.
/// </summary>
public readonly struct DamageInfo
{
    /// <summary>How much health to remove.</summary>
    public readonly int Amount;

    /// <summary>
    /// Suggested velocity change for the target (tiles per second), already pointing away
    /// from the attacker. Targets with a Rigidbody2D can add it; pots can ignore it.
    /// </summary>
    public readonly Vector2 Knockback;

    /// <summary>World position where the hit happened (useful for hit effects).</summary>
    public readonly Vector2 HitPoint;

    /// <summary>The object that dealt the damage (e.g. Pip).</summary>
    public readonly GameObject Source;

    /// <summary>What kind of attack this was.</summary>
    public readonly DamageType Type;

    public DamageInfo(int amount, Vector2 knockback, Vector2 hitPoint, GameObject source, DamageType type = DamageType.Normal)
    {
        Amount = amount;
        Knockback = knockback;
        HitPoint = hitPoint;
        Source = source;
        Type = type;
    }
}
