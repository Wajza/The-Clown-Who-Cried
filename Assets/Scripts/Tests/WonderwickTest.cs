using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// An automatic check of Part 7: the four Wonderwick tasks (rooms reached straight from a New Game).
///
///   W1 Punchline Delivery  carry boxes (attacks/dash blocked), deliver, time runs out (task fails,
///                          everything resets), try again and deliver all 5 (2 stalls up on ledges)
///   W2 Crooked Mirror Maze Blue Sight given; fake mirror; crack/fake floor/secret platform react
///                          to Blue Sight; real mirrors teleport; the exit
///   W3 Runaway Coaster     the wheel turns and carries Pip; 4 switches (F and hits); the waves
///                          start and clear; Juggle Shot is the reward
///   W4 The Audition        a failed round (4 of 8, an off-beat press counts as a miss), then a pass
///   -> the hub's Laugh Tower gate opens; the Journal shows all four done.
/// Results: Logs/Part7TestResults.txt.
/// </summary>
public class WonderwickTest : SystemTestBase
{
    [SerializeField] private TaskData w1, w2, w3, w4;

    protected override string ResultsFileName => "Part7TestResults.txt";
    protected override string LogTag => "Part7Test";

    private const float FloorY = 0f;

    private string _sceneLoaded;
    private TaskData _failed;

    private void OnEnable()
    {
        GameEvents.SceneLoaded += OnSceneLoaded;
        GameEvents.TaskFailed += OnFailed;
    }

    private void OnDisable()
    {
        GameEvents.SceneLoaded -= OnSceneLoaded;
        GameEvents.TaskFailed -= OnFailed;
    }

    private void OnSceneLoaded(string scene, string spawn) => _sceneLoaded = scene;
    private void OnFailed(TaskData task) => _failed = task;

    private static TaskManager Tasks => TaskManager.Instance;
    private static PlayerController Pip => PlayerController.Current;

    // ------------------------------------------------------------------ Test flow

    protected override IEnumerator RunTest()
    {
        bool linked = w1 != null && w2 != null && w3 != null && w4 != null;
        Check("Test driver references are assigned", linked);
        if (!linked)
            yield break;

        yield return WaitUntil(() => GameManager.Instance != null && _sceneLoaded != null, 5f, "first SceneLoaded");
        Check("New Game starts", GameManager.Instance.NewGame());
        yield return WaitForScene("Tumbleton_PipHouse");

        yield return Go("Wonderwick_W1_Delivery");
        yield return Delivery();
        yield return Go("Wonderwick_W2_MirrorMaze");
        yield return MirrorMaze();
        yield return Go("Wonderwick_W3_Coaster");
        yield return Coaster();
        yield return Go("Wonderwick_W4_Audition");
        yield return Audition();

        yield return Go("Wonderwick_Hub");
        Check("Laugh Tower gate opens after W1-W4", !GateClosed("TaskGate_LaughTower"));
        MenuController.Instance.OpenJournal();
        yield return null;
        JournalUI journal = MenuController.Instance.Journal;
        journal.SetTab((int)TaskData.City.Wonderwick);
        Check("Journal: all four Wonderwick tasks done",
            journal.ListText.Contains("[x] Punchline Delivery") && journal.ListText.Contains("[x] Crooked Mirror Maze") &&
            journal.ListText.Contains("[x] Runaway Coaster") && journal.ListText.Contains("[x] The Audition"));
        MenuController.Instance.CloseAll();
    }

    // ------------------------------------------------------------------ W1

    private IEnumerator Delivery()
    {
        yield return Use("NPC_PostmasterPun");
        Check("W1 started", Tasks.IsActive(w1));
        string text = Tasks.GetObjectiveText(w1);
        Check($"Tracker shows count and clock ('{text}')", text.StartsWith("Deliver the punchlines (0/5) ") && text.Contains(":"));
        float left = Tasks.GetTimeLeft(w1);
        Check("About 2 minutes on the clock", left > 100f && left <= 120f);

        // Pick up a box: no attacks or dash, walking still fine.
        yield return PickUp("Box_1");
        Check("Carrying Box_1", CarryableBox.Carried != null && CarryableBox.Carried.name == "Box_1");
        Check("While carrying: no coins, dash or swing", !ActionEnabled("Throw") && !ActionEnabled("Dash") && !ActionEnabled("Swing"));
        Check("While carrying: can still move and jump", ActionEnabled("Move") && ActionEnabled("Jump"));
        Check("Can't pick up a second box", !GameObject.Find("Box_2").GetComponent<CarryableBox>().CanInteract);

        yield return MovePipTo(X("Stall_1"), FloorY);
        Check("Walking into a stall delivers the box", CarryableBox.Carried == null && Tasks.GetObjectiveText(w1).StartsWith("Deliver the punchlines (1/5)"));
        Check("Stall shows it's served", GameObject.Find("Stall_1").GetComponent<DeliveryStall>().Served);
        Check("Box is gone", GameObject.Find("Box_1") == null);
        yield return null;
        Check("Abilities back after delivering", ActionEnabled("Throw") && ActionEnabled("Dash"));

        // The jack-in-the-box lids pop open and shut by themselves.
        TimedLid lid = FindAnyObjectByType<TimedLid>();
        bool was = lid.IsOpen;
        yield return WaitUntil(() => lid.IsOpen != was, lid.CycleSeconds + 0.5f, "a lid to pop");
        Check("Jack-in-the-box lid changes on its own", lid.IsOpen != was);

        // Run out of time while carrying a box: the task fails and everything resets.
        yield return PickUp("Box_2");
        Tasks.SetTimeLeft(w1, 0f);
        yield return null;
        yield return null;
        Check("Out of time: W1 failed (not running, not done)", _failed == w1 && !Tasks.IsActive(w1) && !TaskManager.IsCompleted(w1));
        Check("Fail: the carried box goes back", CarryableBox.Carried == null && GameObject.Find("Box_2") != null);
        Check("Fail: delivered boxes come back too", GameObject.Find("Box_1") != null);
        Check("Fail: stalls are ready again", !GameObject.Find("Stall_1").GetComponent<DeliveryStall>().Served);
        yield return null;
        Check("Fail: abilities back", ActionEnabled("Throw") && ActionEnabled("Dash"));

        // Try again and deliver all five (stalls 2 and 4 are up on ledges).
        yield return Use("NPC_PostmasterPun");
        Check("Postmaster gives W1 again", Tasks.IsActive(w1) && Tasks.GetObjectiveText(w1).StartsWith("Deliver the punchlines (0/5)"));
        for (int i = 1; i <= 5; i++)
        {
            yield return PickUp($"Box_{i}");
            Transform stall = GameObject.Find($"Stall_{i}").transform;
            yield return MovePipTo(stall.position.x, stall.position.y);
        }
        Check("All 5 delivered: W1 done", TaskManager.IsCompleted(w1));
    }

    // ------------------------------------------------------------------ W2

    private IEnumerator MirrorMaze()
    {
        Check("No Blue Sight before W2", !ActionEnabled("BlueSight"));
        yield return Use("NPC_MadameMirabel");
        Check("W2 started", Tasks.IsActive(w2));
        yield return null;
        Check("...and Blue Sight was given", ActionEnabled("BlueSight") && GameManager.Instance.Save.HasFlag("ability_bluesight"));

        // A fake mirror only talks.
        yield return Use("FakeMirror_1"); // Walks Pip to the mirror and uses it.
        Check("Fake mirror doesn't move Pip", Mathf.Abs(Pip.transform.position.x - X("FakeMirror_1")) < 0.5f);

        // Blue Sight reveals the tricks. (Pip's own PlayerBlueSight would switch it straight back
        // off because no button is held, so it's paused for this check.)
        SpriteRenderer crack = GameObject.Find("FakeMirror_1").transform.Find("Crack/Sprite").GetComponent<SpriteRenderer>();
        Collider2D fakeFloor = GameObject.Find("FakeFloor").GetComponentInChildren<Collider2D>(); // Usually none at all.
        SpriteRenderer fakeFloorSprite = GameObject.Find("FakeFloor").GetComponentInChildren<SpriteRenderer>();
        Collider2D secret = GameObject.Find("SecretPlatform").GetComponent<Collider2D>();
        Check("Blue Sight off: crack hidden, secret platform not there", !crack.enabled && !secret.enabled);
        Check("Fake floor is never solid", fakeFloor == null || !fakeFloor.enabled);
        Check("Fake floor looks solid without Blue Sight", fakeFloorSprite.color.a > 0.95f);

        PlayerBlueSight blueSight = Pip.GetComponent<PlayerBlueSight>();
        if (blueSight != null) blueSight.enabled = false;
        BlueSight.SetActive(true);
        yield return new WaitForSeconds(0.4f);
        Check("Blue Sight on: the crack shows", crack.enabled && crack.color.a > 0.9f);
        Check("Blue Sight on: the fake floor turns see-through", fakeFloorSprite.color.a < 0.5f);
        Check("Blue Sight on: the secret platform is solid", secret.enabled);
        BlueSight.SetActive(false);
        if (blueSight != null) blueSight.enabled = true;
        yield return new WaitForSeconds(0.3f);

        // Real mirrors: through the first wall, then the second.
        yield return ThroughMirror("Mirror_A", "Mirror_B");
        yield return ThroughMirror("Mirror_C", "Mirror_D");

        yield return MovePipTo(X("Area_W2_Exit"), FloorY);
        Check("Reaching the exit finishes W2", TaskManager.IsCompleted(w2));
    }

    private IEnumerator ThroughMirror(string from, string to)
    {
        yield return MovePipTo(X(from), FloorY);
        var mirror = GameObject.Find(from).GetComponent<MirrorTeleporter>();
        Check($"'{from}' can be used", InteractionInput.Instance.Target == mirror && InteractionInput.Instance.TryInteract());
        yield return WaitUntil(() => !mirror.IsTravelling, 3f, "the mirror trip");
        Check($"'{from}' takes Pip to '{to}'", Mathf.Abs(Pip.transform.position.x - X(to)) < 0.3f);
        Check("Pip free again after the mirror", !PlayerInputLock.IsLocked);
    }

    // ------------------------------------------------------------------ W3

    private IEnumerator Coaster()
    {
        yield return Use("NPC_CoasterCarl");
        Check("W3 started", Tasks.IsActive(w3));

        // The wheel turns, and a gondola carries Pip.
        RotatingWheel wheel = FindAnyObjectByType<RotatingWheel>();
        Rigidbody2D gondola = wheel.Gondolas[0];
        Vector2 start = gondola.position;
        yield return new WaitForSeconds(0.5f);
        Check("The Ferris wheel turns", (gondola.position - start).magnitude > 0.2f);

        Pip.Teleport(gondola.position + new Vector2(0f, 0.3f), 0f);
        yield return new WaitForSeconds(0.8f);
        Vector2 offset = (Vector2)Pip.transform.position - gondola.position;
        Check($"Pip rides the gondola (offset {offset})", Mathf.Abs(offset.x) < 1.4f && offset.y > 0f && offset.y < 0.6f);

        // Switch 1 by pressing F; 2-4 by hitting them (what the mallet and coins do).
        yield return MovePipTo(X("Switch_1"), FloorY);
        Check("Switch_1 flips with F", InteractionInput.Instance.TryInteract() && GameObject.Find("Switch_1").GetComponent<TaskSwitch>().IsOn);
        Check("Tracker: (1/4)", Tasks.GetObjectiveText(w3) == "Flip the coaster switches (1/4)");
        for (int i = 2; i <= 4; i++)
        {
            GameObject sw = GameObject.Find($"Switch_{i}");
            sw.GetComponent<TaskSwitch>().TakeDamage(new DamageInfo(1, Vector2.zero, sw.transform.position, Pip.gameObject));
        }
        yield return null;
        Check("Switches are remembered in the save", GameManager.Instance.Save.HasFlag("switch_coaster_switch_4"));
        Check("4 switches -> the waves start and (with no enemies yet) clear -> W3 done", TaskManager.IsCompleted(w3));
        Check("The teammate's WaveSpawner ran", FindAnyObjectByType<WaveSpawner>().IsCleared);
        Check("Reward: Juggle Shot", GameManager.Instance.Save.HasFlag("ability_juggle") && PlayerAbilities.IsUnlocked(Ability.JuggleShot));
    }

    // ------------------------------------------------------------------ W4

    private IEnumerator Audition()
    {
        yield return Use("NPC_DirectorGuffaw");
        Check("W4 started", Tasks.IsActive(w4));
        RhythmMinigame game = FindAnyObjectByType<RhythmMinigame>(FindObjectsInactive.Include);

        // Round 1: hit 4, press one note far too early (a miss), let the rest pass -> fail.
        yield return StartRound(game);
        for (int note = 0; note < game.NotesTotal && game.IsPlaying; note++)
        {
            if (note < 4)
                yield return PressOnBeat(game);
            else if (note == 4)
            {
                yield return WaitUntil(() => game.OffsetFromNextNote > -0.45f, 5f, "an early press");
                int misses = game.Misses;
                game.Press();
                Check("An off-beat press counts as a miss", game.Misses == misses + 1);
            }
            else
                break; // Let the rest go by.
        }
        yield return WaitUntil(() => !game.IsPlaying, 15f, "round 1 to end");
        Check($"Round 1: {game.Hits} hits is not enough", !game.LastResultPassed && game.Hits == 4);
        Check("Fail dialogue plays, task still running", Runner.IsRunning && Tasks.IsActive(w4));
        yield return FinishConversation();
        Check("Pip free after the round", !PlayerInputLock.IsLocked);

        // Round 2: hit 6 of 8 -> pass.
        yield return StartRound(game);
        for (int note = 0; note < 6; note++)
            yield return PressOnBeat(game);
        yield return WaitUntil(() => !game.IsPlaying, 15f, "round 2 to end");
        Check($"Round 2: {game.Hits} hits passes", game.LastResultPassed && game.Hits == 6);
        Check("Passing finishes W4", TaskManager.IsCompleted(w4));
        yield return FinishConversation();
    }

    private IEnumerator StartRound(RhythmMinigame game)
    {
        yield return MovePipTo(X("AuditionStage"), FloorY);
        Check("Stage starts the audition", InteractionInput.Instance.TryInteract() && game.IsPlaying && PlayerInputLock.IsLocked);
    }

    private static IEnumerator PressOnBeat(RhythmMinigame game)
    {
        int resolved = game.Hits + game.Misses;
        while (game.IsPlaying && game.OffsetFromNextNote < -0.02f)
            yield return null;
        game.Press();
        while (game.IsPlaying && game.Hits + game.Misses == resolved)
            yield return null;
    }

    // ------------------------------------------------------------------ Helpers

    private IEnumerator Go(string scene)
    {
        _sceneLoaded = null;
        SceneLoader.Instance.LoadScene(scene, scene == "Wonderwick_Hub" ? "FromW4" : "FromHub");
        yield return WaitForScene(scene);
    }

    private IEnumerator WaitForScene(string scene)
    {
        yield return WaitUntil(() => _sceneLoaded == scene && !SceneLoader.Instance.IsLoading, 10f, $"scene '{scene}'");
        yield return null;
        yield return null;
    }

    private static float X(string objectName) => GameObject.Find(objectName).transform.position.x;

    private IEnumerator Use(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        yield return MovePipTo(target.transform.position.x, FloorY);
        bool started = InteractionInput.Instance.Target == target.GetComponent<Interactable>() && InteractionInput.Instance.TryInteract();
        Check($"Can use '{objectName}'", started);
        if (started && Runner.IsRunning)
            yield return FinishConversation();
        yield return null;
    }

    private IEnumerator PickUp(string boxName)
    {
        yield return MovePipTo(X(boxName), FloorY);
        var box = GameObject.Find(boxName).GetComponent<CarryableBox>();
        bool ok = InteractionInput.Instance.Target == box && InteractionInput.Instance.TryInteract();
        if (!ok)
            Fail($"Could not pick up '{boxName}'");
        yield return null; // PlayerAbilities applies its block in LateUpdate.
    }

    private static bool GateClosed(string gateName)
    {
        foreach (TaskGate gate in FindObjectsByType<TaskGate>())
            if (gate.name.StartsWith(gateName))
                return !gate.IsOpen;
        return false;
    }

    private static bool ActionEnabled(string action)
    {
        InputAction a = Pip.InputActions.FindActionMap("Player").FindAction(action);
        return a != null && a.enabled;
    }
}
