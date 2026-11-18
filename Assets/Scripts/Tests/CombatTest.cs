using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// Automatic test for the player/combat side, using REAL key presses on a virtual keyboard
/// (unlike the other tests, which call TakeDamage directly). It plays in the real Tumbleton level:
///   1. Mallet: K swings it, and the swing hits a target in front of Pip.
///   2. Camera: walking out of the town-square CameraZone (the pots area) gives the camera back
///      the whole level, and it follows Pip.
///   3. Spikes: landing on them costs a star and puts Pip back on safe ground.
///   4. Dying inside the pots area: Pip respawns at a checkpoint and the camera follows her.
///   5. The prologue checkpoints, the Console log (PlayerDebugLog) and the Controls menu screen.
///   6. Pip keeps her stars and Tear Gauge through a door (PlayerCarryOver); one AudioListener.
///
/// Run: Tools > Clown Who Cried > Combat > Run Test (or CombatSetup.RunTestsFromCommandLine in
/// batch mode). Results: Logs/CombatTestResults.txt.
/// </summary>
public class CombatTest : SystemTestBase
{
    protected override string ResultsFileName => "CombatTestResults.txt";
    protected override string LogTag => "CombatTest";

    private const float FloorY = 0f;

    private string _sceneLoaded;
    private Keyboard _keyboard;

    private void OnEnable()
    {
        GameEvents.SceneLoaded += OnSceneLoaded;
        Application.logMessageReceived += OnAnyLog;
    }

    private void OnDisable()
    {
        GameEvents.SceneLoaded -= OnSceneLoaded;
        Application.logMessageReceived -= OnAnyLog;
    }
    private void OnSceneLoaded(string scene, string spawn) => _sceneLoaded = scene;

    private static PlayerController Pip => PlayerController.Current;

    protected override IEnumerator RunTest()
    {
        yield return WaitUntil(() => GameManager.Instance != null && _sceneLoaded != null, 5f, "first SceneLoaded");
        Check("New Game starts", GameManager.Instance.NewGame());
        yield return WaitForScene("Tumbleton_PipHouse");
        yield return SkipCutsceneIfPlaying();

        _sceneLoaded = null;
        SceneLoader.Instance.LoadScene("Tumbleton", "FromHouse");
        yield return WaitForScene("Tumbleton");
        Check("Pip is the real prefab (has the mallet)", Pip != null && Pip.GetComponent<PlayerMallet>() != null);

        // The animation art (800px frames) must show at Pip's real size, standing on the floor.
        yield return new WaitForSeconds(0.3f);
        Bounds art = Pip.GetComponentInChildren<SpriteRenderer>().bounds;
        Check($"Pip's sprite is about her collider's size ({art.size.x:F2} x {art.size.y:F2} tiles)", art.size.y > 1.1f && art.size.y < 2.3f);
        Check($"...and her feet are on the floor (sprite bottom {art.min.y - Pip.transform.position.y:F2} from her feet)",
            Mathf.Abs(art.min.y - Pip.transform.position.y) < 0.35f);

        // In batch mode there is no focused Game view, and by default the Input System ignores
        // input while the game isn't focused. Let the virtual keyboard through for this test.
        InputSettings settings = InputSystem.settings;
        InputSettings.BackgroundBehavior oldBackground = settings.backgroundBehavior;
        InputSettings.EditorInputBehaviorInPlayMode oldEditor = settings.editorInputBehaviorInPlayMode;
        settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;

        _keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            yield return Mallet();
            yield return CameraLeavesThePotsArea();
            yield return Spikes();
            yield return DeathInThePotsArea();
            yield return ControlsScreenAndCheckpoints();
            yield return StateSurvivesDoors();
        }
        finally
        {
            InputSystem.RemoveDevice(_keyboard);
            settings.backgroundBehavior = oldBackground;
            settings.editorInputBehaviorInPlayMode = oldEditor;
        }
    }

    // ------------------------------------------------------------------ 1. Mallet

    private IEnumerator Mallet()
    {
        // Before the mallet is earned, K is ignored, and the Console log says why.
        yield return MovePipTo(96f, FloorY);
        Check("Swing is off before the mallet is earned", !ActionEnabled("Swing"));
        Check("Throw and Dash are off too before the prologue teaches them", !ActionEnabled("Throw") && !ActionEnabled("Dash"));
        int listeners = 0;
        foreach (AudioListener l in FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude))
            if (l.enabled) listeners++;
        Check($"The level has exactly one active AudioListener ({listeners})", listeners == 1);
        _logs.Clear();
        yield return Hold(Key.K, 0.1f);
        Check("The log explains an ignored Swing press", LogContains("Pressed Swing", "IGNORED", "Old Tumbles"));

        PlayerAbilities.Unlock(Ability.Mallet); // What starting P4 does.
        yield return null;                      // PlayerAbilities applies in LateUpdate.

        var mallet = Pip.GetComponent<PlayerMallet>();
        int swings = 0, hits = 0;
        mallet.SwingStarted += _ => swings++;
        mallet.HitLanded += (target, info) => hits++;

        // A target 1 tile in front of Pip, in the open street between the pots and the station.
        yield return MovePipTo(96f, FloorY);
        yield return Hold(Key.D, 0.05f); // Face right.
        var dummy = new GameObject("CombatTest_Dummy", typeof(BoxCollider2D)) { layer = LayerMask.NameToLayer("Enemy") };
        dummy.transform.position = new Vector3(Pip.transform.position.x + 1.2f, FloorY, 0f);
        var box = dummy.GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.8f, 1.5f);
        box.offset = new Vector2(0f, 0.75f);
        dummy.AddComponent<TestDummy>();
        yield return new WaitForFixedUpdate();

        Check("Swing action is on after the mallet is unlocked", ActionEnabled("Swing"));
        _logs.Clear();
        yield return Hold(Key.K, 0.1f);
        yield return new WaitForSeconds(0.3f); // Wind-up + active frames.
        Check($"Pressing K swings the mallet (swings: {swings})", swings == 1);
        Check($"The swing hits the target in front of Pip (hits: {hits})", hits == 1);
        Check("The log shows the K press, the swing and the hit",
            LogContains("Pressed Swing: K") && LogContains("Mallet swing") && LogContains("Mallet hit CombatTest_Dummy"));
        Object.Destroy(dummy);
    }

    // ------------------------------------------------------------------ 2. Camera

    private IEnumerator CameraLeavesThePotsArea()
    {
        CameraRig rig = CameraRig.Current;
        Collider2D levelBounds = LevelInfo.Current.CameraBounds;
        Collider2D zone = GameObject.Find("CameraZone_TownSquare").GetComponent<Collider2D>();

        yield return MovePipTo(75f, FloorY);
        yield return new WaitForSeconds(0.2f);
        Check("In the pots area the camera uses the town-square zone", rig.CurrentBounds == zone);

        // Past the spikes, still inside the zone, then WALK right out of it.
        yield return MovePipTo(88f, FloorY);
        yield return Hold(Key.D, 1.2f);
        float x = Pip.transform.position.x;
        Check($"Pip walked out of the zone (x = {x:F1}, zone ends at 92)", x > 92.5f);
        yield return new WaitForSeconds(0.2f);
        Check("...and the camera uses the whole level again", rig.CurrentBounds == levelBounds);

        yield return new WaitForSeconds(1.5f); // Let the camera catch up.
        float cameraX = Camera.main.transform.position.x;
        Check($"The camera follows Pip (camera x {cameraX:F1}, Pip x {Pip.transform.position.x:F1})",
            Mathf.Abs(cameraX - Pip.transform.position.x) < 6f);
    }

    // ------------------------------------------------------------------ 3. Spikes

    private IEnumerator Spikes()
    {
        yield return MovePipTo(87f, FloorY); // Safe ground right of the spikes.
        yield return new WaitForSeconds(0.3f);
        PlayerHealth health = Pip.GetComponent<PlayerHealth>();
        int stars = health.CurrentStars;

        float spikesX = GameObject.Find("Spikes").transform.position.x;
        Pip.Teleport(new Vector2(spikesX, 1f), 0f); // Drop onto the spikes.
        yield return WaitUntil(() => health.CurrentStars < stars, 2f, "the spikes to hurt Pip");
        yield return new WaitForSeconds(0.2f);
        float distance = Mathf.Abs(Pip.transform.position.x - spikesX);
        Check($"Spikes cost a star ({stars} -> {health.CurrentStars})", health.CurrentStars == stars - 1);
        Check($"...and put Pip back on safe ground (x = {Pip.transform.position.x:F1}, spikes at {spikesX})", distance > 2.5f);
    }

    // ------------------------------------------------------------------ 4. Dying in the pots area

    private IEnumerator DeathInThePotsArea()
    {
        yield return MovePipTo(75f, FloorY);
        yield return new WaitForSeconds(0.3f);
        PlayerHealth health = Pip.GetComponent<PlayerHealth>();

        bool respawned = false;
        System.Action onRespawn = () => respawned = true;
        GameEvents.PlayerRespawned += onRespawn;
        while (!health.IsDead)
            health.TakeHazardDamage();
        yield return WaitUntil(() => respawned, 8f, "Pip to respawn after dying");
        GameEvents.PlayerRespawned -= onRespawn;
        yield return new WaitForSeconds(2f); // Fade back in, camera catches up.

        Vector2 pip = PipPosition();
        Check($"After dying Pip respawns with full stars (at x {pip.x:F1})", !health.IsDead && health.CurrentStars == health.MaxStars);
        Check("...and can move again", ActionEnabled("Jump"));
        Collider2D bounds = CameraRig.Current.CurrentBounds;
        Check("...the camera's limits contain her", bounds != null && bounds.OverlapPoint(pip + Vector2.up));
        float cameraX = Camera.main.transform.position.x;
        Check($"...and the camera follows her (camera x {cameraX:F1}, Pip x {pip.x:F1})", Mathf.Abs(cameraX - pip.x) < 16f);
    }

    // ------------------------------------------------------------------ 5. Menu and checkpoints

    private IEnumerator ControlsScreenAndCheckpoints()
    {
        foreach (string id in new[] { "Tumbleton_AfterHonkStreet", "Tumbleton_AfterBalloonWall", "Tumbleton_AfterReflexCheck" })
            Check($"Prologue checkpoint '{id}' exists", GameObject.Find($"Checkpoint_{id}") != null);

        yield return MovePipTo(59f, FloorY);
        yield return new WaitForSeconds(0.2f);
        Check("Touching the checkpoint after P3 saves it as the respawn point",
            GameManager.Instance.Save.spawnPointId == "Tumbleton_AfterBalloonWall");

        MenuController menu = MenuController.Instance;
        menu.OpenPause();
        yield return null;
        menu.OpenControls();
        yield return null;
        string text = menu.Controls != null ? menu.Controls.Text : "";
        Check("Pause > Controls opens the Controls screen", menu.Current == MenuController.Screen.Controls);
        Check("It lists the mallet: K / Right click / X", text.Contains("Mallet swing") && text.Contains("K / Right click") && text.Contains("X"));
        Check("It lists movement and the gamepad", text.Contains("WASD / Arrows") && text.Contains("Left stick"));
        menu.Back();
        yield return null;
        Check("Back returns to the pause menu", menu.Current == MenuController.Screen.Pause);
        menu.CloseAll();
        yield return null;
    }

    // ------------------------------------------------------------------ 6. Through a door

    private IEnumerator StateSurvivesDoors()
    {
        PlayerHealth health = Pip.GetComponent<PlayerHealth>();
        health.TakeHazardDamage(); // -1 star (and +20 tears).
        TearGauge gauge = Pip.GetComponent<TearGauge>();
        gauge.Add(30);
        int stars = health.CurrentStars, tears = gauge.Value;

        _sceneLoaded = null;
        SceneLoader.Instance.LoadScene("Tumbleton_PipHouse", "FromTown");
        yield return WaitForScene("Tumbleton_PipHouse");
        yield return WaitUntil(() => !SceneLoader.Instance.IsLoading, 5f, "the fade-in");
        yield return new WaitForSeconds(0.3f);

        health = Pip.GetComponent<PlayerHealth>();
        gauge = Pip.GetComponent<TearGauge>();
        Check($"Stars survive a door ({stars} -> {health.CurrentStars})", health.CurrentStars == stars);
        Check($"The Tear Gauge survives a door ({tears} -> {gauge.Value})", gauge.Value == tears);
    }

    // ------------------------------------------------------------------ Helpers

    private readonly System.Collections.Generic.List<string> _logs = new System.Collections.Generic.List<string>();

    private void OnAnyLog(string message, string stack, LogType type) => _logs.Add(message);

    private bool LogContains(params string[] parts)
    {
        foreach (string line in _logs)
        {
            bool all = true;
            foreach (string part in parts)
                all &= line.Contains(part);
            if (all)
                return true;
        }
        return false;
    }

    /// <summary>Holds a key on the virtual keyboard for 'seconds', then releases it.</summary>
    private IEnumerator Hold(Key key, float seconds)
    {
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
        yield return new WaitForSeconds(seconds);
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
        yield return null;
        yield return null;
    }

    private IEnumerator WaitForScene(string scene)
    {
        yield return WaitUntil(() => _sceneLoaded == scene && !SceneLoader.Instance.IsLoading, 10f, $"scene '{scene}'");
        yield return null;
        yield return null;
    }

    private static bool ActionEnabled(string action)
    {
        InputAction a = Pip.InputActions.FindActionMap("Player").FindAction(action);
        return a != null && a.enabled;
    }
}
