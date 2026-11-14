using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

/// <summary>
/// An automatic check of Part 5 (levels, camera, scene flow, abilities). It lives in the tiny
/// LevelTest scene, starts a NEW GAME, and travels through the real (greybox) levels:
///
///   Pip's House --door--> Tumbleton --station door--> Train Map --> Wonderwick Hub
///   --door--> W1 room --edge exit--> Hub --Laugh Tower door--> Chortle's arena
///
/// Along the way it checks Pip spawning, spawn points, the camera (follow, bounds, camera zone,
/// pixel perfect, screen shake hookup), name cards, abilities (locked, unlocked, blocked) and
/// the project setup (Pogo tag). Results: Logs/Part5TestResults.txt.
///
/// It uses the levels' names and object names, so if you rename doors or spawn points in the
/// levels, update the names here too (or just ignore this test once the real levels are built).
/// </summary>
public class LevelSystemTest : SystemTestBase
{
    protected override string ResultsFileName => "Part5TestResults.txt";
    protected override string LogTag => "Part5Test";

    private string _sceneLoaded;

    private void OnEnable() => GameEvents.SceneLoaded += OnSceneLoaded;
    private void OnDisable() => GameEvents.SceneLoaded -= OnSceneLoaded;
    private void OnSceneLoaded(string scene, string spawn) => _sceneLoaded = scene;

    private static PlayerController Pip => PlayerController.Current;

    // The greybox levels' floor is at y = 0 (the test scenes' is at -1.5).
    private const float LevelFloorY = 0f;

    // ------------------------------------------------------------------ Test flow

    protected override IEnumerator RunTest()
    {
        yield return WaitUntil(() => GameManager.Instance != null && _sceneLoaded != null, 5f, "first SceneLoaded");
        CheckProjectSetup();

        // 1. New Game starts in Pip's house, in bed.
        Check("New Game starts", GameManager.Instance.NewGame());
        yield return WaitForScene("Tumbleton_PipHouse");
        CheckArrival("Pip's House", "Bed");
        yield return CheckStandsOnFloor("Pip's House");
        CheckCamera();
        LevelNameCard card = FindAnyObjectByType<LevelNameCard>();
        Check("Name card shows the level name", card != null && card.Showing && card.ShownName == "Pip's House");

        yield return CheckAbilities();

        // 2. The front door leads to Tumbleton.
        yield return UseDoor("FrontDoor");
        yield return WaitForScene("Tumbleton");
        CheckArrival("Tumbleton", "FromHouse");
        CheckCamera();
        Check("Earned abilities stay earned in the next level", ActionEnabled("Swing"));
        yield return CheckCameraZone();
        CheckExitRequirement();

        // 3. Station door -> train map (no Pip there) -> Wonderwick.
        yield return UseDoor("StationDoor");
        yield return WaitForScene("TrainMap");
        Check("Train map has no Pip", Pip == null);
        Check("HUD hidden on the train map", !FindAnyObjectByType<HudController>().Visible);
        TrainMap map = FindAnyObjectByType<TrainMap>();
        Check("Train map lists 2 open stations", map != null && map.Stations.Count == 2 && map.IsOpen(0) && map.IsOpen(1));
        map.Stations[1].button.onClick.Invoke(); // "Wonderwick" (the button's Inspector On Click).
        yield return WaitForScene("Wonderwick_Hub");
        CheckArrival("Wonderwick", "FromTrain");
        Check("Wonderwick belongs to the Wonderwick city", LevelInfo.Current.City == TaskData.City.Wonderwick);

        // 4. Into the W1 room by its door, back out by walking into the edge exit.
        yield return UseDoor("Door_W1");
        yield return WaitForScene("Wonderwick_W1_Delivery");
        CheckArrival("Punchline Delivery", "FromHub");
        LevelExit edge = GameObject.Find("ExitToHub").GetComponent<LevelExit>();
        yield return MovePipTo(edge.transform.position.x, LevelFloorY);
        yield return WaitForScene("Wonderwick_Hub");
        CheckNear("Edge exit leads back to the hub at 'FromW1'", PipPosition(), SpawnPoint.Find("FromW1").Position);

        // 5. The Laugh Tower door leads to Chortle's arena.
        yield return UseDoor("LaughTowerDoor");
        yield return WaitForScene("ChortleArena");
        CheckArrival("The Laugh Tower", "Start");
        yield return CheckStandsOnFloor("The Laugh Tower");
        CheckCamera();
    }

    /// <summary>A second later Pip must still be standing on the floor (not falling through it).</summary>
    private IEnumerator CheckStandsOnFloor(string level)
    {
        yield return new WaitForSeconds(1f);
        Check($"{level}: Pip stands on the floor (y = {Pip.transform.position.y:0.00})",
            Mathf.Abs(Pip.transform.position.y - LevelFloorY) < 0.1f);
    }

    // ------------------------------------------------------------------ Checks

    private void CheckProjectSetup()
    {
        bool pogo;
        var probe = new GameObject("PogoTagProbe");
        try { probe.tag = "Pogo"; pogo = probe.CompareTag("Pogo"); }
        catch (UnityException) { pogo = false; }
        Destroy(probe);
        Check("'Pogo' tag exists", pogo);

#if CINEMACHINE_3
        Check("CINEMACHINE_3 symbol is set (ScreenShake uses Cinemachine)", true);
#else
        Check("CINEMACHINE_3 symbol is set (ScreenShake uses Cinemachine)", false);
#endif
        Check("GameSettings has a Player Prefab", GameSettings.Instance != null && GameSettings.Instance.playerPrefab != null);
    }

    private void CheckArrival(string levelName, string spawnId)
    {
        Check($"{levelName}: LevelInfo present", LevelInfo.Current != null && LevelInfo.Current.DisplayName == levelName);
        Check($"{levelName}: exactly one Pip, spawned by the level", Pip != null && FindObjectsByType<PlayerController>().Length == 1);
        SpawnPoint point = SpawnPoint.Find(spawnId);
        Check($"{levelName}: spawn point '{spawnId}' exists", point != null);
        if (point != null)
            CheckNear($"{levelName}: Pip at '{spawnId}'", PipPosition(), point.Position);
        Check($"{levelName}: Playing, Pip free", GameManager.Instance.State == GameState.Playing && !PlayerInputLock.IsLocked);
    }

    private void CheckCamera()
    {
        string level = LevelInfo.Current != null ? LevelInfo.Current.DisplayName : "?";
        CameraRig rig = CameraRig.Current;
        Check($"{level}: camera rig present", rig != null);
        if (rig == null)
            return;
        Check($"{level}: camera follows this level's Pip", rig.PipCamera.Follow == Pip.transform);
        Check($"{level}: camera kept inside the level bounds", rig.CurrentBounds == LevelInfo.Current.CameraBounds);

        Camera cam = Camera.main;
        var pixelPerfect = cam != null ? cam.GetComponent<PixelPerfectCamera>() : null;
        Check($"{level}: Pixel Perfect Camera 480x270 at 16 PPU", pixelPerfect != null &&
            pixelPerfect.assetsPPU == 16 && pixelPerfect.refResolutionX == 480 && pixelPerfect.refResolutionY == 270);
        Check($"{level}: Cinemachine brain + pixel perfect + impulse listener",
            cam.GetComponent<CinemachineBrain>() != null && rig.PipCamera.GetComponent<CinemachinePixelPerfect>() != null &&
            rig.PipCamera.GetComponent<CinemachineImpulseListener>() != null);
        ScreenShake shake = FindAnyObjectByType<ScreenShake>();
        Check($"{level}: teammate's ScreenShake with an impulse source", shake != null && shake.GetComponent<CinemachineImpulseSource>() != null);
    }

    private IEnumerator CheckAbilities()
    {
        Check("Levels lock abilities until earned", LevelInfo.Current.LockAbilitiesUntilEarned);
        yield return null; // PlayerAbilities applies in LateUpdate.
        Check("Mallet locked at the start (Swing off)", !ActionEnabled("Swing") && !PlayerAbilities.CanUse(Ability.Mallet));
        Check("Blue Sight locked at the start", !ActionEnabled("BlueSight"));
        Check("Coins and Dash work from the start", ActionEnabled("Throw") && ActionEnabled("Dash"));
        Check("Movement unaffected", ActionEnabled("Move") && ActionEnabled("Jump"));
        Check("HUD hides the Blue Sight meter", !FindAnyObjectByType<HudBlueSight>().ContentVisible);

        PlayerAbilities.Unlock(Ability.Mallet);
        yield return null;
        Check("Unlocking the mallet turns Swing on", ActionEnabled("Swing"));
        Check("Unlock is saved as a flag", GameManager.Instance.Save.HasFlag("ability_mallet") &&
            SaveSystem.Read()?.HasFlag("ability_mallet") == true);

        // Carrying a box (W1): no attacks or dash, but walking still works.
        var box = new object();
        PlayerAbilities.Block(box, Ability.Mallet, Ability.Coins, Ability.Dash);
        yield return null;
        Check("Block: Swing, Throw and Dash off", !ActionEnabled("Swing") && !ActionEnabled("Throw") && !ActionEnabled("Dash"));
        Check("Block: Move and Jump still on", ActionEnabled("Move") && ActionEnabled("Jump"));

        // A dialogue/cutscene lock re-enables the whole map when it ends; the block must survive.
        var cutscene = new object();
        PlayerInputLock.Lock(cutscene);
        PlayerInputLock.Unlock(cutscene);
        Check("Block survives an input lock/unlock", !ActionEnabled("Swing") && ActionEnabled("Move"));

        PlayerAbilities.Unblock(box);
        yield return null;
        Check("Unblock: Swing, Throw and Dash back", ActionEnabled("Swing") && ActionEnabled("Throw") && ActionEnabled("Dash"));

        PlayerAbilities.Unlock(Ability.BlueSight);
        yield return null;
        Check("Unlocking Blue Sight turns it on and shows its meter",
            ActionEnabled("BlueSight") && FindAnyObjectByType<HudBlueSight>().ContentVisible);
    }

    private IEnumerator CheckCameraZone()
    {
        CameraZone zone = GameObject.Find("CameraZone_TownSquare").GetComponent<CameraZone>();
        yield return MovePipTo(zone.Bounds.bounds.center.x, LevelFloorY);
        Check("Entering a camera zone switches the camera bounds", CameraRig.Current.CurrentBounds == zone.Bounds);
        yield return MovePipTo(20f, LevelFloorY);
        Check("Leaving it switches back to the level bounds", CameraRig.Current.CurrentBounds == LevelInfo.Current.CameraBounds);
    }

    private void CheckExitRequirement()
    {
        var requirement = new ExitRequirement { requiredFlag = "test_exit_flag" };
        Check("Exit with a missing flag is locked", !requirement.IsMet);
        GameManager.Instance.Save.SetFlag("test_exit_flag");
        Check("Setting the flag opens it", requirement.IsMet);
        GameManager.Instance.Save.ClearFlag("test_exit_flag");

        TaskManager tasks = TaskManager.Instance;
        TaskData anyTask = tasks.FindTask("test_kid");
        if (anyTask != null)
        {
            var needsTask = new ExitRequirement();
            needsTask.requiredTasks.Add(anyTask);
            Check("Exit needing an unfinished task is locked", !needsTask.IsMet);
        }
    }

    // ------------------------------------------------------------------ Steps

    private IEnumerator WaitForScene(string scene)
    {
        yield return WaitUntil(() => _sceneLoaded == scene && !SceneLoader.Instance.IsLoading, 10f, $"scene '{scene}'");
        yield return null;
        yield return null; // HUD, camera and abilities update in Update/LateUpdate.
    }

    /// <summary>Walk to the door and press Interact.</summary>
    private IEnumerator UseDoor(string doorName)
    {
        GameObject go = GameObject.Find(doorName);
        Door door = go != null ? go.GetComponent<Door>() : null;
        Check($"Door '{doorName}' found", door != null);
        if (door == null)
            yield break;

        _sceneLoaded = null;
        yield return MovePipTo(door.transform.position.x, LevelFloorY);
        Check($"'{doorName}' is the interaction target", InteractionInput.Instance.Target == door);
        Check($"'{doorName}' opens", InteractionInput.Instance.TryInteract());
    }

    private static bool ActionEnabled(string action)
    {
        InputAction a = Pip != null ? Pip.InputActions.FindActionMap("Player").FindAction(action) : null;
        return a != null && a.enabled;
    }
}
