using UnityEngine;

/// <summary>Restores Smile Stars. Stays in the world while Pip's stars are already full.</summary>
public class CottonCandy : Pickup
{
    [SerializeField, Min(1), Tooltip("Smile Stars restored.")]
    private int starsRestored = 1;

    protected override bool TryApply(PlayerController player)
    {
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        return health != null && health.Heal(starsRestored); // Heal returns false when full or dead.
    }
}
