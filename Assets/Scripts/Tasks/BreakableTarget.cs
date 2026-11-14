using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Something Pip breaks for a task: P3's balloons (popped by Star Coins), P4's pots (smashed by
/// the mallet). My teammate's coins and mallet hit anything with the IDamageable interface, so
/// they work on this without knowing what it is.
///
/// WHEN IT BREAKS (after Hits To Break hits)
///   - sends its task signal (e.g. "p3_balloon"), which a SignalObjective counts,
///   - remembers it in the save (flag "broken_[Save Id]"), so it stays broken after a reload,
///   - switches itself off.
///
/// Only While Task Active: if set, hits do nothing until that task is running. Otherwise a
/// balloon popped before talking to the vendor would be lost for good and the count could
/// never reach 8.
///
/// COINS ONLY? My teammate's DamageInfo can't tell a coin from a mallet hit yet
/// (TEAMMATE_TODO #3). TODO: once DamageType.Coin exists, add a "coins only" option here.
/// For P3 it doesn't matter: Pip only gets the mallet in P4.
///
/// Setup: a GameObject with a Collider2D (the part that can be hit), a sprite child, and this
/// script. For P4's last pot, also give it the "Pogo" tag so the mallet's down-swing bounces.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BreakableTarget : MonoBehaviour, IDamageable
{
    [SerializeField, Tooltip("Task signal sent when it breaks, e.g. 'p3_balloon'.")]
    private string signalKey;

    [SerializeField, Tooltip("Unique name in the whole game, e.g. 'tumbleton_balloon_3'. Used to remember it's broken.")]
    private string saveId;

    [SerializeField, Min(1)] private int hitsToBreak = 1;

    [SerializeField, Tooltip("Optional: ignore hits until this task is running.")]
    private TaskData onlyWhileTaskActive;

    [SerializeField, Tooltip("Optional: flashes white when hit.")]
    private SpriteRenderer sprite;

    // ------------------------------------------------------------------ Public API

    /// <summary>Fired with this target when it breaks.</summary>
    public event Action<BreakableTarget> Broken;

    public string SaveId => saveId;
    public int HitsLeft { get; private set; }
    public bool IsBroken => IsBrokenInSave(saveId);

    /// <summary>Can hits break it right now?</summary>
    public bool AcceptsHits =>
        onlyWhileTaskActive == null || (TaskManager.Instance != null && TaskManager.Instance.IsActive(onlyWhileTaskActive));

    public void TakeDamage(DamageInfo info)
    {
        if (!isActiveAndEnabled || !AcceptsHits)
            return;

        HitsLeft -= Mathf.Max(1, info.Amount);
        if (HitsLeft > 0)
        {
            if (sprite != null)
                StartCoroutine(Flash());
            return;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.Save.SetFlag(BrokenFlag(saveId));
        Broken?.Invoke(this);
        GameEvents.RaiseTaskSignal(signalKey);
        gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake() => HitsLeft = hitsToBreak;

    private void OnEnable() => GameEvents.SceneLoaded += OnSceneLoaded;
    private void OnDisable() => GameEvents.SceneLoaded -= OnSceneLoaded;

    private void Start() => HideIfBroken();

    // The save is only ready by SceneLoaded when Play starts in this scene in the Editor.
    private void OnSceneLoaded(string sceneName, string spawnPointId) => HideIfBroken();

    // ------------------------------------------------------------------ Helpers

    private void HideIfBroken()
    {
        if (IsBroken)
            gameObject.SetActive(false);
    }

    private static string BrokenFlag(string id) => $"broken_{id}";

    private static bool IsBrokenInSave(string id)
    {
        GameManager gm = GameManager.Instance;
        return gm != null && !string.IsNullOrEmpty(id) && gm.Save.HasFlag(BrokenFlag(id));
    }

    private IEnumerator Flash()
    {
        Color original = sprite.color;
        sprite.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        sprite.color = original;
    }
}
