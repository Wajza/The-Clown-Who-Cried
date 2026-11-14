using UnityEngine;

/// <summary>
/// What my teammate's PlayerHealth needs to offer for story rewards like "+1 max Smile Star
/// after beating Chortle": a bonus that Star Fragments DON'T overwrite (TEAMMATE_TODO #2).
///
/// TODO (teammate): add ": IPermanentStarBonus" to PlayerHealth and implement this method so the
/// max is   start + fragment bonus + permanent bonus. Nothing else changes on my side: the
/// SmileStarBonus bridge below finds the interface by itself and stops using its stand-in.
/// </summary>
public interface IPermanentStarBonus
{
    /// <summary>Sets the extra max Smile Stars from story rewards (replaces the previous value).</summary>
    void SetPermanentBonusMaxStars(int bonus);
}

/// <summary>
/// Gives Pip her story-reward max Smile Stars (SaveData.bonusSmileStars) in every level.
///
///   Award(1)   adds one (e.g. after Chortle), saves, applies it now.
///
/// HOW IT APPLIES THE BONUS
///   - If Pip's PlayerHealth implements IPermanentStarBonus: that method, on every level load.
///   - Until then (STAND-IN): PlayerHealth.SetBonusMaxStars(fragment bonus + story bonus).
///     PlayerStarFragments overwrites that bonus whenever a fragment is collected or a save is
///     loaded, so this checks every frame and puts it back. Side effect: after collecting a
///     fragment, the re-added star is refilled (SetBonusMaxStars heals new stars). That's why the
///     real fix is my teammate's method.
///
/// Setup: on the GameSystems prefab (the builder adds it).
/// </summary>
public class SmileStarBonus : MonoBehaviour
{
    public static SmileStarBonus Instance { get; private set; }

    /// <summary>Extra max stars from story rewards, from the save.</summary>
    public static int Bonus => GameManager.Instance != null ? GameManager.Instance.Save.bonusSmileStars : 0;

    /// <summary>True while the stand-in is used (Pip has no IPermanentStarBonus yet).</summary>
    public bool UsingStandIn { get; private set; }

    /// <summary>Gives Pip more max Smile Stars for good (saved).</summary>
    public void Award(int stars)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || stars <= 0)
            return;
        gm.Save.bonusSmileStars += stars;
        gm.SaveGame();
        _appliedFor = null; // Apply again now.
    }

    // ------------------------------------------------------------------ Private state

    private PlayerController _appliedFor; // The Pip we last applied the bonus to.
    private int _expectedMax = -1;        // Stand-in: the max we set, to notice when it's overwritten.

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void LateUpdate()
    {
        PlayerController pip = PlayerController.Current;
        if (pip == null)
            return;

        // Teammate's method available: tell it once per Pip (and again after an Award).
        var permanent = pip.GetComponent<IPermanentStarBonus>();
        if (permanent != null)
        {
            UsingStandIn = false;
            if (_appliedFor != pip)
            {
                permanent.SetPermanentBonusMaxStars(Bonus);
                _appliedFor = pip;
            }
            return;
        }

        // Stand-in. Nothing to do while there's no story bonus.
        UsingStandIn = true;
        if (Bonus <= 0)
            return;

        PlayerHealth health = pip.GetComponent<PlayerHealth>();
        PlayerStarFragments fragments = pip.GetComponent<PlayerStarFragments>();
        if (health == null)
            return;

        // Re-apply when it's a new Pip, after an Award, or when Star Fragments overwrote it.
        if (_appliedFor != pip || health.MaxStars != _expectedMax)
        {
            int fragmentBonus = fragments != null ? fragments.FragmentCount / fragments.FragmentsPerStar : 0;
            health.SetBonusMaxStars(fragmentBonus + Bonus);
            _expectedMax = health.MaxStars;
            _appliedFor = pip;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;
}
