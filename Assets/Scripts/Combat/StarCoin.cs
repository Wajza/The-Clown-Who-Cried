using System;
using UnityEngine;

/// <summary>
/// One thrown Star Coin. Put this on the Star Coin prefab (a SpriteRenderer is enough, no collider
/// or Rigidbody2D needed). Coins are pooled by PlayerCoinThrower, so they are never destroyed:
/// Launch() activates one, and when it finishes it hands itself back through the callback.
///
/// Movement: the coin flies in a straight line at constant speed. Each frame it sweeps a small
/// circle (CircleCast) along the path it is about to travel, so even a fast coin can't skip
/// through a thin wall or a small enemy between frames.
/// </summary>
public class StarCoin : MonoBehaviour
{
    /// <summary>Everything a coin needs for one throw, filled in by the thrower.</summary>
    public struct LaunchData
    {
        public Vector2 Position;     // Start position (world).
        public Vector2 Direction;    // Normalized flight direction.
        public float Speed;          // Tiles per second.
        public float Range;          // Max distance in tiles before the coin vanishes.
        public int Damage;
        public float Knockback;      // Knockback speed given to targets (tiles per second).
        public LayerMask HitLayers;  // Layers that stop the coin (walls and targets).
        public GameObject Owner;     // Who threw it (passed on in DamageInfo).
    }

    [SerializeField, Min(0.01f), Tooltip("Radius of the coin's hit circle in tiles (8px coin = 0.25).")]
    private float radius = 0.25f;
    [SerializeField, Tooltip("Spin speed of the sprite in degrees per second (visual only).")]
    private float spinSpeed = 720f;

    private LaunchData _data;
    private float _distanceLeft;
    private bool _inFlight;
    private Action<StarCoin> _onFinished;

    /// <summary>Starts a throw. onFinished is called once when the coin hits something or runs out of range.</summary>
    public void Launch(LaunchData data, Action<StarCoin> onFinished)
    {
        _data = data;
        _onFinished = onFinished;
        _distanceLeft = data.Range;
        _inFlight = true;

        transform.SetPositionAndRotation(data.Position, Quaternion.identity);
    }

    private void Update()
    {
        if (!_inFlight)
            return;

        float step = Mathf.Min(_data.Speed * Time.deltaTime, _distanceLeft);
        Vector2 position = transform.position;

        // Sweep along this frame's path. If the coin starts inside a collider, distance is 0 and it hits at once.
        RaycastHit2D hit = Physics2D.CircleCast(position, radius, _data.Direction, step, _data.HitLayers);
        if (hit.collider != null)
        {
            transform.position = hit.centroid; // Snap to the contact point so effects appear in the right place.
            OnHit(hit);
            return;
        }

        transform.position = position + _data.Direction * step;
        transform.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);

        _distanceLeft -= step;
        if (_distanceLeft <= 0f)
            Finish(); // Flew its full range without hitting anything.
    }

    private void OnHit(RaycastHit2D hit)
    {
        // Walls simply stop the coin; anything IDamageable also takes damage.
        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            var info = new DamageInfo(_data.Damage, _data.Direction * _data.Knockback, hit.point, _data.Owner);
            target.TakeDamage(info);
        }

        Finish();
    }

    private void Finish()
    {
        if (!_inFlight)
            return; // Guard: never return the same coin to the pool twice.

        _inFlight = false;
        _onFinished?.Invoke(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
