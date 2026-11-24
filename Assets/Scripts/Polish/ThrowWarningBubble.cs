using UnityEngine;

/// <summary>
/// Shows a "!" bubble where something is about to be thrown, for as long as its warning lasts
/// (the attacker's SpriteFlasher.IsWarning), with a small pop so it catches the eye. Used on the
/// boss arena's rubber-chicken launchers, whose cannon body is hidden: you see the "!", then the
/// chicken flies.
///
/// Setup: next to a SpriteFlasher (e.g. on a RubberChickenCannon), with Bubble = a child sprite.
/// </summary>
public class ThrowWarningBubble : MonoBehaviour
{
    [SerializeField, Tooltip("The '!' sprite shown during the warning.")]
    private SpriteRenderer bubble;
    [SerializeField, Tooltip("Whose warning to show. Found on this object if empty.")]
    private SpriteFlasher flasher;

    private Vector3 _baseScale;
    private float _shownAt;

    private void Awake()
    {
        if (flasher == null)
            flasher = GetComponent<SpriteFlasher>();
        if (bubble != null)
        {
            _baseScale = bubble.transform.localScale;
            bubble.enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (bubble == null || flasher == null)
            return;

        bool show = flasher.IsWarning;
        if (show && !bubble.enabled)
            _shownAt = Time.time;
        bubble.enabled = show;
        if (!show)
            return;

        // Pops in (a quick overshoot), then pulses gently until the throw.
        float t = Time.time - _shownAt;
        float pop = t < 0.15f ? Mathf.Lerp(0.4f, 1.15f, t / 0.15f) : 1f + Mathf.Sin(t * 14f) * 0.06f;
        bubble.transform.localScale = _baseScale * pop;
    }
}
