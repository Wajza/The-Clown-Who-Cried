using UnityEngine;

/// <summary>
/// A giant jack-in-the-box whose lid pops open on a timer (W1 "Punchline Delivery"). While it's
/// open, the lid is a solid platform Pip can jump on to reach high stalls; then it snaps shut.
///
///   closed for Closed Seconds -> open for Open Seconds -> closed ... (repeats forever)
///   Offset shifts the timing so neighbouring boxes don't all pop at once.
///
/// It just switches two child objects on and off, so the artist can draw both states:
///   Open Look:  the open lid/jack (needs a solid Collider2D on the Ground layer to stand on)
///   Closed Look: the closed box (optional)
///
/// (My teammate's JackInTheBox enemy is a different thing: it hides and pops out to attack.
/// This is just a level platform with a clock.)
/// </summary>
public class TimedLid : MonoBehaviour
{
    [SerializeField, Tooltip("Shown (and solid) while the lid is open.")]
    private GameObject openLook;
    [SerializeField, Tooltip("Optional: shown while closed.")]
    private GameObject closedLook;
    [SerializeField, Min(0.1f)] private float closedSeconds = 2f;
    [SerializeField, Min(0.1f)] private float openSeconds = 2.5f;
    [SerializeField, Min(0f), Tooltip("Seconds added to the clock, to make lids pop at different times.")]
    private float offset;

    public bool IsOpen { get; private set; }

    public float CycleSeconds => closedSeconds + openSeconds;

    private void Start() => Apply(ShouldBeOpen(), force: true);

    private void Update()
    {
        bool open = ShouldBeOpen();
        if (open != IsOpen)
            Apply(open, force: false);
    }

    private bool ShouldBeOpen() => (Time.time + offset) % CycleSeconds >= closedSeconds;

    private void Apply(bool open, bool force)
    {
        if (!force && open == IsOpen)
            return;
        IsOpen = open;
        if (openLook != null)
            openLook.SetActive(open);
        if (closedLook != null)
            closedLook.SetActive(!open);
    }
}
