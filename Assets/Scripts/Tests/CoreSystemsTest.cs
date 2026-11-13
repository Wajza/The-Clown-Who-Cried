using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// An automatic check of everything in Part 1 (Core). It lives in the CoreTest_A scene.
///
/// Press Play in CoreTest_A and watch: Pip dies, respawns, touches the checkpoint, collects a
/// test Star Fragment, travels to CoreTest_B, dies there, and finally uses Continue. Every check
/// prints PASS or FAIL in the Console, and a summary is written to Logs/Part1TestResults.txt.
/// (The PASS/FAIL, timeout and save-backup machinery is in SystemTestBase.)
/// </summary>
public class CoreSystemsTest : SystemTestBase
{
    private const string SceneA = "CoreTest_A";
    private const string SceneB = "CoreTest_B";
    private const string CheckpointId = "CoreTest_A_CP1";
    private const string FragmentId = "test_fragment_1";

    protected override string ResultsFileName => "Part1TestResults.txt";
    protected override string LogTag => "Part1Test";

    private bool _respawned;
    private bool _sceneLoaded;

    // ------------------------------------------------------------------ Events the test watches

    private void OnEnable()
    {
        GameEvents.PlayerRespawned += OnRespawned;
        GameEvents.SceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        GameEvents.PlayerRespawned -= OnRespawned;
        GameEvents.SceneLoaded -= OnSceneLoaded;
    }

    private void OnRespawned() => _respawned = true;
    private void OnSceneLoaded(string scene, string spawn) => _sceneLoaded = true;

    // ------------------------------------------------------------------ Test flow

    protected override IEnumerator RunTest()
    {
        // 1. The bootstrap created GameSystems, and the first scene was announced.
        yield return WaitUntil(() => GameManager.Instance != null && _sceneLoaded, 5f, "first SceneLoaded");
        Check("GameSystems created by GameBootstrap", GameObject.Find("GameSystems") != null);
        Check("GameManager, SceneLoader and ScreenFader exist",
            GameManager.Instance != null && SceneLoader.Instance != null && ScreenFader.Instance != null);
        Check("State is Playing", GameManager.Instance.State == GameState.Playing);
        Check("Pip found", PlayerController.Current != null);

        GameManager gm = GameManager.Instance;
        gm.Save.starFragmentIds.Clear(); // Start clean.

        // 2. PlayerInputLock: two owners, the map only comes back when both release.
        InputActionMap map = PlayerMap();
        object ownerA = new object(), ownerB = new object();
        PlayerInputLock.Lock(ownerA);
        PlayerInputLock.Lock(ownerB);
        Check("Lock disables the Player map", !map.enabled);
        PlayerInputLock.Unlock(ownerA);
        Check("Map stays off while another owner still locks", !map.enabled);
        PlayerInputLock.Unlock(ownerB);
        Check("Map back on when every owner unlocked", map.enabled);

        // 3. Die with no checkpoint: back to where Pip entered the scene.
        Vector2 entry = PipPosition();
        PlayerController.Current.Teleport(entry + new Vector2(6f, 0f), 0f);
        yield return new WaitForSeconds(0.2f);
        yield return KillPipAndWaitForRespawn();
        CheckNear("Respawn with no checkpoint = scene entry point", PipPosition(), entry);
        CheckPipRevived();

        // 4. Touch the checkpoint: it becomes the respawn point and the game saves.
        SpawnPoint checkpoint = SpawnPoint.Find(CheckpointId);
        Check("Checkpoint registered as a spawn point", checkpoint != null);
        PlayerController.Current.Teleport(checkpoint.Position + Vector2.up * 0.6f, 0f);
        yield return WaitUntil(() => gm.Save.spawnPointId == CheckpointId, 3f, "checkpoint trigger");
        Check("Checkpoint sets Save.spawnPointId", gm.Save.spawnPointId == CheckpointId);
        SaveData onDisk = SaveSystem.Read();
        Check("Checkpoint wrote the save file", onDisk != null && onDisk.spawnPointId == CheckpointId && onDisk.sceneName == SceneA);

        // 5. Die after the checkpoint: back at the checkpoint.
        PlayerController.Current.Teleport(entry, 0f);
        yield return new WaitForSeconds(0.2f);
        yield return KillPipAndWaitForRespawn();
        CheckNear("Respawn at the checkpoint", PipPosition(), checkpoint.Position);
        CheckPipRevived();

        // 6. Star Fragment collected -> stored in the save (memory and disk).
        PlayerStarFragments.Current.Collect(FragmentId);
        Check("Fragment added to Save", gm.Save.starFragmentIds.Contains(FragmentId));
        onDisk = SaveSystem.Read();
        Check("Fragment written to disk", onDisk != null && onDisk.starFragmentIds.Contains(FragmentId));

        // 7. Change scene through SceneLoader.
        _sceneLoaded = false;
        SceneLoader.Instance.LoadScene(SceneB, "FromA");
        Check("Input locked while loading", PlayerInputLock.IsLocked && !PlayerMap().enabled);
        yield return WaitUntil(() => _sceneLoaded && !SceneLoader.Instance.IsLoading, 10f, "load CoreTest_B");
        Check("Now in CoreTest_B", SceneLoader.CurrentSceneName == SceneB);
        Check("Only one GameManager after loading", FindObjectsByType<GameManager>().Length == 1);
        CheckNear("Pip placed at spawn point 'FromA'", PipPosition(), SpawnPoint.Find("FromA").Position);
        Check("New Pip got the saved fragment", PlayerStarFragments.Current.HasCollected(FragmentId));
        Check("Input unlocked after the fade-in", PlayerMap().enabled && !PlayerInputLock.IsLocked);
        Check("Screen fully visible again", !ScreenFader.Instance.IsCovering);
        onDisk = SaveSystem.Read();
        Check("Entering a scene autosaved it", onDisk != null && onDisk.sceneName == SceneB && onDisk.spawnPointId == "FromA");

        // 8. Die in the new scene: back to the door Pip came through.
        PlayerController.Current.Teleport(PipPosition() + new Vector2(5f, 0f), 0f);
        yield return new WaitForSeconds(0.2f);
        yield return KillPipAndWaitForRespawn();
        CheckNear("Respawn in CoreTest_B at 'FromA'", PipPosition(), SpawnPoint.Find("FromA").Position);

        // 9. Continue: forget the in-memory progress, then load it back from disk.
        gm.Save.starFragmentIds.Clear();
        _sceneLoaded = false;
        Check("ContinueGame finds the save", gm.ContinueGame());
        yield return WaitUntil(() => _sceneLoaded && !SceneLoader.Instance.IsLoading, 10f, "Continue");
        Check("Continue loaded the saved scene", SceneLoader.CurrentSceneName == SceneB);
        CheckNear("Continue placed Pip at the saved spawn point", PipPosition(), SpawnPoint.Find("FromA").Position);
        Check("Continue restored the fragment", PlayerStarFragments.Current.HasCollected(FragmentId));
    }

    // ------------------------------------------------------------------ Steps

    private IEnumerator KillPipAndWaitForRespawn()
    {
        _respawned = false;
        PlayerHealth health = PlayerController.Current.GetComponent<PlayerHealth>();

        // Hazard damage ignores invulnerability, so each call costs one star.
        for (int i = 0; i < 50 && !health.IsDead; i++)
            health.TakeHazardDamage();
        Check("Pip died", health.IsDead);

        yield return WaitUntil(() => _respawned, 10f, "respawn");
        yield return WaitUntil(() => !PlayerInputLock.IsLocked, 5f, "unlock after respawn");
        yield return new WaitForSeconds(0.2f); // Let Pip settle on the floor.
    }

    private void CheckPipRevived()
    {
        PlayerHealth health = PlayerController.Current.GetComponent<PlayerHealth>();
        Check("Pip alive with full stars", !health.IsDead && health.CurrentStars == health.MaxStars);
        Check("Pip can move again", PlayerMap().enabled);
        Check("Screen visible again", !ScreenFader.Instance.IsCovering);
    }

    private static InputActionMap PlayerMap() =>
        PlayerController.Current.InputActions.FindActionMap("Player", true);
}
