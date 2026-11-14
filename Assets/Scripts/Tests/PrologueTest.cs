using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// An automatic check of Part 6: the four Tumbleton prologue tasks, played from a NEW GAME.
///
///   P1 Wake Up             the front door is locked -> look in the mirror -> go outside
///   P2 Cross Honk Street   Officer Honk gives it; bumper cars move and hurt; checkpoint; far kerb
///   P3 Pop the Balloon Wall  Balloon Betty gives it (only after P2); 8 balloons; the wall opens
///   P4 Reflex Check        Old Tumbles gives it AND the mallet; 5 pots (last one "Pogo" over spikes)
///   -> the station gate opens; the Journal shows all four done; a reload keeps everything.
///
/// Balloons and pots are "hit" by calling TakeDamage, exactly what my teammate's coins and mallet
/// call. Results: Logs/Part6TestResults.txt.
/// </summary>
public class PrologueTest : SystemTestBase
{
    [SerializeField] private TaskData p1, p2, p3, p4;

    protected override string ResultsFileName => "Part6TestResults.txt";
    protected override string LogTag => "Part6Test";

    private const float FloorY = 0f; // The levels' floor.

    private string _sceneLoaded;
    private void OnEnable() => GameEvents.SceneLoaded += OnSceneLoaded;
    private void OnDisable() => GameEvents.SceneLoaded -= OnSceneLoaded;
    private void OnSceneLoaded(string scene, string spawn) => _sceneLoaded = scene;

    private static TaskManager Tasks => TaskManager.Instance;
    private static PlayerController Pip => PlayerController.Current;

    // ------------------------------------------------------------------ Test flow

    protected override IEnumerator RunTest()
    {
        bool linked = p1 != null && p2 != null && p3 != null && p4 != null;
        Check("Test driver references are assigned", linked);
        if (!linked)
            yield break;

        yield return WaitUntil(() => GameManager.Instance != null && _sceneLoaded != null, 5f, "first SceneLoaded");
        Check("New Game starts", GameManager.Instance.NewGame());
        yield return WaitForScene("Tumbleton_PipHouse");

        yield return WakeUp();
        yield return HonkStreet();
        yield return BalloonWall();
        yield return ReflexCheck();
        yield return AfterThePrologue();
    }

    // ------------------------------------------------------------------ P1

    private IEnumerator WakeUp()
    {
        Check("P1 starts by itself in Pip's house", Tasks.IsActive(p1) && Tasks.GetStep(p1) == 0);
        Check("Tracker: 'Look in the mirror'", Tasks.GetObjectiveText(p1) == "Look in the mirror");

        // The front door won't open yet: Pip says why instead.
        yield return MovePipTo(X("FrontDoor"), FloorY);
        Check("Front door is usable (to hear why it's locked)", InteractionInput.Instance.TryInteract());
        Check("Locked door: Pip says something, no scene change", Runner.IsRunning && !SceneLoader.Instance.IsLoading);
        yield return FinishConversation();
        Check("Still in Pip's house", SceneLoader.CurrentSceneName == "Tumbleton_PipHouse");

        yield return Use("Mirror");
        Check("Mirror sets its flag", GameManager.Instance.Save.HasFlag("p1_looked_in_mirror"));
        Check("Mirror moves P1 on: 'Go outside'", Tasks.GetStep(p1) == 1 && Tasks.GetObjectiveText(p1) == "Go outside");

        yield return MovePipTo(X("FrontDoor"), FloorY);
        Check("Front door opens now", InteractionInput.Instance.TryInteract());
        yield return WaitForScene("Tumbleton");
        Check("Stepping outside finishes P1", TaskManager.IsCompleted(p1));
    }

    // ------------------------------------------------------------------ P2

    private IEnumerator HonkStreet()
    {
        // Balloon Betty won't give P3 before P2, and balloons ignore hits before P3.
        yield return Talk("NPC_BalloonBetty");
        Check("Betty gives nothing before P2 is done", !Tasks.IsActive(p3));
        Hit("Balloon_1");
        Check("Balloons ignore hits before P3", GameObject.Find("Balloon_1") != null);

        yield return Talk("NPC_OfficerHonk");
        Check("Officer Honk gives P2 (after P1)", Tasks.IsActive(p2));

        // The bumper cars drive around and hurt Pip.
        Transform car = GameObject.Find("BumperCar_1").transform;
        float start = car.position.x;
        yield return new WaitForSeconds(0.5f);
        Check("Bumper cars move", Mathf.Abs(car.position.x - start) > 0.5f);

        PlayerHealth health = Pip.GetComponent<PlayerHealth>();
        int stars = health.CurrentStars;
        Pip.Teleport(car.position, 0f);
        yield return WaitUntil(() => health.CurrentStars < stars, 2f, "a bumper car to hit Pip");
        Check("A bumper car costs Pip a star", health.CurrentStars == stars - 1);
        yield return new WaitForSeconds(1.5f); // Invulnerability and knockback wear off.

        yield return MovePipTo(18f, FloorY);
        Check("Checkpoint before the street becomes the respawn point",
            GameManager.Instance.Save.spawnPointId == "Tumbleton_HonkStreet");

        yield return MovePipTo(X("Area_P2_FarKerb"), FloorY);
        Check("Reaching the far kerb finishes P2", TaskManager.IsCompleted(p2));
    }

    // ------------------------------------------------------------------ P3

    private IEnumerator BalloonWall()
    {
        yield return Talk("NPC_BalloonBetty");
        Check("Betty gives P3 (after P2)", Tasks.IsActive(p3));
        Check("Tracker counts: 'Pop the balloons (0/8)'", Tasks.GetObjectiveText(p3) == "Pop the balloons (0/8)");
        Check("Balloon wall is closed", WallUp("TaskGate_BalloonWall"));

        for (int i = 1; i <= 3; i++)
            Hit($"Balloon_{i}");
        yield return null;
        Check("3 balloons popped: (3/8)", Tasks.GetObjectiveText(p3) == "Pop the balloons (3/8)");
        Check("The count is saved", SavedCount(p3) == 3);
        Check("Popped balloons are gone", GameObject.Find("Balloon_1") == null && GameObject.Find("Balloon_4") != null);

        for (int i = 4; i <= 8; i++)
            Hit($"Balloon_{i}");
        yield return null;
        Check("All 8 popped finishes P3", TaskManager.IsCompleted(p3));
        Check("The balloon wall opens", !WallUp("TaskGate_BalloonWall"));
    }

    // ------------------------------------------------------------------ P4

    private IEnumerator ReflexCheck()
    {
        Check("No mallet yet (Swing off)", !ActionEnabled("Swing"));
        yield return Talk("NPC_OldTumbles");
        Check("Old Tumbles gives P4 (after P3)", Tasks.IsActive(p4));
        yield return null; // PlayerAbilities applies in LateUpdate.
        Check("...and the mallet: Swing works now", ActionEnabled("Swing") && GameManager.Instance.Save.HasFlag("ability_mallet"));

        GameObject pogoPot = GameObject.Find("Pot_5_Pogo");
        GameObject spikes = GameObject.Find("Spikes");
        Check("Last pot is tagged 'Pogo' and floats above the spikes",
            pogoPot != null && pogoPot.CompareTag("Pogo") && spikes != null && spikes.CompareTag("Spikes") &&
            Mathf.Abs(pogoPot.transform.position.x - spikes.transform.position.x) < 0.5f && pogoPot.transform.position.y > 1f);
        Check("Pots are on the Enemy layer (what coins/mallet hit)", pogoPot.layer == LayerMask.NameToLayer("Enemy"));

        Check("Station gate closed before P4 is done", WallUp("TaskGate_Station"));
        for (int i = 1; i <= 4; i++)
            Hit($"Pot_{i}");
        yield return null;
        Check("4 pots: (4/5)", Tasks.GetObjectiveText(p4) == "Smash the pots (4/5)");
        Hit("Pot_5_Pogo");
        yield return null;
        Check("All 5 pots finish P4", TaskManager.IsCompleted(p4));
        Check("The station gate opens after P1-P4", !WallUp("TaskGate_Station"));
    }

    // ------------------------------------------------------------------ After

    private IEnumerator AfterThePrologue()
    {
        MenuController.Instance.OpenJournal();
        yield return null;
        JournalUI journal = MenuController.Instance.Journal;
        journal.SetTab((int)TaskData.City.Tumbleton);
        Check("Journal: all four prologue tasks done",
            journal.ListText.Contains("[x] Wake Up") && journal.ListText.Contains("[x] Cross Honk Street") &&
            journal.ListText.Contains("[x] Pop the Balloon Wall") && journal.ListText.Contains("[x] Reflex Check"));
        MenuController.Instance.CloseAll();
        yield return null;

        _sceneLoaded = null;
        SceneLoader.Instance.LoadScene("Tumbleton", "FromHouse");
        yield return WaitForScene("Tumbleton");
        Check("After a reload: balloons and pots stay broken",
            GameObject.Find("Balloon_8") == null && GameObject.Find("Pot_5_Pogo") == null);
        Check("After a reload: balloon wall and station gate stay open",
            !WallUp("TaskGate_BalloonWall") && !WallUp("TaskGate_Station"));
    }

    // ------------------------------------------------------------------ Helpers

    private IEnumerator WaitForScene(string scene)
    {
        yield return WaitUntil(() => _sceneLoaded == scene && !SceneLoader.Instance.IsLoading, 10f, $"scene '{scene}'");
        yield return null;
        yield return null;
    }

    private static float X(string objectName) => GameObject.Find(objectName).transform.position.x;

    /// <summary>Walk to an Interactable (NPC, mirror...) and use it, then finish the conversation.</summary>
    private IEnumerator Use(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        yield return MovePipTo(target.transform.position.x, FloorY);
        bool started = InteractionInput.Instance.Target == target.GetComponent<Interactable>() && InteractionInput.Instance.TryInteract();
        Check($"Can use '{objectName}'", started);
        if (started)
            yield return FinishConversation();
        yield return null;
    }

    private IEnumerator Talk(string npcName) => Use(npcName);

    /// <summary>What my teammate's coins and mallet do on a hit.</summary>
    private static void Hit(string targetName)
    {
        GameObject go = GameObject.Find(targetName);
        if (go == null)
            return;
        go.GetComponent<BreakableTarget>().TakeDamage(new DamageInfo(1, Vector2.zero, go.transform.position, Pip.gameObject));
    }

    /// <summary>Is the TaskGate whose name starts with 'gateName' still closed (its wall showing)?</summary>
    private static bool WallUp(string gateName)
    {
        foreach (TaskGate gate in FindObjectsByType<TaskGate>())
            if (gate.name.StartsWith(gateName))
                return !gate.IsOpen;
        return false;
    }

    private static int SavedCount(TaskData task)
    {
        SaveData disk = SaveSystem.Read();
        if (disk != null)
            foreach (TaskProgress progress in disk.activeTasks)
                if (progress.taskId == task.id)
                    return progress.count;
        return -1;
    }

    private static bool ActionEnabled(string action)
    {
        InputAction a = Pip.InputActions.FindActionMap("Player").FindAction(action);
        return a != null && a.enabled;
    }
}
