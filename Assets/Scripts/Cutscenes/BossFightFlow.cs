using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Runs the whole Chortle encounter in the Laugh Tower (ChortleArena):
///
///   1. Pip arrives  -> the INTRO cutscene plays (watched or skipped)
///   2. the boss's StartFight()                       (fight; the boss bar shows)
///   3. the boss is beaten (IBossFight.Defeated)
///   4. the OUTRO cutscene plays
///   5. REWARDS: journal page 2 (JournalPages.Add) and +1 max Smile Star (SmileStarBonus),
///      flag "chortle_defeated", and the game is saved OUTSIDE the tower (Return Scene/Spawn),
///      so Continue never drops Pip back into the fight or the End of Demo screen
///   6. the End of Demo scene loads
///
/// Coming back after winning: no cutscene, no fight, the boss is gone, and the exit is open.
///
/// Setup: one in ChortleArena (the Part 8 builder makes it). "Boss" is any component that
/// implements IBossFight: ChortleFight on Chortle.
/// </summary>
public class BossFightFlow : MonoBehaviour
{
    public const string DefeatedFlag = "chortle_defeated";

    [SerializeField, Tooltip("A component implementing IBossFight (ChortleFight on Chortle).")]
    private MonoBehaviour boss;

    [Header("Cutscenes")]
    [SerializeField] private PlayableDirector intro;
    [SerializeField] private PlayableDirector outro;

    [Header("Rewards")]
    [SerializeField] private JournalPageData rewardPage;
    [SerializeField, Min(0)] private int rewardSmileStars = 1;

    [Header("Afterwards")]
    [SerializeField] private string endScene = "EndOfDemo";
    [SerializeField, Tooltip("Continue starts here after the win.")]
    private string returnScene = "Wonderwick_Hub";
    [SerializeField] private string returnSpawn = "FromTower";

    public enum Stage { Waiting, Intro, Fighting, Outro, Done }
    public Stage Current { get; private set; } = Stage.Waiting;

    public IBossFight Boss => boss as IBossFight;

    private void OnEnable() => GameEvents.SceneLoaded += OnSceneLoaded;

    private void OnDisable()
    {
        GameEvents.SceneLoaded -= OnSceneLoaded;
        if (Boss != null)
            Boss.Defeated -= OnBossDefeated;
    }

    private void OnSceneLoaded(string sceneName, string spawnPointId)
    {
        if (Current != Stage.Waiting || GameManager.Instance == null)
            return;

        if (Boss == null)
        {
            Debug.LogError("BossFightFlow: 'Boss' must be a component that implements IBossFight.", this);
            return;
        }

        if (GameManager.Instance.Save.HasFlag(DefeatedFlag))
        {
            Current = Stage.Done; // Already won: nothing to do here any more.
            boss.gameObject.SetActive(false); // Chortle is gone once he's been beaten.
            return;
        }

        Boss.Defeated += OnBossDefeated;
        Current = Stage.Intro;
        if (intro == null || CutscenePlayer.Instance == null || !CutscenePlayer.Instance.Play(intro, StartFight))
            StartFight();
    }

    private void StartFight()
    {
        Current = Stage.Fighting;
        Boss.StartFight();
    }

    private void OnBossDefeated()
    {
        Boss.Defeated -= OnBossDefeated;
        Current = Stage.Outro;
        if (outro == null || CutscenePlayer.Instance == null || !CutscenePlayer.Instance.Play(outro, GiveRewardsAndEnd))
            GiveRewardsAndEnd();
    }

    private void GiveRewardsAndEnd()
    {
        Current = Stage.Done;
        GameManager gm = GameManager.Instance;
        gm.Save.SetFlag(DefeatedFlag);
        JournalPages.Add(rewardPage);
        if (SmileStarBonus.Instance != null)
            SmileStarBonus.Instance.Award(rewardSmileStars);
        gm.SaveGameAt(returnScene, returnSpawn);

        SceneLoader.Instance.LoadScene(endScene);
    }
}
