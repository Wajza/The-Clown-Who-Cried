using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
using P6 = Part6Setup;

/// <summary>
/// EDITOR-ONLY tool that puts the player/combat/enemy/boss scripts into the game, with
/// placeholder art (coloured boxes) until the real sprites exist. Closes TEAMMATE_TODO #4, #8, #9, #11.
///
/// Menu: Tools > Clown Who Cried > Combat > Run Everything
///   1. Prefabs in Assets/Prefabs/{Player, Combat, Pickups, Enemies, Boss, Effects}, every
///      Inspector field filled in. Only CREATED if missing: your edits are never overwritten.
///      (Tools > ... > Combat > Rebuild Prefabs (overwrites) remakes them from scratch.)
///   2. GameSettings > Player Prefab = the new Pip, so every level spawns her.
///   3. ChortleArena: the real Chortle replaces the PlaceholderBoss, plus trapdoors, a pit,
///      floating platforms, rubber-chicken cannons, stage points and clone points.
///   4. Wonderwick_W3_Coaster: the "runaway riders" waves.
///   5. Every level: the Blue Sight / washed-out screen Volumes, and post-processing on the camera.
///   6. Pip prefab extras: the Console log (PlayerDebugLog) and the placeholder swing (MalletSwingVisual).
///   7. Prologue checkpoints after each challenge (Pip's house, then after P2, P3 and P4 in Tumbleton).
///   8. Rebuilds the GameSystems prefab (Sarah's builder), which includes the Controls menu screen.
///   9. Story additions (StorySetup): AudioListeners everywhere, prologue ability lessons, the doctor's office.
///  10. W3's riders remember cleared waves (WaveSpawner Save Id), so the room never resets.
///  11. Pip's animations: a "Speed" parameter and Idle <-> Walk transitions in the artist's
///      controller, and PipAnimator on Pip to feed it.
///  12. The Mallet animation: a "Swing" trigger (any state -> Mallet, back to Idle at the end)
///      and "SwingSpeed" so the clip lasts exactly one swing.
///  13. The Jump animation: a "Grounded" bool; Idle/Walk -> Pip_Jump in the air, back to Idle on
///      landing. Pip_Jump starts as a PLACEHOLDER clip (one Walk frame) that the artist replaces.
///  14. The Dash animation: a "Dashing" bool; any state -> Pip_Dash while dashing, back to Idle
///      after. Pip_Dash is also a PLACEHOLDER clip (one Walk frame) for the artist to replace.
///  15. Chortle's Idle animation lives IN the Chortle prefab (the artist had added the Animator to
///      the arena's copy only), so every Chortle animates.
/// Steps 3-7 are skipped where already done.
///
/// To swap in real art later: replace the sprites on the prefabs' child objects (e.g. Pip/Visual/Sprite).
/// </summary>
public static class CombatSetup
{
    private const string PrefabFolder = "Assets/Prefabs";
    private const string LevelsFolder = "Assets/Scenes/Levels";
    private const string InputActionsPath = "Assets/Scripts/Player/PlayerControls.inputactions";

    private static bool s_overwritePrefabs;
    private static Sprite s_square;

    // Colours of the placeholder boxes.
    private static readonly Color PipPink = new Color(1f, 0.85f, 0.9f);
    private static readonly Color ChortlePurple = new Color(0.6f, 0.3f, 0.8f);
    private static readonly Color WarningYellow = new Color(1f, 0.9f, 0.2f);

    // ------------------------------------------------------------------ Menu

    [MenuItem("Tools/Clown Who Cried/Combat/Run Everything")]
    public static void RunEverything()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        s_square = Part1Setup.GetOrCreatePlaceholderSprite();
        // Each step (re)loads the prefabs AFTER opening its scene: opening a scene unloads
        // assets nothing points at, which would leave older references "destroyed".
        UseRealPip(BuildPrefabs().pip);
        SetUpChortleArena();
        FillW3Waves();
        AddScreenEffectsToLevels();
        UpgradePipPrefab();
        AddPrologueCheckpoints();
        StorySetup.RunEverything();
        RememberW3Waves();
        WirePipAnimations();
        WireMalletAnimation();
        WireJumpAnimation();
        WireDashAnimation();
        PutChortleAnimatorInPrefab();
        GameSystemsPrefabBuilder.Build();
        AssetDatabase.SaveAssets();
        Debug.Log("CombatSetup: done.");
    }

    [MenuItem("Tools/Clown Who Cried/Combat/Rebuild Prefabs (overwrites)")]
    public static void RebuildPrefabs()
    {
        if (!EditorUtility.DisplayDialog("Rebuild combat prefabs?",
                "This remakes every prefab in Assets/Prefabs/{Player, Combat, Pickups, Enemies, Boss, Effects} " +
                "and loses any changes you made to them. Scenes keep their links.", "Rebuild", "Cancel"))
            return;

        s_overwritePrefabs = true;
        try { RunEverything(); }
        finally { s_overwritePrefabs = false; }
    }

    private const string TestScenePath = "Assets/Scenes/Tests/CombatTest.unity";

    [MenuItem("Tools/Clown Who Cried/Combat/Run Test (play mode)")]
    public static void RunTestInEditor()
    {
        RunEverything();
        BuildTestScene();
        EditorSceneManager.OpenScene(TestScenePath);
        EditorApplication.EnterPlaymode();
    }

    /// <summary>For batch mode: set up, then play CombatTest (it exits Unity with 0 = passed, 1 = failed).</summary>
    public static void RunTestsFromCommandLine()
    {
        RunEverything();
        BuildTestScene();
        if (Part1Setup.CheckMissingScripts() > 0)
        {
            EditorApplication.Exit(1);
            return;
        }
        EditorSceneManager.OpenScene(TestScenePath);
        EditorApplication.EnterPlaymode();
    }

    private static void BuildTestScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Main Camera", typeof(Camera));
        cam.tag = "MainCamera";
        cam.GetComponent<Camera>().orthographic = true;
        cam.transform.position = new Vector3(0f, 0f, -10f);

        var driver = new GameObject("Combat Test Driver").AddComponent<CombatTest>();
        Set(driver, ("timeout", 120f));

        EditorSceneManager.SaveScene(scene, TestScenePath);
        Part1Setup.AddScenesToBuildProfile(TestScenePath);
    }

    /// <summary>For batch mode: runs everything, then exits with 1 if any scene/prefab has a missing script.</summary>
    public static void RunFromCommandLine()
    {
        RunEverything();
        EditorApplication.Exit(Part1Setup.CheckMissingScripts() > 0 ? 1 : 0);
    }

    // ------------------------------------------------------------------ 1. Prefabs

    private class Prefabs
    {
        public PlayerController pip;
        public StarCoin coin;
        public Pickup cottonCandy, balloonShield, confettiPopper, starFragment;
        public PieProjectile pie, chicken;
        public EnemyBase wobbler, pieSlinger, jackInTheBox;
        public ChortleBoss chortle;
        public ChortleCopy chortleCopy;
        public PunchlineBomb bomb;
        public GameObject confetti, puff, screenEffects;
    }

    private static Prefabs BuildPrefabs()
    {
        var p = new Prefabs();

        // Effects first: other prefabs point at them.
        p.confetti = Save<Transform>("Effects/Confetti", () => Particles(null, "Confetti", new Vector3(-90f, 0f, 0f), 60f,
            new ParticleSystem.MinMaxGradient(new Color(1f, 0.4f, 0.6f), new Color(0.4f, 0.8f, 1f)), 3f, 6f, 1f, 30, true).gameObject).gameObject;
        p.puff = Save<Transform>("Effects/Puff", () => Particles(null, "Puff", new Vector3(-90f, 0f, 0f), 80f,
            new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.85f, 0.9f)), 1f, 2.5f, -0.2f, 16, true).gameObject).gameObject;

        p.coin = Save<StarCoin>("Combat/StarCoin", BuildStarCoin);
        p.cottonCandy = Save<Pickup>("Pickups/CottonCandy", () => BuildPickup<CottonCandy>("CottonCandy", new Color(1f, 0.6f, 0.85f), 0.6f));
        p.balloonShield = Save<Pickup>("Pickups/BalloonShield", () => BuildPickup<BalloonShield>("BalloonShield", new Color(0.5f, 0.8f, 1f), 0.6f));
        p.confettiPopper = Save<Pickup>("Pickups/ConfettiPopper", () => BuildPickup<ConfettiPopper>("ConfettiPopper", new Color(1f, 0.6f, 0.2f), 0.5f));
        p.starFragment = Save<Pickup>("Pickups/StarFragment", BuildStarFragment);
        p.pip = Save<PlayerController>("Player/Pip", () => BuildPip(p.coin));

        p.pie = Save<PieProjectile>("Enemies/Pie", () => BuildProjectile("Pie", new Vector2(0.5f, 0.35f), new Color(1f, 0.95f, 0.8f)));
        p.wobbler = Save<EnemyBase>("Enemies/WhoopeeWobbler", () => BuildWobbler(p));
        p.pieSlinger = Save<EnemyBase>("Enemies/PieSlinger", () => BuildPieSlinger(p));
        p.jackInTheBox = Save<EnemyBase>("Enemies/JackInTheBox", () => BuildJack(p));

        p.chicken = Save<PieProjectile>("Boss/RubberChicken", () => BuildProjectile("RubberChicken", new Vector2(0.8f, 0.4f), new Color(1f, 0.9f, 0.3f)));
        p.bomb = Save<PunchlineBomb>("Boss/PunchlineBomb", BuildBomb);
        p.chortleCopy = Save<ChortleCopy>("Boss/ChortleCopy", () => BuildChortleCopy(p));
        p.chortle = Save<ChortleBoss>("Boss/Chortle", () => BuildChortle(p));

        p.screenEffects = Save<Transform>("Effects/ScreenEffects", BuildScreenEffects).gameObject;

        AssetDatabase.SaveAssets();
        return p;
    }

    /// <summary>Loads Assets/Prefabs/{name}.prefab, or builds and saves it if it's missing (or when rebuilding).</summary>
    private static T Save<T>(string name, Func<GameObject> build) where T : Component
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null && !s_overwritePrefabs)
            return existing.GetComponent<T>();

        Part1Setup.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        GameObject go = build();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab.GetComponent<T>();
    }

    // ---- Player

    private static GameObject BuildPip(StarCoin coin)
    {
        int player = Part1Setup.RequireLayer("Player");
        int ground = 1 << Part1Setup.RequireLayer("Ground");
        int enemy = 1 << Part1Setup.RequireLayer("Enemy");
        int defaultLayer = 1 << LayerMask.NameToLayer("Default");

        // Root = Pip's FEET. Visual (pivot at the feet, what squash & stretch scales) > Sprite.
        var pip = new GameObject("Pip") { layer = player };
        var visual = new GameObject("Visual") { layer = player };
        visual.transform.SetParent(pip.transform, false);
        GameObject sprite = P6.Box(visual.transform, "Sprite", new Vector2(0f, 0.75f), new Vector2(0.9f, 1.5f), PipPink, 10);
        sprite.layer = player;

        var feet = new GameObject("GroundCheck");
        feet.transform.SetParent(pip.transform, false);

        var col = pip.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.75f, 1.5f);
        col.offset = new Vector2(0f, 0.75f);
        col.sharedMaterial = NoFrictionMaterial();

        var controller = pip.AddComponent<PlayerController>(); // Adds the Rigidbody2D.
        var rb = pip.GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
        Set(controller,
            ("inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath)),
            ("groundCheck", feet.transform),
            ("groundLayer", ground),
            ("spriteRenderer", sprite.GetComponent<SpriteRenderer>()));

        pip.AddComponent<PlayerHealth>();
        pip.AddComponent<PlayerHazards>();
        pip.AddComponent<PlayerStarFragments>();
        pip.AddComponent<PlayerBlueSight>();

        // TEAMMATE_TODO #8: the mallet and coins hit the Enemy layer (enemies, balloons, pots).
        // The mallet also checks Default, where the "Spikes" strips are, so Pip can pogo off them.
        Set(pip.AddComponent<PlayerMallet>(), ("hitLayers", enemy | defaultLayer), ("pogoLayers", enemy));
        Set(pip.AddComponent<PlayerCoinThrower>(), ("coinPrefab", coin), ("hitLayers", enemy | ground));
        Set(pip.AddComponent<TearGauge>(), ("targetLayers", enemy));

        // Game feel.
        pip.AddComponent<HitPause>();
        pip.AddComponent<PlayerShake>();
        Set(pip.AddComponent<PlayerSquashStretch>(), ("visual", visual.transform));
        var dustColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.8f, 0.75f));
        ParticleSystem jump = Particles(pip.transform, "JumpDust", new Vector3(-90f, 0f, 0f), 70f, dustColor, 1f, 2f, -0.1f, 0, false);
        ParticleSystem land = Particles(pip.transform, "LandDust", new Vector3(-90f, 0f, 0f), 85f, dustColor, 1.5f, 3f, -0.1f, 0, false);
        ParticleSystem dash = Particles(pip.transform, "DashDust", new Vector3(0f, -90f, 0f), 20f, dustColor, 1.5f, 3f, -0.1f, 0, false);
        dash.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        Set(pip.AddComponent<PlayerDust>(), ("jumpDust", jump), ("landDust", land), ("dashDust", dash));
        AddPipExtras(pip);
        return pip;
    }

    /// <summary>The Console log and the placeholder swing. Safe to call again (adds only what's missing).</summary>
    private static bool AddPipExtras(GameObject pip)
    {
        bool changed = false;
        if (pip.GetComponent<PlayerDebugLog>() == null)
        {
            pip.AddComponent<PlayerDebugLog>();
            changed = true;
        }
        if (pip.GetComponent<PipAnimator>() == null)
        {
            pip.AddComponent<PipAnimator>(); // Idle/Walk follow her movement.
            changed = true;
        }
        if (pip.GetComponent<PlayerCarryOver>() == null)
        {
            pip.AddComponent<PlayerCarryOver>(); // Stars, shield, Tear Gauge... survive doors.
            changed = true;
        }
        if (pip.GetComponent<MalletSwingVisual>() == null)
        {
            // Directly under Pip (not under Visual, which squash & stretch scales).
            GameObject swing = P6.Box(pip.transform, "MalletSwing", Vector2.zero, Vector2.one, Color.white, 11);
            swing.layer = pip.layer;
            swing.GetComponent<SpriteRenderer>().enabled = false;
            Set(pip.AddComponent<MalletSwingVisual>(), ("swingSprite", swing.GetComponent<SpriteRenderer>()));
            changed = true;
        }
        return changed;
    }

    /// <summary>Adds the extras to an EXISTING Pip prefab, keeping any edits made to it.</summary>
    private static void UpgradePipPrefab()
    {
        string path = $"{PrefabFolder}/Player/Pip.prefab";
        if (!File.Exists(path))
            return;
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (AddPipExtras(root))
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("CombatSetup: added the missing extras (log, carry-over, swing visual) to the Pip prefab.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static PhysicsMaterial2D NoFrictionMaterial()
    {
        string path = $"{PrefabFolder}/Player/Pip_NoFriction.physicsMaterial2D";
        var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (material == null)
        {
            Part1Setup.EnsureFolder($"{PrefabFolder}/Player");
            material = new PhysicsMaterial2D("Pip_NoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(material, path); // No friction: Pip never sticks to walls.
        }
        return material;
    }

    private static GameObject BuildStarCoin()
    {
        var coin = new GameObject("StarCoin");
        P6.Box(coin.transform, "Sprite", Vector2.zero, new Vector2(0.45f, 0.45f), new Color(1f, 0.85f, 0.2f), 15);
        coin.AddComponent<StarCoin>();
        return coin;
    }

    // ---- Pickups

    private static GameObject BuildPickup<T>(string name, Color color, float size) where T : Pickup
    {
        var go = new GameObject(name);
        var trigger = go.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.45f;
        GameObject visual = P6.Box(go.transform, "Visual", Vector2.zero, new Vector2(size, size), color, 8);
        Set(go.AddComponent<T>(), ("visual", visual.transform));
        return go;
    }

    private static GameObject BuildStarFragment()
    {
        GameObject go = BuildPickup<StarFragment>("StarFragment", new Color(1f, 1f, 0.5f), 0.4f);
        // Empty on the prefab: every placed fragment needs its own ID
        // (right-click the StarFragment component > Generate New ID).
        Set(go.GetComponent<StarFragment>(), ("fragmentId", ""));
        return go;
    }

    // ---- Enemies

    /// <summary>Shared enemy set-up: Enemy layer, box collider + sprite standing on the pivot, a '!' warning sign.</summary>
    private static GameObject EnemyBody(string name, Vector2 size, Color color, out SpriteRenderer sprite, out GameObject sign)
    {
        var go = new GameObject(name) { layer = Part1Setup.RequireLayer("Enemy") };
        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        box.offset = new Vector2(0f, size.y / 2f);
        sprite = P6.Box(go.transform, "Sprite", new Vector2(0f, size.y / 2f), size, color, 6).GetComponent<SpriteRenderer>();
        sign = P6.Box(go.transform, "WarningSign", new Vector2(0f, size.y + 0.6f), new Vector2(0.25f, 0.6f), WarningYellow, 20);
        sign.SetActive(false);
        return go;
    }

    private static void SetEnemyCommon(EnemyBase enemy, SpriteRenderer sprite, GameObject sign, Prefabs p, GameObject popEffect)
    {
        int player = 1 << Part1Setup.RequireLayer("Player");
        int ground = 1 << Part1Setup.RequireLayer("Ground");
        Set(enemy,
            ("playerLayer", player),
            ("groundLayer", ground),
            ("spriteRenderer", sprite),
            ("warningSign", sign),
            ("popEffectPrefab", popEffect));
        SetDrops(enemy, (p.cottonCandy, 0.15f), (p.confettiPopper, 0.05f));
    }

    private static Rigidbody2D DynamicBody(GameObject go)
    {
        var rb = go.GetComponent<Rigidbody2D>(); // (Not "??": Unity's fake-null breaks it.)
        if (rb == null)
            rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        return rb;
    }

    private static GameObject BuildWobbler(Prefabs p)
    {
        GameObject go = EnemyBody("WhoopeeWobbler", new Vector2(0.9f, 0.9f), new Color(0.4f, 0.85f, 0.4f), out SpriteRenderer sprite, out GameObject sign);
        DynamicBody(go);
        var enemy = go.AddComponent<WhoopeeWobbler>();
        SetEnemyCommon(enemy, sprite, sign, p, p.confetti); // "Bursts into confetti."
        Set(enemy, ("maxHealth", 2));
        return go;
    }

    private static GameObject BuildPieSlinger(Prefabs p)
    {
        GameObject go = EnemyBody("PieSlinger", new Vector2(0.9f, 1.4f), new Color(1f, 0.55f, 0.25f), out SpriteRenderer sprite, out GameObject sign);
        DynamicBody(go);
        var enemy = go.AddComponent<PieSlinger>();
        SetEnemyCommon(enemy, sprite, sign, p, p.puff);
        Set(enemy, ("maxHealth", 3), ("knockbackMultiplier", 0.3f), ("piePrefab", p.pie));
        return go;
    }

    private static GameObject BuildJack(Prefabs p)
    {
        GameObject go = EnemyBody("JackInTheBox", new Vector2(1f, 1f), new Color(0.75f, 0.45f, 0.25f), out SpriteRenderer sprite, out GameObject sign);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; // He never moves.
        var enemy = go.AddComponent<JackInTheBox>();
        SetEnemyCommon(enemy, sprite, sign, p, p.puff);
        Set(enemy, ("maxHealth", 2), ("knockbackMultiplier", 0f));
        return go;
    }

    private static GameObject BuildProjectile(string name, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        P6.Box(go.transform, "Sprite", Vector2.zero, size, color, 15);
        go.AddComponent<PieProjectile>();
        return go;
    }

    // ---- Boss

    /// <summary>Chortle's look, shared by the real one and the copies so they can't be told apart.</summary>
    private static GameObject JesterBody(string name, out SpriteRenderer sprite, out GameObject sign)
    {
        var go = new GameObject(name) { layer = Part1Setup.RequireLayer("Enemy") };
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        var hitbox = go.AddComponent<BoxCollider2D>();
        hitbox.isTrigger = true; // Pip passes through; touch damage and hits still work.
        hitbox.size = new Vector2(2f, 3.5f);
        hitbox.offset = new Vector2(0f, 1.75f);

        // Sprite must be the FIRST SpriteRenderer: ChortleBoss copies it onto the copies.
        sprite = P6.Box(go.transform, "Sprite", new Vector2(0f, 1.75f), new Vector2(2f, 3.5f), ChortlePurple, 5).GetComponent<SpriteRenderer>();
        P6.Box(go.transform, "Mask", new Vector2(0f, 3f), new Vector2(1.4f, 0.8f), Color.white, 6);
        sign = P6.Box(go.transform, "WarningSign", new Vector2(0f, 4.3f), new Vector2(0.3f, 0.7f), WarningYellow, 20);
        sign.SetActive(false);

        var flasher = go.AddComponent<SpriteFlasher>();
        Set(flasher, ("target", sprite), ("warningSign", sign));
        Set(go.AddComponent<ContactDamage>(), ("playerLayer", 1 << Part1Setup.RequireLayer("Player")));
        return go;
    }

    private static GameObject BuildChortleCopy(Prefabs p)
    {
        GameObject go = JesterBody("ChortleCopy", out _, out _);
        Set(go.AddComponent<ChortleCopy>(), ("popEffectPrefab", p.puff));
        return go;
    }

    private static GameObject BuildChortle(Prefabs p)
    {
        GameObject go = JesterBody("Chortle", out _, out _);
        go.tag = "Boss";

        // Only the real one has a shadow, and only Blue Sight shows it.
        GameObject shadow = P6.Box(go.transform, "Shadow", new Vector2(0f, 0.05f), new Vector2(2.4f, 0.3f), new Color(0.1f, 0.1f, 0.25f, 0.8f), 4);
        shadow.AddComponent<BlueSightOnly>();

        GameObject tears = P6.Box(go.transform, "CryingEffect", new Vector2(0.35f, 2.5f), new Vector2(0.15f, 0.6f), new Color(0.5f, 0.75f, 1f), 7);
        tears.SetActive(false);

        var boss = go.AddComponent<ChortleBoss>();
        Set(boss,
            ("startAutomatically", false), // BossFightFlow starts him after the intro cutscene.
            ("piePrefab", p.pie),
            ("bombPrefab", p.bomb),
            ("copyPrefab", p.chortleCopy),
            ("teleportEffectPrefab", p.puff),
            ("cryingEffect", tears),
            ("playerLayer", 1 << Part1Setup.RequireLayer("Player")),
            ("groundLayer", 1 << Part1Setup.RequireLayer("Ground")));

        go.AddComponent<ChortleFight>();         // Sarah's: lets BossFightFlow start/finish him.
        go.AddComponent<ChortleHealthSource>();  // Sarah's: feeds the boss health bar.
        go.AddComponent<BossShake>();
        return go;
    }

    private static GameObject BuildBomb()
    {
        var go = new GameObject("PunchlineBomb");
        SpriteRenderer sprite = P6.Box(go.transform, "Sprite", Vector2.zero, new Vector2(0.5f, 0.5f), new Color(0.15f, 0.15f, 0.2f), 15).GetComponent<SpriteRenderer>();
        GameObject blast = P6.Box(go.transform, "BlastIndicator", Vector2.zero, Vector2.one, new Color(1f, 0.2f, 0.2f, 0.3f), 2);
        blast.SetActive(false);
        Set(go.AddComponent<SpriteFlasher>(), ("target", sprite));
        Set(go.AddComponent<PunchlineBomb>(), ("blastIndicator", blast.transform));
        return go;
    }

    // ---- Screen effects

    private static GameObject BuildScreenEffects()
    {
        var root = new GameObject("ScreenEffects");

        var washed = new GameObject("WashedOutVolume", typeof(Volume));
        washed.transform.SetParent(root.transform, false);
        var washedVolume = washed.GetComponent<Volume>();
        washedVolume.isGlobal = true;
        washedVolume.priority = 0;
        washed.AddComponent<WashedOutView>();

        var blue = new GameObject("BlueSightVolume", typeof(Volume));
        blue.transform.SetParent(root.transform, false);
        var blueVolume = blue.GetComponent<Volume>();
        blueVolume.isGlobal = true;
        blueVolume.priority = 10;
        blueVolume.weight = 0f;
        blue.AddComponent<BlueSightVolume>();
        return root;
    }

    // ---- Particles

    /// <summary>
    /// A small burst particle system using the placeholder square. 'rotation' aims the cone:
    /// (-90, 0, 0) = up, (0, -90, 0) = left. burst > 0 + playOnAwake = a one-shot effect that
    /// destroys itself; otherwise scripts call Emit().
    /// </summary>
    private static ParticleSystem Particles(Transform parent, string name, Vector3 rotation, float angle,
        ParticleSystem.MinMaxGradient color, float speedMin, float speedMax, float gravity, int burst, bool playOnAwake)
    {
        var go = new GameObject(name);
        if (parent != null)
            go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.playOnAwake = playOnAwake;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(0.125f, 0.25f);
        main.startColor = color;
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;
        main.stopAction = playOnAwake ? ParticleSystemStopAction.Destroy : ParticleSystemStopAction.None;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        if (burst > 0)
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = angle;
        shape.radius = 0.2f;
        shape.rotation = rotation;

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ParticleSystem.TextureSheetAnimationModule sheet = ps.textureSheetAnimation;
        sheet.enabled = true;
        sheet.mode = ParticleSystemAnimationMode.Sprites;
        sheet.AddSprite(s_square);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = DefaultSpriteMaterial();
        renderer.sortingOrder = 12;
        return ps;
    }

    private static Material DefaultSpriteMaterial()
    {
        // Whatever material the render pipeline gives new sprites (URP 2D's Sprite-Lit-Default).
        var temp = new GameObject("temp", typeof(SpriteRenderer));
        Material material = temp.GetComponent<SpriteRenderer>().sharedMaterial;
        Object.DestroyImmediate(temp);
        return material;
    }

    // ------------------------------------------------------------------ 2. Pip in every level

    private static void UseRealPip(PlayerController pip)
    {
        GameSettings settings = Resources.Load<GameSettings>("GameSettings");
        if (settings == null)
        {
            Debug.LogError("CombatSetup: Assets/Resources/GameSettings is missing (run Part 1 first).");
            return;
        }
        if (settings.playerPrefab == pip)
            return;

        settings.playerPrefab = pip;
        EditorUtility.SetDirty(settings);
        Debug.Log("CombatSetup: GameSettings > Player Prefab = Pip.");
    }

    // ------------------------------------------------------------------ 3. ChortleArena

    private const string ArenaRootName = "Arena (Combat)";

    private static void SetUpChortleArena()
    {
        Scene? opened = OpenLevel("ChortleArena");
        if (opened == null)
            return;
        Prefabs p = BuildPrefabs();
        Scene scene = opened.Value;
        if (GameObject.Find(ArenaRootName) != null)
        {
            Debug.Log("CombatSetup: ChortleArena is already set up; left unchanged.");
            return;
        }

        var flow = Object.FindAnyObjectByType<BossFightFlow>();
        if (flow == null)
        {
            Debug.LogError("CombatSetup: ChortleArena has no BossFightFlow. Run Part 8 > Run Everything first.");
            return;
        }

        var root = new GameObject(ArenaRootName).transform;
        int groundLayer = Part1Setup.RequireLayer("Ground");

        // ---- The real Chortle replaces the placeholder (TEAMMATE_TODO #11).
        float bossX = 24f;
        var placeholder = Object.FindAnyObjectByType<PlaceholderBoss>(FindObjectsInactive.Include);
        if (placeholder != null)
        {
            bossX = placeholder.transform.position.x;
            Object.DestroyImmediate(placeholder.gameObject);
        }
        var chortleGo = (GameObject)PrefabUtility.InstantiatePrefab(p.chortle.gameObject, scene);
        var chortle = chortleGo.GetComponent<ChortleBoss>();
        chortle.transform.SetParent(flow.transform.parent, false);
        chortle.transform.position = new Vector3(bossX, 0f, 0f);

        Transform[] stagePoints = Points(root, "StagePoint", 22f, 26f, 29.5f);
        Transform[] clonePoints = Points(root, "ClonePoint", 21f, 23.5f, 26f, 28.5f, 30.5f);
        Set(chortle, ("stagePoints", stagePoints), ("clonePoints", clonePoints));
        Set(flow, ("boss", chortle.GetComponent<ChortleFight>()));

        // ---- Trapdoor floor: tiles x 8..19 become 4 trapdoors (3 tiles each) over a pit.
        Tilemap tiles = FindGroundTilemap();
        const int trapFrom = 8, trapTo = 19;
        if (tiles != null)
        {
            for (int x = trapFrom; x <= trapTo; x++)
                for (int y = -3; y <= -1; y++)
                    tiles.SetTile(new Vector3Int(x, y, 0), null);
        }

        var floorGo = new GameObject("TrapdoorFloor");
        floorGo.transform.SetParent(root);
        var floor = floorGo.AddComponent<TrapdoorFloor>();
        var sections = new List<TrapdoorSection>();
        for (int start = trapFrom; start + 2 <= trapTo; start += 3)
        {
            var section = new GameObject($"Trapdoor_{sections.Count + 1}", typeof(BoxCollider2D)) { layer = groundLayer };
            section.transform.SetParent(floorGo.transform);
            section.transform.position = new Vector3(start + 1.5f, -0.5f, 0f);
            section.GetComponent<BoxCollider2D>().size = new Vector2(3f, 1f);
            SpriteRenderer sprite = P6.Box(section.transform, "Sprite", Vector2.zero, new Vector2(2.9f, 1f), new Color(0.55f, 0.4f, 0.3f), 1).GetComponent<SpriteRenderer>();
            Set(section.AddComponent<SpriteFlasher>(), ("target", sprite));
            var trapdoor = section.AddComponent<TrapdoorSection>(); // Also adds UnsafeGround.
            Set(trapdoor, ("spriteRenderer", sprite));
            sections.Add(trapdoor);
        }
        Set(floor, ("boss", chortle), ("sections", sections.ToArray()));

        // The pit under the trapdoors: a fall costs a star and puts Pip back on safe ground.
        var pit = new GameObject("Pit", typeof(BoxCollider2D)) { tag = "Pit" };
        pit.transform.SetParent(root);
        pit.transform.position = new Vector3((trapFrom + trapTo + 1) / 2f, -3f, 0f);
        var pitBox = pit.GetComponent<BoxCollider2D>();
        pitBox.isTrigger = true;
        pitBox.size = new Vector2(trapTo - trapFrom + 1, 2f);

        // ---- Floating platforms over the trapdoors (normal, safe ground).
        // Tops at 2 and 4 tiles: Pip's jump is 2.5, so each one is an easy jump from below.
        Platform(root, "Platform_Left", new Vector2(10f, 2f), 3f, groundLayer);
        Platform(root, "Platform_Right", new Vector2(18f, 2f), 3f, groundLayer);
        Platform(root, "Platform_Top", new Vector2(14f, 4f), 3f, groundLayer);

        // ---- Rubber-chicken cannons on both walls (different heights, out of step).
        Cannon(root, p, chortle, "Cannon_Left", new Vector2(0.5f, 3.2f), Vector2.right, 0f);
        Cannon(root, p, chortle, "Cannon_Right", new Vector2(31.5f, 0.6f), Vector2.left, 2f);

        EditorSceneManager.SaveScene(scene);
        Debug.Log("CombatSetup: ChortleArena now has the real Chortle and his arena.");
    }

    private static Transform[] Points(Transform root, string name, params float[] xs)
    {
        var parent = new GameObject($"{name}s").transform;
        parent.SetParent(root);
        var points = new Transform[xs.Length];
        for (int i = 0; i < xs.Length; i++)
        {
            points[i] = new GameObject($"{name}_{i + 1}").transform;
            points[i].SetParent(parent);
            points[i].position = new Vector3(xs[i], 0f, 0f);
        }
        return points;
    }

    private static void Platform(Transform root, string name, Vector2 top, float width, int layer)
    {
        var go = new GameObject(name, typeof(BoxCollider2D)) { layer = layer };
        go.transform.SetParent(root);
        go.transform.position = new Vector3(top.x, top.y - 0.25f, 0f);
        go.GetComponent<BoxCollider2D>().size = new Vector2(width, 0.5f);
        P6.Box(go.transform, "Sprite", Vector2.zero, new Vector2(width, 0.5f), new Color(0.45f, 0.5f, 0.6f), 1);
    }

    private static void Cannon(Transform root, Prefabs p, ChortleBoss boss, string name, Vector2 position, Vector2 direction, float startOffset)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root);
        go.transform.position = position;
        SpriteRenderer sprite = P6.Box(go.transform, "Sprite", Vector2.zero, new Vector2(1f, 0.6f), new Color(0.5f, 0.5f, 0.55f), 2).GetComponent<SpriteRenderer>();
        var muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(go.transform, false);
        muzzle.localPosition = new Vector3(direction.x * 0.8f, 0f, 0f); // Just in front of the barrel, outside the wall.

        Set(go.AddComponent<SpriteFlasher>(), ("target", sprite));
        Set(go.AddComponent<RubberChickenCannon>(),
            ("boss", boss),
            ("chickenPrefab", p.chicken),
            ("muzzle", muzzle),
            ("direction", direction),
            ("startOffset", startOffset),
            ("playerLayer", 1 << Part1Setup.RequireLayer("Player")),
            ("groundLayer", 1 << Part1Setup.RequireLayer("Ground")));
    }

    private static Tilemap FindGroundTilemap()
    {
        foreach (Tilemap map in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include))
            if (map.name == "Ground")
                return map;
        Debug.LogError("CombatSetup: no 'Ground' tilemap in ChortleArena; trapdoor holes not cut.");
        return null;
    }

    // ------------------------------------------------------------------ 4. W3 waves

    private static void FillW3Waves()
    {
        Scene? opened = OpenLevel("Wonderwick_W3_Coaster");
        if (opened == null)
            return;
        Prefabs p = BuildPrefabs();

        WaveSpawner spawner = P6.FindNamed<WaveSpawner>("WaveSpawner_Riders");
        if (spawner == null)
        {
            Debug.LogError("CombatSetup: W3 has no 'WaveSpawner_Riders'. Run Part 7 > Run Everything first.");
            return;
        }

        var so = new SerializedObject(spawner);
        SerializedProperty waves = so.FindProperty("waves");
        if (waves.arraySize > 0)
        {
            Debug.Log("CombatSetup: W3 already has waves; left unchanged.");
            return;
        }

        // Three runaway-rider waves, getting a little harder each time.
        EnemyBase[][] plan =
        {
            new[] { p.wobbler, p.wobbler },
            new[] { p.wobbler, p.pieSlinger },
            new[] { p.wobbler, p.wobbler, p.pieSlinger },
        };
        waves.arraySize = plan.Length;
        for (int i = 0; i < plan.Length; i++)
        {
            SerializedProperty wave = waves.GetArrayElementAtIndex(i);
            wave.FindPropertyRelative("delayBefore").floatValue = i == 0 ? 0.5f : 1.5f;
            wave.FindPropertyRelative("spawnInterval").floatValue = 0.4f;
            SerializedProperty enemies = wave.FindPropertyRelative("enemies");
            enemies.arraySize = plan[i].Length;
            for (int j = 0; j < plan[i].Length; j++)
                enemies.GetArrayElementAtIndex(j).objectReferenceValue = plan[i][j];
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(opened.Value);
        Debug.Log("CombatSetup: W3 riders waves filled in.");
    }

    // ------------------------------------------------------------------ 5. Screen effects in every level

    private static void AddScreenEffectsToLevels()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { LevelsFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (Object.FindAnyObjectByType<CameraRig>() == null || Object.FindAnyObjectByType<WashedOutView>() != null)
                continue; // Not a playable level (e.g. End of Demo), or already done.

            PrefabUtility.InstantiatePrefab(BuildPrefabs().screenEffects, scene);

            // Volumes only show when the camera renders post-processing.
            foreach (Camera cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            EditorSceneManager.SaveScene(scene);
        }
    }

    private const string PipControllerPath = "Assets/Art/Pip Animation/Walk/Sprite.controller";

    /// <summary>
    /// The artist's controller has Idle, Walk and Mallet states but nothing that switches between
    /// them. Adds a float "Speed" and Idle <-> Walk transitions (instant, no exit time); Idle is
    /// the starting state. Leaves everything else (e.g. Mallet) alone. Skipped once done.
    /// </summary>
    private static void WirePipAnimations()
    {
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(PipControllerPath);
        if (controller == null)
        {
            Debug.LogWarning($"CombatSetup: {PipControllerPath} not found; Pip's animations not wired.");
            return;
        }
        foreach (AnimatorControllerParameter p in controller.parameters)
            if (p.name == "Speed")
                return; // Already wired (maybe edited by hand since).

        UnityEditor.Animations.AnimatorStateMachine machine = controller.layers[0].stateMachine;
        UnityEditor.Animations.AnimatorState idle = null, walk = null;
        foreach (UnityEditor.Animations.ChildAnimatorState child in machine.states)
        {
            if (child.state.name == "Pip_Idle") idle = child.state;
            if (child.state.name == "Pip_Walk") walk = child.state;
        }
        if (idle == null || walk == null)
        {
            Debug.LogWarning("CombatSetup: Pip's controller has no Pip_Idle / Pip_Walk state; not wired.");
            return;
        }

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        machine.defaultState = idle;
        Transition(idle, walk, UnityEditor.Animations.AnimatorConditionMode.Greater);
        Transition(walk, idle, UnityEditor.Animations.AnimatorConditionMode.Less);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("CombatSetup: Pip's Idle/Walk animations now follow her Speed.");
    }

    /// <summary>
    /// Mallet: a "Swing" trigger jumps from ANY state into Pip_Mallet (instantly, and it can restart
    /// itself for quick swings); when the clip has played once it goes back to Pip_Idle (Walk takes
    /// over from there if she's moving). "SwingSpeed" scales the clip to the swing's length.
    /// Skipped once the controller has a "Swing" parameter.
    /// </summary>
    private static void WireMalletAnimation()
    {
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(PipControllerPath);
        if (controller == null)
            return;
        foreach (AnimatorControllerParameter p in controller.parameters)
            if (p.name == "Swing")
                return; // Already wired.

        UnityEditor.Animations.AnimatorStateMachine machine = controller.layers[0].stateMachine;
        UnityEditor.Animations.AnimatorState idle = null, mallet = null;
        foreach (UnityEditor.Animations.ChildAnimatorState child in machine.states)
        {
            if (child.state.name == "Pip_Idle") idle = child.state;
            if (child.state.name == "Pip_Mallet") mallet = child.state;
        }
        if (idle == null || mallet == null)
        {
            Debug.LogWarning("CombatSetup: Pip's controller has no Pip_Idle / Pip_Mallet state; mallet not wired.");
            return;
        }

        controller.AddParameter("Swing", AnimatorControllerParameterType.Trigger);
        controller.AddParameter(new AnimatorControllerParameter
        {
            name = "SwingSpeed",
            type = AnimatorControllerParameterType.Float,
            defaultFloat = 1f,
        });
        mallet.speedParameter = "SwingSpeed";
        mallet.speedParameterActive = true;

        UnityEditor.Animations.AnimatorStateTransition start = machine.AddAnyStateTransition(mallet);
        start.hasExitTime = false;
        start.duration = 0f;
        start.canTransitionToSelf = true; // A new swing restarts the animation.
        start.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Swing");

        UnityEditor.Animations.AnimatorStateTransition end = mallet.AddTransition(idle);
        end.hasExitTime = true;  // When the swing animation has played once...
        end.exitTime = 1f;
        end.duration = 0f;       // ...snap back to Idle (Walk follows if she's moving).

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("CombatSetup: Pip's Mallet animation now plays on each swing.");
    }

    private const string JumpClipPath = "Assets/Art/Pip Animation/Jump/Pip_Jump.anim";
    private const string JumpPlaceholderFrame = "Assets/Art/Pip Animation/Walk/Timeline 1_0003.png";

    /// <summary>
    /// Jump: a "Grounded" bool (PipAnimator sets it). In the air (jumping or falling) Idle and Walk
    /// switch to Pip_Jump; landing goes back to Idle (Walk follows if she's moving). Mallet swings
    /// still work in the air (from any state). Skipped once the controller has "Grounded".
    ///
    /// Pip_Jump.anim is a PLACEHOLDER (one mid-stride Walk frame) until the artist draws a jump:
    /// replace the clip's frames (or the whole file, keeping its name) and nothing else changes.
    /// </summary>
    private static void WireJumpAnimation()
    {
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(PipControllerPath);
        if (controller == null)
            return;
        foreach (AnimatorControllerParameter p in controller.parameters)
            if (p.name == "Grounded")
                return; // Already wired.

        UnityEditor.Animations.AnimatorStateMachine machine = controller.layers[0].stateMachine;
        UnityEditor.Animations.AnimatorState idle = null, walk = null;
        foreach (UnityEditor.Animations.ChildAnimatorState child in machine.states)
        {
            if (child.state.name == "Pip_Idle") idle = child.state;
            if (child.state.name == "Pip_Walk") walk = child.state;
        }
        if (idle == null || walk == null)
        {
            Debug.LogWarning("CombatSetup: Pip's controller has no Pip_Idle / Pip_Walk state; jump not wired.");
            return;
        }

        AnimationClip clip = PlaceholderJumpClip();
        controller.AddParameter(new AnimatorControllerParameter
        {
            name = "Grounded",
            type = AnimatorControllerParameterType.Bool,
            defaultBool = true,
        });
        UnityEditor.Animations.AnimatorState jump = machine.AddState("Pip_Jump");
        jump.motion = clip;

        foreach (UnityEditor.Animations.AnimatorState from in new[] { idle, walk })
        {
            UnityEditor.Animations.AnimatorStateTransition t = from.AddTransition(jump);
            t.hasExitTime = false;
            t.duration = 0f;
            t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0f, "Grounded");
        }
        UnityEditor.Animations.AnimatorStateTransition land = jump.AddTransition(idle);
        land.hasExitTime = false;
        land.duration = 0f;
        land.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Grounded");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("CombatSetup: Pip's Jump animation (placeholder) now plays in the air.");
    }

    /// <summary>Pip_Jump.anim: loads it, or creates the placeholder (one Walk frame, looping).</summary>
    private static AnimationClip PlaceholderJumpClip() =>
        PlaceholderClip(JumpClipPath, "Assets/Art/Pip Animation/Jump", "Pip_Jump", JumpPlaceholderFrame);

    private const string DashClipPath = "Assets/Art/Pip Animation/Dash/Pip_Dash.anim";
    private const string DashPlaceholderFrame = "Assets/Art/Pip Animation/Walk/Timeline 1_0006.png";

    /// <summary>
    /// Dash: a "Dashing" bool (PipAnimator sets it). While Pip dashes, ANY state switches to Pip_Dash
    /// (on the ground or in the air); when the dash ends it goes to Idle, and Jump/Walk take over
    /// from there if she is in the air or moving. Skipped once the controller has "Dashing".
    ///
    /// Pip_Dash.anim is a PLACEHOLDER (one Walk frame) until the artist draws a dash: replace the
    /// clip's frames (or the whole file, keeping its name) and nothing else changes.
    /// </summary>
    private static void WireDashAnimation()
    {
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(PipControllerPath);
        if (controller == null)
            return;
        foreach (AnimatorControllerParameter p in controller.parameters)
            if (p.name == "Dashing")
                return; // Already wired.

        UnityEditor.Animations.AnimatorStateMachine machine = controller.layers[0].stateMachine;
        UnityEditor.Animations.AnimatorState idle = null;
        foreach (UnityEditor.Animations.ChildAnimatorState child in machine.states)
            if (child.state.name == "Pip_Idle")
                idle = child.state;
        if (idle == null)
        {
            Debug.LogWarning("CombatSetup: Pip's controller has no Pip_Idle state; dash not wired.");
            return;
        }

        AnimationClip clip = PlaceholderClip(DashClipPath, "Assets/Art/Pip Animation/Dash", "Pip_Dash", DashPlaceholderFrame);
        controller.AddParameter("Dashing", AnimatorControllerParameterType.Bool);
        UnityEditor.Animations.AnimatorState dash = machine.AddState("Pip_Dash");
        dash.motion = clip;

        UnityEditor.Animations.AnimatorStateTransition start = machine.AddAnyStateTransition(dash);
        start.hasExitTime = false;
        start.duration = 0f;
        start.canTransitionToSelf = false; // Stay in Dash for the whole dash (no restart every frame).
        start.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Dashing");

        UnityEditor.Animations.AnimatorStateTransition end = dash.AddTransition(idle);
        end.hasExitTime = false;
        end.duration = 0f;
        end.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0f, "Dashing");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("CombatSetup: Pip's Dash animation (placeholder) now plays while she dashes.");
    }

    private const string ChortleControllerPath = "Assets/Art/Chortle Animation/Idle/Sprite.controller";

    /// <summary>
    /// The artist added Chortle's Animator (Idle) to the Chortle in ChortleArena only, as a scene
    /// change on top of the prefab. This moves it INTO the prefab (Unity's "Apply to Prefab"), so
    /// every Chortle animates, and the arena keeps working exactly as before. If there's no scene
    /// Animator to move, it adds one to the prefab's Sprite. Skipped once the prefab has one.
    /// </summary>
    private static void PutChortleAnimatorInPrefab()
    {
        string prefabPath = $"{PrefabFolder}/Boss/Chortle.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null || prefab.GetComponentInChildren<Animator>(true) != null)
            return; // No prefab, or already done.

        // 1. Move the arena's Animator into the prefab, if it's there.
        Scene? opened = OpenLevel("ChortleArena");
        if (opened != null)
        {
            ChortleBoss chortle = Object.FindAnyObjectByType<ChortleBoss>(FindObjectsInactive.Include);
            Animator added = chortle != null ? chortle.GetComponentInChildren<Animator>(true) : null;
            if (added != null && PrefabUtility.IsAddedComponentOverride(added))
            {
                PrefabUtility.ApplyAddedComponent(added, prefabPath, InteractionMode.AutomatedAction);
                EditorSceneManager.SaveScene(opened.Value);
                Debug.Log("CombatSetup: moved Chortle's Animator from ChortleArena into the Chortle prefab.");
                return;
            }
        }

        // 2. Otherwise add a fresh one to the prefab's Sprite.
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ChortleControllerPath);
        if (controller == null)
        {
            Debug.LogWarning($"CombatSetup: {ChortleControllerPath} not found; Chortle has no animation.");
            return;
        }
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Transform sprite = root.transform.Find("Sprite");
            if (sprite != null && sprite.GetComponent<Animator>() == null)
            {
                sprite.gameObject.AddComponent<Animator>().runtimeAnimatorController = controller;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log("CombatSetup: added Chortle's Idle Animator to the Chortle prefab.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>Loads a clip, or creates a looping PLACEHOLDER clip that shows one sprite.</summary>
    private static AnimationClip PlaceholderClip(string path, string folder, string name, string framePath)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null)
            return clip;

        Part1Setup.EnsureFolder(folder);
        clip = new AnimationClip { name = name, frameRate = 12f };
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // The Animator sits on the Sprite object itself, so the path is "" (this object).
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        Sprite frame = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
        AnimationUtility.SetObjectReferenceCurve(clip, binding, new[]
        {
            new ObjectReferenceKeyframe { time = 0f, value = frame },
        });
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void Transition(UnityEditor.Animations.AnimatorState from, UnityEditor.Animations.AnimatorState to,
                                   UnityEditor.Animations.AnimatorConditionMode mode)
    {
        UnityEditor.Animations.AnimatorStateTransition t = from.AddTransition(to);
        t.hasExitTime = false;  // Switch the moment Pip starts/stops, not at the end of the loop.
        t.duration = 0f;        // Pixel art: no blending between frames.
        t.AddCondition(mode, 0.1f, "Speed");
    }

    private static void RememberW3Waves()
    {
        Scene? opened = OpenLevel("Wonderwick_W3_Coaster");
        if (opened == null)
            return;
        WaveSpawner spawner = P6.FindNamed<WaveSpawner>("WaveSpawner_Riders");
        if (spawner == null)
            return;
        var so = new SerializedObject(spawner);
        if (!string.IsNullOrEmpty(so.FindProperty("saveId").stringValue))
            return;
        so.FindProperty("saveId").stringValue = "w3_riders";
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(opened.Value);
        Debug.Log("CombatSetup: W3 riders remember cleared waves.");
    }

    // ------------------------------------------------------------------ 7. Prologue checkpoints

    private const string CheckpointRootName = "Checkpoints (Combat)";

    /// <summary>A checkpoint after each prologue challenge, so dying doesn't send Pip far back.</summary>
    private static void AddPrologueCheckpoints()
    {
        AddCheckpoints("Tumbleton_PipHouse", ("PipHouse_Mirror", 16f));        // After P1's mirror.
        AddCheckpoints("Tumbleton",
            ("Tumbleton_AfterHonkStreet", 44.5f),                             // After P2 (the far kerb).
            ("Tumbleton_AfterBalloonWall", 59f),                              // After P3 (behind the balloon wall).
            ("Tumbleton_AfterReflexCheck", 88f));                             // After P4 (right of the spikes).
    }

    private static void AddCheckpoints(string level, params (string id, float x)[] checkpoints)
    {
        Scene? opened = OpenLevel(level);
        if (opened == null)
            return;
        if (GameObject.Find(CheckpointRootName) != null)
            return; // Already added.

        Sprite square = Part1Setup.GetOrCreatePlaceholderSprite();
        var root = new GameObject(CheckpointRootName).transform;
        foreach ((string id, float x) in checkpoints)
            Part1Setup.AddCheckpoint(id, new Vector2(x, 0f), square).transform.SetParent(root);

        EditorSceneManager.SaveScene(opened.Value);
        Debug.Log($"CombatSetup: {checkpoints.Length} checkpoint(s) added to {level}.");
    }

    // ------------------------------------------------------------------ Helpers

    private static Scene? OpenLevel(string name)
    {
        string path = $"{LevelsFolder}/{name}.unity";
        if (!File.Exists(path))
        {
            Debug.LogError($"CombatSetup: {path} is missing. Run Part 5 > Run Everything first.");
            return null;
        }
        return EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }

    /// <summary>Sets private [SerializeField] fields the way the Inspector does. Logs typos.</summary>
    private static void Set(Object target, params (string name, object value)[] fields)
    {
        var so = new SerializedObject(target);
        foreach ((string name, object value) in fields)
        {
            SerializedProperty prop = so.FindProperty(name);
            if (prop == null)
            {
                Debug.LogError($"CombatSetup: {target.GetType().Name} has no field '{name}'.");
                continue;
            }

            switch (value)
            {
                case null: prop.objectReferenceValue = null; break;
                case Object[] array:
                    prop.arraySize = array.Length;
                    for (int i = 0; i < array.Length; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
                    break;
                case Object obj: prop.objectReferenceValue = obj; break;
                case int i: prop.intValue = i; break;          // Also LayerMasks (bit masks).
                case float f: prop.floatValue = f; break;
                case bool b: prop.boolValue = b; break;
                case string s: prop.stringValue = s; break;
                case Vector2 v: prop.vector2Value = v; break;
                default: Debug.LogError($"CombatSetup: can't set {name} to a {value.GetType().Name}."); break;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetDrops(EnemyBase enemy, params (Pickup prefab, float chance)[] drops)
    {
        var so = new SerializedObject(enemy);
        SerializedProperty list = so.FindProperty("drops");
        list.arraySize = drops.Length;
        for (int i = 0; i < drops.Length; i++)
        {
            SerializedProperty drop = list.GetArrayElementAtIndex(i);
            drop.FindPropertyRelative("prefab").objectReferenceValue = drops[i].prefab;
            drop.FindPropertyRelative("chance").floatValue = drops[i].chance;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
