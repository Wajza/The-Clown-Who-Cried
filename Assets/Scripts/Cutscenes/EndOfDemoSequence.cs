using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// The End of Demo screen: "TO BE CONTINUED..." for a moment, then the credits scroll up, then
/// back to the title screen. Advance or Pause (Space, Enter, F, Esc / gamepad buttons) skips
/// straight to the title.
///
/// The progress was already saved before this scene (outside the Laugh Tower), and this level's
/// LevelInfo has "Save On Enter" off, so Continue never starts here.
///
/// Setup: in the EndOfDemo scene, on the canvas (the Part 8 builder adds it). Write the credits
/// in the "Credits" text; the scroll speed adapts to its length.
/// </summary>
public class EndOfDemoSequence : MonoBehaviour
{
    [SerializeField] private CanvasGroup toBeContinued;
    [SerializeField] private RectTransform credits;
    [SerializeField, Min(0f)] private float holdSeconds = 2.5f;
    [SerializeField, Min(1f), Tooltip("UI pixels per second.")]
    private float scrollSpeed = 30f;

    public bool Finished { get; private set; }

    private void Start() => StartCoroutine(Run());

    private void Update()
    {
        if (!Finished && Time.frameCount > _startFrame + 1 &&
            (SystemInput.WasPressed(SystemInput.Advance) || SystemInput.WasPressed(SystemInput.Pause)))
            Skip();
    }

    /// <summary>Straight back to the title screen.</summary>
    public void Skip()
    {
        if (Finished)
            return;
        Finished = true;
        StopAllCoroutines();
        if (GameManager.Instance != null)
            GameManager.Instance.ReturnToTitle();
    }

    private int _startFrame;

    private IEnumerator Run()
    {
        _startFrame = Time.frameCount;
        float height = ((RectTransform)credits.parent).rect.height;
        credits.anchoredPosition = new Vector2(0f, -height / 2f - credits.rect.height / 2f); // Just below the screen.

        if (toBeContinued != null)
        {
            toBeContinued.alpha = 1f;
            yield return new WaitForSecondsRealtime(holdSeconds);
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                toBeContinued.alpha = 1f - t / 0.5f;
                yield return null;
            }
            toBeContinued.alpha = 0f;
        }

        // Scroll until the last line has left the top of the screen.
        float endY = height / 2f + credits.rect.height / 2f;
        while (credits.anchoredPosition.y < endY)
        {
            credits.anchoredPosition += new Vector2(0f, scrollSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.5f);
        Skip();
    }
}
