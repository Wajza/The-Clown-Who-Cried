using System;

/// <summary>
/// What BossFightFlow needs from a boss: start the fight, and say when it's beaten.
/// Implemented by ChortleFight (my teammate's real Chortle) and PlaceholderBoss (a stand-in
/// until Chortle's prefab exists). BossFightFlow never needs to know which one it has.
/// </summary>
public interface IBossFight
{
    /// <summary>Begin fighting (after the intro cutscene).</summary>
    void StartFight();

    bool IsDefeated { get; }

    /// <summary>Fired once when the boss is beaten.</summary>
    event Action Defeated;
}
