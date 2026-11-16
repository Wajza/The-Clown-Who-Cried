using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using B = GameSystemsPrefabBuilder;

/// <summary>
/// EDITOR-ONLY tool for Part 6: the four Tumbleton prologue tasks.
///
/// Menu: Tools > Clown Who Cried > Part 6 > Run Everything
///   1. Task data (only CREATED if missing; never overwrites text you've changed):
///        Assets/Resources/Tasks/Prologue      Task_P1_WakeUp ... Task_P4_ReflexCheck + objectives
///        Assets/Data/Dialogue/Prologue        speakers and conversations (placeholder lines)
///   2. Puts the tasks into the levels, inside one object called "Prologue (Part 6)":
///        Tumbleton_PipHouse   P1: the mirror, a TaskStarter; the front door waits for the mirror
///        Tumbleton            P1 "outside" area, P2 guard + bumper cars + checkpoint + far kerb,
///                             P3 vendor + 8 balloons + wall, P4 old Tumbles + 5 pots + spikes;
///                             the station gate now needs P1-P4.
///      Each level is only changed if it doesn't have a "Prologue (Part 6)" object yet, so your own
///      edits are safe. To rebuild, delete that object (and the level's own changes listed above
///      come back too) and run again.
///   3. Assets/Scenes/Tests/PrologueTest + PrologueTest driver.
/// </summary>
public static class Part6Setup
{
    private const string RootName = "Prologue (Part 6)";
    private const string TaskFolder = "Assets/Resources/Tasks/Prologue";
    private const string DialogueFolder = "Assets/Data/Dialogue/Prologue";
    private const string TestScenePath = Part1Setup.TestScenesFolder + "/PrologueTest.unity";

    // Flags and signal keys used by the prologue (also by the test).
    public const string MirrorFlag = "p1_looked_in_mirror";

    [MenuItem("Tools/Clown Who Cried/Part 6/Run Everything")]
    public static void RunEverything()
    {
        GameSystemsPrefabBuilder.Build();
        BuildData();
        AddToPipHouse();
        AddToTumbleton();
        BuildTestScene();
        Debug.Log("Part6Setup: done.");
    }

    /// <summary>Command line: set up, check for missing scripts, then play PrologueTest.</summary>
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

    /// <summary>Every prologue asset, loaded fresh (needed again after opening a scene).</summary>
    private class Prologue
    {
        public TaskData p1, p2, p3, p4;
        public DialogueData mirror, doorLocked;
        public DialogueData guardAsk, guardActive, guardDone;
        public DialogueData vendorAsk, vendorActive, vendorDone;
        public DialogueData tumblesAsk, tumblesActive, tumblesDone;
    }

    private static Prologue Load() => new Prologue
    {
        p1 = Asset<TaskData>(TaskFolder, "Task_P1_WakeUp"),
        p2 = Asset<TaskData>(TaskFolder, "Task_P2_HonkStreet"),
        p3 = Asset<TaskData>(TaskFolder, "Task_P3_BalloonWall"),
        p4 = Asset<TaskData>(TaskFolder, "Task_P4_ReflexCheck"),
        mirror = Asset<DialogueData>(DialogueFolder, "Dialogue_P1_Mirror"),
        doorLocked = Asset<DialogueData>(DialogueFolder, "Dialogue_P1_DoorLocked"),
        guardAsk = Asset<DialogueData>(DialogueFolder, "Dialogue_P2_Guard_Ask"),
        guardActive = Asset<DialogueData>(DialogueFolder, "Dialogue_P2_Guard_Active"),
        guardDone = Asset<DialogueData>(DialogueFolder, "Dialogue_P2_Guard_Done"),
        vendorAsk = Asset<DialogueData>(DialogueFolder, "Dialogue_P3_Vendor_Ask"),
        vendorActive = Asset<DialogueData>(DialogueFolder, "Dialogue_P3_Vendor_Active"),
        vendorDone = Asset<DialogueData>(DialogueFolder, "Dialogue_P3_Vendor_Done"),
        tumblesAsk = Asset<DialogueData>(DialogueFolder, "Dialogue_P4_Tumbles_Ask"),
        tumblesActive = Asset<DialogueData>(DialogueFolder, "Dialogue_P4_Tumbles_Active"),
        tumblesDone = Asset<DialogueData>(DialogueFolder, "Dialogue_P4_Tumbles_Done"),
    };

    internal static T Asset<T>(string folder, string name) where T : Object =>
        AssetDatabase.LoadAssetAtPath<T>($"{folder}/{name}.asset");

    /// <summary>Creates the asset and calls 'fill' ONLY if it doesn't exist yet.</summary>
    internal static T CreateIfMissing<T>(string folder, string name, System.Action<T> fill) where T : ScriptableObject
    {
        T existing = Asset<T>(folder, name);
        if (existing != null)
            return existing;
        T asset = ScriptableObject.CreateInstance<T>();
        fill(asset);
        AssetDatabase.CreateAsset(asset, $"{folder}/{name}.asset");
        return asset;
    }

    private static Prologue BuildData()
    {
        Part1Setup.EnsureFolder(TaskFolder);
        Part1Setup.EnsureFolder(DialogueFolder);

        DialogueSpeaker pip = Speaker("Speaker_Pip", "Pip", new Color(1f, 0.75f, 0.85f));
        DialogueSpeaker guard = Speaker("Speaker_OfficerHonk", "Officer Honk", new Color(0.6f, 0.8f, 1f));
        DialogueSpeaker vendor = Speaker("Speaker_BalloonBetty", "Balloon Betty", new Color(1f, 0.6f, 0.6f));
        DialogueSpeaker tumbles = Speaker("Speaker_OldTumbles", "Old Tumbles", new Color(0.9f, 0.85f, 0.5f));

        // ---- P1 Wake Up
        Talk("Dialogue_P1_Mirror", MirrorFlag,
            (pip, "Morning, Pip! Big smile for the mirror..."),
            (null, "(Something is running down Pip's cheek.)"),
            (pip, "Is that... a tear? Clowns in Tumbleton don't cry. Do they?"));
        Talk("Dialogue_P1_DoorLocked", "",
            (pip, "I should check my face in the mirror before I go out."));

        // ---- P2 Cross Honk Street
        Talk("Dialogue_P2_Guard_Ask", "",
            (guard, "Halt, little clown! Honk Street is full of bumper cars today."),
            (guard, "Make it to the other side without a bump and I'll call you the bravest clown in town!"));
        Talk("Dialogue_P2_Guard_Active", "", (guard, "Jump over the cars! Timing is everything."));
        Talk("Dialogue_P2_Guard_Done", "", (guard, "Not a single bump! Bravest clown in Tumbleton."));

        // ---- P3 Pop the Balloon Wall
        Talk("Dialogue_P3_Vendor_Ask", "",
            (vendor, "Oh no, oh no! My balloons got stuck and made a wall across the road!"),
            (vendor, "Could you pop them with your Star Coins? All eight of them!"));
        Talk("Dialogue_P3_Vendor_Active", "", (vendor, "Throw your coins at them! Jump to reach the high ones."));
        Talk("Dialogue_P3_Vendor_Done", "", (vendor, "Pop, pop, pop! You're a natural."));

        // ---- P4 Reflex Check
        Talk("Dialogue_P4_Tumbles_Ask", "",
            (tumbles, "So you want to ride the train? First, a little reflex check."),
            (tumbles, "Take my old mallet. Smash all five pots, and mind the spikes!"),
            (tumbles, "Tip: swing DOWN onto the last pot to bounce right over them."));
        Talk("Dialogue_P4_Tumbles_Active", "", (tumbles, "Five pots, young one. A down-swing on the last!"));
        Talk("Dialogue_P4_Tumbles_Done", "", (tumbles, "Ha! Reflexes like a rubber chicken. The station is all yours."));

        // ---- Tasks and their objectives
        Task("Task_P1_WakeUp", "p1_wake_up", "Wake Up", "Something feels different this morning...", 1, null,
            Signal("Objective_P1_Mirror", "p1_mirror", 1, "Look in the mirror"),
            Signal("Objective_P1_Outside", "p1_outside", 1, "Go outside"));
        Task("Task_P2_HonkStreet", "p2_honk_street", "Cross Honk Street", "Officer Honk dares Pip to cross the busy street.", 2, null,
            Signal("Objective_P2_Cross", "p2_crossed", 1, "Get across Honk Street"));
        Task("Task_P3_BalloonWall", "p3_balloon_wall", "Pop the Balloon Wall", "Balloon Betty's balloons are blocking the road.", 3, null,
            Signal("Objective_P3_Balloons", "p3_balloon", 8, "Pop the balloons"));
        Task("Task_P4_ReflexCheck", "p4_reflex_check", "Reflex Check", "Old Tumbles wants to see how quick Pip is.", 4,
            new List<Ability> { Ability.Mallet },
            Signal("Objective_P4_Pots", "p4_pot", 5, "Smash the pots"));

        AssetDatabase.SaveAssets();
        return Load();

        // Local helpers (they use the folders above).
        DialogueSpeaker Speaker(string asset, string displayName, Color color) =>
            CreateIfMissing<DialogueSpeaker>(DialogueFolder, asset, s => { s.displayName = displayName; s.nameColor = color; });

        void Talk(string asset, string flagWhenFinished, params (DialogueSpeaker who, string text)[] lines) =>
            CreateIfMissing<DialogueData>(DialogueFolder, asset, d =>
            {
                foreach ((DialogueSpeaker who, string text) in lines)
                    d.lines.Add(new DialogueData.Line { speaker = who, text = text });
                d.setFlagWhenFinished = flagWhenFinished;
            });

        SignalObjective Signal(string asset, string key, int count, string description) =>
            CreateIfMissing<SignalObjective>(TaskFolder, asset, o => { o.signalKey = key; o.requiredCount = count; o.description = description; });

        void Task(string asset, string id, string title, string description, int order, List<Ability> onStart, params TaskObjective[] objectives) =>
            CreateIfMissing<TaskData>(TaskFolder, asset, t =>
            {
                t.id = id;
                t.title = title;
                t.description = description;
                t.city = TaskData.City.Tumbleton;
                t.boardOrder = order;
                t.objectives = new List<TaskObjective>(objectives);
                if (onStart != null)
                    t.unlockOnStart = onStart;
            });
    }

    // ------------------------------------------------------------------ Levels

    private static string LevelPath(string scene) => $"{Part5Setup.LevelsFolder}/{scene}.unity";

    /// <summary>Opens a level; returns null (and leaves it alone) if it already has the prologue root.</summary>
    private static Scene? OpenForPrologue(string scene, out Transform root)
    {
        root = null;
        string path = LevelPath(scene);
        if (!System.IO.File.Exists(path))
        {
            Debug.LogError($"Part6Setup: {path} is missing. Run Part 5 > Run Everything first.");
            return null;
        }

        Scene opened = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        if (GameObject.Find(RootName) != null)
        {
            Debug.Log($"Part6Setup: {scene} already has '{RootName}'; left unchanged.");
            return null;
        }
        root = new GameObject(RootName).transform;
        return opened;
    }

    private static void AddToPipHouse()
    {
        Scene? scene = OpenForPrologue("Tumbleton_PipHouse", out Transform root);
        if (scene == null)
            return;
        Prologue d = Load();

        // P1 starts when Pip wakes up here.
        var starter = new GameObject("TaskStarter_P1", typeof(TaskStarter));
        starter.transform.SetParent(root);
        B.SetField(starter.GetComponent<TaskStarter>(), "task", d.p1);

        // The mirror (light blue, 1 x 2 tiles) at the old marker's spot.
        float mirrorX = TakeMarker("P1_Mirror (Part 6)", 12f);
        var mirror = new GameObject("Mirror");
        mirror.transform.SetParent(root);
        mirror.transform.position = new Vector3(mirrorX, 0f, 0f);
        Box(mirror.transform, "Sprite", new Vector2(0f, 1.25f), new Vector2(1f, 2f), new Color(0.7f, 0.9f, 1f), 1);
        var range = mirror.AddComponent<BoxCollider2D>();
        range.isTrigger = true;
        range.size = new Vector2(2f, 2.5f);
        range.offset = new Vector2(0f, 1.25f);
        var inspect = mirror.AddComponent<Inspectable>();
        var so = new SerializedObject(inspect);
        so.FindProperty("dialogue").objectReferenceValue = d.mirror;
        so.FindProperty("signalKey").stringValue = "p1_mirror";
        so.FindProperty("prompt").objectReferenceValue = Prompt(mirror.transform, 2.8f);
        so.ApplyModifiedPropertiesWithoutUndo();

        // The front door stays shut until Pip has looked in the mirror.
        Door door = GameObject.Find("FrontDoor").GetComponent<Door>();
        so = new SerializedObject(door);
        so.FindProperty("requirement.requiredFlag").stringValue = MirrorFlag;
        so.FindProperty("requirement.lockedDialogue").objectReferenceValue = d.doorLocked;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene.Value);
    }

    private static void AddToTumbleton()
    {
        Scene? scene = OpenForPrologue("Tumbleton", out Transform root);
        if (scene == null)
            return;
        Prologue d = Load();
        Sprite square = Part1Setup.GetOrCreatePlaceholderSprite();
        int enemyLayer = Part1Setup.RequireLayer("Enemy");

        // ---- P1: stepping outside the house (the area around the "FromHouse" spawn point).
        Area(root, "Area_P1_Outside", 9f, 6f, "p1_outside");

        // ---- P2: Officer Honk, two bumper cars, a checkpoint before the street, the far kerb.
        float streetX = TakeMarker("P2_HonkStreet (Part 6)", 25f);
        Npc(root, "NPC_OfficerHonk", "officer_honk", 15f, new Color(0.4f, 0.55f, 0.9f), d.guardAsk, d.guardActive, d.guardDone, d.p2, d.p1.DoneFlag);
        Car(root, "BumperCar_1", streetX - 5f, streetX + 13f, 6f, new Color(0.9f, 0.3f, 0.3f));
        Car(root, "BumperCar_2", streetX + 13f, streetX - 3f, 4.5f, new Color(0.3f, 0.7f, 0.9f));
        GameObject checkpoint = Part1Setup.AddCheckpoint("Tumbleton_HonkStreet", new Vector2(18f, 0f), square);
        checkpoint.transform.SetParent(root);
        Area(root, "Area_P2_FarKerb", 42.5f, 3f, "p2_crossed");

        // ---- P3: Balloon Betty and a wall of 8 balloons in front of a TaskGate wall.
        float wallX = TakeMarker("P3_BalloonWall (Part 6)", 45f) + 11f; // = 56
        Npc(root, "NPC_BalloonBetty", "balloon_betty", 47f, new Color(0.95f, 0.5f, 0.6f), d.vendorAsk, d.vendorActive, d.vendorDone, d.p3, d.p2.DoneFlag);
        Color[] colors = { new Color(1f, 0.4f, 0.4f), new Color(1f, 0.85f, 0.3f), new Color(0.5f, 0.8f, 1f), new Color(0.6f, 1f, 0.6f) };
        for (int i = 0; i < 8; i++)
        {
            int column = i / 4, row = i % 4; // 2 columns x 4 rows, low to high.
            Target(root, $"Balloon_{i + 1}", new Vector2(wallX - 3f + column, 0.6f + row), "p3_balloon",
                $"tumbleton_balloon_{i + 1}", d.p3, colors[row], circle: true, enemyLayer, pogo: false);
        }
        Wall(root, "TaskGate_BalloonWall", wallX, d.p3);

        // ---- P4: Old Tumbles, 5 pots; the last one floats above a spike pit (tagged "Pogo").
        float potsX = TakeMarker("P4_ReflexCheck (Part 6)", 76f) - 9f; // = 67
        Npc(root, "NPC_OldTumbles", "old_tumbles", 63f, new Color(0.85f, 0.75f, 0.4f), d.tumblesAsk, d.tumblesActive, d.tumblesDone, d.p4, d.p3.DoneFlag);
        Color potColor = new Color(0.75f, 0.45f, 0.3f);
        for (int i = 0; i < 4; i++)
            Target(root, $"Pot_{i + 1}", new Vector2(potsX + i * 3f, 0.4f), "p4_pot", $"tumbleton_pot_{i + 1}", d.p4, potColor,
                circle: false, enemyLayer, pogo: false);
        float spikesX = potsX + 15f; // = 82
        Spikes(root, spikesX, 4f);
        Target(root, "Pot_5_Pogo", new Vector2(spikesX, 2f), "p4_pot", "tumbleton_pot_5", d.p4, potColor,
            circle: false, enemyLayer, pogo: true);

        // ---- The station only opens after the whole prologue.
        TaskGate gate = FindNamed<TaskGate>("TaskGate_Station");
        if (gate != null)
        {
            var so = new SerializedObject(gate);
            SerializedProperty tasks = so.FindProperty("requiredTasks");
            TaskData[] all = { d.p1, d.p2, d.p3, d.p4 };
            tasks.arraySize = all.Length;
            for (int i = 0; i < all.Length; i++)
                tasks.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            gate.name = "TaskGate_Station (needs P1-P4)";
        }

        EditorSceneManager.SaveScene(scene.Value);
    }

    // ------------------------------------------------------------------ Pieces

    /// <summary>Removes a Part 5 marker and returns its x (or 'fallback' if it's gone).</summary>
    internal static float TakeMarker(string name, float fallback)
    {
        GameObject marker = GameObject.Find(name);
        if (marker == null)
            return fallback;
        float x = marker.transform.position.x;
        Object.DestroyImmediate(marker);
        return x;
    }

    internal static T FindNamed<T>(string startOfName) where T : Component
    {
        foreach (T component in Object.FindObjectsByType<T>(FindObjectsInactive.Include))
            if (component.name.StartsWith(startOfName))
                return component;
        return null;
    }

    internal static GameObject Box(Transform parent, string name, Vector2 localPos, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name, typeof(SpriteRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = Part1Setup.GetOrCreatePlaceholderSprite();
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    internal static GameObject Prompt(Transform parent, float height)
    {
        GameObject prompt = Box(parent, "Prompt", new Vector2(0f, height), new Vector2(0.25f, 0.4f), new Color(1f, 0.9f, 0.2f), 20);
        prompt.SetActive(false);
        return prompt;
    }

    internal static void Area(Transform root, string name, float centerX, float width, string key)
    {
        var area = new GameObject(name, typeof(BoxCollider2D));
        area.transform.SetParent(root);
        area.transform.position = new Vector3(centerX, 0f, 0f);
        var box = area.GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(width, 4f);
        box.offset = new Vector2(0f, 2f);
        var so = new SerializedObject(area.AddComponent<AreaTrigger>());
        so.FindProperty("signalKey").stringValue = key;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>A task-giving NPC: asks (default), "keep going" while active, "thanks" when done.</summary>
    internal static void Npc(Transform root, string name, string npcId, float x, Color color,
        DialogueData ask, DialogueData active, DialogueData done, TaskData task, string requiredFlag)
    {
        NpcTalk npc = Part2Setup.AddNpcAt(name, npcId, new Vector2(x, 0f), color, Part1Setup.GetOrCreatePlaceholderSprite(), ask,
            new NpcTalk.ConditionalDialogue { requiredFlag = task.DoneFlag, dialogue = done },
            new NpcTalk.ConditionalDialogue { requiredFlag = task.ActiveFlag, dialogue = active });
        npc.transform.SetParent(root);

        var giver = npc.gameObject.AddComponent<TaskGiver>();
        var so = new SerializedObject(giver);
        so.FindProperty("task").objectReferenceValue = task;
        so.FindProperty("requiredFlag").stringValue = requiredFlag; // The previous task must be done.
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>A bumper car driving between two x positions on the street (hurts Pip on touch).</summary>
    private static void Car(Transform root, string name, float fromX, float toX, float speed, Color color)
    {
        var car = new GameObject(name, typeof(Rigidbody2D), typeof(BoxCollider2D));
        car.transform.SetParent(root);
        car.transform.position = new Vector3(fromX, 0f, 0f);
        car.layer = Part1Setup.RequireLayer("Enemy");
        var body = car.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        var box = car.GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2f, 1f);
        box.offset = new Vector2(0f, 0.5f);
        GameObject sprite = Box(car.transform, "Sprite", new Vector2(0f, 0.5f), new Vector2(2f, 1f), color, 6);

        var damage = car.AddComponent<ContactDamage>(); // My teammate's script.
        var so = new SerializedObject(damage);
        so.FindProperty("playerLayer").intValue = 1 << Part1Setup.RequireLayer("Player");
        so.ApplyModifiedPropertiesWithoutUndo();

        var path = new GameObject($"{name}_Path").transform;
        path.SetParent(root);
        Transform a = new GameObject("A").transform, b = new GameObject("B").transform;
        a.SetParent(path);
        b.SetParent(path);
        a.position = new Vector3(fromX, 0f, 0f);
        b.position = new Vector3(toX, 0f, 0f);

        var mover = car.AddComponent<WaypointMover>();
        so = new SerializedObject(mover);
        SerializedProperty points = so.FindProperty("waypoints");
        points.arraySize = 2;
        points.GetArrayElementAtIndex(0).objectReferenceValue = a;
        points.GetArrayElementAtIndex(1).objectReferenceValue = b;
        so.FindProperty("speed").floatValue = speed;
        so.FindProperty("spriteToFlip").objectReferenceValue = sprite.GetComponent<SpriteRenderer>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    internal static void Target(Transform root, string name, Vector2 position, string key, string saveId, TaskData task,
        Color color, bool circle, int layer, bool pogo)
    {
        var target = new GameObject(name);
        target.transform.SetParent(root);
        target.transform.position = position;
        target.layer = layer; // Enemy: Pip's coins and mallet hit that layer.
        if (pogo)
            target.tag = "Pogo";

        Collider2D col;
        if (circle)
        {
            var c = target.AddComponent<CircleCollider2D>();
            c.radius = 0.4f;
            col = c;
        }
        else
        {
            var b = target.AddComponent<BoxCollider2D>();
            b.size = new Vector2(0.8f, 0.8f);
            col = b;
        }
        col.isTrigger = true; // Hit by coins and the mallet, but Pip walks through.

        GameObject sprite = Box(target.transform, "Sprite", Vector2.zero, circle ? new Vector2(0.8f, 0.9f) : new Vector2(0.8f, 0.8f), color, 5);

        var breakable = target.AddComponent<BreakableTarget>();
        var so = new SerializedObject(breakable);
        so.FindProperty("signalKey").stringValue = key;
        so.FindProperty("saveId").stringValue = saveId;
        so.FindProperty("onlyWhileTaskActive").objectReferenceValue = task;
        so.FindProperty("sprite").objectReferenceValue = sprite.GetComponent<SpriteRenderer>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>A solid wall that disappears when the task is done (a TaskGate).</summary>
    internal static void Wall(Transform root, string name, float x, TaskData task)
    {
        var gate = new GameObject(name);
        gate.transform.SetParent(root);
        gate.transform.position = new Vector3(x, 0f, 0f);
        GameObject wall = Box(gate.transform, "Wall", new Vector2(0f, 2.5f), new Vector2(1f, 5f), new Color(0.95f, 0.7f, 0.8f), 2);
        wall.AddComponent<BoxCollider2D>().size = Vector2.one;

        var taskGate = gate.AddComponent<TaskGate>();
        var so = new SerializedObject(taskGate);
        SerializedProperty tasks = so.FindProperty("requiredTasks");
        tasks.arraySize = 1;
        tasks.GetArrayElementAtIndex(0).objectReferenceValue = task;
        SerializedProperty closed = so.FindProperty("activeWhileClosed");
        closed.arraySize = 1;
        closed.GetArrayElementAtIndex(0).objectReferenceValue = wall;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>A strip of spikes on the floor (my teammate's PlayerHazards reacts to the "Spikes" tag).</summary>
    internal static void Spikes(Transform root, float centerX, float width)
    {
        var spikes = new GameObject("Spikes", typeof(BoxCollider2D));
        spikes.transform.SetParent(root);
        spikes.transform.position = new Vector3(centerX, 0f, 0f);
        spikes.tag = "Spikes";
        var box = spikes.GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(width, 0.5f);
        box.offset = new Vector2(0f, 0.25f);
        Box(spikes.transform, "Sprite", new Vector2(0f, 0.25f), new Vector2(width, 0.5f), new Color(0.7f, 0.7f, 0.75f), 3);
    }

    // ------------------------------------------------------------------ Test scene

    private static void BuildTestScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Main Camera", typeof(Camera));
        cam.tag = "MainCamera";
        cam.GetComponent<Camera>().orthographic = true;
        cam.transform.position = new Vector3(0f, 0f, -10f);

        Prologue d = Load();
        var driver = new GameObject("Part6 Test Driver").AddComponent<PrologueTest>();
        var so = new SerializedObject(driver);
        so.FindProperty("p1").objectReferenceValue = d.p1;
        so.FindProperty("p2").objectReferenceValue = d.p2;
        so.FindProperty("p3").objectReferenceValue = d.p3;
        so.FindProperty("p4").objectReferenceValue = d.p4;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, TestScenePath);
        Part1Setup.AddScenesToBuildProfile(TestScenePath);
    }
}
