using UnityEngine;

/// <summary>
/// Sounds for things in the world that announce themselves with my teammate's STATIC events
/// (one event for every object of that kind):
///   Pickup.AnyCollected              -> pickup
///   PunchlineBomb.AnyExploded        -> bomb
///   RubberChickenCannon.AnyFired     -> cannon
/// plus my own GameEvents.CheckpointReached -> checkpoint.
///
/// Enemy pops have no static event (each enemy has its own Popped), so they're not here yet
/// (TEAMMATE_TODO #12).
///
/// Setup: on the GameSystems prefab (the builder adds it).
/// </summary>
public class WorldSounds : MonoBehaviour
{
    private void OnEnable()
    {
        global::Pickup.AnyCollected += OnPickup;
        PunchlineBomb.AnyExploded += OnBomb;
        RubberChickenCannon.AnyFired += OnCannon;
        GameEvents.CheckpointReached += OnCheckpoint;
    }

    private void OnDisable()
    {
        global::Pickup.AnyCollected -= OnPickup;
        PunchlineBomb.AnyExploded -= OnBomb;
        RubberChickenCannon.AnyFired -= OnCannon;
        GameEvents.CheckpointReached -= OnCheckpoint;
    }

    private static void OnPickup(global::Pickup pickup) => AudioManager.Play(SoundIds.Pickup);
    private static void OnBomb(Vector2 position) => AudioManager.Play(SoundIds.Bomb);
    private static void OnCannon(RubberChickenCannon cannon) => AudioManager.Play(SoundIds.Cannon);
    private static void OnCheckpoint(string id) => AudioManager.Play(SoundIds.Checkpoint);
}
