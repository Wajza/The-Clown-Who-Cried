using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Remembers which Star Fragments Pip has collected (by ID) and turns every 4 of them into
/// +1 max Smile Star. Put it on Pip next to PlayerHealth.
///
/// For the save system:
///   - Save:  write out CollectedIds.
///   - Load:  call LoadCollected(savedIds). Max stars are recalculated and already-found
///            fragments in the level are hidden.
///   - Listen to FragmentCollected to save as soon as a fragment is picked up.
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
public class PlayerStarFragments : MonoBehaviour
{
    [SerializeField, Min(1), Tooltip("Fragments needed for +1 max Smile Star.")]
    private int fragmentsPerStar = 4;

    /// <summary>The active player's collector, so fragments in the level can check if they were already found.</summary>
    public static PlayerStarFragments Current { get; private set; }

    /// <summary>IDs of every fragment collected so far.</summary>
    public IReadOnlyCollection<string> CollectedIds => _collected;

    public int FragmentCount => _collected.Count;

    /// <summary>Progress toward the next max star (0..fragmentsPerStar-1). For a UI like "2/4".</summary>
    public int FragmentsTowardNextStar => _collected.Count % fragmentsPerStar;

    public int FragmentsPerStar => fragmentsPerStar;

    /// <summary>Fired with (fragment ID, total collected) when a new fragment is picked up.</summary>
    public event Action<string, int> FragmentCollected;

    private readonly HashSet<string> _collected = new HashSet<string>();
    private PlayerHealth _health;

    private void Awake()
    {
        _health = GetComponent<PlayerHealth>();
    }

    private void OnEnable() => Current = this;

    private void OnDisable()
    {
        if (Current == this)
            Current = null;
    }

    public bool HasCollected(string id) => !string.IsNullOrEmpty(id) && _collected.Contains(id);

    /// <summary>Records a fragment. Returns false if the ID is empty or was already collected.</summary>
    public bool Collect(string id)
    {
        if (string.IsNullOrEmpty(id) || !_collected.Add(id))
            return false;

        FragmentCollected?.Invoke(id, _collected.Count);
        UpdateMaxStars();
        return true;
    }

    /// <summary>Replaces the collected set with saved IDs (call when loading a save).</summary>
    public void LoadCollected(IEnumerable<string> ids)
    {
        _collected.Clear();
        foreach (string id in ids)
        {
            if (!string.IsNullOrEmpty(id))
                _collected.Add(id);
        }

        UpdateMaxStars();
        StarFragment.HideCollected(this);
    }

    private void UpdateMaxStars()
    {
        // Integer division: 0-3 fragments = +0, 4-7 = +1, 8-11 = +2, ...
        _health.SetBonusMaxStars(_collected.Count / fragmentsPerStar);
    }
}
