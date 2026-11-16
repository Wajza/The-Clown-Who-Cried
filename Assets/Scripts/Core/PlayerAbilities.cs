using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Pip's abilities that the story unlocks, or that something can block for a while.</summary>
public enum Ability
{
    Mallet,     // Swing (and pogo). Given in P4 "Reflex Check".
    Coins,      // Throw Star Coins. Available from the start.
    Dash,       // Dash. Available from the start.
    BlueSight,  // Blue Sight. Given in W2 "Crooked Mirror Maze".
    JuggleShot, // Coin upgrade: throw up to 3 at once. Reward for W3 "Runaway Coaster".
}

/// <summary>
/// Decides which of Pip's abilities work right now, WITHOUT changing my teammate's scripts.
///
/// HOW IT SWITCHES THINGS OFF
/// Each ability is one action in my teammate's "Player" input map (Swing, Throw, Dash, BlueSight).
/// Disabling that single action makes the button do nothing; their scripts don't need to know.
/// Juggle Shot is their PlayerCoinThrower.JuggleShotUnlocked property.
///
/// TWO REASONS AN ABILITY CAN BE OFF
///   1. Not earned yet. Earned abilities are flags in the save: "ability_mallet",
///      "ability_bluesight", "ability_juggle". Give one with PlayerAbilities.Unlock(Ability.Mallet)
///      (from a task reward, dialogue flag or cutscene). Coins and Dash start unlocked.
///      This only applies in MY levels: scenes whose LevelInfo has "Lock Abilities Until Earned"
///      ticked. Everywhere else (my teammate's test scenes, my test scenes) everything works.
///   2. Blocked for a while, e.g. while carrying a box in W1: Block(owner, Mallet, Coins, Dash)
///      and later Unblock(owner). Like PlayerInputLock, several owners can block at once.
///
/// WHY IT CHECKS EVERY FRAME
/// Enabling the whole "Player" map (PlayerInputLock, my teammate's Revive...) switches every
/// action back on. LateUpdate puts the rules back straight away, so nothing slips through.
///
/// Setup: on the GameSystems prefab (the builder adds it).
/// </summary>
public class PlayerAbilities : MonoBehaviour
{
    private const string PlayerMapName = "Player";

    // Which input action each ability is (Juggle Shot has none; it's a property).
    private static readonly Dictionary<Ability, string> ActionNames = new Dictionary<Ability, string>
    {
        { Ability.Mallet, "Swing" },
        { Ability.Coins, "Throw" },
        { Ability.Dash, "Dash" },
        { Ability.BlueSight, "BlueSight" },
    };

    // Save flags for abilities that must be earned. Others start unlocked.
    private static readonly Dictionary<Ability, string> UnlockFlags = new Dictionary<Ability, string>
    {
        { Ability.Mallet, "ability_mallet" },
        { Ability.BlueSight, "ability_bluesight" },
        { Ability.JuggleShot, "ability_juggle" },
    };

    // ------------------------------------------------------------------ Public API

    /// <summary>Fired when an ability is unlocked or blocks change (the HUD uses it).</summary>
    public static event Action Changed;

    /// <summary>Has Pip earned it? (Always true outside levels that lock abilities.)</summary>
    public static bool IsUnlocked(Ability ability)
    {
        if (!LevelLocksAbilities || !UnlockFlags.TryGetValue(ability, out string flag))
            return true;
        SaveData save = GameManager.Instance != null ? GameManager.Instance.Save : null;
        return save != null && save.HasFlag(flag);
    }

    /// <summary>Is something blocking it right now (e.g. carrying a box)?</summary>
    public static bool IsBlocked(Ability ability)
    {
        foreach (HashSet<Ability> blocked in s_blocks.Values)
            if (blocked.Contains(ability))
                return true;
        return false;
    }

    /// <summary>Can Pip use it right now?</summary>
    public static bool CanUse(Ability ability) => IsUnlocked(ability) && !IsBlocked(ability);

    /// <summary>Gives Pip an ability for good (saved as a flag). Saves the game.</summary>
    public static void Unlock(Ability ability)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || !UnlockFlags.TryGetValue(ability, out string flag) || gm.Save.HasFlag(flag))
            return;

        gm.Save.SetFlag(flag);
        gm.SaveGame();
        Apply();
        Changed?.Invoke();
    }

    /// <summary>Switches abilities off until Unblock(owner). Calling again replaces this owner's list.</summary>
    public static void Block(object owner, params Ability[] abilities)
    {
        if (owner == null)
            return;
        s_blocks[owner] = new HashSet<Ability>(abilities);
        Apply();
        Changed?.Invoke();
    }

    public static void Unblock(object owner)
    {
        if (owner == null || !s_blocks.Remove(owner))
            return;
        Apply();
        Changed?.Invoke();
    }

    /// <summary>Removes every block (e.g. going back to the title screen).</summary>
    public static void ClearBlocks()
    {
        s_blocks.Clear();
        Apply();
        Changed?.Invoke();
    }

    /// <summary>
    /// Makes Pip's input match the rules. Only touches single actions, and only while the map is
    /// on (if PlayerInputLock switched the whole map off, that wins).
    /// </summary>
    public static void Apply()
    {
        PlayerController pip = PlayerController.Current;
        if (pip == null || pip.InputActions == null)
            return;

        InputActionMap map = pip.InputActions.FindActionMap(PlayerMapName, throwIfNotFound: false);
        if (map != null && map.enabled)
        {
            foreach (KeyValuePair<Ability, string> pair in ActionNames)
            {
                InputAction action = map.FindAction(pair.Value, throwIfNotFound: false);
                if (action == null)
                    continue;

                bool allowed = CanUse(pair.Key);
                if (!allowed && action.enabled)
                {
                    action.Disable();
                    s_disabledByUs.Add(action);
                }
                else if (allowed && s_disabledByUs.Remove(action) && !action.enabled)
                {
                    action.Enable(); // Only switch back on what WE switched off.
                }
            }
        }

        // Juggle Shot is only managed in my levels; elsewhere my teammate's setting stays as is.
        PlayerCoinThrower thrower = pip.GetComponent<PlayerCoinThrower>();
        bool juggle = CanUse(Ability.JuggleShot);
        if (LevelLocksAbilities && thrower != null && thrower.JuggleShotUnlocked != juggle)
            thrower.JuggleShotUnlocked = juggle;
    }

    // ------------------------------------------------------------------ Private state

    private static readonly Dictionary<object, HashSet<Ability>> s_blocks = new Dictionary<object, HashSet<Ability>>();
    private static readonly HashSet<InputAction> s_disabledByUs = new HashSet<InputAction>();

    private static bool LevelLocksAbilities => LevelInfo.Current != null && LevelInfo.Current.LockAbilitiesUntilEarned;

    // ------------------------------------------------------------------ Unity lifecycle

    private void LateUpdate() => Apply();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_blocks.Clear();
        s_disabledByUs.Clear();
        Changed = null;
    }
}
