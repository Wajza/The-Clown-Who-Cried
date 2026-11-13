using UnityEngine;

/// <summary>
/// Fake walls and floors that are only an illusion.
/// Blue Sight off: look and act like normal, solid level pieces.
/// Blue Sight on: see-through and not solid, so Pip can walk (or fall) through them.
/// </summary>
public class FakeInBlueSight : BlueSightReactive
{
    [SerializeField, Range(0f, 1f), Tooltip("Opacity while Blue Sight is on (see-through).")]
    private float seeThroughAlpha = 0.35f;

    protected override float AlphaWhen(bool blueSightOn) => blueSightOn ? seeThroughAlpha : 1f;
    protected override bool SolidWhen(bool blueSightOn) => !blueSightOn;
}
