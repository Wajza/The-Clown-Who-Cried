using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Pip's normal "washed out" view of the world, done with a URP Global Volume.
///
/// Depth goes from 0 to 1:
///   0 = the start of the game: colors are drained (low saturation, flat contrast).
///   1 = full color.
/// Raise Depth after each boss (e.g. washedOut.Depth += 0.25f) and the color fades back in
/// over transitionTime seconds. When loading a save, use SetDepthInstant so it doesn't fade.
///
/// Setup: put this on a GameObject with a Volume (Global, Priority 0). No profile asset is
/// needed: the script adds a Color Adjustments override to the Volume's own copy of the profile.
/// </summary>
[RequireComponent(typeof(Volume))]
public class WashedOutView : MonoBehaviour
{
    [SerializeField, Range(0f, 1f), Tooltip("0 = washed out (game start), 1 = full color.")]
    private float depth = 0f;
    [SerializeField, Min(0f), Tooltip("Seconds to fade from one Depth to another.")]
    private float transitionTime = 2f;

    [Header("Look at Depth 0")]
    [SerializeField, Range(-100f, 0f), Tooltip("Saturation at Depth 0. -100 = black and white.")]
    private float saturationAtZero = -75f;
    [SerializeField, Range(-100f, 0f), Tooltip("Contrast at Depth 0. Negative = flatter, more faded.")]
    private float contrastAtZero = -15f;

    [Header("Look at Depth 1")]
    [SerializeField, Range(-100f, 100f), Tooltip("Saturation at Depth 1. 0 = the art's real colors.")]
    private float saturationAtOne = 0f;
    [SerializeField, Range(-100f, 100f), Tooltip("Contrast at Depth 1.")]
    private float contrastAtOne = 0f;

    /// <summary>Target color depth (0..1). Setting it fades the screen toward the new value.</summary>
    public float Depth
    {
        get => depth;
        set => depth = Mathf.Clamp01(value);
    }

    /// <summary>The depth currently on screen (it moves toward Depth during a transition).</summary>
    public float ShownDepth { get; private set; }

    private Volume _volume;
    private ColorAdjustments _colorAdjustments;

    private void Reset()
    {
        // Sensible Volume settings when the component is first added.
        Volume volume = GetComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0;
    }

    private void Awake()
    {
        _volume = GetComponent<Volume>();
        _volume.weight = 1f;

        // volume.profile gives this Volume its own copy, so play-mode changes never edit an asset.
        VolumeProfile profile = _volume.profile;
        if (!profile.TryGet(out _colorAdjustments))
            _colorAdjustments = profile.Add<ColorAdjustments>();

        ShownDepth = depth;
        ApplyLook();
    }

    private void Update()
    {
        if (Mathf.Approximately(ShownDepth, depth))
            return;

        float step = transitionTime > 0f ? Time.deltaTime / transitionTime : 1f;
        ShownDepth = Mathf.MoveTowards(ShownDepth, depth, step);
        ApplyLook();
    }

    private void OnValidate()
    {
        // Lets you drag the sliders while playing and see the result.
        if (Application.isPlaying && _colorAdjustments != null)
            ApplyLook();
    }

    /// <summary>Jumps straight to a depth with no fade (e.g. when loading a save).</summary>
    public void SetDepthInstant(float value)
    {
        Depth = value;
        ShownDepth = depth;
        ApplyLook();
    }

    private void ApplyLook()
    {
        // Blend between the Depth 0 look and the Depth 1 look.
        _colorAdjustments.saturation.Override(Mathf.Lerp(saturationAtZero, saturationAtOne, ShownDepth));
        _colorAdjustments.contrast.Override(Mathf.Lerp(contrastAtZero, contrastAtOne, ShownDepth));
    }
}
