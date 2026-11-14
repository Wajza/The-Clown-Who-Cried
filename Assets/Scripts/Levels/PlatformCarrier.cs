using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes Pip ride along on a moving platform (Ferris wheel gondolas, moving platforms).
///
/// WHY IT'S NEEDED
/// A moving kinematic platform doesn't drag a dynamic Rigidbody2D (Pip) sideways or down with
/// it; she'd slide off or float. So every physics step this script moves Pip by exactly as much
/// as the platform moved, while she's standing on top. It only moves her position; it doesn't
/// touch my teammate's PlayerController.
///
/// "Standing on top" = touching the platform with her feet at or above its top edge.
///
/// Setup: on the moving platform, next to its kinematic Rigidbody2D and solid collider.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlatformCarrier : MonoBehaviour
{
    private Rigidbody2D _body;
    private Collider2D _collider;
    private Vector2 _lastPosition;
    private readonly HashSet<Rigidbody2D> _riders = new HashSet<Rigidbody2D>();

    /// <summary>For tests: is Pip riding right now?</summary>
    public bool HasRider => _riders.Count > 0;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _lastPosition = _body.position;
    }

    private void FixedUpdate()
    {
        Vector2 delta = _body.position - _lastPosition;
        _lastPosition = _body.position;
        if (delta == Vector2.zero)
            return;

        foreach (Rigidbody2D rider in _riders)
            if (rider != null)
                rider.position += delta;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        Rigidbody2D other = collision.rigidbody;
        if (other == null || other.GetComponent<PlayerController>() == null)
            return;

        // Pip's pivot is at her feet: she's on top if her feet are near or above our top edge.
        bool onTop = other.position.y >= _collider.bounds.max.y - 0.15f;
        if (onTop)
            _riders.Add(other);
        else
            _riders.Remove(other);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.rigidbody != null)
            _riders.Remove(collision.rigidbody);
    }
}
