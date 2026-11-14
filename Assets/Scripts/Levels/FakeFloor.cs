using UnityEngine;

/// <summary>
/// A trap floor (W2 "Crooked Mirror Maze"): it LOOKS like solid ground, but Pip falls straight
/// through it. With Blue Sight on, it turns see-through, so the player can spot the trick.
///
/// Built on my teammate's BlueSightReactive (I only say what it should look like; their base
/// class does the fading). Their FakeInBlueSight is the opposite idea: solid until Blue Sight is
/// on. This one is never solid.
///
/// Setup: put the "floor" sprites (or a Tilemap) under this object. Any Collider2D on it is
/// switched off. Put a pit (a trigger tagged "Pit") underneath so a fall sends Pip back.
/// </summary>
public class FakeFloor : BlueSightReactive
{
    [SerializeField, Range(0f, 1f), Tooltip("Opacity while Blue Sight is on.")]
    private float seeThroughAlpha = 0.3f;

    protected override float AlphaWhen(bool blueSightOn) => blueSightOn ? seeThroughAlpha : 1f;

    protected override bool SolidWhen(bool blueSightOn) => false;
}
