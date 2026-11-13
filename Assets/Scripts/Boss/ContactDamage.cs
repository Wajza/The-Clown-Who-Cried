using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hurts Pip while she overlaps this object's collider. Disable the component to turn it off.
/// Works even if the collider is a trigger, and even if Player/Enemy layers don't collide.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ContactDamage : MonoBehaviour
{
    [SerializeField, Min(0), Tooltip("Damage on touch (Pip always loses 1 star per hit).")]
    private int damage = 1;
    [SerializeField, Tooltip("Pip's layer (Player).")]
    private LayerMask playerLayer;

    private Collider2D _collider;
    private readonly List<Collider2D> _overlaps = new List<Collider2D>();

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
    }

    private void FixedUpdate()
    {
        if (damage <= 0 || !_collider.enabled)
            return;

        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(playerLayer);
        _collider.Overlap(filter, _overlaps);

        foreach (Collider2D col in _overlaps)
        {
            PlayerHealth health = col.GetComponentInParent<PlayerHealth>();
            if (health != null) // PlayerHealth ignores hits while Pip is invulnerable.
                health.TakeDamage(new DamageInfo(damage, Vector2.zero, col.ClosestPoint(transform.position), gameObject));
        }
    }
}
