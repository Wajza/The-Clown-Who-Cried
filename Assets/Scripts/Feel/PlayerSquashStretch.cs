using UnityEngine;

/// <summary>
/// Squash and stretch on Pip: tall and thin when jumping, short and wide when landing
/// (more for harder landings), long and flat when dashing. The shape then springs back.
///
/// It scales a VISUAL child, never Pip's root, so colliders and physics are not affected.
/// Pip's sprite should sit on a child object with its pivot at the feet, so it squashes
/// down onto the ground instead of shrinking toward its middle.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerSquashStretch : MonoBehaviour
{
    [SerializeField, Tooltip("The child holding Pip's sprite. Found automatically if empty.")]
    private Transform visual;

    [Header("Shapes (x = width, y = height)")]
    [SerializeField] private Vector2 jumpStretch = new Vector2(0.8f, 1.25f);
    [SerializeField, Tooltip("Shape for the hardest landing. Softer landings squash less.")]
    private Vector2 landSquash = new Vector2(1.3f, 0.7f);
    [SerializeField] private Vector2 dashStretch = new Vector2(1.3f, 0.8f);

    [Header("Tuning")]
    [SerializeField, Min(0f), Tooltip("Landing speed (tiles/s) that gives the full squash.")]
    private float fullSquashFallSpeed = 18f;
    [SerializeField, Min(0f), Tooltip("Landings slower than this don't squash at all (e.g. stepping off a tiny ledge).")]
    private float minSquashFallSpeed = 3f;
    [SerializeField, Min(0.1f), Tooltip("How fast the shape springs back to normal. Higher = snappier.")]
    private float returnSpeed = 14f;

    private PlayerController _controller;
    private Vector3 _baseScale;
    private Vector2 _shape = Vector2.one;
    private float _fastestFall;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();

        if (visual == null)
        {
            SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null)
                visual = sprite.transform;
        }

        if (visual == null || visual == transform)
        {
            Debug.LogWarning("PlayerSquashStretch: put Pip's sprite on a child object and assign it as Visual. Disabled for now.", this);
            enabled = false;
            return;
        }

        _baseScale = visual.localScale;
    }

    private void OnEnable()
    {
        _controller.Jumped += OnJumped;
        _controller.GroundedChanged += OnGroundedChanged;
        _controller.DashingChanged += OnDashingChanged;
    }

    private void OnDisable()
    {
        _controller.Jumped -= OnJumped;
        _controller.GroundedChanged -= OnGroundedChanged;
        _controller.DashingChanged -= OnDashingChanged;

        _shape = Vector2.one;
        if (visual != null)
            visual.localScale = _baseScale;
    }

    private void FixedUpdate()
    {
        // Remember the fastest fall speed of this airtime, to size the landing squash.
        if (!_controller.IsGrounded)
            _fastestFall = Mathf.Max(_fastestFall, -_controller.Velocity.y);
    }

    private void Update()
    {
        // Spring back toward normal. Using Exp makes it frame-rate independent.
        float t = 1f - Mathf.Exp(-returnSpeed * Time.deltaTime);
        _shape = Vector2.Lerp(_shape, Vector2.one, t);
        visual.localScale = new Vector3(_baseScale.x * _shape.x, _baseScale.y * _shape.y, _baseScale.z);
    }

    private void OnJumped() => _shape = jumpStretch;

    private void OnDashingChanged(bool dashing)
    {
        if (dashing)
            _shape = dashStretch;
    }

    private void OnGroundedChanged(bool grounded)
    {
        if (!grounded)
            return;

        // Landing: squash more the harder Pip hit the ground.
        if (_fastestFall >= minSquashFallSpeed)
        {
            float amount = Mathf.InverseLerp(0f, fullSquashFallSpeed, _fastestFall);
            _shape = Vector2.Lerp(Vector2.one, landSquash, amount);
        }
        _fastestFall = 0f;
    }
}
