/// <summary>
/// Anything that can be hit: enemies, the boss, pots, breakable walls...
/// The attacker only knows about this interface, so it never needs to know
/// *what* it hit. Each damageable object decides how to react (lose HP, break,
/// apply knockback to its own Rigidbody2D, play a sound, etc.).
/// </summary>
public interface IDamageable
{
    void TakeDamage(DamageInfo info);
}
