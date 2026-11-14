using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using B = GameSystemsPrefabBuilder;
using P6 = Part6Setup;

/// <summary>
/// EDITOR-ONLY tool for Part 7: the four Wonderwick tasks.
///
/// Menu: Tools > Clown Who Cried > Part 7 > Run Everything
///   1. Task data (only CREATED if missing; your text edits are never overwritten):
///        Assets/Resources/Tasks/Wonderwick   Task_W1_PunchlineDelivery ... Task_W4_Audition
///        Assets/Data/Dialogue/Wonderwick     speakers and conversations (placeholder lines)
///   2. Builds each task into its room, inside one object called "Wonderwick (Part 7)":
///        W1 Delivery     Postmaster Pun, 5 boxes, 5 stalls (2 up on ledges), 2 timed jack-in-the-box lids
///        W2 Mirror Maze  Madame Mirabel (gives Blue Sight), 2 walls, 2 pairs of real mirrors,
///                        fake mirrors (crack shows in Blue Sight), a pit with a fake floor and a
///                        secret Blue-Sight-only platform, the exit
///        W3 Coaster      Coaster Carl, a Ferris wheel with 4 gondolas, 2 ledges, 4 switches, the
///                        teammate's WaveSpawner (no waves yet) + bridge; reward Juggle Shot
///        W4 Audition     Director Guffaw, the stage, the rhythm mini-game UI
///      and the hub's Laugh Tower gate now needs W1-W4.
///      A room is only changed if it has no "Wonderwick (Part 7)" object yet.
///   3. Assets/Scenes/Tests/WonderwickTest + driver.
/// </summary>
public static class Part7Setup
{
    private const string RootName = "Wonderwick (Part 7)";
    private const string TaskFolder = "Assets/Resources/Tasks/Wonderwick";
    private const string DialogueFolder = "Assets/Data/Dialogue/Wonderwick";
    private const string TestScenePath = Part1Setup.TestScenesFolder + "/WonderwickTest.unity";

    [MenuItem("Tools/Clown Who Cried/Part 7/Run Everything")]
    public static void RunEverything()
    {
        GameSystemsPrefabBuilder.Build();
        BuildData();
        BuildW1();
        BuildW2();
        BuildW3();
        BuildW4();
        UpdateHubGate();
        BuildTestScene();
        Debug.Log("Part7Setup: done.");
    }

    public static void RunTestsFromCommandLine()
    {
        RunEverything();
        if (Part1Setup.CheckMissingScripts() > 0)
        {
            EditorApplication.Exit(1);
            return;
        }
        EditorSceneManager.OpenScene(TestScenePath);
        EditorApplication.EnterPlaymode();
    }

    // ------------------------------------------------------------------ Data

    private class Data
    {
        public TaskData w1, w2, w3, w4;
        public DialogueData[] pun, mirabel, carl, guffaw; // ask, active, done
        public DialogueData fakeMirror, auditionPass, auditionFail;
    }

    private static DialogueData D(string name) => P6.Asset<DialogueData>(DialogueFolder, name);

    private static Data Load() => new Data
    {
        w1 = P6.Asset<TaskData>(TaskFolder, "Task_W1_PunchlineDelivery"),
        w2 = P6.Asset<TaskData>(TaskFolder, "Task_W2_MirrorMaze"),
        w3 = P6.Asset<TaskData>(TaskFolder, "Task_W3_RunawayCoaster"),
        w4 = P6.Asset<TaskData>(TaskFolder, "Task_W4_Audition"),
        pun = new[] { D("Dialogue_W1_Pun_Ask"), D("Dialogue_W1_Pun_Active"), D("Dialogue_W1_Pun_Done") },
        mirabel = new[] { D("Dialogue_W2_Mirabel_Ask"), D("Dialogue_W2_Mirabel_Active"), D("Dialogue_W2_Mirabel_Done") },
        carl = new[] { D("Dialogue_W3_Carl_Ask"), D("Dialogue_W3_Carl_Active"), D("Dialogue_W3_Carl_Done") },
        guffaw = new[] { D("Dialogue_W4_Guffaw_Ask"), D("Dialogue_W4_Guffaw_Active"), D("Dialogue_W4_Guffaw_Done") },
        fakeMirror = D("Dialogue_W2_FakeMirror"),
        auditionPass = D("Dialogue_W4_AuditionPass"),
        auditionFail = D("Dialogue_W4_AuditionFail"),
    };

    private static void BuildData()
    {
        Part1Setup.EnsureFolder(TaskFolder);
        Part1Setup.EnsureFolder(DialogueFolder);

        DialogueSpeaker pun = Speaker("Speaker_PostmasterPun", "Postmaster Pun", new Color(0.7f, 0.9f, 0.6f));
        DialogueSpeaker mirabel = Speaker("Speaker_MadameMirabel", "Madame Mirabel", new Color(0.7f, 0.7f, 1f));
        DialogueSpeaker carl = Speaker("Speaker_CoasterCarl", "Coaster Carl", new Color(1f, 0.7f, 0.4f));
        DialogueSpeaker guffaw = Speaker("Speaker_DirectorGuffaw", "Director Guffaw", new Color(1f, 0.85f, 0.4f));

        // ---- W1 Punchline Delivery
        Talk("Dialogue_W1_Pun_Ask",
            (pun, "Ahh! Five boxes of punchlines to deliver and my legs are all jelly!"),
            (pun, "Carry each box to a stall before time runs out. No juggling while you carry!"),
            (pun, "The jack-in-the-boxes pop up now and then. Hop on their lids to reach the high stalls."));
        Talk("Dialogue_W1_Pun_Active", (pun, "Hurry, hurry! Jokes go stale fast!"));
        Talk("Dialogue_W1_Pun_Done", (pun, "Every joke delivered fresh. You're a lifesaver!"));

        // ---- W2 Crooked Mirror Maze
        Talk("Dialogue_W2_Mirabel_Ask",
            (mirabel, "Welcome to my Crooked Mirror Maze, little clown."),
            (mirabel, "Take this gift: Blue Sight. It shows what is real and what only pretends to be."),
            (mirabel, "Real mirrors take you places. Fake ones just stare back."));
        Talk("Dialogue_W2_Mirabel_Active", (mirabel, "Use your Blue Sight when something feels... off."));
        Talk("Dialogue_W2_Mirabel_Done", (mirabel, "You see clearly now. Keep that gift close."));
        Talk("Dialogue_W2_FakeMirror", (null, "(It's only a painting of a mirror.)"));

        // ---- W3 Runaway Coaster
        Talk("Dialogue_W3_Carl_Ask",
            (carl, "The coaster's gone wild! Flip all four switches to stop it."),
            (carl, "Ride the Ferris wheel to reach the high ones. Then deal with whatever jumps out!"));
        Talk("Dialogue_W3_Carl_Active", (carl, "Four switches! Hit them or pull them, I'm not fussy!"));
        Talk("Dialogue_W3_Carl_Done",
            (carl, "You tamed the coaster! Here, let me teach you my Juggle Shot."),
            (null, "(Pip can now throw up to three Star Coins at once.)"));

        // ---- W4 Audition
        Talk("Dialogue_W4_Guffaw_Ask",
            (guffaw, "Auditions for the Laugh Tower show! Hit the beat: five out of eight."),
            (guffaw, "Step onto the stage when you're ready."));
        Talk("Dialogue_W4_Guffaw_Active", (guffaw, "The stage awaits! Press on the beat."));
        Talk("Dialogue_W4_Guffaw_Done", (guffaw, "Bravo! The doors of the Laugh Tower are open to you."));
        Talk("Dialogue_W4_AuditionPass", (guffaw, "What timing! You're in the show!"));
        Talk("Dialogue_W4_AuditionFail", (guffaw, "Not quite! Catch your breath and try again."));

        // ---- Tasks
        Task("Task_W1_PunchlineDelivery", "w1_punchline_delivery", "Punchline Delivery",
            "Postmaster Pun needs five boxes of jokes delivered, fast.", 1, null, null,
            Signal("Objective_W1_Deliver", "w1_delivered", 5, "Deliver the punchlines", 120f));
        Task("Task_W2_MirrorMaze", "w2_mirror_maze", "Crooked Mirror Maze",
            "Madame Mirabel's maze is full of tricks. Blue Sight shows the truth.", 2,
            new List<Ability> { Ability.BlueSight }, null,
            Signal("Objective_W2_Exit", "w2_exit", 1, "Find the way through the maze", 0f));
        Task("Task_W3_RunawayCoaster", "w3_runaway_coaster", "Runaway Coaster",
            "The coaster is out of control! Stop it before it's too late.", 3,
            null, new List<Ability> { Ability.JuggleShot },
            Signal("Objective_W3_Switches", "w3_switch", 4, "Flip the coaster switches", 0f),
            Signal("Objective_W3_Waves", "w3_waves", 1, "Deal with the runaway riders", 0f));
        Task("Task_W4_Audition", "w4_audition", "The Audition",
            "Director Guffaw is casting for the Laugh Tower show.", 4, null, null,
            Signal("Objective_W4_Pass", "w4_passed", 1, "Pass the audition (5 of 8 beats)", 0f));

        AssetDatabase.SaveAssets();

        DialogueSpeaker Speaker(string asset, string displayName, Color color) =>
            P6.CreateIfMissing<DialogueSpeaker>(DialogueFolder, asset, s => { s.displayName = displayName; s.nameColor = color; });

        void Talk(string asset, params (DialogueSpeaker who, string text)[] lines) =>
            P6.CreateIfMissing<DialogueData>(DialogueFolder, asset, d =>
            {
                foreach ((DialogueSpeaker who, string text) in lines)
                    d.lines.Add(new DialogueData.Line { speaker = who, text = text });
            });

        SignalObjective Signal(string asset, string key, int count, string description, float timeLimit) =>
            P6.CreateIfMissing<SignalObjective>(TaskFolder, asset, o =>
            {
                o.signalKey = key;
                o.requiredCount = count;
                o.description = description;
                o.timeLimit = timeLimit;
            });

        void Task(string asset, string id, string title, string description, int order,
            List<Ability> onStart, List<Ability> onComplete, params TaskObjective[] objectives) =>
            P6.CreateIfMissing<TaskData>(TaskFolder, asset, t =>
            {
                t.id = id;
                t.title = title;
                t.description = description;
                t.city = TaskData.City.Wonderwick;
                t.boardOrder = order;
                t.objectives = new List<TaskObjective>(objectives);
                if (onStart != null) t.unlockOnStart = onStart;
                if (onComplete != null) t.unlockOnComplete = onComplete;
            });
    }

    // ------------------------------------------------------------------ Opening rooms

    /// <summary>Opens a level; null (unchanged) if it already has the Part 7 root.</summary>
    private static Scene? Open(string scene, out Transform root)
    {
        root = null;
        string path = $"{Part5Setup.LevelsFolder}/{scene}.unity";
        if (!System.IO.File.Exists(path))
        {
            Debug.LogError($"Part7Setup: {path} is missing. Run Part 5 > Run Everything first.");
            return null;
        }
        Scene opened = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        if (GameObject.Find(RootName) != null)
        {
            Debug.Log($"Part7Setup: {scene} already has '{RootName}'; left unchanged.");
            return null;
        }
        root = new GameObject(RootName).transform;
        return opened;
    }

    private static Color Ground = new Color(0.42f, 0.3f, 0.22f);

    /// <summary>A solid block on the Ground layer (ledges, maze walls, lid tops).</summary>
    private static GameObject Solid(Transform parent, string name, Vector2 center, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(BoxCollider2D));
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.layer = Part1Setup.RequireLayer("Ground");
        go.GetComponent<BoxCollider2D>().size = size;
        P6.Box(go.transform, "Sprite", Vector2.zero, size, color, 2);
        return go;
    }

    // ------------------------------------------------------------------ W1 Punchline Delivery

    private static void BuildW1()
    {
        Scene? scene = Open("Wonderwick_W1_Delivery", out Transform root);
        if (scene == null)
            return;
        Data d = Load();
        P6.TakeMarker("W1_TaskArea (Part 7)", 30f);
        P6.Npc(root, "NPC_PostmasterPun", "postmaster_pun", 10f, new Color(0.5f, 0.8f, 0.4f), d.pun[0], d.pun[1], d.pun[2], d.w1, "");

        // 5 boxes in a row (the "depot").
        for (int i = 0; i < 5; i++)
        {
            var box = new GameObject($"Box_{i + 1}", typeof(BoxCollider2D));
            box.transform.SetParent(root);
            box.transform.position = new Vector3(13f + i, 0f, 0f);
            var range = box.GetComponent<BoxCollider2D>();
            range.isTrigger = true;
            range.size = new Vector2(1f, 1.5f);
            range.offset = new Vector2(0f, 0.75f);
            P6.Box(box.transform, "Sprite", new Vector2(0f, 0.4f), new Vector2(0.8f, 0.8f), new Color(0.75f, 0.55f, 0.3f), 7);
            var carry = box.AddComponent<CarryableBox>();
            var so = new SerializedObject(carry);
            so.FindProperty("onlyWhileTaskActive").objectReferenceValue = d.w1;
            so.FindProperty("prompt").objectReferenceValue = P6.Prompt(box.transform, 1.3f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 5 stalls; 2 and 4 sit on ledges 3 tiles up, reached from a jack-in-the-box lid.
        float[] stallX = { 24f, 31f, 38f, 45f, 52f };
        for (int i = 0; i < stallX.Length; i++)
        {
            bool high = i == 1 || i == 3;
            float y = 0f;
            if (high)
            {
                y = 3f;
                Solid(root, $"Ledge_{i + 1}", new Vector2(stallX[i], 2.75f), new Vector2(3f, 0.5f), Ground);
                Lid(root, $"JackLid_{i + 1}", stallX[i] - 3.5f, offset: i * 0.9f);
            }
            Stall(root, $"Stall_{i + 1}", new Vector2(stallX[i], y), d.w1);
        }

        EditorSceneManager.SaveScene(scene.Value);
    }

    private static void Stall(Transform root, string name, Vector2 feet, TaskData task)
    {
        var stall = new GameObject(name, typeof(BoxCollider2D));
        stall.transform.SetParent(root);
        stall.transform.position = feet;
        var box = stall.GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2f, 2f);
        box.offset = new Vector2(0f, 1f);
        P6.Box(stall.transform, "Sprite", new Vector2(0f, 0.75f), new Vector2(2f, 1.5f), new Color(0.9f, 0.5f, 0.7f), 3);
        GameObject flag = P6.Box(stall.transform, "ServedFlag", new Vector2(0.8f, 2f), new Vector2(0.3f, 0.6f), new Color(0.3f, 0.9f, 0.4f), 4);
        var component = stall.AddComponent<DeliveryStall>();
        var so = new SerializedObject(component);
        so.FindProperty("task").objectReferenceValue = task;
        so.FindProperty("servedMarker").objectReferenceValue = flag;
        so.ApplyModifiedPropertiesWithoutUndo();
        flag.SetActive(false);
    }

    private static void Lid(Transform root, string name, float x, float offset)
    {
        var jack = new GameObject(name);
        jack.transform.SetParent(root);
        jack.transform.position = new Vector3(x, 0f, 0f);
        GameObject closed = P6.Box(jack.transform, "ClosedLook", new Vector2(0f, 0.5f), new Vector2(1.5f, 1f), new Color(0.9f, 0.3f, 0.3f), 2);
        var open = new GameObject("OpenLook");
        open.transform.SetParent(jack.transform, false);
        P6.Box(open.transform, "Box", new Vector2(0f, 0.5f), new Vector2(1.5f, 1f), new Color(0.9f, 0.3f, 0.3f), 2);
        P6.Box(open.transform, "Spring", new Vector2(0f, 1.25f), new Vector2(0.2f, 0.5f), new Color(0.8f, 0.8f, 0.8f), 2);
        Solid(open.transform, "Lid", new Vector2(x, 1.6f), new Vector2(2f, 0.3f), new Color(1f, 0.85f, 0.3f));
        var lid = jack.AddComponent<TimedLid>();
        var so = new SerializedObject(lid);
        so.FindProperty("openLook").objectReferenceValue = open;
        so.FindProperty("closedLook").objectReferenceValue = closed;
        so.FindProperty("offset").floatValue = offset;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------ W2 Crooked Mirror Maze

    private static void BuildW2()
    {
        Scene? scene = Open("Wonderwick_W2_MirrorMaze", out Transform root);
        if (scene == null)
            return;
        Data d = Load();
        P6.TakeMarker("W2_TaskArea (Part 7)", 30f);
        P6.Npc(root, "NPC_MadameMirabel", "madame_mirabel", 10f, new Color(0.55f, 0.5f, 0.9f), d.mirabel[0], d.mirabel[1], d.mirabel[2], d.w2, "");

        Color wall = new Color(0.45f, 0.4f, 0.6f);
        Solid(root, "MazeWall_1", new Vector2(20f, 9f), new Vector2(1f, 18f), wall);
        Solid(root, "MazeWall_2", new Vector2(40f, 9f), new Vector2(1f, 18f), wall);

        FakeMirror(root, "FakeMirror_1", 14f, d.fakeMirror);
        MirrorPair(root, "Mirror_A", 17f, "Mirror_B", 23f);

        // A pit with a fake floor over it, and a secret platform only Blue Sight shows.
        Tilemap ground = GameObject.Find("Ground").GetComponent<Tilemap>();
        for (int x = 28; x <= 32; x++)
            for (int y = -3; y <= -1; y++)
                ground.SetTile(new Vector3Int(x, y, 0), null);
        var pit = new GameObject("Pit", typeof(BoxCollider2D));
        pit.transform.SetParent(root);
        pit.transform.position = new Vector3(30.5f, -5f, 0f);
        pit.tag = "Pit";
        var pitBox = pit.GetComponent<BoxCollider2D>();
        pitBox.isTrigger = true;
        pitBox.size = new Vector2(5f, 2f);

        var fake = new GameObject("FakeFloor");
        fake.transform.SetParent(root);
        fake.transform.position = new Vector3(30.5f, -0.5f, 0f);
        P6.Box(fake.transform, "Sprite", Vector2.zero, new Vector2(5f, 1f), Ground, 2);
        fake.AddComponent<FakeFloor>();

        GameObject secret = Solid(root, "SecretPlatform", new Vector2(30.5f, 1.25f), new Vector2(3f, 0.5f), new Color(0.5f, 0.85f, 1f));
        secret.AddComponent<BlueSightOnly>(); // My teammate's script: only there in Blue Sight.

        FakeMirror(root, "FakeMirror_2", 34f, d.fakeMirror);
        MirrorPair(root, "Mirror_C", 37f, "Mirror_D", 43f);

        P6.Area(root, "Area_W2_Exit", 52f, 4f, "w2_exit");
        EditorSceneManager.SaveScene(scene.Value);
    }

    private static MirrorTeleporter Mirror(Transform root, string name, float x)
    {
        var mirror = new GameObject(name, typeof(BoxCollider2D));
        mirror.transform.SetParent(root);
        mirror.transform.position = new Vector3(x, 0f, 0f);
        var range = mirror.GetComponent<BoxCollider2D>();
        range.isTrigger = true;
        range.size = new Vector2(1.6f, 2.5f);
        range.offset = new Vector2(0f, 1.25f);
        P6.Box(mirror.transform, "Sprite", new Vector2(0f, 1.25f), new Vector2(1f, 2f), new Color(0.7f, 0.9f, 1f), 1);
        var teleporter = mirror.AddComponent<MirrorTeleporter>();
        B.SetField(teleporter, "prompt", P6.Prompt(mirror.transform, 2.8f));
        return teleporter;
    }

    private static void MirrorPair(Transform root, string nameA, float xA, string nameB, float xB)
    {
        MirrorTeleporter a = Mirror(root, nameA, xA), b = Mirror(root, nameB, xB);
        B.SetField(a, "exit", b);
        B.SetField(b, "exit", a);
    }

    /// <summary>Looks like a mirror; using it only says it's fake. A crack shows in Blue Sight.</summary>
    private static void FakeMirror(Transform root, string name, float x, DialogueData line)
    {
        var mirror = new GameObject(name, typeof(BoxCollider2D));
        mirror.transform.SetParent(root);
        mirror.transform.position = new Vector3(x, 0f, 0f);
        var range = mirror.GetComponent<BoxCollider2D>();
        range.isTrigger = true;
        range.size = new Vector2(1.6f, 2.5f);
        range.offset = new Vector2(0f, 1.25f);
        P6.Box(mirror.transform, "Sprite", new Vector2(0f, 1.25f), new Vector2(1f, 2f), new Color(0.7f, 0.9f, 1f), 1);
        var crack = new GameObject("Crack");
        crack.transform.SetParent(mirror.transform, false);
        P6.Box(crack.transform, "Sprite", new Vector2(0f, 1.25f), new Vector2(0.15f, 1.6f), new Color(0.15f, 0.15f, 0.3f), 2);
        crack.AddComponent<BlueSightOnly>(); // My teammate's script: visible only in Blue Sight.
        var inspect = mirror.AddComponent<Inspectable>();
        var so = new SerializedObject(inspect);
        so.FindProperty("dialogue").objectReferenceValue = line;
        so.FindProperty("prompt").objectReferenceValue = P6.Prompt(mirror.transform, 2.8f);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------ W3 Runaway Coaster

    private static void BuildW3()
    {
        Scene? scene = Open("Wonderwick_W3_Coaster", out Transform root);
        if (scene == null)
            return;
        Data d = Load();
        P6.TakeMarker("W3_TaskArea (Part 7)", 30f);
        P6.Npc(root, "NPC_CoasterCarl", "coaster_carl", 10f, new Color(0.95f, 0.6f, 0.3f), d.carl[0], d.carl[1], d.carl[2], d.w3, "");

        // Ferris wheel: centre (24, 6.5), radius 4.5, 4 one-way gondolas Pip can ride.
        var wheelGo = new GameObject("FerrisWheel");
        wheelGo.transform.SetParent(root);
        wheelGo.transform.position = new Vector3(24f, 6.5f, 0f);
        P6.Box(wheelGo.transform, "Hub", Vector2.zero, new Vector2(0.6f, 0.6f), new Color(0.6f, 0.6f, 0.6f), 0);
        var wheel = wheelGo.AddComponent<RotatingWheel>();
        var gondolas = new List<Rigidbody2D>();
        for (int i = 0; i < 4; i++)
        {
            var gondola = new GameObject($"Gondola_{i + 1}", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(PlatformEffector2D));
            gondola.transform.SetParent(root);
            gondola.layer = Part1Setup.RequireLayer("Ground");
            var body = gondola.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = gondola.GetComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 0.5f);
            col.usedByEffector = true; // One-way: Pip can jump up through it from below.
            P6.Box(gondola.transform, "Sprite", Vector2.zero, new Vector2(3f, 0.5f), new Color(0.9f, 0.4f, 0.5f), 3);
            gondola.AddComponent<PlatformCarrier>();
            gondolas.Add(body);
        }
        var so = new SerializedObject(wheel);
        SerializedProperty list = so.FindProperty("gondolas");
        list.arraySize = gondolas.Count;
        for (int i = 0; i < gondolas.Count; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = gondolas[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        // Two high ledges next to the wheel, each with a switch; two switches on the floor.
        Solid(root, "Ledge_Left", new Vector2(16f, 8.75f), new Vector2(3f, 0.5f), Ground);
        Solid(root, "Ledge_Right", new Vector2(32f, 8.75f), new Vector2(3f, 0.5f), Ground);
        Switch(root, "Switch_1", new Vector2(14f, 0f), "coaster_switch_1", d.w3);
        Switch(root, "Switch_2", new Vector2(16f, 9f), "coaster_switch_2", d.w3);
        Switch(root, "Switch_3", new Vector2(32f, 9f), "coaster_switch_3", d.w3);
        Switch(root, "Switch_4", new Vector2(36f, 0f), "coaster_switch_4", d.w3);

        // My teammate's WaveSpawner (no waves yet: they add enemy prefabs) + my bridge.
        var spawnerGo = new GameObject("WaveSpawner_Riders");
        spawnerGo.transform.SetParent(root);
        spawnerGo.transform.position = new Vector3(46f, 0f, 0f);
        var spawner = spawnerGo.AddComponent<WaveSpawner>();
        Transform p1 = new GameObject("SpawnPoint_1").transform, p2 = new GameObject("SpawnPoint_2").transform;
        p1.SetParent(spawnerGo.transform, false);
        p2.SetParent(spawnerGo.transform, false);
        p1.localPosition = new Vector3(-2f, 0f, 0f);
        p2.localPosition = new Vector3(4f, 0f, 0f);
        so = new SerializedObject(spawner);
        SerializedProperty points = so.FindProperty("spawnPoints");
        points.arraySize = 2;
        points.GetArrayElementAtIndex(0).objectReferenceValue = p1;
        points.GetArrayElementAtIndex(1).objectReferenceValue = p2;
        so.FindProperty("startOnPlayerEnter").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();

        var bridge = spawnerGo.AddComponent<WaveSpawnerBridge>();
        so = new SerializedObject(bridge);
        so.FindProperty("spawner").objectReferenceValue = spawner;
        so.FindProperty("task").objectReferenceValue = d.w3;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene.Value);
    }

    private static void Switch(Transform root, string name, Vector2 feet, string saveId, TaskData task)
    {
        var go = new GameObject(name, typeof(BoxCollider2D));
        go.transform.SetParent(root);
        go.transform.position = feet;
        go.layer = Part1Setup.RequireLayer("Enemy"); // Hit by Pip's coins and mallet.
        var range = go.GetComponent<BoxCollider2D>();
        range.isTrigger = true;
        range.size = new Vector2(1.5f, 1.5f);
        range.offset = new Vector2(0f, 0.75f);
        P6.Box(go.transform, "Base", new Vector2(0f, 0.15f), new Vector2(0.8f, 0.3f), new Color(0.4f, 0.4f, 0.45f), 4);
        GameObject lever = P6.Box(go.transform, "Sprite", new Vector2(0f, 0.6f), new Vector2(0.25f, 0.7f), Color.white, 5);
        var component = go.AddComponent<TaskSwitch>();
        var so = new SerializedObject(component);
        so.FindProperty("saveId").stringValue = saveId;
        so.FindProperty("onlyWhileTaskActive").objectReferenceValue = task;
        so.FindProperty("sprite").objectReferenceValue = lever.GetComponent<SpriteRenderer>();
        so.FindProperty("prompt").objectReferenceValue = P6.Prompt(go.transform, 1.5f);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------ W4 Audition

    private static void BuildW4()
    {
        Scene? scene = Open("Wonderwick_W4_Audition", out Transform root);
        if (scene == null)
            return;
        Data d = Load();
        P6.TakeMarker("W4_TaskArea (Part 7)", 30f);
        P6.Npc(root, "NPC_DirectorGuffaw", "director_guffaw", 10f, new Color(0.95f, 0.8f, 0.3f), d.guffaw[0], d.guffaw[1], d.guffaw[2], d.w4, "");

        // The rhythm game's UI (its own canvas in this level).
        GameObject canvas = B.CreateCanvas("RhythmCanvas", root, sortingOrder: 60);
        var game = canvas.AddComponent<RhythmMinigame>();
        Image panel = B.CreateImage("Panel", canvas.transform, new Color(0.1f, 0.08f, 0.15f, 0.95f));
        // Just below the middle: clear of the task tracker (top right) and the dialogue box (bottom).
        B.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -15f), new Vector2(320f, 70f));
        TMP_Text title = B.CreateText("Title", panel.transform, "AUDITION! Press on the beat", PlaceholderArt.FontSize, TextAlignmentOptions.Center);
        B.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(300f, 10f));
        Image track = B.CreateImage("Track", panel.transform, new Color(0.25f, 0.22f, 0.32f));
        B.Place(track.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(280f, 10f));
        Image zone = B.CreateImage("HitZone", track.transform, new Color(1f, 0.85f, 0.3f));
        B.Place(zone.rectTransform, new Vector2(0.85f, 0.5f), Vector2.zero, new Vector2(8f, 16f));
        Image noteImage = B.CreateImage("Note", track.transform, new Color(0.5f, 0.9f, 1f));
        noteImage.sprite = PlaceholderArt.StarSprite;
        noteImage.type = Image.Type.Simple;
        noteImage.rectTransform.anchorMin = noteImage.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        noteImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        noteImage.rectTransform.sizeDelta = new Vector2(8f, 8f);
        TMP_Text score = B.CreateText("Score", panel.transform, "", PlaceholderArt.FontSize, TextAlignmentOptions.Left);
        B.Place(score.rectTransform, new Vector2(0f, 0f), new Vector2(10f, 6f), new Vector2(200f, 10f));
        TMP_Text feedback = B.CreateText("Feedback", panel.transform, "", PlaceholderArt.FontSize, TextAlignmentOptions.Right);
        B.Place(feedback.rectTransform, new Vector2(1f, 0f), new Vector2(-10f, 6f), new Vector2(120f, 10f));

        var so = new SerializedObject(game);
        so.FindProperty("panel").objectReferenceValue = panel.gameObject;
        so.FindProperty("track").objectReferenceValue = track.rectTransform;
        so.FindProperty("note").objectReferenceValue = noteImage.rectTransform;
        so.FindProperty("scoreText").objectReferenceValue = score;
        so.FindProperty("feedbackText").objectReferenceValue = feedback;
        so.FindProperty("passDialogue").objectReferenceValue = d.auditionPass;
        so.FindProperty("failDialogue").objectReferenceValue = d.auditionFail;
        so.ApplyModifiedPropertiesWithoutUndo();
        panel.gameObject.SetActive(false);

        // The stage: press F on it to start.
        var stage = new GameObject("AuditionStage", typeof(BoxCollider2D));
        stage.transform.SetParent(root);
        stage.transform.position = new Vector3(30f, 0f, 0f);
        var range = stage.GetComponent<BoxCollider2D>();
        range.isTrigger = true;
        range.size = new Vector2(6f, 2f);
        range.offset = new Vector2(0f, 1f);
        P6.Box(stage.transform, "Sprite", new Vector2(0f, 0.1f), new Vector2(6f, 0.2f), new Color(0.8f, 0.2f, 0.3f), 1);
        P6.Box(stage.transform, "Curtain_L", new Vector2(-3.2f, 2.5f), new Vector2(0.4f, 5f), new Color(0.7f, 0.1f, 0.2f), 1);
        P6.Box(stage.transform, "Curtain_R", new Vector2(3.2f, 2.5f), new Vector2(0.4f, 5f), new Color(0.7f, 0.1f, 0.2f), 1);
        var rhythmStage = stage.AddComponent<RhythmStage>();
        so = new SerializedObject(rhythmStage);
        so.FindProperty("minigame").objectReferenceValue = game;
        so.FindProperty("task").objectReferenceValue = d.w4;
        so.FindProperty("prompt").objectReferenceValue = P6.Prompt(stage.transform, 2.2f);
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene.Value);
    }

    // ------------------------------------------------------------------ Hub

    private static void UpdateHubGate()
    {
        string path = $"{Part5Setup.LevelsFolder}/Wonderwick_Hub.unity";
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        TaskGate gate = P6.FindNamed<TaskGate>("TaskGate_LaughTower");
        if (gate == null)
            return;
        var so = new SerializedObject(gate);
        SerializedProperty tasks = so.FindProperty("requiredTasks");
        if (tasks.arraySize > 0)
            return; // Already set (maybe by hand): leave it.

        Data d = Load();
        TaskData[] all = { d.w1, d.w2, d.w3, d.w4 };
        tasks.arraySize = all.Length;
        for (int i = 0; i < all.Length; i++)
            tasks.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        gate.name = "TaskGate_LaughTower (needs W1-W4)";
        EditorSceneManager.SaveScene(scene);
    }

    // ------------------------------------------------------------------ Test scene

    private static void BuildTestScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Main Camera", typeof(Camera));
        cam.tag = "MainCamera";
        cam.GetComponent<Camera>().orthographic = true;
        cam.transform.position = new Vector3(0f, 0f, -10f);

        Data d = Load();
        var driver = new GameObject("Part7 Test Driver").AddComponent<WonderwickTest>();
        var so = new SerializedObject(driver);
        so.FindProperty("w1").objectReferenceValue = d.w1;
        so.FindProperty("w2").objectReferenceValue = d.w2;
        so.FindProperty("w3").objectReferenceValue = d.w3;
        so.FindProperty("w4").objectReferenceValue = d.w4;
        so.FindProperty("timeout").floatValue = 300f; // The audition rounds take a while.
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, TestScenePath);
        Part1Setup.AddScenesToBuildProfile(TestScenePath);
    }
}
