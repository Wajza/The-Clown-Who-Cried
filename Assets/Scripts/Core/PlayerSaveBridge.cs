using UnityEngine;

/// <summary>
/// Connects my save data to my teammate's Star Fragment system. A "bridge": it only listens to
/// their events and calls their public methods; it never changes their code.
///
/// WHY IT'S NEEDED
/// Every scene has its own Pip, and a new Pip starts with no fragments. The real list of
/// collected fragments lives in GameManager.Save, which survives scene changes. So:
///   - When a scene loads: give the new Pip the saved fragments (PlayerStarFragments.LoadCollected).
///     Their script then works out the bonus max stars and hides fragments already found.
///   - When Pip collects a fragment (their FragmentCollected event): add its id to the save.
///
/// Setup: on the GameSystems prefab. Nothing to assign.
/// </summary>
public class PlayerSaveBridge : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector fields

    [SerializeField, Tooltip("Write the save file as soon as a Star Fragment is collected.")]
    private bool saveOnFragmentCollected = true;

    // ------------------------------------------------------------------ Private state

    private PlayerStarFragments _fragments; // The current scene's Pip, so we can unsubscribe later.

    // ------------------------------------------------------------------ Unity lifecycle

    private void OnEnable() => GameEvents.SceneLoaded += OnSceneLoaded;

    private void OnDisable()
    {
        GameEvents.SceneLoaded -= OnSceneLoaded;
        Unhook();
    }

    // ------------------------------------------------------------------ Event handlers

    private void OnSceneLoaded(string sceneName, string spawnPointId)
    {
        Unhook(); // Let go of the previous scene's Pip.

        GameManager gm = GameManager.Instance;
        _fragments = PlayerStarFragments.Current;
        if (gm == null || _fragments == null)
            return; // No Pip in this scene.

        _fragments.LoadCollected(gm.Save.starFragmentIds);
        _fragments.FragmentCollected += OnFragmentCollected;
    }

    private void OnFragmentCollected(string fragmentId, int totalCollected)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
            return;

        if (!gm.Save.starFragmentIds.Contains(fragmentId))
            gm.Save.starFragmentIds.Add(fragmentId);

        if (saveOnFragmentCollected)
            gm.SaveGame();
    }

    private void Unhook()
    {
        // Unsubscribing from an object that was destroyed with its scene is still safe in C#.
        if (_fragments != null)
            _fragments.FragmentCollected -= OnFragmentCollected;
        _fragments = null;
    }
}
