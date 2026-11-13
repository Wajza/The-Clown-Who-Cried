using UnityEngine;

/// <summary>Makes Star Coins hit harder for a while. Collecting another one restarts the timer.</summary>
public class ConfettiPopper : Pickup
{
    [SerializeField, Min(1f), Tooltip("Coin damage multiplier. 1.5 = 50% more damage.")]
    private float damageMultiplier = 1.5f;
    [SerializeField, Min(0f), Tooltip("How long the boost lasts (seconds).")]
    private float duration = 20f;

    protected override bool TryApply(PlayerController player)
    {
        PlayerCoinThrower thrower = player.GetComponent<PlayerCoinThrower>();
        if (thrower == null)
            return false;

        thrower.ApplyDamageBoost(damageMultiplier, duration);
        return true;
    }
}
