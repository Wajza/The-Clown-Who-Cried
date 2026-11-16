using System;
using UnityEngine;

/// <summary>
/// What the boss health bar needs to know about a boss: name, health, phase, and whether the
/// fight is on. The bar only talks to this class, never to a boss script directly.
///
/// WHY
///   - Any boss can get a bar by adding a small "source" next to it (ChortleHealthSource for
///     Grand Jester Chortle), without the bar knowing about that boss's code.
///   - The bar can be tested with a fake source, without spawning the real boss.
///
/// The most recently enabled source is "Active". The bar listens to ActiveChanged and Changed.
/// </summary>
public abstract class BossHealthSource : MonoBehaviour
{
    // ------------------------------------------------------------------ What a source must tell

    public abstract string BossName { get; }
    public abstract float Health { get; }
    public abstract float MaxHealth { get; }
    public abstract int Phase { get; }

    /// <summary>True while the fight is on (the bar is only shown then).</summary>
    public abstract bool IsFighting { get; }

    // ------------------------------------------------------------------ Events

    /// <summary>The boss whose bar should be shown (null = none).</summary>
    public static BossHealthSource Active { get; private set; }

    /// <summary>Fired when Active changes.</summary>
    public static event Action ActiveChanged;

    /// <summary>Fired by the source whenever anything above changes.</summary>
    public event Action Changed;

    protected void RaiseChanged() => Changed?.Invoke();

    // ------------------------------------------------------------------ Unity lifecycle

    protected virtual void OnEnable()
    {
        Active = this;
        ActiveChanged?.Invoke();
    }

    protected virtual void OnDisable()
    {
        if (Active != this)
            return;
        Active = null;
        ActiveChanged?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Active = null;
        ActiveChanged = null;
    }
}
