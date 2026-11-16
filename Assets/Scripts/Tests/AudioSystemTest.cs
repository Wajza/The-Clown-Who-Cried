using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// An automatic check of Part 9 (audio). It lives in the AudioTest scene (stand-in Pip with a
/// Tear Gauge) and then visits real levels:
///
///   - every SoundIds id has a clip in the SoundLibrary
///   - music: plays, reaches its volume, follows the Music setting, gets quieter when paused,
///     crossfades between tracks
///   - sound effects: volume follows the Sound setting; my teammate's events (star lost, fragment,
///     tear gauge full) and my GameEvents (pause, tasks, journal page) play their sounds
///   - level music from LevelInfo (Tumbleton, the Audition room, the title screen)
///   - boss music: fight -> final phase -> victory sting -> End of Demo music
///
/// It checks what the AudioManager ASKED for (clip, volume, recent sounds) rather than what comes
/// out of the speakers, because batch-mode Unity may have no audio device. Results:
/// Logs/Part9TestResults.txt. Your Music/Sound settings are put back at the end.
/// </summary>
public class AudioSystemTest : SystemTestBase
{
    [SerializeField] private TaskData testTask;
    [SerializeField] private JournalPageData testPage;

    protected override string ResultsFileName => "Part9TestResults.txt";
    protected override string LogTag => "Part9Test";

    private string _sceneLoaded;
    private float _music, _sfx;

    private void OnEnable() => GameEvents.SceneLoaded += OnSceneLoaded;
    private void OnDisable() => GameEvents.SceneLoaded -= OnSceneLoaded;
    private void OnSceneLoaded(string scene, string spawn) => _sceneLoaded = scene;

    private static AudioManager Audio => AudioManager.Instance;

    protected override IEnumerator RunTest()
    {
        bool linked = testTask != null && testPage != null;
        Check("Test driver references are assigned", linked);
        if (!linked)
            yield break;

        yield return WaitUntil(() => GameManager.Instance != null && _sceneLoaded != null, 5f, "first SceneLoaded");
        _music = VolumeSettings.Music;
        _sfx = VolumeSettings.Sfx;
        VolumeSettings.Music = 0.8f;
        VolumeSettings.Sfx = 0.8f;

        CheckLibrary();
        yield return Music();
        yield return SoundEffects();
        yield return LevelMusic();
        yield return BossMusic();

        VolumeSettings.Music = _music;
        VolumeSettings.Sfx = _sfx;
    }

    // ------------------------------------------------------------------ Library

    private void CheckLibrary()
    {
        Check("AudioManager with a SoundLibrary", Audio != null && Audio.Library != null);
        string[] missing = SoundIds.AllMusic.Concat(SoundIds.AllSfx)
            .Where(id => SoundLibrary.PickClip(Audio.Library.Find(id)) == null).ToArray();
        Check($"Every sound id has a clip (missing: {string.Join(", ", missing)})", missing.Length == 0);
        Check("No music in a scene without LevelInfo (except the title)", Audio.CurrentMusicId == "");
    }

    // ------------------------------------------------------------------ Music

    private IEnumerator Music()
    {
        Audio.PlayMusic(SoundIds.MusicTrain);
        AudioClip trainClip = Audio.MusicSource.clip;
        Check("PlayMusic picks the track", Audio.CurrentMusicId == SoundIds.MusicTrain &&
            trainClip == Audio.Library.Find(SoundIds.MusicTrain).clips[0] && Audio.MusicSource.loop);
        yield return new WaitForSecondsRealtime(1.3f);
        float full = Audio.Library.Find(SoundIds.MusicTrain).volume * 0.8f;
        Check($"Music fades in to its volume ({Audio.MusicSource.volume:0.00})", Mathf.Abs(Audio.MusicSource.volume - full) < 0.02f);

        VolumeSettings.Music = 0.4f;
        yield return new WaitForSecondsRealtime(1.3f);
        Check("Music follows the Music setting", Mathf.Abs(Audio.MusicSource.volume - full / 2f) < 0.02f);
        VolumeSettings.Music = 0.8f;
        yield return new WaitForSecondsRealtime(1.3f);

        MenuController.Instance.OpenPause();
        yield return new WaitForSecondsRealtime(1.3f);
        Check("Music is quieter while paused", Audio.MusicSource.volume < full * 0.5f && Audio.MusicSource.volume > 0f);
        Check("Pause sound", Audio.RecentSfx.Contains(SoundIds.UiPause));
        MenuController.Instance.CloseAll();
        yield return new WaitForSecondsRealtime(1.3f);
        Check("...and back after unpausing", Mathf.Abs(Audio.MusicSource.volume - full) < 0.02f && Audio.RecentSfx.Contains(SoundIds.UiUnpause));

        // Crossfade: the new track fades in while the old one fades out.
        Audio.PlayMusic(SoundIds.MusicWonderwick);
        yield return new WaitForSecondsRealtime(0.4f);
        Check("Crossfade: new track coming in, old track going out",
            Audio.MusicSource.volume > 0.05f && Audio.FadingMusicSource.volume > 0.05f &&
            Audio.FadingMusicSource.clip == trainClip);
        yield return new WaitForSecondsRealtime(1.2f);
        Check("Crossfade done: old track silent", Audio.FadingMusicSource.volume == 0f);
        Audio.PlayMusic(SoundIds.MusicWonderwick);
        Check("Asking for the same track again changes nothing", Audio.CurrentMusicId == SoundIds.MusicWonderwick);
    }

    // ------------------------------------------------------------------ Sound effects

    private IEnumerator SoundEffects()
    {
        AudioSource source = Audio.PlaySfx(SoundIds.Jump);
        float expected = Audio.Library.Find(SoundIds.Jump).volume * 0.8f;
        Check("PlaySfx uses the entry's clip and the Sound setting",
            source != null && source.clip == Audio.Library.Find(SoundIds.Jump).clips[0] && Mathf.Abs(source.volume - expected) < 0.01f);
        Check("Jump has a little random pitch", Mathf.Abs(source.pitch - 1f) <= Audio.Library.Find(SoundIds.Jump).pitchVariation + 0.001f);
        Check("An unknown id plays nothing (and warns once)", Audio.PlaySfx("sfx_does_not_exist") == null);

        // My teammate's events -> sounds.
        PlayerController.Current.GetComponent<PlayerHealth>().TakeHazardDamage();
        yield return null;
        Check("Losing a star plays 'star lost'", Audio.RecentSfx.Contains(SoundIds.StarLost));
        PlayerStarFragments.Current.Collect("audio_test_fragment");
        Check("Collecting a fragment plays 'fragment'", Audio.RecentSfx.Last() == SoundIds.Fragment);
        TearGauge gauge = PlayerController.Current.GetComponent<TearGauge>();
        gauge.Add(gauge.MaxValue);
        gauge.Add(1);
        Check("Tear Gauge full plays once", Audio.RecentSfx.Count(id => id == SoundIds.TearFull) == 1);

        // My GameEvents -> jingles.
        TaskManager.Instance.StartTask(testTask);
        Check("New task jingle", Audio.RecentSfx.Last() == SoundIds.TaskNew);
        TaskManager.Instance.CompleteTask(testTask);
        Check("Task done jingle", Audio.RecentSfx.Last() == SoundIds.TaskDone);
        JournalPages.Add(testPage);
        Check("Journal page jingle", Audio.RecentSfx.Last() == SoundIds.PageFound);
        yield return new WaitForSeconds(1.5f); // Let the hazard knockback settle.
    }

    // ------------------------------------------------------------------ Level music

    private IEnumerator LevelMusic()
    {
        yield return Go("Tumbleton", "FromHouse");
        Check("Tumbleton plays its music (LevelInfo)", Audio.CurrentMusicId == SoundIds.MusicTumbleton);
        yield return Go("Wonderwick_W4_Audition", "FromHub");
        Check("The audition room plays the 100 BPM track", Audio.CurrentMusicId == SoundIds.MusicAudition);
        yield return Go("Title", null);
        Check("Title screen music", Audio.CurrentMusicId == SoundIds.MusicTitle);
    }

    // ------------------------------------------------------------------ Boss music

    private IEnumerator BossMusic()
    {
        yield return Go("ChortleArena", "Start");
        yield return SkipCutsceneIfPlaying(); // The intro.
        yield return null;
        // The boss (real Chortle or placeholder) through BossFightFlow; its phase through the boss bar's source.
        IBossFight boss = FindAnyObjectByType<BossFightFlow>()?.Boss;
        var bossComponent = boss as Component;
        IDamageable target = bossComponent != null ? bossComponent.GetComponent<IDamageable>() : null;
        BossHealthSource bar = BossHealthSource.Active;
        Check("Fight on: boss music", target != null && bar != null && bar.IsFighting && Audio.CurrentMusicId == SoundIds.MusicBoss);
        if (target == null || bar == null)
            yield break;

        // One big hit per frame (the real Chortle ignores hits during his short phase intros).
        float giveUp = Time.time + 20f;
        while (bar.Phase < 3 && Time.time < giveUp)
        {
            target.TakeDamage(new DamageInfo(30, Vector2.zero, bossComponent.transform.position, null, DamageType.TearBurst));
            yield return null;
        }
        yield return null;
        Check("Last phase: final-phase music", Audio.CurrentMusicId == SoundIds.MusicBossFinal);

        while (!boss.IsDefeated && Time.time < giveUp)
        {
            target.TakeDamage(new DamageInfo(30, Vector2.zero, bossComponent.transform.position, null, DamageType.TearBurst));
            yield return null;
        }
        yield return null;
        Check("Victory sting", Audio.RecentSfx.Contains(SoundIds.BossDefeat));
        Check("Boss music stops after the fight", Audio.CurrentMusicId != SoundIds.MusicBoss && Audio.CurrentMusicId != SoundIds.MusicBossFinal);

        _sceneLoaded = null;
        yield return SkipCutsceneIfPlaying(); // The outro -> rewards -> End of Demo.
        yield return WaitUntil(() => _sceneLoaded == "EndOfDemo" && !SceneLoader.Instance.IsLoading, 10f, "End of Demo");
        Check("End of Demo music", Audio.CurrentMusicId == SoundIds.MusicEnd);
    }

    private IEnumerator Go(string scene, string spawn)
    {
        _sceneLoaded = null;
        SceneLoader.Instance.LoadScene(scene, spawn);
        yield return WaitUntil(() => _sceneLoaded == scene && !SceneLoader.Instance.IsLoading, 10f, $"scene '{scene}'");
        yield return null;
    }
}
