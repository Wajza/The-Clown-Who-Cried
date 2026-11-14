using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// The W4 "Audition" rhythm mini-game. A note slides along a track toward the hit zone, one per
/// beat-gap; press Advance (Space / Enter / F / gamepad South) as it reaches the zone.
///
/// RULES (all in the Inspector)
///   Notes 8, Hits To Pass 5, BPM 100, Beats Per Note 2 (= a note every 1.2 s),
///   Hit Window 0.15 s either side of the beat, Lead In 1.5 s before the first note.
///   A press too far from the next note counts as a MISS for that note (no button mashing).
///
/// WHEN IT ENDS
///   Pass: sends the task signal (e.g. "w4_passed"), and plays Pass Dialogue (optional).
///   Fail: plays Fail Dialogue ("Not quite! Try again.") and the stage can be used again.
///
/// Timing uses unscaled real time measured from the start, so hit pauses never bend the beat.
/// (Part 9 can start a music track in Begin() at the same moment for the beat to follow.)
///
/// Pip is locked (PlayerInputLock) while it runs, so Advance doesn't also make her jump.
///
/// Setup: in the W4 level, on a "RhythmCanvas" with the track UI (the level builder makes it).
/// A RhythmStage (press F on the stage) starts it.
/// </summary>
public class RhythmMinigame : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector fields

    [Header("Rules")]
    [SerializeField, Min(1)] private int notes = 8;
    [SerializeField, Min(1)] private int hitsToPass = 5;
    [SerializeField, Min(30f)] private float bpm = 100f;
    [SerializeField, Min(0.5f)] private float beatsPerNote = 2f;
    [SerializeField, Min(0.02f)] private float hitWindow = 0.15f;
    [SerializeField, Min(0f)] private float leadIn = 1.5f;

    [Header("Result")]
    [SerializeField] private string passSignal = "w4_passed";
    [SerializeField] private DialogueData passDialogue;
    [SerializeField] private DialogueData failDialogue;

    [Header("UI")]
    [SerializeField, Tooltip("Shown only while playing.")]
    private GameObject panel;
    [SerializeField, Tooltip("The track the note slides along (left to the hit zone).")]
    private RectTransform track;
    [SerializeField] private RectTransform note;
    [SerializeField, Tooltip("Where on the track the hit zone is (0 = left end, 1 = right end).")]
    private float hitZonePosition = 0.85f;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text feedbackText;

    // ------------------------------------------------------------------ Public API (also for tests)

    public bool IsPlaying { get; private set; }
    public int Hits { get; private set; }
    public int Misses { get; private set; }
    public int NotesTotal => notes;
    public int HitsToPass => hitsToPass;
    public bool LastResultPassed { get; private set; }

    /// <summary>Seconds between notes.</summary>
    public float NoteInterval => 60f / bpm * beatsPerNote;

    /// <summary>How far the NEXT note is from its perfect moment (negative = still coming).</summary>
    public float OffsetFromNextNote => Now - NoteTime(_next);

    /// <summary>Starts a round. Ignored if one is already running.</summary>
    public void Begin()
    {
        if (IsPlaying)
            return;
        StartCoroutine(Play());
    }

    /// <summary>The player pressed the button (Update calls this; tests can too).</summary>
    public void Press()
    {
        if (!IsPlaying || _next >= notes)
            return;

        float offset = OffsetFromNextNote;
        if (Mathf.Abs(offset) <= hitWindow)
            Resolve(hit: true);
        else if (offset > -NoteInterval * 0.5f) // Close enough to "mean" this note, but off-beat.
            Resolve(hit: false);
        // Presses long before the note are ignored.
    }

    // ------------------------------------------------------------------ Private state

    private float _startTime;
    private int _next;
    private int _pressFrame = -1;

    private float Now => Time.unscaledTime - _startTime;
    private float NoteTime(int index) => leadIn + index * NoteInterval;

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private void Update()
    {
        // Ignore the press that started the game (Interact F is also an Advance key).
        if (IsPlaying && Time.frameCount != _pressFrame && SystemInput.WasPressed(SystemInput.Advance))
            Press();
    }

    // ------------------------------------------------------------------ The round

    private IEnumerator Play()
    {
        IsPlaying = true;
        Hits = Misses = _next = 0;
        _pressFrame = Time.frameCount;
        _startTime = Time.unscaledTime;
        PlayerInputLock.Lock(this);
        if (panel != null)
            panel.SetActive(true);
        Feedback("Get ready...");

        while (_next < notes)
        {
            // Too late for this note: it's a miss.
            if (Now > NoteTime(_next) + hitWindow)
                Resolve(hit: false);
            DrawNote();
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.6f);
        Finish();
    }

    private void Resolve(bool hit)
    {
        if (hit) Hits++;
        else Misses++;
        _next++;
        Feedback(hit ? "Great!" : "Miss!");
        if (scoreText != null)
            scoreText.text = $"{Hits} / {hitsToPass}   ({_next}/{notes})";
    }

    private void Finish()
    {
        IsPlaying = false;
        if (panel != null)
            panel.SetActive(false);
        PlayerInputLock.Unlock(this);

        LastResultPassed = Hits >= hitsToPass;
        if (LastResultPassed)
            GameEvents.RaiseTaskSignal(passSignal);

        DialogueData talk = LastResultPassed ? passDialogue : failDialogue;
        if (talk != null && DialogueRunner.Instance != null)
            DialogueRunner.Instance.StartDialogue(talk);
    }

    private void DrawNote()
    {
        if (track == null || note == null || _next >= notes)
            return;
        // 0 = the note appears at the left end one interval early; 1 = it's on the hit zone.
        float t = 1f - (NoteTime(_next) - Now) / NoteInterval;
        float x = Mathf.LerpUnclamped(0f, hitZonePosition, t) * track.rect.width;
        note.anchoredPosition = new Vector2(x, 0f);
        note.gameObject.SetActive(t >= 0f);
    }

    private void Feedback(string text)
    {
        if (feedbackText != null)
            feedbackText.text = text;
    }

    private void OnDisable()
    {
        if (IsPlaying) // Scene changed mid-round.
        {
            IsPlaying = false;
            PlayerInputLock.Unlock(this);
        }
    }
}
