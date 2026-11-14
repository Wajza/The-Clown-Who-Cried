/// <summary>
/// Every sound and music id the game uses, in one place. Code uses these constants (never typed
/// strings), so a typo is a compile error instead of a silent missing sound. Each id is an entry in
/// the SoundLibrary asset (Assets/Resources/Audio/SoundLibrary), where the actual clip is chosen.
/// </summary>
public static class SoundIds
{
    // ---- Music (looping; one plays at a time, with a crossfade)
    public const string MusicTitle = "music_title";
    public const string MusicTumbleton = "music_tumbleton";
    public const string MusicTrain = "music_train";
    public const string MusicWonderwick = "music_wonderwick";
    public const string MusicAudition = "music_audition";   // 100 BPM, for the W4 rhythm game
    public const string MusicBoss = "music_boss";
    public const string MusicBossFinal = "music_boss_final"; // Chortle's last phase
    public const string MusicEnd = "music_end";             // End of Demo credits

    // ---- Pip (my teammate's player events; PlayerSounds)
    public const string Jump = "sfx_jump";
    public const string Land = "sfx_land";
    public const string Dash = "sfx_dash";
    public const string Swing = "sfx_swing";
    public const string Hit = "sfx_hit";
    public const string Pogo = "sfx_pogo";
    public const string CoinThrow = "sfx_coin_throw";
    public const string StarLost = "sfx_star_lost";
    public const string Death = "sfx_death";
    public const string Shield = "sfx_shield";
    public const string TearBurst = "sfx_tear_burst";
    public const string TearFull = "sfx_tear_full";
    public const string BlueSightOn = "sfx_bluesight_on";
    public const string BlueSightOff = "sfx_bluesight_off";
    public const string Hazard = "sfx_hazard";
    public const string Fragment = "sfx_fragment";

    // ---- World (teammate's static events; WorldSounds) and my task objects
    public const string Pickup = "sfx_pickup";
    public const string Bomb = "sfx_bomb";
    public const string Cannon = "sfx_cannon";
    public const string Checkpoint = "sfx_checkpoint";
    public const string Break = "sfx_break";
    public const string Switch = "sfx_switch";
    public const string Mirror = "sfx_mirror";
    public const string BoxPick = "sfx_box_pick";
    public const string BoxDeliver = "sfx_box_deliver";
    public const string RhythmHit = "sfx_rhythm_hit";
    public const string RhythmMiss = "sfx_rhythm_miss";

    // ---- Boss (Chortle's events; BossSounds)
    public const string BossWarn = "sfx_boss_warn";
    public const string BossAttack = "sfx_boss_attack";
    public const string BossPhase = "sfx_boss_phase";
    public const string BossDefeat = "sfx_boss_defeat";

    // ---- UI and jingles (my GameEvents; GameEventSounds)
    public const string UiPause = "ui_pause";
    public const string UiUnpause = "ui_unpause";
    public const string UiDialogue = "ui_dialogue";
    public const string TaskNew = "jingle_task_new";
    public const string TaskDone = "jingle_task_done";
    public const string TaskFail = "jingle_task_fail";
    public const string PageFound = "jingle_page";

    /// <summary>All music ids (used by the setup tool and the test).</summary>
    public static readonly string[] AllMusic =
    {
        MusicTitle, MusicTumbleton, MusicTrain, MusicWonderwick, MusicAudition, MusicBoss, MusicBossFinal, MusicEnd,
    };

    /// <summary>All sound effect ids.</summary>
    public static readonly string[] AllSfx =
    {
        Jump, Land, Dash, Swing, Hit, Pogo, CoinThrow, StarLost, Death, Shield, TearBurst, TearFull, BlueSightOn, BlueSightOff,
        Hazard, Fragment, Pickup, Bomb, Cannon, Checkpoint, Break, Switch, Mirror, BoxPick, BoxDeliver, RhythmHit, RhythmMiss,
        BossWarn, BossAttack, BossPhase, BossDefeat, UiPause, UiUnpause, UiDialogue, TaskNew, TaskDone, TaskFail, PageFound,
    };
}
