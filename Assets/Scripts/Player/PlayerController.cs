using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves Pip with a Rigidbody2D: running, variable-height jumping (with coyote time and
/// jump buffering), snappy falling and a horizontal dash.
///
/// Units: Pixels Per Unit = 16, so 1 Unity unit = 1 tile. Every distance below is in tiles.
///
/// Flow per physics step (FixedUpdate):
///   1. Check the ground.
///   2. If dashing, keep the dash going and skip everything else.
///   3. Otherwise: start a dash or jump if one was requested, then run and apply extra gravity.
/// Input is read in callbacks/Update and only *requested* there; physics happens in FixedUpdate.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector fields

    [Header("Input")]
    [SerializeField, Tooltip("PlayerControls.inputactions. Needs a 'Player' map with Move, Jump and Dash.")]
    private InputActionAsset inputActions;

    [Header("Running")]
    [SerializeField, Min(0f), Tooltip("Top running speed (tiles per second).")]
    private float runSpeed = 6f;
    [SerializeField, Min(0f), Tooltip("How fast Pip reaches top speed (tiles/s²). Higher = snappier.")]
    private float acceleration = 70f;
    [SerializeField, Min(0f), Tooltip("How fast Pip stops when no direction is held (tiles/s²).")]
    private float deceleration = 90f;

    [Header("Jumping")]
    [SerializeField, Min(0.01f), Tooltip("Peak jump height in tiles when Jump is held.")]
    private float jumpHeight = 2.5f;
    [SerializeField, Min(0.01f), Tooltip("Gravity multiplier (x Physics2D gravity). Higher = faster, less floaty jump.")]
    private float gravityScale = 3f;
    [SerializeField, Min(1f), Tooltip("Extra gravity multiplier while falling. Makes falling faster than rising.")]
    private float fallGravityMultiplier = 1.8f;
    [SerializeField, Min(1f), Tooltip("Extra gravity multiplier while rising after Jump is released (short hop).")]
    private float jumpCutGravityMultiplier = 2.2f;
    [SerializeField, Min(0f), Tooltip("Maximum falling speed (tiles per second).")]
    private float maxFallSpeed = 18f;
    [SerializeField, Min(0f), Tooltip("Seconds after walking off a ledge during which Jump still works.")]
    private float coyoteTime = 0.1f;
    [SerializeField, Min(0f), Tooltip("Seconds a Jump press is remembered before landing.")]
    private float jumpBufferTime = 0.1f;

    [Header("Ground Check")]
    [SerializeField, Tooltip("Child transform placed at Pip's feet.")]
    private Transform groundCheck;
    [SerializeField, Tooltip("Size of the box under the feet (tiles). Keep it slightly narrower than the collider.")]
    private Vector2 groundCheckSize = new Vector2(0.7f, 0.1f);
    [SerializeField, Tooltip("Layers that count as ground (set to 'Ground').")]
    private LayerMask groundLayer;

    [Header("Dash")]
    [SerializeField, Min(0.01f), Tooltip("Dash distance in tiles.")]
    private float dashDistance = 3f;
    [SerializeField, Min(0.01f), Tooltip("How long the dash movement lasts (seconds).")]
    private float dashDuration = 0.15f;
    [SerializeField, Min(0f), Tooltip("Seconds between dashes, counted from the start of a dash.")]
    private float dashCooldown = 0.8f;
    [SerializeField, Min(0f), Tooltip("Seconds of invulnerability from the start of a dash.")]
    private float dashInvulnerability = 0.3f;

    [Header("Visuals")]
    [SerializeField, Tooltip("Sprite to flip. Found automatically on Pip or a child if left empty.")]
    private SpriteRenderer spriteRenderer;

    // ------------------------------------------------------------------ Public API (animation / health)

    /// <summary>True while Pip's feet touch the Ground layer.</summary>
    public bool IsGrounded { get; private set; }

    /// <summary>True during the dash movement.</summary>
    public bool IsDashing { get; private set; }

    /// <summary>True for a short time after a dash starts. A health script should ignore damage while true.</summary>
    public bool IsInvulnerable => Time.time < _invulnerableUntil;

    /// <summary>Current velocity (tiles per second). Use x for run speed and y for jump/fall animations.</summary>
    public Vector2 Velocity => _rb.linearVelocity;

    /// <summary>1 = facing right, -1 = facing left.</summary>
    public int FacingDirection { get; private set; } = 1;

    /// <summary>The active player, so enemies can find Pip without searching the scene.</summary>
    public static PlayerController Current { get; private set; }

    /// <summary>The input asset, shared with other player scripts (e.g. PlayerMallet).</summary>
    public InputActionAsset InputActions => inputActions;

    /// <summary>Layers that count as ground (used by PlayerHazards to find safe ground).</summary>
    public LayerMask GroundLayer => groundLayer;

    /// <summary>True while running, jumping and dashing are ignored (after being hit or respawning).</summary>
    public bool ControlsLocked => Time.time < _controlsLockedUntil;

    /// <summary>Fired with the new value when Pip lands (true) or leaves the ground (false).</summary>
    public event Action<bool> GroundedChanged;

    /// <summary>Fired with true when a dash starts and false when it ends.</summary>
    public event Action<bool> DashingChanged;

    /// <summary>Fired the moment Pip jumps.</summary>
    public event Action Jumped;

    // ------------------------------------------------------------------ Private state

    private Rigidbody2D _rb;
    private InputActionMap _playerMap;
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _dashAction;

    private float _moveX;                                    // Horizontal input, -1..1.
    private bool _jumpHeld;                                  // Is the Jump button currently down?
    private bool _dashRequested;                             // Dash pressed, waiting for FixedUpdate.
    private bool _ignoreJumpCut;                             // True during a pogo bounce until Pip starts falling.
    private float _controlsLockedUntil = float.NegativeInfinity; // Hurt/respawn lock.

    private float _lastGroundedTime = float.NegativeInfinity; // Used for coyote time.
    private float _lastJumpPressTime = float.NegativeInfinity; // Used for jump buffering.

    private bool _airDashAvailable = true;                   // One dash allowed per airtime.
    private float _dashTimeLeft;
    private int _dashDirection = 1;
    private float _nextDashTime;                             // Cooldown: dash allowed again after this time.
    private float _invulnerableUntil = float.NegativeInfinity;

    /// <summary>Strength of gravity on Pip, as a positive number (tiles/s²).</summary>
    private float Gravity => Mathf.Abs(Physics2D.gravity.y) * gravityScale;

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.freezeRotation = true;         // Pip should never tip over.
        _rb.gravityScale = gravityScale;   // This script owns the gravity value.

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (groundCheck == null)
            Debug.LogError("PlayerController: assign the Ground Check transform.", this);
    }

    private void OnEnable()
    {
        Current = this;

        if (inputActions == null)
        {
            Debug.LogError("PlayerController: assign the PlayerControls Input Actions asset.", this);
            enabled = false;
            return;
        }

        // Look up the actions by name. 'true' throws a clear error if a name is wrong.
        _playerMap = inputActions.FindActionMap("Player", true);
        _moveAction = _playerMap.FindAction("Move", true);
        _jumpAction = _playerMap.FindAction("Jump", true);
        _dashAction = _playerMap.FindAction("Dash", true);

        _jumpAction.performed += OnJumpPressed;
        _jumpAction.canceled += OnJumpReleased;
        _dashAction.performed += OnDashPressed;
        _playerMap.Enable();
    }

    private void OnDisable()
    {
        if (Current == this)
            Current = null;

        if (_playerMap == null)
            return;

        _jumpAction.performed -= OnJumpPressed;
        _jumpAction.canceled -= OnJumpReleased;
        _dashAction.performed -= OnDashPressed;
        _playerMap.Disable();
    }

    private void Update()
    {
        _moveX = _moveAction.ReadValue<Vector2>().x;

        // Face the direction being pressed (but don't turn around mid-dash).
        if (!IsDashing && !ControlsLocked && Mathf.Abs(_moveX) > 0.01f)
        {
            FacingDirection = _moveX > 0f ? 1 : -1;
            if (spriteRenderer != null)
                spriteRenderer.flipX = FacingDirection < 0;
        }
    }

    private void FixedUpdate()
    {
        UpdateGroundCheck();

        if (IsDashing)
        {
            ContinueDash();
            return;
        }

        // While locked (hurt knockback, respawn) Pip ignores input but still falls normally.
        bool locked = ControlsLocked;

        if (_dashRequested && !locked)
            TryStartDash();
        _dashRequested = false;
        if (IsDashing)
            return;

        if (!locked)
        {
            TryJump();
            Run();
        }
        ApplyExtraGravity();
    }

    // ------------------------------------------------------------------ Input callbacks

    private void OnJumpPressed(InputAction.CallbackContext ctx)
    {
        _jumpHeld = true;
        _lastJumpPressTime = Time.time; // Remember the press so it can be "buffered".
    }

    private void OnJumpReleased(InputAction.CallbackContext ctx)
    {
        _jumpHeld = false;
    }

    private void OnDashPressed(InputAction.CallbackContext ctx)
    {
        _dashRequested = true; // Handled in FixedUpdate.
    }

    // ------------------------------------------------------------------ Ground

    private void UpdateGroundCheck()
    {
        bool touchingGround = groundCheck != null &&
            Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer) != null;

        // Right after a jump the feet box still overlaps the floor for a frame or two.
        // Ignoring ground while moving upward prevents that from counting as "landed".
        bool grounded = touchingGround && _rb.linearVelocity.y <= 0.01f;

        if (grounded)
        {
            _lastGroundedTime = Time.time; // Coyote time counts from the last grounded moment.
            _airDashAvailable = true;      // Landing gives the air dash back.
        }

        if (grounded != IsGrounded)
        {
            IsGrounded = grounded;
            GroundedChanged?.Invoke(grounded);
        }
    }

    // ------------------------------------------------------------------ Running

    private void Run()
    {
        // Move the current x speed toward the target speed by a fixed amount each step.
        float targetSpeed = _moveX * runSpeed;
        float rate = Mathf.Abs(_moveX) > 0.01f ? acceleration : deceleration;

        Vector2 v = _rb.linearVelocity;
        v.x = Mathf.MoveTowards(v.x, targetSpeed, rate * Time.fixedDeltaTime);
        _rb.linearVelocity = v;
    }

    // ------------------------------------------------------------------ Jumping

    private void TryJump()
    {
        bool jumpBuffered = Time.time - _lastJumpPressTime <= jumpBufferTime; // Pressed recently?
        bool canJump = Time.time - _lastGroundedTime <= coyoteTime;          // Grounded recently?
        if (!jumpBuffered || !canJump)
            return;

        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, LaunchSpeedFor(jumpHeight));

        // Use up the buffered press and the coyote window so one press = one jump.
        _lastJumpPressTime = float.NegativeInfinity;
        _lastGroundedTime = float.NegativeInfinity;
        Jumped?.Invoke();
    }

    /// <summary>
    /// Launches Pip upward to a fixed height, e.g. a mallet pogo or a spring.
    /// The bounce is not cut short by releasing Jump, and it gives the air dash back.
    /// </summary>
    public void Bounce(float height)
    {
        if (IsDashing)
            return; // The dash controls velocity; don't fight it.

        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, LaunchSpeedFor(height));
        _ignoreJumpCut = true;
        _airDashAvailable = true;
        _lastGroundedTime = float.NegativeInfinity; // No coyote jump on top of the bounce.
    }

    /// <summary>
    /// Throws Pip with a fixed velocity (e.g. when hurt) and ignores input for lockTime seconds,
    /// so the run code doesn't immediately cancel the knockback. Cancels any dash.
    /// </summary>
    public void ApplyKnockback(Vector2 velocity, float lockTime)
    {
        CancelDash();
        _rb.linearVelocity = velocity;
        _ignoreJumpCut = true;                      // Don't cut the upward part of the knockback.
        _lastGroundedTime = float.NegativeInfinity; // No coyote jump out of a hit.
        _controlsLockedUntil = Time.time + lockTime;
    }

    /// <summary>Moves Pip instantly (e.g. hazard respawn), stops all motion and locks input for lockTime seconds.</summary>
    public void Teleport(Vector2 position, float lockTime)
    {
        CancelDash();
        _rb.position = position;
        transform.position = position; // Also move the transform now so cameras/interpolation don't lag a frame.
        _rb.linearVelocity = Vector2.zero;
        _lastJumpPressTime = float.NegativeInfinity;
        _controlsLockedUntil = Time.time + lockTime;
    }

    /// <summary>
    /// Upward speed needed to rise exactly 'height' tiles.
    /// Physics: to reach height h under gravity g you need speed v = sqrt(2gh).
    /// The extra half-step (g * dt / 2) makes up for Unity's step-by-step physics,
    /// so the real peak matches the height almost exactly.
    /// </summary>
    private float LaunchSpeedFor(float height)
    {
        return Mathf.Sqrt(2f * Gravity * height) + 0.5f * Gravity * Time.fixedDeltaTime;
    }

    /// <summary>
    /// The Rigidbody already applies normal gravity (gravityScale). On top of that we add:
    /// - extra gravity while falling (snappy, not floaty), capped at maxFallSpeed;
    /// - extra gravity while rising with Jump released (variable jump height).
    /// </summary>
    private void ApplyExtraGravity()
    {
        Vector2 v = _rb.linearVelocity;

        if (v.y <= 0f)
            _ignoreJumpCut = false; // A bounce ends once Pip stops rising.

        if (v.y < 0f)
        {
            v.y -= Gravity * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
            v.y = Mathf.Max(v.y, -maxFallSpeed);
        }
        else if (v.y > 0f && !_jumpHeld && !_ignoreJumpCut)
        {
            v.y -= Gravity * (jumpCutGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }

        _rb.linearVelocity = v;
    }

    // ------------------------------------------------------------------ Dash

    private void TryStartDash()
    {
        if (Time.time < _nextDashTime)
            return; // Still on cooldown.
        if (!IsGrounded && !_airDashAvailable)
            return; // Already used the air dash.

        if (!IsGrounded)
            _airDashAvailable = false;

        // Dash toward the held direction, or the facing direction if nothing is held.
        _dashDirection = Mathf.Abs(_moveX) > 0.01f ? (_moveX > 0f ? 1 : -1) : FacingDirection;

        _dashTimeLeft = dashDuration;
        _nextDashTime = Time.time + dashCooldown;
        _invulnerableUntil = Time.time + dashInvulnerability;
        _rb.gravityScale = 0f; // No falling during the dash: it is perfectly horizontal.

        IsDashing = true;
        DashingChanged?.Invoke(true);
        ContinueDash();
    }

    private void ContinueDash()
    {
        // Speed = distance / time. On the last step only part of a step may remain,
        // so scale the speed down to land exactly on dashDistance.
        float stepTime = Mathf.Min(Time.fixedDeltaTime, _dashTimeLeft);
        float speed = dashDistance / dashDuration * (stepTime / Time.fixedDeltaTime);
        _rb.linearVelocity = new Vector2(_dashDirection * speed, 0f);

        _dashTimeLeft -= Time.fixedDeltaTime;
        if (_dashTimeLeft <= 0f)
            EndDash();
    }

    private void EndDash()
    {
        IsDashing = false;
        _rb.gravityScale = gravityScale; // Gravity back on.

        // Leave the dash at normal running speed so Pip doesn't slide past the 3 tiles.
        _rb.linearVelocity = new Vector2(_dashDirection * runSpeed, 0f);
        DashingChanged?.Invoke(false);
    }

    /// <summary>Stops a dash early without changing velocity (the caller sets it).</summary>
    private void CancelDash()
    {
        if (!IsDashing)
            return;

        IsDashing = false;
        _dashTimeLeft = 0f;
        _rb.gravityScale = gravityScale;
        DashingChanged?.Invoke(false);
    }

    // ------------------------------------------------------------------ Editor helpers

    private void OnValidate()
    {
        // Keep the Rigidbody in sync while tuning gravity in Play mode.
        if (Application.isPlaying && _rb != null && !IsDashing)
            _rb.gravityScale = gravityScale;
    }

    private void OnDrawGizmosSelected()
    {
        // Draws the ground-check box in the Scene view when Pip is selected.
        if (groundCheck == null)
            return;
        Gizmos.color = IsGrounded ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}
