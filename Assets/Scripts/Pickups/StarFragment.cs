using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A collectible Star Fragment with a unique ID, so the save system can remember which ones
/// were found. Collecting it hands the ID to PlayerStarFragments (on Pip), which fires the
/// event and grants +1 max Smile Star for every 4 fragments.
///
/// IDs: a new random ID is generated when the component is added. If you DUPLICATE a fragment
/// in the scene the copy keeps the same ID, so right-click the component > "Generate New ID".
/// Duplicate IDs in the same scene are reported as errors in the Console.
/// </summary>
public class StarFragment : Pickup
{
    [SerializeField, Tooltip("Unique ID saved when collected. Must be different for every fragment in the game.")]
    private string fragmentId;

    public string FragmentId => fragmentId;

    // Every fragment currently active in the loaded scenes. Used to hide fragments after loading a save.
    private static readonly List<StarFragment> s_active = new List<StarFragment>();

    // ------------------------------------------------------------------ IDs

    protected override void Reset()
    {
        base.Reset();
        GenerateNewId();
    }

    [ContextMenu("Generate New ID")]
    private void GenerateNewId()
    {
        fragmentId = Guid.NewGuid().ToString();
    }

    // ------------------------------------------------------------------ Lifecycle

    private void OnEnable()
    {
        foreach (StarFragment other in s_active)
        {
            if (other.fragmentId == fragmentId)
                Debug.LogError($"Star Fragments '{name}' and '{other.name}' share the ID {fragmentId}. Use 'Generate New ID' on one.", this);
        }
        s_active.Add(this);
    }

    private void OnDisable()
    {
        s_active.Remove(this);
    }

    private void Start()
    {
        // Already found in this save? Then it shouldn't be in the level.
        PlayerStarFragments collector = PlayerStarFragments.Current;
        if (collector != null && collector.HasCollected(fragmentId))
            gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ Pickup

    protected override bool TryApply(PlayerController player)
    {
        PlayerStarFragments collector = player.GetComponent<PlayerStarFragments>();
        return collector != null && collector.Collect(fragmentId);
    }

    /// <summary>Hides every fragment in the loaded scenes that the collector already has. Called after loading a save.</summary>
    public static void HideCollected(PlayerStarFragments collector)
    {
        // Copy first: SetActive(false) removes items from s_active while we loop.
        foreach (StarFragment fragment in s_active.ToArray())
        {
            if (collector.HasCollected(fragment.fragmentId))
                fragment.gameObject.SetActive(false);
        }
    }
}
