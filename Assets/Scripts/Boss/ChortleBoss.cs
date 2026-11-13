using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Boss difficulty. Changes attack speeds and timings, never the warning minimum.</summary>
public enum Difficulty { Cozy, Standard, Showstopper }

/// <summary>Every state Chortle can be in. Only one is active at a time.</summary>
public enum ChortleState
{
    Waiting,      // Before the fight starts.
    PhaseIntro,   // Short pause when a new phase begins (can't be hurt).
    PieVolley,    // Phase 1: winds up and throws 3 pies in arcs.
    Panting,      // Phase 1: tired after a volley. Takes extra damage.
    Stunned,      // Phases 1-2: stunned by a Tear Burst. Takes extra damage.
    CloneShow,    // Phase 2: hides among 4 copies; everyone throws pies.
    BombBarrage,  // Phase 3: throws punchline bombs.
    Defeated,     // Mask at 0: stops and cries. Never dies.
}

/// <summary>
/// Grand Jester Chortle, the first boss.
///
/// HOW THE STATE MACHINE WORKS
/// Each state in ChortleState has one coroutine below (e.g. PieVolleyState). ChangeState() stops
/// the running state and starts the new one, so exactly one state runs at a time. A state ends
/// by calling ChangeState(next). Outside events can also force a change: getting the Mask to a
/// phase threshold (-> PhaseIntro or Defeated) and Tear Burst stuns (-> Stunned).
///
///   Phase 1 (Mask 90-60): PieVolley -> Panting -> PieVolley ...
///   Phase 2 (60-30):      CloneShow (loops by itself)
///   Phase 3 (30-0):       BombBarrage (loops by itself); only Tear Burst really hurts
///   Mask reaches 0:       Defeated (cries, fires OnDefeated)
///
/// Damage can never skip a phase: the Mask stops at each threshold.
/// The arena (TrapdoorFloor, RubberChickenCannon) listens to PhaseChanged and adjusts itself.
///
/// Setup: tag "Boss", Enemy layer, Kinematic Rigidbody2D, trigger Collider2D, SpriteRenderer,
/// SpriteFlasher, ContactDamage, and a child shadow sprite with BlueSightOnly.
/// </summary>
[RequireComponent(typeof(Collider2D), typeof(SpriteFlasher))]
public class ChortleBoss : MonoBehaviour, IDamageable, IStunnable
{
    // ------------------------------------------------------------------ Inspector fields

    [Header("Mask (health)")]
    [SerializeField, Min(1f)] private float maxMask = 90f;
    [SerializeField, Tooltip("Phase 2 starts when the Mask reaches this.")]
    private float phase2At = 60f;
    [SerializeField, Tooltip("Phase 3 starts when the Mask reaches this.")]
    private float phase3At = 30f;

    [Header("Difficulty")]
    [SerializeField] private Difficulty difficulty = Difficulty.Standard;
    [SerializeField, Range(0.3f, 1f), Tooltip("Cozy: attack speed multiplier (lower = slower, more time to react).")]
    private float cozySpeed = 0.75f;
    [SerializeField, Range(1f, 2f), Tooltip("Showstopper: attack speed multiplier (higher = faster).")]
    private float showstopperSpeed = 1.3f;

    [Header("Fight Start")]
    [SerializeField, Tooltip("Start by itself after Start Delay. Turn off to start from a cutscene with StartFight().")]
    private bool startAutomatically = true;
    [SerializeField, Min(0f)] private float startDelay = 2f;

    [Header("References")]
    [SerializeField] private PieProjectile piePrefab;
    [SerializeField] private PunchlineBomb bombPrefab;
    [SerializeField] private ChortleCopy copyPrefab;
    [SerializeField, Tooltip("Spots Chortle teleports between in phases 1 and 3 (pivot = feet).")]
    private Transform[] stagePoints;
    [SerializeField, Tooltip("Spots for Chortle + copies in phase 2. Need at least Copy Count + 1.")]
    private Transform[] clonePoints;
    [SerializeField, Tooltip("Optional puff effect for teleports.")]
    private GameObject teleportEffectPrefab;
    [SerializeField, Tooltip("Optional child (e.g. tear particles) switched on when he is defeated.")]
    private GameObject cryingEffect;
    [SerializeField, Tooltip("Pip's layer (Player).")]
    private LayerMask playerLayer;
    [SerializeField, Tooltip("Ground layer, so pies and bombs stop on floors and walls.")]
    private LayerMask groundLayer;
    [SerializeField, Tooltip("Where pies/bombs leave his hand, relative to the feet (facing right).")]
    private Vector2 throwOffset = new Vector2(0.6f, 1.8f);
    [SerializeField, Tooltip("Tick if the sprite art faces right.")]
    private bool spriteFacesRight = true;

    [Header("Damage Taken")]
    [SerializeField, Min(0f), Tooltip("Phase 1: damage multiplier while panting or stunned.")]
    private float pantingDamageMultiplier = 2f;
    [SerializeField, Min(0f), Tooltip("Phase 2: damage multiplier on the real Chortle.")]
    private float phase2DamageMultiplier = 1f;
    [SerializeField, Min(0f), Tooltip("Phase 3: multiplier for mallet/coins ('barely hurts').")]
    private float phase3RegularMultiplier = 0.1f;
    [SerializeField, Min(0f), Tooltip("Phase 3: multiplier for Tear Burst. 12 x 2.5 = 30 = one burst breaks the mask.")]
    private float phase3TearBurstMultiplier = 2.5f;
    [SerializeField, Min(0f)] private float hitFlashTime = 0.1f;
    [SerializeField, Min(0f), Tooltip("Pause at the start of each phase (he can't be hurt).")]
    private float phaseIntroTime = 1.5f;

    [Header("Phase 1: Pie Volley")]
    [SerializeField, Min(EnemyBase.MinWarningTime)] private float volleyWarning = 0.8f;
    [SerializeField, Min(1)] private int piesPerVolley = 3;
    [SerializeField, Min(0f), Tooltip("Distance between the pies' targets (tiles).")]
    private float pieSpacing = 2f;
    [SerializeField, Min(0f), Tooltip("Seconds between the pies of one volley.")]
    private float pieInterval = 0.2f;
    [SerializeField, Min(0.1f), Tooltip("Pie horizontal speed (tiles per second).")]
    private float pieSpeed = 6f;
    [SerializeField, Min(0.1f), Tooltip("Gravity on pies (tiles/s²).")]
    private float pieGravity = 15f;
    [SerializeField, Min(0)] private int pieDamage = 1;
    [SerializeField, Min(0f), Tooltip("Seconds he pants after each volley (his weak moment).")]
    private float pantTime = 2f;

    [Header("Phase 2: Copies")]
    [SerializeField, Min(0)] private int copyCount = 4;
    [SerializeField, Min(0f), Tooltip("Pause after he and the copies swap places.")]
    private float shufflePause = 0.6f;
    [SerializeField, Min(EnemyBase.MinWarningTime)] private float cloneThrowWarning = 0.7f;
    [SerializeField, Min(0f), Tooltip("Seconds between each jester's throw.")]
    private float cloneThrowStagger = 0.3f;
    [SerializeField, Min(0f), Tooltip("Rest after everyone has thrown.")]
    private float cloneRoundPause = 1.5f;
    [SerializeField, Min(1), Tooltip("Rounds before he shuffles and makes new copies.")]
    private int roundsPerShuffle = 2;

    [Header("Phase 3: Punchline Bombs")]
    [SerializeField, Min(EnemyBase.MinWarningTime)] private float bombWarning = 0.7f;
    [SerializeField, Min(1)] private int bombsPerVolley = 2;
    [SerializeField, Min(0f), Tooltip("Distance between bomb targets (tiles).")]
    private float bombSpread = 3f;
    [SerializeField, Min(0f)] private float bombInterval = 0.3f;
    [SerializeField, Min(0.1f)] private float bombSpeed = 5f;
    [SerializeField, Min(0.1f)] private float bombGravity = 15f;
    [SerializeField, Min(EnemyBase.MinWarningTime), Tooltip("Seconds a landed bomb blinks before exploding.")]
    private float bombFuse = 1f;
    [SerializeField, Min(0.1f), Tooltip("Blast radius (tiles).")]
    private float bombBlastRadius = 1.5f;
    [SerializeField, Min(0)] private int bombDamage = 1;
    [SerializeField, Min(0f), Tooltip("Rest between bomb volleys.")]
    private float bombRestTime = 2f;
    [SerializeField, Min(0), Tooltip("Teleport after this many bomb volleys (0 = never).")]
    private int teleportEveryVolleys = 2;

    // ------------------------------------------------------------------ Public API

    public float Mask { get; private set; }
    public float MaxMask => maxMask;

    /// <summary>0 before the fight, then 1, 2 or 3.</summary>
    public int Phase { get; private set; }

    public ChortleState State { get; private set; } = ChortleState.Waiting;
    public bool IsDefeated => State == ChortleState.Defeated;

    public Difficulty Difficulty
    {
        get => difficulty;
        set => difficulty = value;
    }

    /// <summary>1 on Standard, lower on Cozy, higher on Showstopper.</summary>
    public float SpeedMultiplier => difficulty switch
    {
        Difficulty.Cozy => cozySpeed,
        Difficulty.Showstopper => showstopperSpeed,
        _ => 1f,
    };

    /// <summary>Fired with (mask, max) whenever the Mask bar changes. For the boss health bar.</summary>
    public event Action<float, float> MaskChanged;

    /// <summary>Fired with the new phase (1, 2, 3). The arena and UI listen to this.</summary>
    public event Action<int> PhaseChanged;

    /// <summary>Fired whenever the state changes. Drive the Animator from this.</summary>
    public event Action<ChortleState> StateChanged;

    /// <summary>Fired with the warning length whenever an attack warning starts. For sounds.</summary>
    public event Action<float> AttackWarned;

    /// <summary>Fired when a warned attack actually goes off (a volley, a round of throws). For shake/sound.</summary>
    public event Action AttackLaunched;

    /// <summary>Fired once when the Mask reaches 0 and Chortle starts crying.</summary>
    public event Action OnDefeated;

    // ------------------------------------------------------------------ Difficulty helpers (also used by the arena)

    /// <summary>Scales a duration: shorter on Showstopper, longer on Cozy.</summary>
    public float ScaleTime(float seconds) => seconds / SpeedMultiplier;

    /// <summary>Scales a warning duration, but never below the 0.5 s minimum.</summary>
    public float ScaleWarning(float seconds) => Mathf.Max(ScaleTime(seconds), EnemyBase.MinWarningTime);

    /// <summary>Scales a projectile speed.</summary>
    public float ScaleSpeed(float speed) => speed * SpeedMultiplier;

    // ------------------------------------------------------------------ Private state

    private SpriteRenderer _sprite;
    private SpriteFlasher _flasher;
    private ContactDamage _contactDamage;
    private Coroutine _stateRoutine;
    private float _stunDuration;
    private readonly List<ChortleCopy> _copies = new List<ChortleCopy>();

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        _sprite = GetComponentInChildren<SpriteRenderer>();
        _flasher = GetComponent<SpriteFlasher>();
        _contactDamage = GetComponent<ContactDamage>();
        Mask = maxMask;
    }

    private IEnumerator Start()
    {
        MaskChanged?.Invoke(Mask, maxMask);

        if (startAutomatically)
        {
            yield return new WaitForSeconds(startDelay);
            StartFight();
        }
    }

    private void Update()
    {
        if (State == ChortleState.Defeated || State == ChortleState.Waiting)
            return;

        FacePlayer(transform, _sprite);
        SyncCopies();
    }

    // ------------------------------------------------------------------ Fight control

    /// <summary>Begins phase 1. Call from a cutscene or door if Start Automatically is off.</summary>
    public void StartFight()
    {
        if (State != ChortleState.Waiting)
            return;

        Mask = maxMask;
        MaskChanged?.Invoke(Mask, maxMask);
        ChangeState(ChortleState.PhaseIntro);
    }

    /// <summary>The heart of the state machine: stop the current state, start the next.</summary>
    private void ChangeState(ChortleState next)
    {
        if (_stateRoutine != null)
            StopCoroutine(_stateRoutine);
        _flasher.Stop(); // Cancel any half-finished warning blink.

        State = next;
        StateChanged?.Invoke(next);

        _stateRoutine = StartCoroutine(next switch
        {
            ChortleState.PhaseIntro => PhaseIntroState(),
            ChortleState.PieVolley => PieVolleyState(),
            ChortleState.Panting => PantingState(),
            ChortleState.Stunned => StunnedState(),
            ChortleState.CloneShow => CloneShowState(),
            ChortleState.BombBarrage => BombBarrageState(),
            _ => IdleState(), // Waiting and Defeated do nothing.
        });
    }

    /// <summary>The state each phase starts in (and returns to after a stun).</summary>
    private ChortleState MainStateForPhase => Phase switch
    {
        1 => ChortleState.PieVolley,
        2 => ChortleState.CloneShow,
        _ => ChortleState.BombBarrage,
    };

    // ------------------------------------------------------------------ States

    private IEnumerator IdleState()
    {
        yield break;
    }

    private IEnumerator PhaseIntroState()
    {
        PopAllCopies();
        Phase++;
        PhaseChanged?.Invoke(Phase);

        yield return new WaitForSeconds(phaseIntroTime);
        if (Phase != 2)
            TeleportToRandomStagePoint();
        ChangeState(MainStateForPhase);
    }

    private IEnumerator PieVolleyState()
    {
        // Warning, then a volley of arcing pies aimed around Pip.
        yield return Warn(_flasher, volleyWarning);
        AttackLaunched?.Invoke();

        for (int i = 0; i < piesPerVolley; i++)
        {
            // Spread the targets evenly around Pip: e.g. -2, 0, +2 tiles for three pies.
            float offset = (i - (piesPerVolley - 1) / 2f) * pieSpacing;
            ThrowPie(transform, offset);
            yield return new WaitForSeconds(ScaleTime(pieInterval));
        }

        ChangeState(ChortleState.Panting);
    }

    private IEnumerator PantingState()
    {
        // His weak moment: DamageMultiplier() doubles damage while in this state.
        yield return new WaitForSeconds(ScaleTime(pantTime));
        TeleportToRandomStagePoint();
        ChangeState(ChortleState.PieVolley);
    }

    private IEnumerator StunnedState()
    {
        yield return new WaitForSeconds(_stunDuration);
        ChangeState(MainStateForPhase);
    }

    private IEnumerator CloneShowState()
    {
        while (true)
        {
            yield return ShuffleWithCopies();

            for (int round = 0; round < roundsPerShuffle; round++)
            {
                // Everyone (real + copies still standing) blinks together, then throws one by one.
                List<Transform> throwers = AliveJesters();
                float warning = ScaleWarning(cloneThrowWarning);
                foreach (ChortleCopy copy in _copies)
                    copy.Flasher.Warn(warning);
                yield return Warn(_flasher, cloneThrowWarning);
                AttackLaunched?.Invoke();

                Shuffle(throwers);
                foreach (Transform thrower in throwers)
                {
                    if (thrower != null) // A copy may have been popped during the warning.
                        ThrowPie(thrower, 0f);
                    yield return new WaitForSeconds(ScaleTime(cloneThrowStagger));
                }

                yield return new WaitForSeconds(ScaleTime(cloneRoundPause));

                if (_copies.Count == 0)
                    break; // All copies found: make new ones straight away.
            }
        }
    }

    private IEnumerator BombBarrageState()
    {
        int volleys = 0;
        while (true)
        {
            yield return Warn(_flasher, bombWarning);
            AttackLaunched?.Invoke();

            for (int i = 0; i < bombsPerVolley; i++)
            {
                float offset = (i - (bombsPerVolley - 1) / 2f) * bombSpread;
                ThrowBomb(offset);
                yield return new WaitForSeconds(ScaleTime(bombInterval));
            }

            yield return new WaitForSeconds(ScaleTime(bombRestTime));

            volleys++;
            if (teleportEveryVolleys > 0 && volleys % teleportEveryVolleys == 0)
                TeleportToRandomStagePoint();
        }
    }

    // ------------------------------------------------------------------ Taking damage

    public void TakeDamage(DamageInfo info)
    {
        if (State == ChortleState.Waiting || State == ChortleState.PhaseIntro || State == ChortleState.Defeated)
            return;

        float amount = info.Amount * DamageMultiplier(info.Type);
        if (amount <= 0f)
            return;

        // The Mask can't drop past the current phase's threshold, so no phase is ever skipped.
        float floor = Phase == 1 ? phase2At : Phase == 2 ? phase3At : 0f;
        Mask = Mathf.Max(Mask - amount, floor);
        _flasher.Hit(hitFlashTime);
        MaskChanged?.Invoke(Mask, maxMask);

        if (Mask <= floor)
        {
            if (Phase >= 3)
                Defeat();
            else
                ChangeState(ChortleState.PhaseIntro);
        }
    }

    private float DamageMultiplier(DamageType type)
    {
        bool weak = State == ChortleState.Panting || State == ChortleState.Stunned;

        return Phase switch
        {
            1 => weak ? pantingDamageMultiplier : 1f,
            2 => weak ? pantingDamageMultiplier : phase2DamageMultiplier,
            _ => type == DamageType.TearBurst ? phase3TearBurstMultiplier : phase3RegularMultiplier,
        };
    }

    /// <summary>Tear Burst stuns him in phases 1 and 2. In phase 3 the burst breaks the mask instead.</summary>
    public void Stun(float duration)
    {
        if (Phase >= 3 || State == ChortleState.Waiting || State == ChortleState.PhaseIntro || State == ChortleState.Defeated)
            return;

        _stunDuration = duration;
        ChangeState(ChortleState.Stunned);
    }

    private void Defeat()
    {
        ChangeState(ChortleState.Defeated);
        PopAllCopies();
        Mask = 0f;

        if (_contactDamage != null)
            _contactDamage.enabled = false; // A crying jester doesn't hurt anyone.
        if (cryingEffect != null)
            cryingEffect.SetActive(true);

        OnDefeated?.Invoke();
    }

    // ------------------------------------------------------------------ Attacks

    /// <summary>Blinks the flasher for the (difficulty-scaled, at least 0.5 s) warning time and waits.</summary>
    private IEnumerator Warn(SpriteFlasher flasher, float seconds)
    {
        float duration = ScaleWarning(seconds);
        flasher.Warn(duration);
        AttackWarned?.Invoke(duration);
        yield return new WaitForSeconds(duration);
    }

    /// <summary>Throws an arcing pie from 'thrower' at Pip, shifted sideways by targetOffset tiles.</summary>
    private void ThrowPie(Transform thrower, float targetOffset)
    {
        if (piePrefab == null || PlayerController.Current == null)
            return;

        Vector2 from = HandPosition(thrower);
        Vector2 to = PlayerCenter() + new Vector2(targetOffset, 0f);
        Vector2 velocity = PieProjectile.ArcVelocity(from, to, ScaleSpeed(pieSpeed), pieGravity);

        PieProjectile pie = Instantiate(piePrefab, from, Quaternion.identity);
        pie.Launch(velocity, pieGravity, pieDamage, playerLayer, groundLayer);
    }

    private void ThrowBomb(float targetOffset)
    {
        if (bombPrefab == null || PlayerController.Current == null)
            return;

        Vector2 from = HandPosition(transform);
        Vector2 to = (Vector2)PlayerController.Current.transform.position + new Vector2(targetOffset, 0f);
        Vector2 velocity = PieProjectile.ArcVelocity(from, to, ScaleSpeed(bombSpeed), bombGravity);

        PunchlineBomb bomb = Instantiate(bombPrefab, from, Quaternion.identity);
        bomb.Launch(velocity, bombGravity, ScaleWarning(bombFuse), bombBlastRadius, bombDamage, playerLayer, groundLayer);
    }

    // ------------------------------------------------------------------ Copies (phase 2)

    /// <summary>Clears old copies, then puts Chortle and fresh copies on random clone points.</summary>
    private IEnumerator ShuffleWithCopies()
    {
        PopAllCopies();

        if (copyPrefab == null || clonePoints == null || clonePoints.Length == 0)
        {
            yield return new WaitForSeconds(ScaleTime(shufflePause));
            yield break;
        }

        var points = new List<Transform>(clonePoints);
        Shuffle(points);

        // Point 0 is the real Chortle; the rest get copies.
        TeleportTo(points[0].position);
        int copies = Mathf.Min(copyCount, points.Count - 1);
        for (int i = 1; i <= copies; i++)
        {
            ChortleCopy copy = Instantiate(copyPrefab, points[i].position, Quaternion.identity);
            copy.Popped += OnCopyPopped;
            _copies.Add(copy);
            SpawnPuff(points[i].position);
        }

        SyncCopies();
        yield return new WaitForSeconds(ScaleTime(shufflePause));
    }

    private void OnCopyPopped(ChortleCopy copy)
    {
        copy.Popped -= OnCopyPopped;
        _copies.Remove(copy);
    }

    private void PopAllCopies()
    {
        // Copy the list first: popping removes copies from _copies.
        foreach (ChortleCopy copy in _copies.ToArray())
        {
            if (copy != null)
                copy.Pop();
        }
        _copies.Clear();
    }

    private List<Transform> AliveJesters()
    {
        var list = new List<Transform> { transform };
        foreach (ChortleCopy copy in _copies)
            list.Add(copy.transform);
        return list;
    }

    /// <summary>Copies show exactly the same sprite (animation frame) as the real Chortle.</summary>
    private void SyncCopies()
    {
        if (_sprite == null)
            return;

        foreach (ChortleCopy copy in _copies)
        {
            if (copy.SpriteRenderer == null)
                continue;
            copy.SpriteRenderer.sprite = _sprite.sprite;
            FacePlayer(copy.transform, copy.SpriteRenderer);
        }
    }

    // ------------------------------------------------------------------ Movement and helpers

    private void TeleportToRandomStagePoint()
    {
        if (stagePoints == null || stagePoints.Length == 0)
            return;

        // Pick a different point from where he stands now, when possible.
        Transform point = stagePoints[UnityEngine.Random.Range(0, stagePoints.Length)];
        if (stagePoints.Length > 1 && Vector2.Distance(point.position, transform.position) < 0.1f)
            point = stagePoints[(Array.IndexOf(stagePoints, point) + 1) % stagePoints.Length];

        TeleportTo(point.position);
    }

    private void TeleportTo(Vector2 position)
    {
        SpawnPuff(transform.position);
        transform.position = position;
        SpawnPuff(position);
    }

    private void SpawnPuff(Vector2 feetPosition)
    {
        if (teleportEffectPrefab != null)
            Instantiate(teleportEffectPrefab, feetPosition + Vector2.up, Quaternion.identity);
    }

    private Vector2 HandPosition(Transform thrower)
    {
        int facing = PlayerController.Current != null && PlayerController.Current.transform.position.x < thrower.position.x ? -1 : 1;
        return (Vector2)thrower.position + new Vector2(throwOffset.x * facing, throwOffset.y);
    }

    private static Vector2 PlayerCenter() =>
        (Vector2)PlayerController.Current.transform.position + new Vector2(0f, 0.75f);

    private void FacePlayer(Transform who, SpriteRenderer sprite)
    {
        if (PlayerController.Current == null || sprite == null)
            return;

        bool playerOnLeft = PlayerController.Current.transform.position.x < who.position.x;
        sprite.flipX = spriteFacesRight ? playerOnLeft : !playerOnLeft;
    }

    /// <summary>Fisher-Yates shuffle: puts a list in random order.</summary>
    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
