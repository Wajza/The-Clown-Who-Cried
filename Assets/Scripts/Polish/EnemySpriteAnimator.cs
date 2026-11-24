using UnityEngine;

/// <summary>
/// Plays an enemy's animations from what it is doing, without touching the enemy's own code:
///   - Idle state while standing still,
///   - Move state while it moves or is in the air (e.g. the Wobbler's hop),
///   - Attack state (or a single Alert sprite) during its wind-up warning (EnemyBase.AttackWarned)
///     and for a short moment after it, so the throw itself shows.
/// State names must match the states in the enemy's Animator controller. Empty = not used.
///
/// Setup: on the enemy root (next to EnemyBase). Finds the Animator in its children by itself.
/// </summary>
public class EnemySpriteAnimator : MonoBehaviour
{
    [SerializeField] private string idleState = "";
    [SerializeField] private string moveState = "";
    [SerializeField] private string attackState = "";
    [SerializeField, Tooltip("Optional single sprite shown during the wind-up instead of an attack state.")]
    private Sprite alertSprite;
    [SerializeField, Min(0f), Tooltip("Keep the attack pose this long after the wind-up ends (the throw).")]
    private float attackHold = 0.3f;
    [SerializeField, Min(0f), Tooltip("Faster than this (tiles per second) counts as moving.")]
    private float moveSpeed = 0.25f;

    private Animator _animator;
    private SpriteRenderer _renderer;
    private Rigidbody2D _body;
    private EnemyBase _enemy;
    private float _attackUntil;
    private int _current;
    private bool _alertShown;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _renderer = _animator != null ? _animator.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        _body = GetComponent<Rigidbody2D>();
        _enemy = GetComponent<EnemyBase>();
    }

    private void OnEnable()
    {
        if (_enemy != null)
            _enemy.AttackWarned += OnAttackWarned;
        _current = 0;
    }

    private void OnDisable()
    {
        if (_enemy != null)
            _enemy.AttackWarned -= OnAttackWarned;
        SetAlert(false);
    }

    private void OnAttackWarned(float duration)
    {
        _attackUntil = Time.time + duration + attackHold;
    }

    private void Update()
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
            return;

        bool attacking = Time.time < _attackUntil;
        bool useAlert = attacking && alertSprite != null && string.IsNullOrEmpty(attackState);
        SetAlert(useAlert);
        if (useAlert)
            return;

        string state = idleState;
        if (attacking && !string.IsNullOrEmpty(attackState))
            state = attackState;
        else if (_body != null && !string.IsNullOrEmpty(moveState) && _body.linearVelocity.sqrMagnitude > moveSpeed * moveSpeed)
            state = moveState;

        if (string.IsNullOrEmpty(state))
            return;

        int hash = Animator.StringToHash(state);
        if (hash == _current || !_animator.HasState(0, hash))
            return;
        _current = hash;
        _animator.Play(hash, 0, 0f);
    }

    private void SetAlert(bool on)
    {
        if (on == _alertShown || _renderer == null)
            return;
        _alertShown = on;
        if (_animator != null)
            _animator.enabled = !on; // The Animator would overwrite the sprite every frame.
        if (on)
            _renderer.sprite = alertSprite;
        _current = 0; // Restart the right state when the Animator comes back.
    }
}
