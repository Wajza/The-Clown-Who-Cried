using System;

/// <summary>
/// What BossFightFlow needs from a boss: start the fight, and say when it's beaten.
/// Implemented by ChortleFight (on Chortle). BossFightFlow only talks to this interface, so a
/// future boss can plug in the same way.
/// </summary>
public interface IBossFight
{
    /// <summary>Begin fighting (after the intro cutscene).</summary>
    void StartFight();

    bool IsDefeated { get; }

    /// <summary>Fired once when the boss is beaten.</summary>
    event Action Defeated;
}
