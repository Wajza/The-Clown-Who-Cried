using UnityEngine;

/// <summary>
/// Drives Pip's Animator from the movement and combat code, so the artist's animations play by themselves:
///   Speed (float)       = how fast Pip moves sideways (tiles per second, always positive).
///   Swing (trigger)     = fired the moment a mallet swing starts.
///   SwingSpeed (float)  = how fast to play the Mallet clip so it lasts exactly one swing
///                         (the clip is 0.8 s, a swing ~0.3 s; retuning the swing keeps them in sync).
///   Grounded (bool)     = Pip is standing on something (false while jumping or falling).
/// The controller (Assets/Art/Pip Animation/Walk/Sprite.controller) uses them:
///   Idle -> Walk when Speed > 0.1, Walk -> Idle when Speed < 0.1,
///   any state -> Mallet on Swing, Mallet -> Idle when the clip ends,
///   Idle/Walk -> Jump when Grounded is false, Jump -> Idle when she lands.
///
/// Facing left/right is NOT done here: PlayerController flips the sprite already.
/// New animations (jump, hurt, mallet...) can get their own parameters later.
///
/// Setup: on Pip (the Combat setup tool adds it). Finds the Animator on her sprite by itself.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PipAnimator : MonoBehaviour
{
    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int SwingParam = Animator.StringToHash("Swing");
    private static readonly int SwingSpeedParam = Animator.StringToHash("SwingSpeed");
    private static readonly int GroundedParam = Animator.StringToHash("Grounded");
    private const string MalletClipName = "Pip_Mallet";

    [SerializeField, Tooltip("Pip's Animator (on Visual/Sprite). Found automatically if empty.")]
    private Animator animator;

    private PlayerController _controller;
    private PlayerMallet _mallet;
    private bool _hasSpeed;
    private bool _hasSwing;
    private bool _hasSwingSpeed;
    private bool _hasGrounded;
    private float _malletClipLength;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _mallet = GetComponent<PlayerMallet>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Only drive parameters the controller really has (no warnings if the art changes).
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.nameHash == SpeedParam && p.type == AnimatorControllerParameterType.Float) _hasSpeed = true;
                if (p.nameHash == SwingParam && p.type == AnimatorControllerParameterType.Trigger) _hasSwing = true;
                if (p.nameHash == SwingSpeedParam && p.type == AnimatorControllerParameterType.Float) _hasSwingSpeed = true;
                if (p.nameHash == GroundedParam && p.type == AnimatorControllerParameterType.Bool) _hasGrounded = true;
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
    }

    private void OnDisable()
    {
        if (_mallet != null)
            _mallet.SwingStarted -= OnSwingStarted;
    }

    private void OnSwingStarted(SwingDirection direction)
    {
        if (!_hasSwing)
            return;

        // Play the whole Mallet clip in exactly one swing's time.
        if (_hasSwingSpeed && _malletClipLength > 0f && _mallet.SwingDuration > 0f)
            animator.SetFloat(SwingSpeedParam, _malletClipLength / _mallet.SwingDuration);
        animator.SetTrigger(SwingParam);
    }

    private void Update()
    {
        if (_hasSpeed)
            animator.SetFloat(SpeedParam, Mathf.Abs(_controller.Velocity.x));
        if (_hasGrounded)
            animator.SetBool(GroundedParam, _controller.IsGrounded);
    }
}
