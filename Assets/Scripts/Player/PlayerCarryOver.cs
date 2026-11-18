using System.Collections;
using UnityEngine;

/// <summary>
/// Keeps Pip's state when she goes through a door. Every scene creates its own Pip, so without
/// this she would arrive in each new scene with full stars, an empty Tear Gauge and no shield.
///
/// Carried over: Smile Stars, Balloon Shield, Tear Gauge, and the Confetti Popper boost (time left).
/// NOT carried over: Star Fragments and bonus max stars (Sarah's PlayerSaveBridge / SmileStarBonus
/// already do those), and coins in flight (Pip arrives with all her coins).
///
/// How: when this Pip is destroyed by a scene change, it writes its state to a static "suitcase";
/// the next scene's Pip unpacks it a couple of frames after it appears (after the other
/// systems have set max stars). The suitcase is emptied on the title screen, so New Game and
/// Continue always start fresh. Dying and respawning is a different thing (full stars, Revive).
///
/// Setup: on Pip (the Combat setup tool adds it to the Pip prefab).
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
public class PlayerCarryOver : MonoBehaviour
{
    private struct Suitcase
    {
        public int stars;
        public bool shield;
        public int tears;
        public float boostMultiplier;
        public float boostTimeLeft;
    }

    private static Suitcase? s_suitcase;
    private bool _sceneIsChanging;

    // ------------------------------------------------------------------ Packing

    private void OnEnable() => GameEvents.SceneLoadStarted += OnSceneLoadStarted;
    private void OnDisable() => GameEvents.SceneLoadStarted -= OnSceneLoadStarted;

    // Pack when a scene change STARTS (Pip still exists and is healthy), not on every destroy.
    private void OnSceneLoadStarted(string sceneName)
    {
        _sceneIsChanging = true;

        var health = GetComponent<PlayerHealth>();
        if (health.IsDead)
        {
            s_suitcase = null; // A dead Pip arrives fresh.
            return;
        }

        var tears = GetComponent<TearGauge>();
        var coins = GetComponent<PlayerCoinThrower>();
        s_suitcase = new Suitcase
        {
            stars = health.CurrentStars,
            shield = health.HasShield,
            tears = tears != null ? tears.Value : 0,
            boostMultiplier = coins != null ? coins.DamageBoostMultiplier : 1f,
            boostTimeLeft = coins != null ? coins.DamageBoostTimeLeft : 0f,
        };
    }

    // ------------------------------------------------------------------ Unpacking

    private IEnumerator Start()
    {
        // Wait until the scene change is completely over (SceneLoaded has run, so max stars from
        // fragments/bonus are set), then one more frame for good measure.
        yield return null;
        while (SceneLoader.Instance != null && SceneLoader.Instance.IsLoading)
            yield return null;
        yield return null;

        if (s_suitcase == null || _sceneIsChanging)
            yield break;
        Suitcase bag = s_suitcase.Value;
        s_suitcase = null;

        GetComponent<PlayerHealth>().RestoreState(bag.stars, bag.shield);

        var tears = GetComponent<TearGauge>();
        if (tears != null && bag.tears > 0)
            tears.Add(bag.tears - tears.Value);

        var coins = GetComponent<PlayerCoinThrower>();
        if (coins != null && bag.boostTimeLeft > 0f)
            coins.ApplyDamageBoost(bag.boostMultiplier, bag.boostTimeLeft);
    }

    // ------------------------------------------------------------------ Fresh starts

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        s_suitcase = null;
        GameEvents.GameStateChanged -= OnGameStateChanged;
        GameEvents.GameStateChanged += OnGameStateChanged;
    }

    // The title screen (Quit to Title, End of Demo): the next game starts fresh.
    private static void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Title)
            s_suitcase = null;
    }
}
