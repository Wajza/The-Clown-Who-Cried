using System;
using UnityEngine;

/// <summary>
/// The one shared "is Blue Sight on?" switch for the whole game.
///
/// PlayerBlueSight turns it on and off; the world listens to it (BlueSightOnly, FakeInBlueSight,
/// BlueSightVolume). Because it's static, level objects don't need a reference to Pip,
/// and they work in any scene. A cutscene could also call SetActive directly.
/// </summary>
public static class BlueSight
{
    /// <summary>True while Blue Sight is on.</summary>
    public static bool IsActive { get; private set; }

    /// <summary>Fired with the new state whenever Blue Sight turns on or off.</summary>
    public static event Action<bool> Changed;

    public static void SetActive(bool active)
    {
        if (IsActive == active)
            return;

        IsActive = active;
        Changed?.Invoke(active);
    }

    // Static values survive between Play sessions when "Enter Play Mode Options" skips the domain
    // reload. This resets them at the start of every Play session so old state never leaks in.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        IsActive = false;
        Changed = null;
    }
}
