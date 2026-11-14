using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves an object along a list of points, over and over: P2's bumper cars on Honk Street,
/// later moving platforms. The points are child Transforms (or any Transforms) you place in the
/// scene, so the path is easy to see and drag around.
///
///   Loop       A -> B -> C -> A -> B ...
///   Ping Pong  A -> B -> C -> B -> A ...
///
/// It moves a KINEMATIC Rigidbody2D (MovePosition in FixedUpdate), so physics knows it's moving
/// and triggers/collisions behave. Pair it with my teammate's ContactDamage to make it hurt Pip.
///
/// Setup: a GameObject with a Rigidbody2D (set to Kinematic automatically), a collider, a sprite
/// and this script. Add empty GameObjects as waypoints (NOT children of the moving object, or
/// they'd move with it) and drag them into Waypoints.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class WaypointMover : MonoBehaviour
{
    public enum PathMode { Loop, PingPong }

    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [SerializeField, Min(0.01f), Tooltip("Tiles per second.")]
    private float speed = 4f;
    [SerializeField, Min(0f), Tooltip("Seconds to wait at each waypoint.")]
    private float waitAtPoint = 0.3f;
    [SerializeField] private PathMode mode = PathMode.PingPong;
    [SerializeField, Tooltip("Optional sprite flipped to face the direction of travel.")]
    private SpriteRenderer spriteToFlip;

    public int TargetIndex => _target;

    private Rigidbody2D _body;
    private int _target = 1;
    private int _direction = 1;
    private float _waitUntil;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _body.bodyType = RigidbodyType2D.Kinematic;
        _body.interpolation = RigidbodyInterpolation2D.Interpolate;
        if (waypoints.Count > 0 && waypoints[0] != null)
            _body.position = waypoints[0].position; // Start on the first point.
    }

    private void FixedUpdate()
    {
        if (waypoints.Count < 2 || Time.time < _waitUntil)
            return;

        Vector2 goal = waypoints[_target].position;
        Vector2 next = Vector2.MoveTowards(_body.position, goal, speed * Time.fixedDeltaTime);
        if (spriteToFlip != null && Mathf.Abs(next.x - _body.position.x) > 0.0001f)
            spriteToFlip.flipX = next.x < _body.position.x;
        _body.MovePosition(next);

        if ((next - goal).sqrMagnitude < 0.0001f)
        {
            _waitUntil = Time.time + waitAtPoint;
            PickNextTarget();
        }
    }

    private void PickNextTarget()
    {
        if (mode == PathMode.Loop)
        {
            _target = (_target + 1) % waypoints.Count;
            return;
        }

        if (_target + _direction < 0 || _target + _direction >= waypoints.Count)
            _direction = -_direction;
        _target += _direction;
    }

    // Draw the path in the Scene view.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i + 1 < waypoints.Count; i++)
            if (waypoints[i] != null && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        if (mode == PathMode.Loop && waypoints.Count > 2 && waypoints[0] != null && waypoints[waypoints.Count - 1] != null)
            Gizmos.DrawLine(waypoints[waypoints.Count - 1].position, waypoints[0].position);
    }
}
