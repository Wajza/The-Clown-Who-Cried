using UnityEngine;

/// <summary>
/// Creates the GameSystems prefab automatically when the game starts, in ANY scene.
///
/// WHY
/// GameManager, SceneLoader, ScreenFader etc. must exist exactly once and live for the whole game.
/// If they were placed by hand in the Title scene, pressing Play in Tumbleton to test a level
/// would start without them. This script removes that problem: Unity calls CreateGameSystems()
/// by itself right after the first scene loads (that's what [RuntimeInitializeOnLoadMethod] does),
/// and it spawns the prefab if it isn't there yet.
///
/// Setup: the prefab must be at Assets/Resources/GameSystems.prefab. Resources.Load can only find
/// assets inside a folder named "Resources", and the name must match PrefabName exactly.
/// </summary>
public static class GameBootstrap
{
    private const string PrefabName = "GameSystems";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateGameSystems()
    {
        if (GameManager.Instance != null)
            return; // Already there.

        GameObject prefab = Resources.Load<GameObject>(PrefabName);
        if (prefab == null)
        {
            Debug.LogError($"GameBootstrap: no prefab at Assets/Resources/{PrefabName}.prefab. Core systems will not run.");
            return;
        }

        GameObject systems = Object.Instantiate(prefab);
        systems.name = PrefabName; // Remove the "(Clone)" suffix, so the Hierarchy reads nicely.
    }
}
