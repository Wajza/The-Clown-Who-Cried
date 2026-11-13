using System.Collections;
using UnityEngine;

/// <summary>
/// Whoopee Wobbler: hops along the ground toward Pip. 2 health. Bursts into confetti
/// (assign a confetti particle prefab as Pop Effect).
///
/// Loop: if Pip is in range, wobble in place as a warning (0.5 s), hop toward her,
/// wait until it lands, rest a moment, repeat. It hurts Pip by touching her.
///
/// Needs a Dynamic Rigidbody2D (Gravity Scale 3 feels good) and a collider.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class WhoopeeWobbler : EnemyBase
{
    [Header("Wobbler")]
    [SerializeField, Min(0f), Tooltip("Starts hopping toward Pip when she is this close (tiles).")]
    private float detectRange = 10f;
    [SerializeField, Min(MinWarningTime), Tooltip("Wobble warning before each hop (seconds, at least 0.5).")]
    private float wobbleTime = 0.5f;
    [SerializeField, Min(0.1f), Tooltip("Hop height (tiles).")]
    private float hopHeight = 1f;
    [SerializeField, Min(0.1f), Tooltip("Hop length (tiles).")]
    private float hopDistance = 1.5f;
    [SerializeField, Min(0f), Tooltip("Rest after landing (seconds).")]
    private float restTime = 0.2f;

    protected override IEnumerator Behaviour()
    {
        while (true)
        {
            // Wait (standing still) until Pip is close and we're on the ground.
            if (!PlayerExists || DistanceToPlayer > detectRange || !IsGrounded())
            {
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            int direction = DirectionToPlayer;
            Face(direction);

            // Warning: wobble in place before hopping.
            yield return Warn(wobbleTime);

            Hop(direction);

            // Wait until we've left the ground, then until we land again (with a safety timeout).
            float timeout = Time.time + 2f;
            yield return new WaitForSeconds(0.1f);
            while (!IsGrounded() && Time.time < timeout)
                yield return null;

            // Stop sliding after landing.
            Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
            yield return new WaitForSeconds(restTime);
        }
    }

    private void Hop(int direction)
    {
        // Physics: to rise hopHeight you need vy = sqrt(2 g h). The hop lasts t = 2 vy / g,
        // so to cover hopDistance in that time, vx = hopDistance / t.
        float vy = Mathf.Sqrt(2f * Gravity * hopHeight);
        float airTime = 2f * vy / Gravity;
        float vx = hopDistance / airTime;
        Body.linearVelocity = new Vector2(direction * vx, vy);
    }
}
