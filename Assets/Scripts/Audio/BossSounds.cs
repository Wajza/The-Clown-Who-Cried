using UnityEngine;

/// <summary>
/// Boss sounds and boss music.
///
/// SOUNDS (my teammate's ChortleBoss, found in the level when it loads):
///   AttackWarned -> warning; AttackLaunched -> attack; PhaseChanged -> phase; OnDefeated -> defeat
///
/// MUSIC (any boss, through BossHealthSource, the same thing the boss bar uses):
///   fight on                       -> Boss Music
///   last phase (Final Phase+)      -> Final Phase Music
///   fight over                     -> back to the level's own music (LevelInfo)
///
/// Setup: on the GameSystems prefab (the builder adds it).
/// </summary>
public class BossSounds : MonoBehaviour
{
    [SerializeField] private string bossMusic = SoundIds.MusicBoss;
    [SerializeField] private string finalPhaseMusic = SoundIds.MusicBossFinal;
    [SerializeField, Min(1)] private int finalPhase = 3;

    private ChortleBoss _chortle;
    private BossHealthSource _source;
    private bool _fightMusicOn;

    private void OnEnable()
    {
        GameEvents.SceneLoaded += OnSceneLoaded;
        BossHealthSource.ActiveChanged += RelinkSource;
        RelinkSource();
    }

    private void OnDisable()
    {
        GameEvents.SceneLoaded -= OnSceneLoaded;
        BossHealthSource.ActiveChanged -= RelinkSource;
        LinkChortle(null);
        if (_source != null)
            _source.Changed -= UpdateMusic;
        _source = null;
    }

    // ------------------------------------------------------------------ Chortle's sounds

    private void OnSceneLoaded(string sceneName, string spawnPointId) => LinkChortle(FindAnyObjectByType<ChortleBoss>());

    private void LinkChortle(ChortleBoss boss)
    {
        if (_chortle != null)
        {
            _chortle.AttackWarned -= OnWarned;
            _chortle.AttackLaunched -= OnLaunched;
            _chortle.PhaseChanged -= OnPhase;
            _chortle.OnDefeated -= OnDefeated;
        }
        _chortle = boss;
        if (_chortle != null)
        {
            _chortle.AttackWarned += OnWarned;
            _chortle.AttackLaunched += OnLaunched;
            _chortle.PhaseChanged += OnPhase;
            _chortle.OnDefeated += OnDefeated;
        }
    }

    private static void OnWarned(float seconds) => AudioManager.Play(SoundIds.BossWarn);
    private static void OnLaunched() => AudioManager.Play(SoundIds.BossAttack);
    private static void OnPhase(int phase) => AudioManager.Play(SoundIds.BossPhase);
    private static void OnDefeated() => AudioManager.Play(SoundIds.BossDefeat);

    // ------------------------------------------------------------------ Boss music

    private void RelinkSource()
    {
        if (_source != null)
            _source.Changed -= UpdateMusic;
        _source = BossHealthSource.Active;
        if (_source != null)
            _source.Changed += UpdateMusic;
        UpdateMusic();
    }

    private void UpdateMusic()
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null)
            return;

        bool fighting = _source != null && _source.IsFighting;
        if (fighting)
        {
            audio.PlayMusic(_source.Phase >= finalPhase ? finalPhaseMusic : bossMusic);
            _fightMusicOn = true;
        }
        else if (_fightMusicOn)
        {
            _fightMusicOn = false;
            // Placeholder boss defeats don't raise Chortle's OnDefeated, so play the sting here too.
            if (_chortle == null && _source != null && _source.Health <= 0f)
                AudioManager.Play(SoundIds.BossDefeat);
            LevelInfo level = LevelInfo.Current;
            audio.PlayMusic(level != null ? level.MusicId : "");
        }
    }
}
