using UnityEngine;

/// <summary>
/// A pie thrown by the Pie Slinger. Flies in a straight line (gravity 0) or an arc (gravity > 0),
/// hurts Pip on contact, and splats on the ground or walls.
///
/// Prefab: a SpriteRenderer and this script. No collider or Rigidbody2D needed: like the
/// Star Coin, it sweeps a small circle along its path every frame.
/// </summary>
public class PieProjectile : MonoBehaviour
{
    [SerializeField, Min(0.01f), Tooltip("Radius of the pie's hit circle (tiles).")]
    private float radius = 0.3f;
    [SerializeField, Min(0.1f), Tooltip("Seconds before the pie disappears if it hits nothing.")]
    private float lifetime = 6f;
    [SerializeField, Tooltip("Optional splat effect spawned where the pie hits.")]
    private GameObject splatEffectPrefab;

    private Vector2 _velocity;
    private float _gravity;
    private int _damage;
    private LayerMask _playerLayer;
    private LayerMask _hitLayers;
    private float _dieAt;

    /// <summary>
    /// Launch velocity for an arc that passes through 'to'. The flight time comes from the
    /// horizontal distance; then the upward speed needed to arrive at that time is
    /// y = vy t - ½ g t²  =>  vy = dy / t + ½ g t.
    /// </summary>
    public static Vector2 ArcVelocity(Vector2 from, Vector2 to, float horizontalSpeed, float gravity, float minTime = 0.4f)
    {
        float dx = to.x - from.x;
        float time = Mathf.Max(Mathf.Abs(dx) / horizontalSpeed, minTime);
        return new Vector2(dx / time, (to.y - from.y) / time + 0.5f * gravity * time);
    }

    /// <summary>Starts the pie. gravity = 0 for a straight throw, positive (tiles/s²) for an arc.</summary>
    public void Launch(Vector2 velocity, float gravity, int damage, LayerMask playerLayer, LayerMask groundLayer)
    {
        _velocity = velocity;
        _gravity = gravity;
        _damage = damage;
        _playerLayer = playerLayer;
        _hitLayers = playerLayer | groundLayer;
        _dieAt = Time.time + lifetime;
    }

    private void Update()
    {
        if (Time.time >= _dieAt)
        {
            Destroy(gameObject);
            return;
        }

        _velocity.y -= _gravity * Time.deltaTime;
        Vector2 step = _velocity * Time.deltaTime;
        Vector2 position = transform.position;

        RaycastHit2D hit = Physics2D.CircleCast(position, radius, step.normalized, step.magnitude, _hitLayers);
        if (hit.collider != null)
        {
            Splat(hit);
            return;
        }

        transform.position = position + step;
    }

    private void Splat(RaycastHit2D hit)
    {
        bool hitPlayer = (_playerLayer.value & (1 << hit.collider.gameObject.layer)) != 0;
        if (hitPlayer)
        {
            PlayerHealth health = hit.collider.GetComponentInParent<PlayerHealth>();
            if (health != null) // Knockback direction comes from the pie's position (Source).
                health.TakeDamage(new DamageInfo(_damage, Vector2.zero, hit.point, gameObject));
        }

        if (splatEffectPrefab != null)
            Instantiate(splatEffectPrefab, hit.point, Quaternion.identity);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
