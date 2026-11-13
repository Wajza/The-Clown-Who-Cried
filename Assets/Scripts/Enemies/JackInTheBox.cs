using System.Collections;
using UnityEngine;

/// <summary>
/// Jack-in-the-Box: waits hidden in its box. When Pip comes within 3 tiles, the box rattles
/// (warning, 0.5 s), then Jack springs up and hits upward. He stays up (and can be hit) for a
/// while, then hides again. 2 health.
///
/// While hidden: can't be damaged and doesn't hurt on touch (the lid is closed).
/// Visuals: assign the closed-box and sprung-up sprites, or leave them empty and let an
/// Animator react to the Hidden/Revealed events.
///
/// Rigidbody2D: Kinematic or Static (it never moves), or leave it out.
/// </summary>
public class JackInTheBox : EnemyBase
{
    [Header("Jack-in-the-Box")]
    [SerializeField, Min(0f), Tooltip("Springs when Pip is this close (tiles).")]
    private float triggerRange = 3f;
    [SerializeField, Min(MinWarningTime), Tooltip("Rattle warning before springing (seconds, at least 0.5).")]
    private float rattleTime = 0.5f;
    [SerializeField, Min(0.02f), Tooltip("Seconds the upward hit is active.")]
    private float hitActiveTime = 0.15f;
    [SerializeField, Min(0f), Tooltip("Seconds Jack stays up (vulnerable) after springing.")]
    private float stayUpTime = 1.5f;
    [SerializeField, Min(0f), Tooltip("Seconds hidden before he can spring again.")]
    private float rearmTime = 1f;
    [SerializeField, Min(0), Tooltip("Damage of the upward hit.")]
    private int hitDamage = 1;

    [Header("Upward Hitbox")]
    [SerializeField, Tooltip("Center of the upward hit, relative to the feet (tiles).")]
    private Vector2 hitOffset = new Vector2(0f, 1.75f);
    [SerializeField, Tooltip("Size of the upward hit (tiles).")]
    private Vector2 hitSize = new Vector2(1.25f, 2.5f);

    [Header("Sprites (optional)")]
    [SerializeField] private Sprite hiddenSprite;
    [SerializeField] private Sprite revealedSprite;

    /// <summary>True while Jack is tucked inside the box.</summary>
    public bool IsHidden { get; private set; } = true;

    /// <summary>Fired with true when Jack hides and false when he springs out. For the Animator.</summary>
    public event System.Action<bool> HiddenChanged;

    protected override bool CanBeHit => !IsHidden;
    protected override bool ContactDamageActive => !IsHidden;

    protected override IEnumerator Behaviour()
    {
        SetHidden(true);

        while (true)
        {
            // Wait in the box until Pip comes close.
            while (!PlayerExists || DistanceToPlayer > triggerRange)
                yield return new WaitForSeconds(0.1f);

            // Warning: the box rattles before Jack springs.
            yield return Warn(rattleTime);

            // Spring up and hit upward for a short time.
            SetHidden(false);
            float hitEnds = Time.time + hitActiveTime;
            while (Time.time < hitEnds)
            {
                HitPlayerInBox((Vector2)transform.position + hitOffset, hitSize, hitDamage);
                yield return new WaitForFixedUpdate();
            }

            // Stay up (vulnerable), then hide and rearm.
            yield return new WaitForSeconds(stayUpTime);
            SetHidden(true);
            yield return new WaitForSeconds(rearmTime);
        }
    }

    private void SetHidden(bool hidden)
    {
        bool changed = IsHidden != hidden;
        IsHidden = hidden;

        Sprite sprite = hidden ? hiddenSprite : revealedSprite;
        if (spriteRenderer != null && sprite != null)
            spriteRenderer.sprite = sprite;

        if (changed)
            HiddenChanged?.Invoke(hidden);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, triggerRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube((Vector2)transform.position + hitOffset, hitSize);
    }
}
