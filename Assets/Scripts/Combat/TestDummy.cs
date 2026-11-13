using UnityEngine;

/// <summary>
/// TESTING ONLY: a punching bag for checking mallet hits, damage and knockback.
/// Shows how an enemy can implement IDamageable. Delete once real enemies exist.
/// </summary>
public class TestDummy : MonoBehaviour, IDamageable, IStunnable
{
    private float _stunnedUntil;

    /// <summary>Logs the stun and tints the dummy blue while stunned.</summary>
    public void Stun(float duration)
    {
        _stunnedUntil = Time.time + duration;
        Debug.Log($"{name} stunned for {duration} s.", this);
    }

    [SerializeField, Min(1), Tooltip("Health before the dummy is destroyed.")]
    private int maxHealth = 9;
    [SerializeField, Min(0f), Tooltip("Seconds the sprite flashes red when hit.")]
    private float flashTime = 0.1f;

    private int _health;
    private Rigidbody2D _rb;
    private SpriteRenderer _sprite;
    private float _flashUntil;

    private void Awake()
    {
        _health = maxHealth;
        _rb = GetComponent<Rigidbody2D>();          // Optional: no Rigidbody2D = no knockback.
        _sprite = GetComponentInChildren<SpriteRenderer>();
    }

    public void TakeDamage(DamageInfo info)
    {
        _health -= info.Amount;
        _flashUntil = Time.time + flashTime;
        Debug.Log($"{name} took {info.Amount} damage ({_health}/{maxHealth} left).", this);

        // Knockback is a velocity change, so it feels the same whatever the dummy's mass.
        if (_rb != null)
            _rb.linearVelocity = info.Knockback;

        if (_health <= 0)
            Destroy(gameObject);
    }

    private void Update()
    {
        if (_sprite != null)
            _sprite.color = Time.time < _flashUntil ? Color.red
                : Time.time < _stunnedUntil ? Color.cyan
                : Color.white;
    }
}
