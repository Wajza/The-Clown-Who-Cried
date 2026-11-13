using UnityEngine;

/// <summary>
/// Hidden platforms, walls and paths that only exist in Blue Sight.
/// Blue Sight off: invisible and not solid. Blue Sight on: visible and solid.
/// For platforms Pip stands on, also put the object on the Ground layer.
/// </summary>
public class BlueSightOnly : BlueSightReactive
{
    [SerializeField, Range(0f, 1f), Tooltip("Opacity when Blue Sight is off. 0 = invisible; a little (0.05) gives players a hint.")]
    private float hiddenAlpha = 0f;

    protected override float AlphaWhen(bool blueSightOn) => blueSightOn ? 1f : hiddenAlpha;
    protected override bool SolidWhen(bool blueSightOn) => blueSightOn;
}
