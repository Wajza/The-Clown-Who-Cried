using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The screen look while Blue Sight is on, done with a URP Global Volume.
///
/// Blue Sight shows the world in its NORMAL, full colours: Pip's everyday view is washed out
/// (WashedOutView), and Blue Sight lets her see things as they really are. So the defaults are
/// neutral (full saturation, no tint, normal contrast, no vignette); change them here for a
/// different look (e.g. a light blue tint or a soft vignette).
///
/// How it works: this Volume's Weight fades from 0 (no effect) to 1 (full effect) when Blue Sight
/// turns on. Because its Priority is higher than the WashedOutView Volume, URP blends from the
/// washed-out look into this one by Weight automatically.
///
/// Setup: put this on a GameObject with a Volume (Global, Priority 10). No profile asset needed.
/// </summary>
[RequireComponent(typeof(Volume))]
public class BlueSightVolume : MonoBehaviour
{
    [SerializeField, Min(0f), Tooltip("Seconds to fade the look in or out.")]
    private float fadeTime = 0.25f;

    [Header("Color")]
    [SerializeField, Range(-100f, 0f), Tooltip("Saturation while Blue Sight is on.")]
    private float saturation = 0f;
    [SerializeField, Tooltip("Tint multiplied over the screen. White = no tint (normal colours).")]
    private Color tint = Color.white;
    [SerializeField, Range(-100f, 100f), Tooltip("Contrast while Blue Sight is on. Negative = softer.")]
    private float contrast = 0f;

    [Header("Vignette")]
    [SerializeField, Range(0f, 1f), Tooltip("Darkening at the screen edges. 0 = none.")]
    private float vignetteIntensity = 0f;
    [SerializeField, Tooltip("Vignette color.")]
    private Color vignetteColor = new Color(0.08f, 0.12f, 0.25f);

    private Volume _volume;
    private ColorAdjustments _colorAdjustments;
    private Vignette _vignette;

    private void Reset()
    {
        Volume volume = GetComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10; // Above WashedOutView, so it blends on top.
    }

    private void Awake()
    {
        _volume = GetComponent<Volume>();
        _volume.weight = BlueSight.IsActive ? 1f : 0f;

        // Get (or create) the overrides on this Volume's own profile copy.
        VolumeProfile profile = _volume.profile;
        if (!profile.TryGet(out _colorAdjustments))
            _colorAdjustments = profile.Add<ColorAdjustments>();
        if (!profile.TryGet(out _vignette))
            _vignette = profile.Add<Vignette>();

        ApplyLook();
    }

    private void Update()
    {
        // Fade the whole look in or out by changing the Volume's weight.
        float target = BlueSight.IsActive ? 1f : 0f;
        float step = fadeTime > 0f ? Time.deltaTime / fadeTime : 1f;
        _volume.weight = Mathf.MoveTowards(_volume.weight, target, step);
    }

    private void OnValidate()
    {
        // Lets you tweak the look while playing.
        if (Application.isPlaying && _colorAdjustments != null)
            ApplyLook();
    }

    private void ApplyLook()
    {
        _colorAdjustments.saturation.Override(saturation);
        _colorAdjustments.colorFilter.Override(tint);
        _colorAdjustments.contrast.Override(contrast);

        _vignette.intensity.Override(vignetteIntensity);
        _vignette.color.Override(vignetteColor);
        _vignette.smoothness.Override(0.5f);
    }
}
