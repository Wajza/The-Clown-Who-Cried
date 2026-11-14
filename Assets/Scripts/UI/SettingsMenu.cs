using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Settings screen. Each control reads its value when the screen opens and writes it back
/// the moment it changes, so there's no "Apply" button.
///
///   Music volume, Sound volume   -> VolumeSettings (mine; the AudioManager uses them in Part 9)
///   Screen shake on/off + strength, Hit pause on/off -> GameFeelSettings (my teammate's static
///                                   settings class; I only read and set its public properties)
///
/// Both settings classes save to PlayerPrefs themselves.
///
/// Setup: on the Settings panel inside the MenuCanvas (the builder adds it). MenuController
/// shows and hides it; the Back button calls MenuController.Back().
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector fields

    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle screenShakeToggle;
    [SerializeField] private Slider screenShakeStrengthSlider;
    [SerializeField] private Toggle hitPauseToggle;

    [Header("Value labels (optional)")]
    [SerializeField] private TMP_Text musicValue;
    [SerializeField] private TMP_Text sfxValue;
    [SerializeField] private TMP_Text shakeStrengthValue;

    // ------------------------------------------------------------------ Public API

    /// <summary>The control that gets keyboard/gamepad focus when the screen opens.</summary>
    public GameObject FirstSelectable => musicSlider.gameObject;

    public Slider MusicSlider => musicSlider;
    public Slider SfxSlider => sfxSlider;
    public Toggle ScreenShakeToggle => screenShakeToggle;
    public Slider ScreenShakeStrengthSlider => screenShakeStrengthSlider;
    public Toggle HitPauseToggle => hitPauseToggle;

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        // Sliders go 0..1. Unity moves a slider by 10% per arrow/d-pad press.
        foreach (Slider s in new[] { musicSlider, sfxSlider, screenShakeStrengthSlider })
        {
            s.minValue = 0f;
            s.maxValue = 1f;
        }

        musicSlider.onValueChanged.AddListener(v => { VolumeSettings.Music = Step(v); ShowLabels(); });
        sfxSlider.onValueChanged.AddListener(v => { VolumeSettings.Sfx = Step(v); ShowLabels(); });
        screenShakeStrengthSlider.onValueChanged.AddListener(v => { GameFeelSettings.ScreenShakeStrength = Step(v); ShowLabels(); });
        screenShakeToggle.onValueChanged.AddListener(on =>
        {
            GameFeelSettings.ScreenShakeEnabled = on;
            screenShakeStrengthSlider.interactable = on;
        });
        hitPauseToggle.onValueChanged.AddListener(on => GameFeelSettings.HitPauseEnabled = on);
    }

    private void OnEnable() => ReadValues();

    // ------------------------------------------------------------------ Values

    /// <summary>Shows the current settings without triggering the "changed" callbacks.</summary>
    public void ReadValues()
    {
        musicSlider.SetValueWithoutNotify(VolumeSettings.Music);
        sfxSlider.SetValueWithoutNotify(VolumeSettings.Sfx);
        screenShakeToggle.SetIsOnWithoutNotify(GameFeelSettings.ScreenShakeEnabled);
        screenShakeStrengthSlider.SetValueWithoutNotify(GameFeelSettings.ScreenShakeStrength);
        screenShakeStrengthSlider.interactable = GameFeelSettings.ScreenShakeEnabled;
        hitPauseToggle.SetIsOnWithoutNotify(GameFeelSettings.HitPauseEnabled);
        ShowLabels();
    }

    // Rounds to 5% steps so values look tidy ("80%", not "79.6%").
    private static float Step(float value) => Mathf.Round(value * 20f) / 20f;

    private void ShowLabels()
    {
        SetLabel(musicValue, VolumeSettings.Music);
        SetLabel(sfxValue, VolumeSettings.Sfx);
        SetLabel(shakeStrengthValue, GameFeelSettings.ScreenShakeStrength);
    }

    private static void SetLabel(TMP_Text label, float value)
    {
        if (label != null)
            label.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }
}
