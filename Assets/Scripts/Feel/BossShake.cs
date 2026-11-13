using UnityEngine;

/// <summary>
/// Screen shake for the Chortle fight. Put it on Chortle (or any object in the boss scene and
/// assign the boss). Set any strength to 0 to turn that shake off.
/// </summary>
public class BossShake : MonoBehaviour
{
    [SerializeField, Tooltip("Found on this object if empty.")]
    private ChortleBoss boss;

    [SerializeField, Range(0f, 1f), Tooltip("When a warned attack goes off (pie volley, bomb volley).")]
    private float attackLaunched = 0.08f;
    [SerializeField, Range(0f, 1f), Tooltip("When a punchline bomb explodes.")]
    private float bombExplosion = 0.3f;
    [SerializeField, Range(0f, 1f), Tooltip("When a rubber-chicken cannon fires.")]
    private float cannonFired = 0.1f;
    [SerializeField, Range(0f, 1f), Tooltip("When a new phase starts.")]
    private float phaseChange = 0.45f;
    [SerializeField, Range(0f, 1f), Tooltip("When Chortle's mask breaks.")]
    private float defeated = 0.7f;

    private void Awake()
    {
        if (boss == null)
            boss = GetComponent<ChortleBoss>();
    }

    private void OnEnable()
    {
        if (boss != null)
        {
            boss.AttackLaunched += OnAttackLaunched;
            boss.PhaseChanged += OnPhaseChanged;
            boss.OnDefeated += OnDefeated;
        }
        PunchlineBomb.AnyExploded += OnBombExploded;
        RubberChickenCannon.AnyFired += OnCannonFired;
    }

    private void OnDisable()
    {
        if (boss != null)
        {
            boss.AttackLaunched -= OnAttackLaunched;
            boss.PhaseChanged -= OnPhaseChanged;
            boss.OnDefeated -= OnDefeated;
        }
        PunchlineBomb.AnyExploded -= OnBombExploded;
        RubberChickenCannon.AnyFired -= OnCannonFired;
    }

    private void OnAttackLaunched() => ScreenShake.Shake(attackLaunched);
    private void OnPhaseChanged(int phase) { if (phase > 1) ScreenShake.Shake(phaseChange); }
    private void OnDefeated() => ScreenShake.Shake(defeated);
    private void OnBombExploded(Vector2 position) => ScreenShake.Shake(bombExplosion);
    private void OnCannonFired(RubberChickenCannon cannon) => ScreenShake.Shake(cannonFired);
}
