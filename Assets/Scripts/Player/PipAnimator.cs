using UnityEngine;

/// <summary>
/// Drives Pip's Animator from the movement code, so the artist's animations play by themselves:
///   Speed (float) = how fast Pip moves sideways (tiles per second, always positive).
/// The controller (Assets/Art/Pip Animation/Walk/Sprite.controller) uses it:
///   Idle -> Walk when Speed > 0.1, Walk -> Idle when Speed < 0.1.
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

    [SerializeField, Tooltip("Pip's Animator (on Visual/Sprite). Found automatically if empty.")]
    private Animator animator;

    private PlayerController _controller;
    private bool _hasSpeed;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Only drive parameters the controller really has (no warnings if the art changes).
        if (animator != null && animator.runtimeAnimatorController != null)
            foreach (AnimatorControllerParameter p in animator.parameters)
                if (p.nameHash == SpeedParam && p.type == AnimatorControllerParameterType.Float)
                    _hasSpeed = true;

        if (animator != null && !_hasSpeed)
            Debug.LogWarning("PipAnimator: Pip's Animator controller has no float 'Speed' parameter; " +
                             "run Tools > Clown Who Cried > Combat > Run Everything.", this);
    }

    private void Update()
    {
        if (_hasSpeed)
            animator.SetFloat(SpeedParam, Mathf.Abs(_controller.Velocity.x));
    }
}
