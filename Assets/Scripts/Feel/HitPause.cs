using System.Collections;
using UnityEngine;

/// <summary>
/// "Hit stop": freezes the game for a split second when Pip's mallet lands, so hits feel heavy.
/// Put it on Pip (next to PlayerMallet). Respects GameFeelSettings.HitPauseEnabled.
///
/// It sets Time.timeScale to (almost) 0 and waits in real time. Several hits in a row extend
/// the pause instead of stacking. It never starts while the game is already paused, and it
/// never "unpauses" a pause menu that opened during the freeze.
/// </summary>
[RequireComponent(typeof(PlayerMallet))]
public class HitPause : MonoBehaviour
{
    [SerializeField, Range(0f, 0.2f), Tooltip("Freeze length when the mallet hits something (seconds, real time).")]
    private float malletHitPause = 0.06f;
    [SerializeField, Range(0f, 0.2f), Tooltip("Freeze length when the mallet hits the boss.")]
    private float bossHitPause = 0.09f;
    [SerializeField, Range(0f, 0.5f), Tooltip("Game speed during the freeze. 0 = full stop, 0.05 = almost stopped.")]
    private float frozenTimeScale = 0f;

    private PlayerMallet _mallet;
    private Coroutine _routine;
    private float _resumeAt;
    private float _timeScaleBefore = 1f;

    private void Awake()
    {
        _mallet = GetComponent<PlayerMallet>();
    }

    private void OnEnable() => _mallet.HitLanded += OnHitLanded;

    private void OnDisable()
    {
        _mallet.HitLanded -= OnHitLanded;
        EndFreeze(); // Never leave the game frozen.
    }

    private void OnHitLanded(IDamageable target, DamageInfo info)
    {
        bool isBoss = target is ChortleBoss;
        Freeze(isBoss ? bossHitPause : malletHitPause);
    }

    /// <summary>Freezes the game for 'seconds' of real time. Public so other moments can use it too.</summary>
    public void Freeze(float seconds)
    {
        if (!GameFeelSettings.HitPauseEnabled || seconds <= 0f)
            return;

        if (_routine != null)
        {
            // Already frozen: just make the freeze last a little longer if needed.
            _resumeAt = Mathf.Max(_resumeAt, Time.unscaledTime + seconds);
            return;
        }

        if (Time.timeScale <= 0f)
            return; // The game is paused (e.g. pause menu): don't interfere.

        _timeScaleBefore = Time.timeScale;
        _resumeAt = Time.unscaledTime + seconds;
        Time.timeScale = frozenTimeScale;
        _routine = StartCoroutine(WaitAndResume());
    }

    private IEnumerator WaitAndResume()
    {
        // Unscaled time keeps running while timeScale is 0.
        while (Time.unscaledTime < _resumeAt)
            yield return null;

        EndFreeze();
    }

    private void EndFreeze()
    {
        if (_routine == null)
            return; // Not frozen.

        StopCoroutine(_routine);
        _routine = null;

        // Only restore if nobody else changed the time scale meanwhile (e.g. a pause menu).
        if (Mathf.Approximately(Time.timeScale, frozenTimeScale))
            Time.timeScale = _timeScaleBefore;
    }
}
