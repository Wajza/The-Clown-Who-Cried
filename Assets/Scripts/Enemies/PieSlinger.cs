using System.Collections;
using UnityEngine;

/// <summary>
/// Pie Slinger: stands still and throws slow pies at Pip. 3 health.
///
/// Loop: wait until Pip is in range, face her, wind up (warning, 0.7 s), throw a pie
/// (straight at her, or in an arc that lands on her), cool down, repeat.
/// The aim is taken at the moment of the throw, so moving during the wind-up dodges it.
/// </summary>
public class PieSlinger : EnemyBase
{
    public enum ThrowStyle { Straight, Arc, Alternate }

    [Header("Pie Slinger")]
    [SerializeField, Tooltip("Pie prefab (must have PieProjectile).")]
    private PieProjectile piePrefab;
    [SerializeField, Tooltip("Straight = aimed line, Arc = lobbed, Alternate = switch every throw.")]
    private ThrowStyle throwStyle = ThrowStyle.Alternate;
    [SerializeField, Min(0f), Tooltip("Throws when Pip is this close (tiles).")]
    private float attackRange = 9f;
    [SerializeField, Min(MinWarningTime), Tooltip("Wind-up warning before each throw (seconds, at least 0.5).")]
    private float windupTime = 0.7f;
    [SerializeField, Min(0f), Tooltip("Seconds between throws.")]
    private float cooldown = 1.5f;
    [SerializeField, Tooltip("Where the pie leaves the hand, relative to the feet (facing right).")]
    private Vector2 throwOffset = new Vector2(0.5f, 1.2f);
    [SerializeField, Min(0), Tooltip("Damage of a pie hit.")]
    private int pieDamage = 1;

    [Header("Pie Flight")]
    [SerializeField, Min(0.1f), Tooltip("Straight pie speed (tiles per second). Slow = dodgeable.")]
    private float straightSpeed = 5f;
    [SerializeField, Min(0.1f), Tooltip("Arc pie horizontal speed (tiles per second).")]
    private float arcHorizontalSpeed = 5f;
    [SerializeField, Min(0.1f), Tooltip("Gravity on arc pies (tiles/s²). Lower = floatier arc.")]
    private float arcGravity = 15f;

    private bool _nextIsArc;

    protected override IEnumerator Behaviour()
    {
        while (true)
        {
            if (!PlayerExists || DistanceToPlayer > attackRange)
            {
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            Face(DirectionToPlayer);
            yield return Warn(windupTime);

            if (PlayerExists)
            {
                Face(DirectionToPlayer); // Turn again in case Pip jumped over during the wind-up.
                ThrowPie();
            }

            yield return new WaitForSeconds(cooldown);
        }
    }

    private void ThrowPie()
    {
        if (piePrefab == null)
            return;

        int facing = DirectionToPlayer;
        Vector2 from = (Vector2)transform.position + new Vector2(throwOffset.x * facing, throwOffset.y);
        Vector2 to = PlayerCenter;

        bool arc = throwStyle == ThrowStyle.Arc || (throwStyle == ThrowStyle.Alternate && _nextIsArc);
        _nextIsArc = !_nextIsArc;

        Vector2 velocity;
        float gravity;
        if (arc)
        {
            velocity = PieProjectile.ArcVelocity(from, to, arcHorizontalSpeed, arcGravity);
            gravity = arcGravity;
        }
        else
        {
            velocity = (to - from).normalized * straightSpeed;
            gravity = 0f;
        }

        PieProjectile pie = Instantiate(piePrefab, from, Quaternion.identity);
        pie.Launch(velocity, gravity, pieDamage, playerLayer, groundLayer);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
