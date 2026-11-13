using System.Collections;
using UnityEngine;

/// <summary>
/// A rubber-chicken cannon on the arena wall. Fires a chicken straight across the arena.
/// Each shot is warned by the cannon blinking (at least 0.5 s). How often it fires depends on
/// the boss phase (0 = off); by default it is off in phase 1 and fires faster in phase 3.
/// Stops when Chortle is defeated.
///
/// The chicken prefab uses the PieProjectile script (with a chicken sprite): it flies straight,
/// hurts Pip, and splats on walls.
/// </summary>
[RequireComponent(typeof(SpriteFlasher))]
public class RubberChickenCannon : MonoBehaviour
{
    [SerializeField] private ChortleBoss boss;
    [SerializeField, Tooltip("Chicken prefab (PieProjectile with a chicken sprite).")]
    private PieProjectile chickenPrefab;
    [SerializeField, Tooltip("Where the chicken appears. Place it just OUTSIDE the wall collider.")]
    private Transform muzzle;
    [SerializeField, Tooltip("Firing direction, e.g. (1, 0) for a cannon on the left wall.")]
    private Vector2 direction = Vector2.right;

    [Header("Shots")]
    [SerializeField, Min(0.1f), Tooltip("Chicken speed (tiles per second).")]
    private float chickenSpeed = 7f;
    [SerializeField, Min(0)] private int damage = 1;
    [SerializeField, Min(EnemyBase.MinWarningTime), Tooltip("Blink warning before each shot (seconds).")]
    private float warningTime = 0.8f;
    [SerializeField, Tooltip("Seconds between shots in phases 1, 2 and 3. 0 = off in that phase.")]
    private float[] intervalPerPhase = { 0f, 6f, 4f };
    [SerializeField, Min(0f), Tooltip("Extra delay before the first shot, so two cannons don't fire together.")]
    private float startOffset = 0f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;

    /// <summary>Fired whenever any cannon fires. For shake/sound.</summary>
    public static event System.Action<RubberChickenCannon> AnyFired;

    private SpriteFlasher _flasher;
    private Coroutine _firing;

    private void Awake()
    {
        _flasher = GetComponent<SpriteFlasher>();
    }

    private void OnEnable()
    {
        if (boss == null)
            return;
        boss.PhaseChanged += OnPhaseChanged;
        boss.OnDefeated += StopFiring;
    }

    private void OnDisable()
    {
        if (boss == null)
            return;
        boss.PhaseChanged -= OnPhaseChanged;
        boss.OnDefeated -= StopFiring;
    }

    private void OnPhaseChanged(int phase)
    {
        StopFiring();

        int index = phase - 1;
        if (index >= 0 && index < intervalPerPhase.Length && intervalPerPhase[index] > 0f)
            _firing = StartCoroutine(FireLoop(intervalPerPhase[index]));
    }

    private void StopFiring()
    {
        if (_firing != null)
            StopCoroutine(_firing);
        _firing = null;
        _flasher.Stop();
    }

    private IEnumerator FireLoop(float interval)
    {
        yield return new WaitForSeconds(boss.ScaleTime(startOffset));

        while (true)
        {
            // The interval includes the warning, so shots are 'interval' seconds apart.
            float warning = boss.ScaleWarning(warningTime);
            yield return new WaitForSeconds(Mathf.Max(0f, boss.ScaleTime(interval) - warning));

            _flasher.Warn(warning);
            yield return new WaitForSeconds(warning);

            Fire();
        }
    }

    private void Fire()
    {
        if (chickenPrefab == null)
            return;

        Vector2 from = muzzle != null ? muzzle.position : transform.position;
        PieProjectile chicken = Instantiate(chickenPrefab, from, Quaternion.identity);
        chicken.Launch(direction.normalized * boss.ScaleSpeed(chickenSpeed), 0f, damage, playerLayer, groundLayer);
        AnyFired?.Invoke(this);
    }
}
