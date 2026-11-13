using System;
using UnityEngine;

/// <summary>
/// Global game-feel options for the settings menu. Saved with PlayerPrefs, so they are
/// remembered between sessions. Example: GameFeelSettings.ScreenShakeEnabled = false;
/// </summary>
public static class GameFeelSettings
{
    private const string ShakeKey = "feel.screenShake";
    private const string ShakeStrengthKey = "feel.screenShakeStrength";
    private const string HitPauseKey = "feel.hitPause";

    private static bool s_loaded;
    private static bool s_screenShake = true;
    private static float s_screenShakeStrength = 1f;
    private static bool s_hitPause = true;

    /// <summary>Fired when any setting changes.</summary>
    public static event Action Changed;

    /// <summary>Screen shake on/off (accessibility: some players get motion sick).</summary>
    public static bool ScreenShakeEnabled
    {
        get { Load(); return s_screenShake; }
        set { Load(); s_screenShake = value; PlayerPrefs.SetInt(ShakeKey, value ? 1 : 0); Changed?.Invoke(); }
    }

    /// <summary>Screen shake strength from 0 to 1, for a slider.</summary>
    public static float ScreenShakeStrength
    {
        get { Load(); return s_screenShakeStrength; }
        set { Load(); s_screenShakeStrength = Mathf.Clamp01(value); PlayerPrefs.SetFloat(ShakeStrengthKey, s_screenShakeStrength); Changed?.Invoke(); }
    }

    /// <summary>Hit pause (the tiny freeze when the mallet lands) on/off.</summary>
    public static bool HitPauseEnabled
    {
        get { Load(); return s_hitPause; }
        set { Load(); s_hitPause = value; PlayerPrefs.SetInt(HitPauseKey, value ? 1 : 0); Changed?.Invoke(); }
    }

    /// <summary>Reads the saved values the first time any setting is used.</summary>
    private static void Load()
    {
        if (s_loaded)
            return;

        s_loaded = true;
        s_screenShake = PlayerPrefs.GetInt(ShakeKey, 1) == 1;
        s_screenShakeStrength = PlayerPrefs.GetFloat(ShakeStrengthKey, 1f);
        s_hitPause = PlayerPrefs.GetInt(HitPauseKey, 1) == 1;
    }

    // Re-read the saved values at the start of every Play session (matters when domain reload is off).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_loaded = false;
        Changed = null;
    }
}
