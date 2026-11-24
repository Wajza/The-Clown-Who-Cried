using UnityEngine;

/// <summary>
/// Drives Pip's Animator from the movement and combat code, so the artist's animations play by themselves:
///   Speed (float)       = how fast Pip moves sideways (tiles per second, always positive).
///   WalkSpeed (float)   = playback speed of the Walk clip, so her feet match how fast she moves.
///   Swing (trigger)     = fired the moment a mallet swing starts.
///   SwingSpeed (float)  = how fast to play the Mallet clip so it lasts exactly one swing
///                         (only used by controllers without SwingPhase).
///   SwingPhase (float)  = which Mallet drawing to show, from the swing's own timing: raising during
///                         the wind-up, the smash exactly while the hitbox is on, then recovering.
///   Swinging (bool)     = a swing is in progress (the Mallet state ends when it turns false).
///   Throw (trigger)     = fired when Pip throws Star Coins (plays the throw pose).
///   Grounded (bool)     = Pip is standing on something (false while jumping or falling). Short
///                         blips off the ground (bumps, tile seams) are ignored so the jump pose
///                         doesn't flicker.
///   AirPose (float 0-1) = which jump drawing to show: the stretched pose while rising, the tucked
///                         pose near the top and while falling (the Jump state uses it as its time).
///   Dashing (bool)      = Pip is in the middle of a dash.
/// The controller (Assets/Art/Pip Animation/Walk/Sprite.controller) uses them; Tools > Clown Who
/// Cried > Demo Polish sets the controller up.
///
/// Facing left/right is NOT done here: PlayerController flips the sprite already.
///
/// Setup: on Pip (the Combat setup tool adds it). Finds the Animator on her sprite by itself.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PipAnimator : MonoBehaviour
{
    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int WalkSpeedParam = Animator.StringToHash("WalkSpeed");
    private static readonly int SwingParam = Animator.StringToHash("Swing");
    private static readonly int SwingSpeedParam = Animator.StringToHash("SwingSpeed");
    private static readonly int ThrowParam = Animator.StringToHash("Throw");
    private static readonly int GroundedParam = Animator.StringToHash("Grounded");
    private static readonly int AirPoseParam = Animator.StringToHash("AirPose");
    private static readonly int DashingParam = Animator.StringToHash("Dashing");
    private static readonly int SwingPhaseParam = Animator.StringToHash("SwingPhase");
    private static readonly int SwingingParam = Animator.StringToHash("Swinging");
    private const string MalletClipName = "Pip_Mallet";

    // Pip_Jump has three drawings: crouch (start), stretched (middle) and tucked (end).
    private const float StretchPose = 0.6f;
    private const float TuckPose = 0.95f;

    [SerializeField, Tooltip("Pip's Animator (on Visual/Sprite). Found automatically if empty.")]
    private Animator animator;
    [SerializeField, Min(0.1f), Tooltip("Pip's run speed (tiles/s). At this speed the Walk clip plays at normal speed.")]
    private float walkClipSpeed = 6f;
    [SerializeField, Min(0f), Tooltip("Off the ground for less than this (seconds, without jumping) still counts as grounded.")]
    private float airGrace = 0.08f;
    [SerializeField, Tooltip("Rising faster than this (tiles/s) shows the stretched jump pose; slower shows the tucked pose.")]
    private float stretchAboveSpeed = 3f;

    private PlayerController _controller;
    private PlayerMallet _mallet;
    private PlayerCoinThrower _coins;
    private bool _hasSpeed;
    private bool _hasWalkSpeed;
    private bool _hasSwing;
    private bool _hasSwingSpeed;
    private bool _hasThrow;
    private bool _hasGrounded;
    private bool _hasAirPose;
    private bool _hasDashing;
    private bool _hasSwingPhase;
    private bool _hasSwinging;
    private float _swingStart = -10f;
    private float _malletClipLength;
    private float _airTime;
    private float _airPose = StretchPose;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _mallet = GetComponent<PlayerMallet>();
        _coins = GetComponent<PlayerCoinThrower>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Only drive parameters the controller really has (no warnings if the art changes).
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                bool isFloat = p.type == AnimatorControllerParameterType.Float;
                bool isBool = p.type == AnimatorControllerParameterType.Bool;
                bool isTrigger = p.type == AnimatorControllerParameterType.Trigger;
                if (p.nameHash == SpeedParam && isFloat) _hasSpeed = true;
                if (p.nameHash == WalkSpeedParam && isFloat) _hasWalkSpeed = true;
                if (p.nameHash == SwingParam && isTrigger) _hasSwing = true;
                if (p.nameHash == SwingSpeedParam && isFloat) _hasSwingSpeed = true;
                if (p.nameHash == ThrowParam && isTrigger) _hasThrow = true;
                if (p.nameHash == GroundedParam && isBool) _hasGrounded = true;
                if (p.nameHash == AirPoseParam && isFloat) _hasAirPose = true;
                if (p.nameHash == DashingParam && isBool) _hasDashing = true;
                if (p.nameHash == SwingPhaseParam && isFloat) _hasSwingPhase = true;
                if (p.nameHash == SwingingParam && isBool) _hasSwinging = true;
            }
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                if (clip.name == MalletClipName)
                    _malletClipLength = clip.length;
        }

        if (animator != null && !_hasSpeed)
            Debug.LogWarning("PipAnimator: Pip's Animator controller has no float 'Speed' parameter; " +
                             "run Tools > Clown Who Cried > Combat > Run Everything.", this);
    }

    private void OnEnable()
    {
        if (_mallet != null)
            _mallet.SwingStarted += OnSwingStarted;
        if (_coins != null)
            _coins.CoinsThrown += OnCoinsThrown;
    }

    private void OnDisable()
    {
        if (_mallet != null)
            _mallet.SwingStarted -= OnSwingStarted;
        if (_coins != null)
            _coins.CoinsThrown -= OnCoinsThrown;
    }

    private void OnSwingStarted(SwingDirection direction)
    {
        _swingStart = Time.time;
        if (!_hasSwing)
            return;

        // Play the whole Mallet clip in exactly one swing's time (only used without SwingPhase).
        if (_hasSwingSpeed && _malletClipLength > 0f && _mallet.SwingDuration > 0f)
            animator.SetFloat(SwingSpeedParam, _malletClipLength / _mallet.SwingDuration);
        if (_hasSwingPhase)
            animator.SetFloat(SwingPhaseParam, SwingPhase(0f));
        animator.SetTrigger(SwingParam);
    }

    private static readonly System.Collections.Generic.List<Collider2D> s_feetHits = new System.Collections.Generic.List<Collider2D>();

    /// <summary>True when Pip's feet are on a moving platform (a PlatformCarrier, e.g. a Ferris wheel gondola).</summary>
    private bool OnMovingPlatform()
    {
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = _controller.GroundLayer, useTriggers = false };
        Physics2D.OverlapBox((Vector2)transform.position + new Vector2(0f, -0.05f), new Vector2(0.7f, 0.2f), 0f,
                             filter, s_feetHits);
        foreach (Collider2D hit in s_feetHits)
            if (hit != null && hit.GetComponentInParent<PlatformCarrier>() != null)
                return true;
        return false;
    }

    /// <summary>
    /// Which part of the Pip_Mallet clip to show, so the smash lands exactly when the hitbox is on:
    /// wind-up = raising the mallet overhead, hitbox on = the swing down and the smash, recovery =
    /// lifting it back. (Clip: 0-0.54 raise, 0.54-0.83 swing + smash, 0.83-1 recover.)
    /// </summary>
    private float SwingPhase(float elapsed)
    {
        float windup = _mallet != null ? _mallet.WindupTime : 0.05f;
        float active = _mallet != null ? _mallet.ActiveTime : 0.1f;
        float total = _mallet != null ? _mallet.SwingDuration : 0.3f;
        if (elapsed < windup)
            return Mathf.Lerp(0.3f, 0.54f, windup > 0f ? elapsed / windup : 1f);
        if (elapsed < windup + active)
            return Mathf.Lerp(0.58f, 0.82f, (elapsed - windup) / active);
        float recovery = Mathf.Max(0.01f, total - windup - active);
        return Mathf.Lerp(0.84f, 0.99f, Mathf.Clamp01((elapsed - windup - active) / recovery));
    }

    private void OnCoinsThrown(int count)
    {
        if (_hasThrow)
            animator.SetTrigger(ThrowParam);
    }

    private void Update()
    {
        Vector2 velocity = _controller.Velocity;
        float speed = Mathf.Abs(velocity.x);

        if (_hasSpeed)
            animator.SetFloat(SpeedParam, speed);
        if (_hasWalkSpeed)
            animator.SetFloat(WalkSpeedParam, Mathf.Clamp(speed / walkClipSpeed, 0.6f, 1.3f));

        // Grounded, ignoring tiny blips off the ground (but a real jump counts at once). Riding a
        // moving platform counts as standing: a rising gondola pushes Pip upward every physics step,
        // so the controller doesn't call that "grounded", but she should look like she's standing.
        bool grounded = _controller.IsGrounded || (velocity.y < 1f && OnMovingPlatform());
        _airTime = grounded ? 0f : _airTime + Time.deltaTime;
        bool showAir = !grounded && (_airTime > airGrace || velocity.y > 1f);
        if (_hasGrounded)
            animator.SetBool(GroundedParam, !showAir);

        // Stretched while rising fast, tucked near the top and on the way down.
        if (_hasAirPose)
        {
            float target = velocity.y > stretchAboveSpeed ? StretchPose : TuckPose;
            _airPose = showAir ? target : StretchPose;
            animator.SetFloat(AirPoseParam, _airPose);
        }

        if (_hasDashing)
            animator.SetBool(DashingParam, _controller.IsDashing);

        bool swinging = _mallet != null && _mallet.IsSwinging;
        if (_hasSwinging)
            animator.SetBool(SwingingParam, swinging);
        if (_hasSwingPhase && swinging)
            animator.SetFloat(SwingPhaseParam, SwingPhase(Time.time - _swingStart));
    }
}
