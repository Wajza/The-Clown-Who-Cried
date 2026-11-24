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

    [Header("Pounce on Pip")]
    [SerializeField, Tooltip("Aim each hop so it lands ON Pip (a bouncing pounce) instead of a fixed short hop.")]
    private bool pounceOnPip = true;
    [SerializeField, Min(0.1f), Tooltip("Pounce height (tiles). Higher than Pip (1.5) so he comes down on top of her.")]
    private float pounceHeight = 2.4f;
    [SerializeField, Min(0.5f), Tooltip("Longest pounce (tiles). Farther away he hops this far toward her.")]
    private float maxPounceDistance = 5f;
    [SerializeField, Min(0f), Tooltip("Bounce height (tiles) when he lands on Pip.")]
    private float bounceOffPipHeight = 1.5f;

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
        // Pounce: land where Pip stands now (as far as he can reach), from high enough to come down on her.
        float height = hopHeight;
        float distance = hopDistance;
        if (pounceOnPip && PlayerExists)
        {
            height = Mathf.Max(hopHeight, pounceHeight);
            distance = Mathf.Clamp(Mathf.Abs(Player.transform.position.x - transform.position.x), 0.3f, maxPounceDistance);
        }

        // Physics: to rise 'height' you need vy = sqrt(2 g h). The hop lasts t = 2 vy / g,
        // so to cover 'distance' in that time, vx = distance / t.
        float vy = Mathf.Sqrt(2f * Gravity * height);
        float airTime = 2f * vy / Gravity;
        float vx = distance / airTime;
        Body.linearVelocity = new Vector2(direction * vx, vy);
    }

    // Coming down on top of Pip: bounce back up off her (touching her hurts her as usual).
    // Pip and enemies pass through each other (EnemyPassThrough), so this checks for overlap
    // instead of a physics collision.
    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // EnemyBase's own checks (contact damage etc.).

        if (!pounceOnPip || IsPopped || !PlayerExists || BodyCollider == null || Body.linearVelocity.y >= 0f)
            return;
        Collider2D pip = Player.GetComponent<Collider2D>();
        if (pip == null || !pip.bounds.Intersects(BodyCollider.bounds))
            return;
        bool fromAbove = BodyCollider.bounds.min.y > pip.bounds.center.y; // Landing on her head, not walking into her.
        if (!fromAbove)
            return;
        float vy = Mathf.Sqrt(2f * Gravity * bounceOffPipHeight);
        int away = Player.transform.position.x < transform.position.x ? 1 : -1;
        Body.linearVelocity = new Vector2(away * 2f, vy);
    }
}
