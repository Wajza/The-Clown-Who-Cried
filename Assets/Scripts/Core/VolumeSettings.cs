using System;
using UnityEngine;

/// <summary>
/// Music and sound-effect volume (0 = silent, 1 = full), set in the Settings menu and kept
/// between play sessions in PlayerPrefs. Same pattern as my teammate's GameFeelSettings.
///
/// The Settings menu (Part 4) writes these. The AudioManager (Part 9) reads them and listens to
/// Changed to update the AudioMixer.
/// </summary>
public static class VolumeSettings
{
    private const string MusicKey = "volume.music";
    private const string SfxKey = "volume.sfx";

    private static bool s_loaded;
    private static float s_music = 0.8f;
    private static float s_sfx = 0.8f;

    /// <summary>Fired when either volume changes.</summary>
    public static event Action Changed;

    public static float Music
    {
        get { Load(); return s_music; }
        set { Load(); s_music = Mathf.Clamp01(value); PlayerPrefs.SetFloat(MusicKey, s_music); Changed?.Invoke(); }
    }

    public static float Sfx
    {
        get { Load(); return s_sfx; }
        set { Load(); s_sfx = Mathf.Clamp01(value); PlayerPrefs.SetFloat(SfxKey, s_sfx); Changed?.Invoke(); }
    }

    /// <summary>Reads the saved values the first time a volume is used.</summary>
    private static void Load()
    {
        if (s_loaded)
            return;
        s_loaded = true;
        s_music = PlayerPrefs.GetFloat(MusicKey, 0.8f);
        s_sfx = PlayerPrefs.GetFloat(SfxKey, 0.8f);
    }

    // Forget cached values and listeners when Play starts (needed if Domain Reload is off).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_loaded = false;
        Changed = null;
    }
}
