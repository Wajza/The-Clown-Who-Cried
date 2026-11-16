using System.Collections;
using UnityEngine;

/// <summary>
/// An automatic check of Part 8 (cutscenes, the Chortle encounter, End of Demo):
///
///   1. New Game: the opening cutscene plays once in Pip's house (Pip locked, HUD hidden), its
///      conversations pause the timeline, and it never plays again.
///   2. Chortle's arena: the intro plays; SKIPPING it starts the fight; the exit is locked.
///   3. Beat the (placeholder) boss: the outro plays, then the rewards (journal page 2, +1 max
///      Smile Star, "chortle_defeated"), and the save points OUTSIDE the tower.
///   4. End of Demo: no HUD, no autosave; skipping returns to the title.
///   5. Continue: Pip is outside the Laugh Tower with +1 max star, which survives collecting a
///      Star Fragment (my teammate's code resets the bonus; SmileStarBonus puts it back).
///   6. Back in the arena: no cutscene, no boss, the exit is open.
/// Results: Logs/Part8TestResults.txt.
/// </summary>
public class CutsceneTest : SystemTestBase
{
    [SerializeField] private JournalPageData rewardPage;

    protected override string ResultsFileName => "Part8TestResults.txt";
    protected override string LogTag => "Part8Test";

    private string _sceneLoaded;
    private string _cutsceneStarted, _cutsceneEnded;

    private void OnEnable()
    {
        GameEvents.SceneLoaded += OnSceneLoaded;
        GameEvents.CutsceneStarted += OnStarted;
        GameEvents.CutsceneEnded += OnEnded;
    }

    private void OnDisable()
    {
        GameEvents.SceneLoaded -= OnSceneLoaded;
        GameEvents.CutsceneStarted -= OnStarted;
        GameEvents.CutsceneEnded -= OnEnded;
    }

    private void OnSceneLoaded(string scene, string spawn) => _sceneLoaded = scene;
    private void OnStarted(string name) => _cutsceneStarted = name;
    private void OnEnded(string name) => _cutsceneEnded = name;

    private static CutscenePlayer Cutscenes => CutscenePlayer.Instance;
    private static PlayerController Pip => PlayerController.Current;
    private static GameState State => GameManager.Instance.State;

    // ------------------------------------------------------------------ Test flow

    protected override IEnumerator RunTest()
    {
        Check("Test driver references are assigned", rewardPage != null);
        if (rewardPage == null)
            yield break;

        yield return WaitUntil(() => GameManager.Instance != null && _sceneLoaded != null, 5f, "first SceneLoaded");
        Check("New Game starts", GameManager.Instance.NewGame());
        yield return WaitForScene("Tumbleton_PipHouse");

        yield return Opening();
        yield return BossIntroAndFight();
        yield return EndOfDemo();
        yield return ContinueAfterTheWin();
    }

    // ------------------------------------------------------------------ 1. Opening

    private IEnumerator Opening()
    {
        Check("Opening cutscene plays on New Game", Cutscenes.IsPlaying && _cutsceneStarted == "Timeline_Opening");
        yield return new WaitForSeconds(0.8f); // The first dialogue clip starts at 0.5 s.
        Check("Cutscene: Pip locked, HUD hidden", PlayerInputLock.IsLocked && !FindAnyObjectByType<HudController>().Visible);
        Check("Its conversation pauses the timeline", Runner.IsRunning && Cutscenes.Current.time < 1.2);

        yield return WatchToTheEnd();
        Check("Opening finished: Playing, Pip free", State == GameState.Playing && !PlayerInputLock.IsLocked);
        Check("CutsceneEnded raised", _cutsceneEnded == "Timeline_Opening");
        Check("Remembered as seen", GameManager.Instance.Save.HasFlag("cs_opening_seen"));

        _cutsceneStarted = null;
        yield return Go("Tumbleton_PipHouse", "Bed");
        Check("The opening doesn't play twice", !Cutscenes.IsPlaying && _cutsceneStarted == null);
    }

    // ------------------------------------------------------------------ 2-3. Boss

    private IEnumerator BossIntroAndFight()
    {
        yield return Go("ChortleArena", "Start");
        var flow = FindAnyObjectByType<BossFightFlow>();
        IBossFight boss = flow != null ? flow.Boss : null; // The real Chortle or the placeholder.
        IDamageable bossTarget = (boss as Component) != null ? ((Component)boss).GetComponent<IDamageable>() : null;
        Check("Arena has the boss flow and a boss", flow != null && boss != null && bossTarget != null);
        Check("Boss intro plays", Cutscenes.IsPlaying && _cutsceneStarted == "Timeline_ChortleIntro" && flow.Current == BossFightFlow.Stage.Intro);

        yield return new WaitForSeconds(0.8f);
        Cutscenes.Skip();
        yield return null;
        Check("Skipping ends the cutscene and closes its conversation", !Cutscenes.IsPlaying && !Runner.IsRunning);
        Check("...and the fight starts", flow.Current == BossFightFlow.Stage.Fighting &&
            BossHealthSource.Active != null && BossHealthSource.Active.IsFighting);
        Check("Boss health bar shows", FindAnyObjectByType<BossHealthBar>().Visible);
        Check("Pip free to fight", !PlayerInputLock.IsLocked && State == GameState.Playing);
        Check("The exit is locked during the fight", !GameObject.Find("ExitToHub").GetComponent<Door>().Requirement.IsMet);

        int baseMax = Pip.GetComponent<PlayerHealth>().MaxStars;
        _maxBeforeReward = baseMax;

        // Hit until beaten: one big hit per frame. (The real Chortle ignores hits during his short
        // phase intros, and in phase 3 only Tear Burst really hurts, so hit like a Tear Burst.)
        float giveUp = Time.time + 20f;
        while (!boss.IsDefeated && Time.time < giveUp)
        {
            bossTarget.TakeDamage(new DamageInfo(30, Vector2.zero, ((Component)boss).transform.position,
                Pip.gameObject, DamageType.TearBurst));
            yield return null;
        }
        Check("Boss defeated", boss.IsDefeated);
        Check("Outro plays", Cutscenes.IsPlaying && _cutsceneStarted == "Timeline_ChortleOutro");

        _sceneLoaded = null;
        yield return WatchToTheEnd();
        yield return WaitForScene("EndOfDemo");

        SaveData save = GameManager.Instance.Save;
        Check("Reward: journal page 2 found", JournalPages.IsFound(rewardPage));
        Check("Reward: +1 story Smile Star saved", save.bonusSmileStars == 1);
        Check("Flag 'chortle_defeated' set", save.HasFlag(BossFightFlow.DefeatedFlag));
        SaveData disk = SaveSystem.Read();
        Check("Saved OUTSIDE the tower (Continue starts there)",
            disk != null && disk.sceneName == "Wonderwick_Hub" && disk.spawnPointId == "FromTower" && disk.bonusSmileStars == 1);
    }

    private int _maxBeforeReward;

    // ------------------------------------------------------------------ 4. End of Demo

    private IEnumerator EndOfDemo()
    {
        var sequence = FindAnyObjectByType<EndOfDemoSequence>();
        Check("End of Demo screen with credits", sequence != null && GameObject.Find("Credits") != null);
        Check("End of Demo: no Pip, no HUD", Pip == null && !FindAnyObjectByType<HudController>().Visible);
        yield return new WaitForSeconds(1f);
        Check("Credits keep going until skipped or finished", !sequence.Finished);
        Check("End of Demo didn't overwrite the save", SaveSystem.Read()?.sceneName == "Wonderwick_Hub");

        _sceneLoaded = null;
        sequence.Skip();
        yield return WaitForScene("Title");
        Check("Skipping goes back to the title screen", State == GameState.Title);
    }

    // ------------------------------------------------------------------ 5-6. After

    private IEnumerator ContinueAfterTheWin()
    {
        _sceneLoaded = null;
        Check("Continue works", GameManager.Instance.ContinueGame());
        yield return WaitForScene("Wonderwick_Hub");
        CheckNear("Continue: outside the Laugh Tower", PipPosition(), SpawnPoint.Find("FromTower").Position);
        yield return null;
        PlayerHealth health = Pip.GetComponent<PlayerHealth>();
        Check($"+1 max Smile Star after the win ({_maxBeforeReward} -> {health.MaxStars})", health.MaxStars == _maxBeforeReward + 1);
        Check("(The stand-in is used until my teammate adds IPermanentStarBonus)", SmileStarBonus.Instance.UsingStandIn);

        PlayerStarFragments.Current.Collect("cutscene_test_fragment");
        yield return null;
        yield return null;
        Check("The bonus star survives collecting a Star Fragment", health.MaxStars == _maxBeforeReward + 1);

        _cutsceneStarted = null;
        yield return Go("ChortleArena", "Start");
        Check("Back in the arena: no cutscene", !Cutscenes.IsPlaying && _cutsceneStarted == null);
        var flowAfter = FindAnyObjectByType<BossFightFlow>();
        var bossAfter = flowAfter.Boss as Component;
        Check("Back in the arena: no boss", (bossAfter == null || !bossAfter.gameObject.activeInHierarchy) &&
            flowAfter.Current == BossFightFlow.Stage.Done);
        Check("Back in the arena: the exit is open", GameObject.Find("ExitToHub").GetComponent<Door>().Requirement.IsMet);
        Check("No boss bar", !FindAnyObjectByType<BossHealthBar>().Visible);
    }

    // ------------------------------------------------------------------ Helpers

    /// <summary>Read every conversation of the playing cutscene until it ends.</summary>
    private IEnumerator WatchToTheEnd()
    {
        float end = Time.realtimeSinceStartup + 30f;
        while (Cutscenes.IsPlaying && Time.realtimeSinceStartup < end)
        {
            if (Runner.IsRunning)
                yield return CompleteAndAdvance();
            else
                yield return null;
        }
        Check("Cutscene could be watched to the end", !Cutscenes.IsPlaying);
        yield return null;
    }

    private IEnumerator Go(string scene, string spawn)
    {
        _sceneLoaded = null;
        SceneLoader.Instance.LoadScene(scene, spawn);
        yield return WaitForScene(scene);
    }

    private IEnumerator WaitForScene(string scene)
    {
        yield return WaitUntil(() => _sceneLoaded == scene && !SceneLoader.Instance.IsLoading, 10f, $"scene '{scene}'");
        yield return null;
        yield return null;
    }
}
