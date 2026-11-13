using UnityEngine;

/// <summary>
/// Chortle's phase-3 bomb. It flies in an arc and passes through Pip while flying (no surprise
/// hits). When it lands, it blinks and shows its blast area for the fuse time (at least 0.5 s),
/// then explodes, hurting Pip if she is inside the blast radius.
///
/// If the ground under it disappears (a trapdoor opens), it falls again; the fuse keeps counting.
///
/// Prefab: SpriteRenderer, SpriteFlasher, this. Optional: a child circle sprite as Blast Indicator
/// (a 1-tile-wide circle; it is scaled to the blast size). No collider needed.
/// </summary>
[RequireComponent(typeof(SpriteFlasher))]
public class PunchlineBomb : MonoBehaviour
{
    [SerializeField, Min(0.01f), Tooltip("Radius of the bomb body, for landing (tiles).")]
    private float bodyRadius = 0.3f;
    [SerializeField, Min(0.1f), Tooltip("Seconds before a bomb that never lands disappears.")]
    private float lifetime = 6f;
    [SerializeField, Tooltip("Optional child sprite showing the blast area while the fuse burns.")]
    private Transform blastIndicator;
    [SerializeField, Tooltip("Optional explosion effect.")]
    private GameObject explosionEffectPrefab;

    /// <summary>Fired with the position whenever any punchline bomb explodes. For shake/sound.</summary>
    public static event System.Action<Vector2> AnyExploded;

    private SpriteFlasher _flasher;
    private Vector2 _velocity;
    private float _gravity;
    private float _fuse;
    private float _blastRadius;
    private int _damage;
    private LayerMask _playerLayer;
    private LayerMask _groundLayer;

    private bool _landed;
    private bool _fuseLit;
    private float _explodeAt;
    private float _dieAt;

    private void Awake()
    {
        _flasher = GetComponent<SpriteFlasher>();
        if (blastIndicator != null)
            blastIndicator.gameObject.SetActive(false);
    }

    public void Launch(Vector2 velocity, float gravity, float fuseTime, float blastRadius, int damage,
                       LayerMask playerLayer, LayerMask groundLayer)
    {
        _velocity = velocity;
        _gravity = gravity;
        _fuse = Mathf.Max(fuseTime, EnemyBase.MinWarningTime); // The warning rule: never under 0.5 s.
        _blastRadius = blastRadius;
        _damage = damage;
        _playerLayer = playerLayer;
        _groundLayer = groundLayer;
        _dieAt = Time.time + lifetime;
    }

    private void Update()
    {
        if (_fuseLit && Time.time >= _explodeAt)
        {
            Explode();
            return;
        }

        if (!_fuseLit && Time.time >= _dieAt)
        {
            Destroy(gameObject); // Fell out of the arena without landing.
            return;
        }

        if (_landed)
        {
            // Ground vanished (trapdoor)? Start falling again.
            if (Physics2D.OverlapCircle(transform.position, bodyRadius + 0.05f, _groundLayer) == null)
            {
                _landed = false;
                _velocity = Vector2.zero;
            }
            return;
        }

        Fly();
    }

    private void Fly()
    {
        _velocity.y -= _gravity * Time.deltaTime;
        Vector2 step = _velocity * Time.deltaTime;
        Vector2 position = transform.position;

        // Only ground stops the bomb; it flies through Pip.
        RaycastHit2D hit = Physics2D.CircleCast(position, bodyRadius, step.normalized, step.magnitude, _groundLayer);
        if (hit.collider != null)
        {
            transform.position = hit.centroid;
            _landed = true;
            if (!_fuseLit)
                LightFuse();
            return;
        }

        transform.position = position + step;
    }

    private void LightFuse()
    {
        _fuseLit = true;
        _explodeAt = Time.time + _fuse;
        _flasher.Warn(_fuse);

        if (blastIndicator != null)
        {
            blastIndicator.gameObject.SetActive(true);
            blastIndicator.localScale = Vector3.one * (_blastRadius * 2f);
        }
    }

    private void Explode()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, _blastRadius, _playerLayer);
        if (hit != null)
        {
            PlayerHealth health = hit.GetComponentInParent<PlayerHealth>();
            if (health != null)
                health.TakeDamage(new DamageInfo(_damage, Vector2.zero, transform.position, gameObject));
        }

        if (explosionEffectPrefab != null)
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        AnyExploded?.Invoke(transform.position);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _blastRadius > 0f ? _blastRadius : 1.5f);
    }
}
