using System.Collections;
using UnityEngine;

/// <summary>
/// Fades the whole screen to black and back, used for scene changes and respawning.
///
/// HOW IT WORKS
/// A black Image covers the screen inside a Canvas that draws on top of everything. A CanvasGroup
/// controls how see-through it is: alpha 0 = invisible, alpha 1 = fully black.
/// The fades are coroutines, so other coroutines can WAIT for them to finish:
///   yield return ScreenFader.Instance.FadeOut(0.4f);   // screen is now black
///   ...move Pip, load a scene...
///   yield return ScreenFader.Instance.FadeIn(0.4f);    // screen is visible again
///
/// It uses unscaled time, so it still works when the game is paused (Time.timeScale = 0).
///
/// Setup (inside the GameSystems prefab): a Canvas (Screen Space - Overlay, Sort Order 1000)
/// with a CanvasGroup, and a child Image set to black and stretched to fill the screen.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    // ------------------------------------------------------------------ Public API

    /// <summary>The fader in the GameSystems prefab. Null if there isn't one.</summary>
    public static ScreenFader Instance { get; private set; }

    /// <summary>True while the screen is fully or partly covered.</summary>
    public bool IsCovering => _group.alpha > 0f;

    // ------------------------------------------------------------------ Private state

    private CanvasGroup _group;
    private int _fadeCounter; // Goes up by 1 for every fade started (see FadeTo).

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        SetAlpha(0f); // Start invisible.
    }

    private void OnEnable() => Instance = this;

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    // ------------------------------------------------------------------ Fading

    /// <summary>Fades TO black. Use with 'yield return' to wait until the screen is black.</summary>
    public IEnumerator FadeOut(float duration) => FadeTo(1f, duration);

    /// <summary>Fades FROM black back to the game. Use with 'yield return' to wait for it.</summary>
    public IEnumerator FadeIn(float duration) => FadeTo(0f, duration);

    /// <summary>Jumps straight to an alpha with no fade (0 = clear, 1 = black).</summary>
    public void SetAlpha(float alpha)
    {
        _group.alpha = Mathf.Clamp01(alpha);

        // While the screen is covered, block clicks so buttons underneath can't be pressed.
        _group.blocksRaycasts = _group.alpha > 0f;
    }

    /// <summary>
    /// The actual fade. It runs inside the CALLER's coroutine (that's why the caller writes
    /// 'yield return'), so the caller continues exactly when the fade ends.
    /// </summary>
    private IEnumerator FadeTo(float target, float duration)
    {
        // Each fade takes a new number. If another fade starts while this one is running, the
        // numbers no longer match and this one quietly stops, so two fades never fight over
        // the alpha. The caller of the old fade simply carries on.
        int myFade = ++_fadeCounter;

        float start = _group.alpha;
        if (duration <= 0f)
        {
            SetAlpha(target);
            yield break;
        }

        // Move from 'start' to 'target' over 'duration' real-time seconds.
        float t = 0f;
        while (t < 1f)
        {
            if (myFade != _fadeCounter)
                yield break; // A newer fade took over.

            t += Time.unscaledDeltaTime / duration;
            SetAlpha(Mathf.Lerp(start, target, t));
            yield return null; // Wait one frame.
        }

        SetAlpha(target);
    }
}
