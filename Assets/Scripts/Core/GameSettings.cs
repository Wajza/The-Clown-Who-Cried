using UnityEngine;

/// <summary>
/// Project-wide settings that several systems need, kept in ONE asset:
/// Assets/Resources/GameSettings.asset (loaded by name, so it must stay in Resources).
///
///   Player Prefab   The Pip that levels spawn (LevelInfo). Right now it's Placeholder_Pip.
///                   When my teammate commits the real Pip prefab, drag it in here, and
///                   every level uses it with no other changes.
/// </summary>
[CreateAssetMenu(menuName = "Clown Who Cried/Game Settings", fileName = "GameSettings")]
public class GameSettings : ScriptableObject
{
    [Tooltip("The Pip prefab every level spawns. Pivot at her feet.")]
    public PlayerController playerPrefab;

    private static GameSettings s_instance;

    /// <summary>The settings asset (Resources/GameSettings), or null if it's missing.</summary>
    public static GameSettings Instance
    {
        get
        {
            if (s_instance == null)
                s_instance = Resources.Load<GameSettings>("GameSettings");
            return s_instance;
        }
    }
}
