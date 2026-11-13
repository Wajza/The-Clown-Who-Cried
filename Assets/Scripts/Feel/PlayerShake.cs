using UnityEngine;

/// <summary>
/// Screen shake for Pip's moments. Put it on Pip. Set any strength to 0 to turn that shake off.
/// Strength ~ how far the camera jolts in tiles (0.1 = tap, 0.5 = big).
/// </summary>
public class PlayerShake : MonoBehaviour
{
    [SerializeField, Range(0f, 1f), Tooltip("When the mallet hits something. Jolts in the hit direction.")]
    private float malletHit = 0.12f;
    [SerializeField, Range(0f, 1f), Tooltip("When Pip loses a star.")]
    private float starLost = 0.35f;
    [SerializeField, Range(0f, 1f), Tooltip("When Pip releases a Tear Burst.")]
    private float tearBurst = 0.5f;

    private PlayerMallet _mallet;
    private PlayerHealth _health;
    private TearGauge _tearGauge;

    private void Awake()
    {
        _mallet = GetComponent<PlayerMallet>();
        _health = GetComponent<PlayerHealth>();
        _tearGauge = GetComponent<TearGauge>();
    }

    private void OnEnable()
    {
        if (_mallet != null) _mallet.HitLanded += OnMalletHit;
        if (_health != null) _health.StarLost += OnStarLost;
        if (_tearGauge != null) _tearGauge.TearBurstReleased += OnTearBurst;
    }

    private void OnDisable()
    {
        if (_mallet != null) _mallet.HitLanded -= OnMalletHit;
        if (_health != null) _health.StarLost -= OnStarLost;
        if (_tearGauge != null) _tearGauge.TearBurstReleased -= OnTearBurst;
    }

    private void OnMalletHit(IDamageable target, DamageInfo info) => ScreenShake.Shake(malletHit, info.Knockback);
    private void OnStarLost() => ScreenShake.Shake(starLost);
    private void OnTearBurst(Vector2 center, float radius) => ScreenShake.Shake(tearBurst);
}
