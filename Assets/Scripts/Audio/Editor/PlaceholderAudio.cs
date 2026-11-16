using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// EDITOR-ONLY. Makes placeholder sounds and music (simple chip-tune beeps and loops) as WAV files
/// in Assets/Audio/Placeholders, named after their id, e.g. "Placeholder_sfx_jump.wav" and
/// "Placeholder_music_tumbleton.wav".
///
/// TO REPLACE ONE: overwrite the file with the real sound, KEEPING THE NAME (Unity keeps its ID,
/// so the SoundLibrary entry still points at it). Or drag the new clip into the SoundLibrary entry.
/// Files are only created when missing, so real audio is never overwritten.
///
/// Everything is generated with maths: square waves (the classic chip-tune sound), triangle
/// waves (soft bass), noise (hits and explosions), with a short fade at the end of each note.
/// </summary>
public static class PlaceholderAudio
{
    public const string Folder = "Assets/Audio/Placeholders";
    private const int Rate = 22050; // Samples per second (plenty for chip-tune placeholders).

    public static string PathFor(string id) => $"{Folder}/Placeholder_{id}.wav";

    public static AudioClip Load(string id) => AssetDatabase.LoadAssetAtPath<AudioClip>(PathFor(id));

    /// <summary>Creates every missing placeholder file.</summary>
    public static void EnsureAll()
    {
        Part1Setup.EnsureFolder(Folder);
        foreach (KeyValuePair<string, Func<float[]>> sound in Sounds())
        {
            string path = PathFor(sound.Key);
            if (File.Exists(path))
                continue;
            WriteWav(path, sound.Value());
            AssetDatabase.ImportAsset(path);
        }
    }

    // ------------------------------------------------------------------ The sounds

    private static Dictionary<string, Func<float[]>> Sounds() => new Dictionary<string, Func<float[]>>
    {
        // Pip
        { SoundIds.Jump, () => Sweep(300, 700, 0.12f, Square) },
        { SoundIds.Land, () => Mix(Noise(0.06f, 0.25f), Sweep(160, 80, 0.06f, Triangle, 0.3f)) },
        { SoundIds.Dash, () => Mix(Noise(0.15f, 0.25f), Sweep(500, 200, 0.15f, Square, 0.2f)) },
        { SoundIds.Swing, () => Noise(0.1f, 0.3f, highPass: true) },
        { SoundIds.Hit, () => Mix(Noise(0.08f, 0.5f), Sweep(200, 80, 0.1f, Square, 0.4f)) },
        { SoundIds.Pogo, () => Sweep(400, 900, 0.15f, Square) },
        { SoundIds.CoinThrow, () => Notes(0.06f, Square, 88, 95) },
        { SoundIds.StarLost, () => Sweep(600, 150, 0.35f, Square) },
        { SoundIds.Death, () => Notes(0.15f, Square, 72, 69, 65, 60) },
        { SoundIds.Shield, () => Notes(0.07f, Triangle, 72, 76, 79, 84) },
        { SoundIds.TearBurst, () => Mix(Noise(0.5f, 0.4f), Sweep(900, 100, 0.5f, Triangle, 0.5f)) },
        { SoundIds.TearFull, () => Notes(0.08f, Square, 79, 84, 88) },
        { SoundIds.BlueSightOn, () => Sweep(300, 1200, 0.25f, Triangle) },
        { SoundIds.BlueSightOff, () => Sweep(1200, 300, 0.2f, Triangle) },
        { SoundIds.Hazard, () => Mix(Noise(0.2f, 0.4f), Sweep(300, 60, 0.25f, Square, 0.3f)) },
        { SoundIds.Fragment, () => Notes(0.07f, Square, 84, 88, 91, 96) },
        // World
        { SoundIds.Pickup, () => Notes(0.06f, Square, 81, 88) },
        { SoundIds.Bomb, () => Noise(0.6f, 0.7f) },
        { SoundIds.Cannon, () => Mix(Noise(0.3f, 0.5f), Sweep(150, 50, 0.3f, Square, 0.5f)) },
        { SoundIds.Checkpoint, () => Notes(0.1f, Triangle, 72, 79, 84) },
        { SoundIds.Break, () => Mix(Noise(0.2f, 0.6f), Sweep(900, 300, 0.12f, Square, 0.2f)) },
        { SoundIds.Switch, () => Notes(0.05f, Square, 60, 72) },
        { SoundIds.Mirror, () => Sweep(200, 1600, 0.4f, Triangle) },
        { SoundIds.BoxPick, () => Notes(0.06f, Square, 64, 67) },
        { SoundIds.BoxDeliver, () => Notes(0.07f, Square, 72, 76, 79) },
        { SoundIds.RhythmHit, () => Notes(0.08f, Square, 84) },
        { SoundIds.RhythmMiss, () => Notes(0.12f, Square, 48) },
        // Boss
        { SoundIds.BossWarn, () => Notes(0.12f, Square, 70, 70) },
        { SoundIds.BossAttack, () => Mix(Noise(0.25f, 0.4f), Sweep(400, 100, 0.25f, Square, 0.4f)) },
        { SoundIds.BossPhase, () => Notes(0.12f, Square, 60, 63, 66, 69) },
        { SoundIds.BossDefeat, () => Notes(0.18f, Square, 72, 71, 70, 69, 60) },
        // UI and jingles
        { SoundIds.UiPause, () => Notes(0.05f, Square, 76, 72) },
        { SoundIds.UiUnpause, () => Notes(0.05f, Square, 72, 76) },
        { SoundIds.UiDialogue, () => Notes(0.04f, Triangle, 79) },
        { SoundIds.TaskNew, () => Notes(0.1f, Square, 72, 76, 79) },
        { SoundIds.TaskDone, () => Notes(0.1f, Square, 72, 76, 79, 84, 84) },
        { SoundIds.TaskFail, () => Notes(0.14f, Square, 67, 63, 60) },
        { SoundIds.PageFound, () => Notes(0.1f, Triangle, 79, 83, 86, 91) },
        // Music loops: (bpm, root note, melody in eighth notes as steps above the root, -1 = rest)
        { SoundIds.MusicTitle, () => Loop(90, 60, new[] { 0, 4, 7, 12, 7, 4, 0, -1, 2, 5, 9, 14, 9, 5, 2, -1 }) },
        { SoundIds.MusicTumbleton, () => Loop(120, 60, new[] { 0, -1, 4, 7, 9, 7, 4, -1, 5, -1, 9, 12, 11, 9, 7, -1 }) },
        { SoundIds.MusicTrain, () => Loop(140, 55, new[] { 0, 0, 7, 0, 5, 0, 7, 0, 0, 0, 7, 0, 12, 10, 7, 5 }) },
        { SoundIds.MusicWonderwick, () => Loop(110, 57, new[] { 0, 3, 7, 3, 8, 7, 3, -1, 0, 3, 7, 10, 8, 7, 5, -1 }) },
        { SoundIds.MusicAudition, () => Loop(100, 60, new[] { 0, -1, 7, -1, 4, -1, 7, -1, 0, -1, 9, -1, 7, -1, 4, -1 }, clickEvery: 2) },
        { SoundIds.MusicBoss, () => Loop(150, 52, new[] { 0, 0, 3, 0, 6, 0, 3, 0, 0, 0, 3, 0, 7, 6, 3, 1 }) },
        { SoundIds.MusicBossFinal, () => Loop(170, 52, new[] { 0, 12, 0, 11, 0, 10, 0, 9, 0, 12, 0, 11, 3, 6, 9, 12 }) },
        { SoundIds.MusicEnd, () => Loop(80, 60, new[] { 0, -1, 4, -1, 7, -1, 12, -1, 11, -1, 7, -1, 4, -1, 2, -1 }) },
    };

    // ------------------------------------------------------------------ Building blocks

    private static float Square(float phase) => phase % 1f < 0.5f ? 1f : -1f;
    private static float Triangle(float phase) => 4f * Mathf.Abs(phase % 1f - 0.5f) - 1f;
    private static float Hz(int midiNote) => 440f * Mathf.Pow(2f, (midiNote - 69) / 12f);

    /// <summary>A pitch slide from one frequency to another, fading out.</summary>
    private static float[] Sweep(float fromHz, float toHz, float seconds, Func<float, float> wave, float volume = 0.35f)
    {
        var s = new float[(int)(seconds * Rate)];
        float phase = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float t = (float)i / s.Length;
            phase += Mathf.Lerp(fromHz, toHz, t) / Rate;
            s[i] = wave(phase) * volume * (1f - t);
        }
        return s;
    }

    /// <summary>Short notes one after another (MIDI note numbers; 60 = middle C).</summary>
    private static float[] Notes(float noteSeconds, Func<float, float> wave, params int[] notes)
    {
        int per = (int)(noteSeconds * Rate);
        var s = new float[per * notes.Length];
        for (int n = 0; n < notes.Length; n++)
        {
            float hz = Hz(notes[n]);
            for (int i = 0; i < per; i++)
                s[n * per + i] = wave(i * hz / Rate) * 0.3f * (1f - (float)i / per * 0.7f);
        }
        return s;
    }

    /// <summary>A burst of noise (hits, explosions), fading out. 'highPass' makes it hissier (swooshes).</summary>
    private static float[] Noise(float seconds, float volume, bool highPass = false)
    {
        var rng = new System.Random(seconds.GetHashCode());
        var s = new float[(int)(seconds * Rate)];
        float last = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            float value = highPass ? white - last : white;
            last = white;
            s[i] = value * volume * (1f - (float)i / s.Length);
        }
        return s;
    }

    private static float[] Mix(float[] a, float[] b)
    {
        var s = new float[Mathf.Max(a.Length, b.Length)];
        for (int i = 0; i < s.Length; i++)
            s[i] = (i < a.Length ? a[i] : 0f) + (i < b.Length ? b[i] : 0f);
        return s;
    }

    /// <summary>
    /// A seamless music loop: the melody (eighth notes, square wave) twice, over a triangle bass
    /// on every beat (root, then fifth). 'clickEvery' adds a click every N beats (the audition beat).
    /// </summary>
    private static float[] Loop(int bpm, int root, int[] melody, int clickEvery = 0)
    {
        float eighth = 60f / bpm / 2f;
        int per = (int)(eighth * Rate);
        int totalEighths = melody.Length * 2;
        var s = new float[per * totalEighths];

        for (int e = 0; e < totalEighths; e++)
        {
            int step = melody[e % melody.Length];
            int beat = e / 2;
            float bassHz = Hz(root - 24 + (beat % 2 == 0 ? 0 : 7));
            for (int i = 0; i < per; i++)
            {
                int index = e * per + i;
                float noteFade = 1f - (float)i / per * 0.6f;
                float value = Triangle(index * bassHz / Rate) * 0.22f;              // Bass (continuous phase).
                if (step >= 0)
                    value += Square(i * Hz(root + step) / Rate) * 0.12f * noteFade; // Melody.
                if (clickEvery > 0 && e % (clickEvery * 2) == 0 && i < per / 6)
                    value += Square(i * 1800f / Rate) * 0.2f * (1f - (float)i / (per / 6));
                s[index] = value;
            }
        }
        return s;
    }

    // ------------------------------------------------------------------ WAV file

    /// <summary>Writes 16-bit mono PCM WAV (the simplest audio file Unity can import).</summary>
    private static void WriteWav(string path, float[] samples)
    {
        using var stream = new FileStream(path, FileMode.Create);
        using var w = new BinaryWriter(stream);
        int dataBytes = samples.Length * 2;
        w.Write(new[] { 'R', 'I', 'F', 'F' });
        w.Write(36 + dataBytes);
        w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
        w.Write(16);            // Format chunk size
        w.Write((short)1);      // PCM
        w.Write((short)1);      // Mono
        w.Write(Rate);
        w.Write(Rate * 2);      // Bytes per second
        w.Write((short)2);      // Bytes per sample
        w.Write((short)16);     // Bits per sample
        w.Write(new[] { 'd', 'a', 't', 'a' });
        w.Write(dataBytes);
        foreach (float sample in samples)
            w.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
    }
}
