using UnityEngine;

/// <summary>
/// Dust puffs at Pip's feet when she jumps, lands and dashes.
///
/// Uses three child Particle Systems (any can be left empty to turn that dust off).
/// Each should have: Looping off, Play On Awake off, Emission Rate 0 (this script emits bursts),
/// and Simulation Space = World so the dust stays where it was kicked up.
/// The dash dust is flipped to face backwards: make it blow toward -X (left) while Pip faces right.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerDust : MonoBehaviour
{
    [Header("Particle Systems (children at Pip's feet)")]
    [SerializeField] private ParticleSystem jumpDust;
    [SerializeField] private ParticleSystem landDust;
    [SerializeField] private ParticleSystem dashDust;

    [Header("Amounts")]
    [SerializeField, Min(0)] private int jumpParticles = 6;
    [SerializeField, Min(0), Tooltip("Particles for a soft landing.")]
    private int landParticlesMin = 4;
    [SerializeField, Min(0), Tooltip("Particles for the hardest landing.")]
    private int landParticlesMax = 14;
    [SerializeField, Min(0f), Tooltip("Landing speed (tiles/s) that gives the most particles.")]
    private float hardLandingSpeed = 18f;
    [SerializeField, Min(0f), Tooltip("Landings slower than this make no dust.")]
    private float minLandingSpeed = 3f;
    [SerializeField, Min(0)] private int dashParticles = 8;
    [SerializeField, Tooltip("Also puff dust for air dashes (no ground underneath).")]
    private bool dustOnAirDash = true;

    private PlayerController _controller;
    private float _fastestFall;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
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
    }

    private void FixedUpdate()
    {
        if (!_controller.IsGrounded)
            _fastestFall = Mathf.Max(_fastestFall, -_controller.Velocity.y);
    }

    private void OnJumped() => Emit(jumpDust, jumpParticles);

    private void OnGroundedChanged(bool grounded)
    {
        if (!grounded)
            return;

        if (_fastestFall >= minLandingSpeed)
        {
            float amount = Mathf.InverseLerp(minLandingSpeed, hardLandingSpeed, _fastestFall);
            Emit(landDust, Mathf.RoundToInt(Mathf.Lerp(landParticlesMin, landParticlesMax, amount)));
        }
        _fastestFall = 0f;
    }

    private void OnDashingChanged(bool dashing)
    {
        if (!dashing || dashDust == null)
            return;
        if (!_controller.IsGrounded && !dustOnAirDash)
            return;

        // Flip the dash dust so it always blows out behind Pip.
        Vector3 scale = dashDust.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * _controller.FacingDirection;
        dashDust.transform.localScale = scale;

        Emit(dashDust, dashParticles);
    }

    private static void Emit(ParticleSystem system, int count)
    {
        if (system != null && count > 0)
            system.Emit(count);
    }
}
