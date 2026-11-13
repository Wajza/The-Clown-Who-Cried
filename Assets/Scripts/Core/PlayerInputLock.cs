using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Stops Pip from moving, jumping, attacking... while something else is happening
/// (scene loading, dialogue, cutscenes, the pause menu).
///
/// HOW IT WORKS
/// My teammate's PlayerController reads all of Pip's buttons from the "Player" action map.
/// Turning that map off makes Pip ignore input; turning it back on gives control back.
/// (Their PlayerHealth does the same thing when Pip dies.)
///
/// WHY "OWNERS" INSTEAD OF A SIMPLE ON/OFF
/// Imagine a cutscene locks Pip, and during it a line of dialogue also locks and then unlocks.
/// With a single on/off, the dialogue would give control back in the middle of the cutscene.
/// So each system locks with itself as the "owner", and Pip only gets control back when EVERY
/// owner has unlocked.
///
///   PlayerInputLock.Lock(this);     // e.g. when dialogue opens
///   PlayerInputLock.Unlock(this);   // when it closes
///
/// Pip is also never given control back while dead (PlayerHealth.IsDead): Revive() does that.
/// </summary>
public static class PlayerInputLock
{
    private const string PlayerMapName = "Player";

    // Everyone currently holding a lock. A HashSet ignores duplicates, so locking twice with
    // the same owner still only needs one Unlock.
    private static readonly HashSet<object> s_owners = new HashSet<object>();

    /// <summary>True while at least one system is holding the lock.</summary>
    public static bool IsLocked => s_owners.Count > 0;

    /// <summary>Takes control away from the player. 'owner' is usually 'this'.</summary>
    public static void Lock(object owner)
    {
        if (owner == null)
            return;
        s_owners.Add(owner);
        Refresh();
    }

    /// <summary>Releases this owner's lock. Control returns once no owners are left.</summary>
    public static void Unlock(object owner)
    {
        if (owner == null)
            return;
        s_owners.Remove(owner);
        Refresh();
    }

    /// <summary>
    /// Re-applies the lock to the current Pip. Call it after anything that may have switched
    /// the "Player" map back on by itself, e.g. a new scene (PlayerController enables the map
    /// in OnEnable) or PlayerHealth.Revive().
    /// </summary>
    public static void Refresh()
    {
        InputActionMap map = FindPlayerMap();
        if (map == null)
            return; // No Pip in this scene (e.g. the title screen). Nothing to do.

        if (IsLocked)
        {
            map.Disable();
            return;
        }

        // Unlocked: give control back, but never to a dead Pip.
        PlayerHealth health = PlayerController.Current.GetComponent<PlayerHealth>();
        if (health == null || !health.IsDead)
            map.Enable();
    }

    /// <summary>Removes every lock at once (e.g. when going back to the title screen).</summary>
    public static void ClearAll()
    {
        s_owners.Clear();
        Refresh();
    }

    private static InputActionMap FindPlayerMap()
    {
        PlayerController player = PlayerController.Current;
        if (player == null || player.InputActions == null)
            return null;
        return player.InputActions.FindActionMap(PlayerMapName, throwIfNotFound: false);
    }

    // Forget old locks when Play starts (needed if Domain Reload is turned off).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => s_owners.Clear();
}
