using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays all music and sound effects. Lives on the GameSystems prefab (survives scene changes).
///
/// MUSIC
///   - One track at a time. PlayMusic(id) CROSSFADES: the new track fades in while the old one
///     fades out (Crossfade Seconds), so level changes never cut abruptly.
///   - Each level's track comes from its LevelInfo > Music Id, picked automatically when the
///     level loads. The title screen (no LevelInfo) uses Title Music.
///   - Quieter while the game is paused (Paused Music Volume).
///   - Boss music is switched by BossSounds.
///
/// SOUND EFFECTS
///   - AudioManager.Play(SoundIds.Jump) from anywhere. A small pool of AudioSources plays them,
///     so many sounds can overlap; the oldest is reused when all are busy.
///
/// VOLUME
///   Every source's volume = the SoundLibrary entry's Volume x the player's setting
///   (VolumeSettings.Music / .Sfx), updated live when the Settings sliders move.
///   (An AudioMixer can't be created from code, so volumes are applied directly. If the project
///   gets a mixer later, assign its groups to the sources' "Output" in Awake.)
///
/// Setup: on the GameSystems prefab, with the SoundLibrary assigned (the builder does both).
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector fields

    [SerializeField] private SoundLibrary library;
    [SerializeField, Min(1)] private int sfxVoices = 12;
    [SerializeField, Min(0.01f)] private float crossfadeSeconds = 1f;
    [SerializeField, Range(0f, 1f), Tooltip("Music volume multiplier while the game is paused.")]
    private float pausedMusicVolume = 0.35f;
    [SerializeField, Tooltip("Music on the title screen (scenes without a LevelInfo named 'Title').")]
    private string titleMusic = SoundIds.MusicTitle;

    // ------------------------------------------------------------------ Public API

    public static AudioManager Instance { get; private set; }

    public SoundLibrary Library => library;

    /// <summary>Id of the music playing (or fading in), or "" for none.</summary>
    public string CurrentMusicId { get; private set; } = "";

    /// <summary>The AudioSource of the current music (for tests and the rhythm game).</summary>
    public AudioSource MusicSource => _music[_activeMusic];

    /// <summary>The other music source: the old track while it fades out (for tests).</summary>
    public AudioSource FadingMusicSource => _music[1 - _activeMusic];

    /// <summary>The last few sound effect ids played, newest last (handy for tests and debugging).</summary>
    public IReadOnlyList<string> RecentSfx => _recent;

    /// <summary>Plays a sound effect if the AudioManager exists. Safe to call from anywhere.</summary>
    public static void Play(string id)
    {
        if (Instance != null)
            Instance.PlaySfx(id);
    }

    public AudioSource PlaySfx(string id, float volumeScale = 1f)
    {
        SoundLibrary.Entry entry = Find(id);
        AudioClip clip = SoundLibrary.PickClip(entry);
        if (clip == null)
            return null;

        AudioSource source = _sfx[_nextSfx];
        _nextSfx = (_nextSfx + 1) % _sfx.Length;
        source.Stop();
        source.clip = clip;
        source.volume = entry.volume * VolumeSettings.Sfx * volumeScale;
        source.pitch = 1f + Random.Range(-entry.pitchVariation, entry.pitchVariation);
        source.Play();

        _recent.Add(id);
        if (_recent.Count > 20)
            _recent.RemoveAt(0);
        return source;
    }

    /// <summary>Crossfades to a music track. Same id = nothing happens. Empty id = fade out.</summary>
    public void PlayMusic(string id)
    {
        id ??= "";
        if (id == CurrentMusicId)
            return;
        CurrentMusicId = id;
        _volumeAtChange = _music[_activeMusic].volume; // How loud things were when the change began.

        if (id == "")
        {
            _musicEntry = null; // The current source just fades to 0.
            return;
        }

        SoundLibrary.Entry entry = Find(id);
        AudioClip clip = SoundLibrary.PickClip(entry);
        if (clip == null)
            return;

        // Swap to the other source; the old one fades out in Update over Crossfade Seconds.
        _fadeOutRate = Mathf.Max(0.05f, _music[_activeMusic].volume) / crossfadeSeconds;
        _activeMusic = 1 - _activeMusic;
        AudioSource source = _music[_activeMusic];
        source.clip = clip;
        source.volume = 0f;
        source.Play();
        _musicEntry = entry;
    }

    public void StopMusic() => PlayMusic("");

    /// <summary>The volume the current music is fading towards.</summary>
    public float MusicTargetVolume =>
        _musicEntry == null ? 0f : _musicEntry.volume * VolumeSettings.Music * (_paused ? pausedMusicVolume : 1f);

    // ------------------------------------------------------------------ Private state

    private readonly AudioSource[] _music = new AudioSource[2];
    private int _activeMusic;
    private SoundLibrary.Entry _musicEntry;
    private float _volumeAtChange;   // Active track's volume when the music last changed.
    private float _fadeOutRate = 1f; // Volume per second for the track fading out.
    private AudioSource[] _sfx;
    private int _nextSfx;
    private bool _paused;
    private readonly List<string> _recent = new List<string>();
    private readonly HashSet<string> _warned = new HashSet<string>();

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        Instance = this;

        for (int i = 0; i < 2; i++)
            _music[i] = CreateSource($"Music{i + 1}", loop: true);
        _sfx = new AudioSource[sfxVoices];
        for (int i = 0; i < sfxVoices; i++)
            _sfx[i] = CreateSource($"Sfx{i + 1}", loop: false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        GameEvents.SceneLoaded += OnSceneLoaded;
        GameEvents.GamePausedChanged += OnPausedChanged;
    }

    private void OnDisable()
    {
        GameEvents.SceneLoaded -= OnSceneLoaded;
        GameEvents.GamePausedChanged -= OnPausedChanged;
    }

    private void Update()
    {
        // Crossfade and live volume: the active track moves towards its target, the other to 0.
        // Speeds are "volume per second" chosen so each fade takes Crossfade Seconds, however loud
        // the track is (a quiet track shouldn't vanish in half the time).
        AudioSource active = _music[_activeMusic], fading = _music[1 - _activeMusic];
        float target = MusicTargetVolume;
        float inRate = Mathf.Max(0.05f, Mathf.Max(target, _volumeAtChange)) / crossfadeSeconds;
        active.volume = Mathf.MoveTowards(active.volume, target, inRate * Time.unscaledDeltaTime);

        fading.volume = Mathf.MoveTowards(fading.volume, 0f, _fadeOutRate * Time.unscaledDeltaTime);
        if (fading.volume <= 0f && fading.isPlaying)
            fading.Stop();
    }

    // ------------------------------------------------------------------ Event handlers

    private void OnSceneLoaded(string sceneName, string spawnPointId)
    {
        LevelInfo level = LevelInfo.Current;
        if (level != null)
        {
            if (!string.IsNullOrEmpty(level.MusicId))
                PlayMusic(level.MusicId); // Empty = keep whatever is playing.
        }
        else if (sceneName == "Title")
        {
            PlayMusic(titleMusic);
        }
    }

    private void OnPausedChanged(bool paused) => _paused = paused;

    // ------------------------------------------------------------------ Helpers

    private SoundLibrary.Entry Find(string id)
    {
        SoundLibrary.Entry entry = library != null ? library.Find(id) : null;
        if (entry == null && _warned.Add(id ?? "(null)"))
            Debug.LogWarning($"AudioManager: no sound '{id}' in the SoundLibrary.", this);
        return entry;
    }

    private AudioSource CreateSource(string name, bool loop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f; // 2D: same volume wherever the camera is.
        return source;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;
}
