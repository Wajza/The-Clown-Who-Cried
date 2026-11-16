using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using B = GameSystemsPrefabBuilder;

/// <summary>
/// EDITOR-ONLY tool for Part 5 (levels, camera, scene flow, abilities).
///
/// Menu: Tools > Clown Who Cried > Part 5 > ...
///   "1. Configure Project": adds the CINEMACHINE_3 scripting symbol (my teammate's ScreenShake
///      uses it) and the "Pogo" tag. Unity recompiles afterwards.
///   "2. Run Everything":
///      - placeholder art (tiles, door), the GameSystems prefab (now with PlayerAbilities and the
///        level name card), Assets/Prefabs/Placeholders/Placeholder_Pip and Resources/GameSettings,
///      - GREYBOX versions of every level (only the ones that don't exist yet, so your own work
///        in a level is never overwritten): Pip's house, Tumbleton, the train map, Wonderwick's hub
///        and its 4 task rooms, Chortle's arena and the End of Demo scene,
///      - Assets/Scenes/Tests/LevelTest and the Build Profile scene list (Title stays first).
///
/// The greyboxes are walkable placeholders: a tile floor, walls, doors, spawn points, the camera
/// rig and empty "markers" where Parts 6-8 will put the task objects. Replace or paint over them.
/// </summary>
public static class Part5Setup
{
    public const string LevelsFolder = "Assets/Scenes/Levels";
    private const string PipPrefabPath = "Assets/Prefabs/Placeholders/Placeholder_Pip.prefab";
    private const string GameSettingsPath = "Assets/Resources/GameSettings.asset";
    private const string TestScenePath = Part1Setup.TestScenesFolder + "/LevelTest.unity";
    private const string CinemachineSymbol = "CINEMACHINE_3";

    private const int LevelHeight = 18; // Tiles. The screen is 16.9 tiles tall.

    // ------------------------------------------------------------------ Menu items

    [MenuItem("Tools/Clown Who Cried/Part 5/1. Configure Project (Cinemachine symbol, Pogo tag)")]
    public static void ConfigureProject()
    {
        // Scripting symbol for every desktop build (and the Editor).
        NamedBuildTarget target = NamedBuildTarget.Standalone;
        string symbols = PlayerSettings.GetScriptingDefineSymbols(target);
        if (!(";" + symbols + ";").Contains(";" + CinemachineSymbol + ";"))
            PlayerSettings.SetScriptingDefineSymbols(target, string.IsNullOrEmpty(symbols) ? CinemachineSymbol : symbols + ";" + CinemachineSymbol);

        AddTag("Pogo");
        AssetDatabase.SaveAssets();
        Debug.Log("Part5Setup: project configured (CINEMACHINE_3, Pogo tag).");
    }

    public static void ConfigureProjectFromCommandLine()
    {
        ConfigureProject();
        EditorApplication.Exit(0);
    }

    [MenuItem("Tools/Clown Who Cried/Part 5/2. Run Everything")]
    public static void RunEverything()
    {
        GameSystemsPrefabBuilder.Build(); // Also makes the placeholder art.
        BuildPlaceholderPip();
        BuildGameSettings();
        Part1Setup.EnsureFolder(LevelsFolder);
        BuildAllLevels(overwrite: false);
        BuildTestScene();
        Debug.Log("Part5Setup: done.");
    }

    /// <summary>Command line: set up, check for missing scripts, then play LevelTest.</summary>
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

    private static void AddTag(string tag)
    {
        var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = manager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        manager.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------ Placeholder Pip + GameSettings

    /// <summary>
    /// A stand-in Pip prefab (pink box, pivot at the feet) with my teammate's player components
    /// that work without extra prefabs. Only made if missing. Replaced by their real Pip later.
    /// </summary>
    private static void BuildPlaceholderPip()
    {
        if (File.Exists(PipPrefabPath))
            return;

        Part1Setup.EnsureFolder(Path.GetDirectoryName(PipPrefabPath).Replace('\\', '/'));
        GameObject pip = Part1Setup.BuildStandInPip(Part1Setup.GetOrCreatePlaceholderSprite());
        pip.name = "Placeholder_Pip";
        pip.transform.position = Vector3.zero;
        pip.AddComponent<PlayerHazards>();
        pip.AddComponent<TearGauge>();
        pip.AddComponent<PlayerBlueSight>();
        PrefabUtility.SaveAsPrefabAsset(pip, PipPrefabPath);
        Object.DestroyImmediate(pip);
    }

    private static void BuildGameSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<GameSettings>(GameSettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<GameSettings>();
            AssetDatabase.CreateAsset(settings, GameSettingsPath);
        }
        if (settings.playerPrefab == null) // Never replace a Pip someone chose (e.g. the real one).
        {
            settings.playerPrefab = AssetDatabase.LoadAssetAtPath<PlayerController>(PipPrefabPath);
            EditorUtility.SetDirty(settings);
        }
        AssetDatabase.SaveAssets();
    }

    // ------------------------------------------------------------------ All levels

    private static void BuildAllLevels(bool overwrite)
    {
        var built = new List<string>();

        Level house = Begin("Tumbleton_PipHouse", "Pip's House", TaskData.City.Tumbleton, 32, overwrite);
        if (house != null)
        {
            house.Spawn("Bed", 5f, isDefault: true);
            house.Spawn("FromTown", 25f);
            house.Door("FrontDoor", 28f, "Tumbleton", "FromHouse", "Town");
            house.Marker("P1_Mirror (Part 6)", 12f);
            built.Add(house.Save());
        }

        Level town = Begin("Tumbleton", "Tumbleton", TaskData.City.Tumbleton, 120, overwrite);
        if (town != null)
        {
            town.Spawn("FromHouse", 8f, isDefault: true);
            town.Spawn("FromTrain", 110f);
            town.Door("HouseDoor", 5f, "Tumbleton_PipHouse", "FromTown", "Home");
            town.Marker("P2_HonkStreet (Part 6)", 25f);
            town.Marker("P3_BalloonWall (Part 6)", 45f);
            town.CameraZone("CameraZone_TownSquare", 60f, 92f);
            town.Marker("P4_ReflexCheck (Part 6)", 76f);
            town.Gate("TaskGate_Station (needs P1-P4, Part 6)", 104f);
            town.Door("StationDoor", 114f, "TrainMap", "", "Train");
            built.Add(town.Save());
        }

        string trainMap = BuildTrainMap(overwrite);
        if (trainMap != null)
            built.Add(trainMap);

        Level hub = Begin("Wonderwick_Hub", "Wonderwick", TaskData.City.Wonderwick, 140, overwrite);
        if (hub != null)
        {
            hub.Spawn("FromTrain", 8f, isDefault: true);
            hub.Door("StationDoor", 4f, "TrainMap", "", "Train");
            string[] rooms = { "Wonderwick_W1_Delivery", "Wonderwick_W2_MirrorMaze", "Wonderwick_W3_Coaster", "Wonderwick_W4_Audition" };
            string[] labels = { "W1 Delivery", "W2 Mirrors", "W3 Coaster", "W4 Audition" };
            for (int i = 0; i < rooms.Length; i++)
            {
                float x = 30f + i * 25f;
                hub.Spawn($"FromW{i + 1}", x + 3f);
                hub.Door($"Door_W{i + 1}", x, rooms[i], "FromHub", labels[i]);
            }
            hub.Gate("TaskGate_LaughTower (needs W1-W4, Part 7)", 126f);
            hub.Spawn("FromTower", 128f);
            hub.Door("LaughTowerDoor", 134f, "ChortleArena", "Start", "Laugh Tower");
            built.Add(hub.Save());
        }

        string[] roomScenes = { "Wonderwick_W1_Delivery", "Wonderwick_W2_MirrorMaze", "Wonderwick_W3_Coaster", "Wonderwick_W4_Audition" };
        string[] roomNames = { "Punchline Delivery", "Crooked Mirror Maze", "Runaway Coaster", "The Audition" };
        for (int i = 0; i < roomScenes.Length; i++)
        {
            Level room = Begin(roomScenes[i], roomNames[i], TaskData.City.Wonderwick, 60, overwrite);
            if (room == null)
                continue;
            room.Spawn("FromHub", 6f, isDefault: true);
            room.Door("BackToHub", 3f, "Wonderwick_Hub", $"FromW{i + 1}", "Hub");
            room.Marker($"W{i + 1}_TaskArea (Part 7)", 30f);
            room.EdgeExit("ExitToHub", 58.5f, "Wonderwick_Hub", $"FromW{i + 1}");
            built.Add(room.Save());
        }

        Level arena = Begin("ChortleArena", "The Laugh Tower", TaskData.City.Wonderwick, 32, overwrite);
        if (arena != null)
        {
            arena.Spawn("Start", 4f, isDefault: true);
            arena.Marker("Chortle (Part 8)", 24f);
            built.Add(arena.Save());
        }

        string endOfDemo = BuildEndOfDemo(overwrite);
        if (endOfDemo != null)
            built.Add(endOfDemo);

        // Every level in the scene list (adding is harmless if they're already there).
        var all = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { LevelsFolder }))
            all.Add(AssetDatabase.GUIDToAssetPath(guid));
        Part1Setup.AddScenesToBuildProfile(all.ToArray());

        Debug.Log(built.Count > 0
            ? $"Part5Setup: built {built.Count} level(s): {string.Join(", ", built)}"
            : "Part5Setup: all levels already exist; nothing overwritten.");
    }

    // ------------------------------------------------------------------ One greybox level

    /// <summary>Starts a new level scene, or returns null if it already exists (and overwrite is off).</summary>
    private static Level Begin(string scene, string displayName, TaskData.City city, int width, bool overwrite)
    {
        string path = $"{LevelsFolder}/{scene}.unity";
        if (File.Exists(path) && !overwrite)
            return null;
        return new Level(path, displayName, city, width, spawnPlayer: true);
    }

    /// <summary>Builds one level scene. Units are tiles; the floor's top is y = 0.</summary>
    private class Level
    {
        private readonly string _path;
        private readonly int _width;
        private readonly LevelInfo _info;
        private readonly Tilemap _ground;
        private readonly Scene _scene;

        public Level(string path, string displayName, TaskData.City city, int width, bool spawnPlayer)
        {
            _path = path;
            _width = width;
            _scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Tiles: floor 3 deep (grass on top), walls at both ends, on the Ground layer.
            var grid = new GameObject("Grid", typeof(Grid));
            _ground = CreateTilemap(grid.transform, "Ground");
            for (int x = -2; x <= width + 1; x++)
                for (int y = -3; y <= -1; y++)
                    _ground.SetTile(new Vector3Int(x, y, 0), y == -1 ? PlaceholderArt.GroundTopTile : PlaceholderArt.GroundTile);
            for (int y = 0; y < LevelHeight; y++)
            {
                _ground.SetTile(new Vector3Int(-2, y, 0), PlaceholderArt.WallTile);
                _ground.SetTile(new Vector3Int(-1, y, 0), PlaceholderArt.WallTile);
                _ground.SetTile(new Vector3Int(width, y, 0), PlaceholderArt.WallTile);
                _ground.SetTile(new Vector3Int(width + 1, y, 0), PlaceholderArt.WallTile);
            }

            // Camera bounds: the whole level, from the bottom of the floor up LevelHeight tiles.
            var bounds = new GameObject("CameraBounds", typeof(BoxCollider2D));
            var box = bounds.GetComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(width + 2f, LevelHeight + 3f);
            box.offset = new Vector2(width / 2f, (LevelHeight - 3f) / 2f);

            _info = new GameObject("LevelInfo").AddComponent<LevelInfo>();
            var so = new SerializedObject(_info);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("city").enumValueIndex = (int)city;
            so.FindProperty("spawnPlayer").boolValue = spawnPlayer;
            so.FindProperty("cameraBounds").objectReferenceValue = box;
            so.ApplyModifiedPropertiesWithoutUndo();

            BuildCameraRig(box);
        }

        public void Spawn(string id, float x, bool isDefault = false)
        {
            Part1Setup.AddSpawnPoint(id, new Vector2(x, 0f), isDefault);
            if (isDefault)
                _info.transform.position = new Vector3(x, 0f, 0f); // Where Pip is first created.
        }

        public void Door(string name, float x, string toScene, string toSpawn, string label)
        {
            var door = new GameObject(name);
            door.transform.position = new Vector3(x, 0f, 0f);

            var art = new GameObject("Sprite", typeof(SpriteRenderer));
            art.transform.SetParent(door.transform, false);
            var sr = art.GetComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.DoorSprite;
            sr.sortingOrder = 1;

            var prompt = new GameObject("Prompt", typeof(SpriteRenderer));
            prompt.transform.SetParent(door.transform, false);
            prompt.transform.localPosition = new Vector3(0f, 2.6f, 0f);
            prompt.transform.localScale = new Vector3(0.25f, 0.4f, 1f);
            var promptSprite = prompt.GetComponent<SpriteRenderer>();
            promptSprite.sprite = Part1Setup.GetOrCreatePlaceholderSprite();
            promptSprite.color = new Color(1f, 0.9f, 0.2f);
            promptSprite.sortingOrder = 20;

            var range = door.AddComponent<BoxCollider2D>();
            range.isTrigger = true;
            range.size = new Vector2(2f, 2.5f);
            range.offset = new Vector2(0f, 1.25f);

            var component = door.AddComponent<Door>();
            var so = new SerializedObject(component);
            so.FindProperty("destination.sceneName").stringValue = toScene;
            so.FindProperty("destination.spawnPointId").stringValue = toSpawn;
            so.FindProperty("prompt").objectReferenceValue = prompt;
            so.ApplyModifiedPropertiesWithoutUndo();
            prompt.SetActive(false);

            Label(door.transform, label, 3.2f);
        }

        /// <summary>A walk-into exit at the level's edge (1 x 4 tiles).</summary>
        public void EdgeExit(string name, float x, string toScene, string toSpawn)
        {
            var exit = new GameObject(name, typeof(BoxCollider2D));
            exit.transform.position = new Vector3(x, 0f, 0f);
            var box = exit.GetComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1f, 4f);
            box.offset = new Vector2(0f, 2f);
            var component = exit.AddComponent<LevelExit>();
            var so = new SerializedObject(component);
            so.FindProperty("destination.sceneName").stringValue = toScene;
            so.FindProperty("destination.spawnPointId").stringValue = toSpawn;
            so.ApplyModifiedPropertiesWithoutUndo();
            Label(exit.transform, "Exit >", 4.5f);
        }

        /// <summary>A TaskGate with a solid wall. No tasks yet (so it's open); Parts 6/7 fill them in.</summary>
        public void Gate(string name, float x)
        {
            var gate = new GameObject(name);
            gate.transform.position = new Vector3(x, 0f, 0f);
            var wall = new GameObject("Wall", typeof(SpriteRenderer), typeof(BoxCollider2D));
            wall.transform.SetParent(gate.transform, false);
            wall.transform.localPosition = new Vector3(0f, 2f, 0f);
            wall.transform.localScale = new Vector3(1f, 4f, 1f);
            wall.GetComponent<SpriteRenderer>().sprite = Part1Setup.GetOrCreatePlaceholderSprite();
            wall.GetComponent<SpriteRenderer>().color = new Color(0.8f, 0.25f, 0.25f);
            wall.GetComponent<BoxCollider2D>().size = Vector2.one;
            var taskGate = gate.AddComponent<TaskGate>();
            var so = new SerializedObject(taskGate);
            SerializedProperty closed = so.FindProperty("activeWhileClosed");
            closed.arraySize = 1;
            closed.GetArrayElementAtIndex(0).objectReferenceValue = wall;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A room the camera stays inside while Pip is in it (x from..to, full height).</summary>
        public void CameraZone(string name, float fromX, float toX)
        {
            var zone = new GameObject(name, typeof(BoxCollider2D));
            var box = zone.GetComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(toX - fromX, LevelHeight + 3f);
            box.offset = new Vector2((fromX + toX) / 2f, (LevelHeight - 3f) / 2f);
            zone.AddComponent<global::CameraZone>();
        }

        /// <summary>An empty, labelled spot where a later part puts something (e.g. a task).</summary>
        public void Marker(string name, float x)
        {
            var marker = new GameObject(name);
            marker.transform.position = new Vector3(x, 0f, 0f);
            Label(marker.transform, name, 1f);
        }

        public string Save()
        {
            EditorSceneManager.SaveScene(_scene, _path);
            return Path.GetFileNameWithoutExtension(_path);
        }

        private static Tilemap CreateTilemap(Transform grid, string name)
        {
            // A plain TilemapCollider2D: one box per tile, made from each tile's Collider Type.
            // (A CompositeCollider2D "Merge" set-up generated NO shapes on this Unity version, so
            // Pip fell through the floor. Plain per-tile boxes are fine for levels this size.)
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
            go.transform.SetParent(grid, false);
            go.layer = Part1Setup.RequireLayer("Ground");
            return go.GetComponent<Tilemap>();
        }

        private void BuildCameraRig(BoxCollider2D bounds) => CreateCameraRig(bounds);
    }

    // ------------------------------------------------------------------ Camera rig

    /// <summary>
    /// CameraRig (ScreenShake) / Main Camera (Brain, Pixel Perfect) / PipCamera (CinemachineCamera
    /// following Pip, confined to the bounds, pixel perfect, listening for shakes).
    /// </summary>
    private static void CreateCameraRig(Collider2D bounds)
    {
        var rig = new GameObject("CameraRig", typeof(CinemachineImpulseSource));
        rig.AddComponent<ScreenShake>(); // My teammate's script.
        var cameraRig = rig.AddComponent<CameraRig>();

        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(CinemachineBrain), typeof(PixelPerfectCamera));
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(rig.transform, false);
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        Camera cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 270f / 16f / 2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.6f, 0.75f);
        var pixelPerfect = camGo.GetComponent<PixelPerfectCamera>();
        pixelPerfect.assetsPPU = 16;
        pixelPerfect.refResolutionX = 480;
        pixelPerfect.refResolutionY = 270;

        var vcamGo = new GameObject("PipCamera", typeof(CinemachineCamera), typeof(CinemachinePositionComposer),
            typeof(CinemachineConfiner2D), typeof(CinemachinePixelPerfect), typeof(CinemachineImpulseListener));
        vcamGo.transform.SetParent(rig.transform, false);
        vcamGo.transform.position = new Vector3(0f, 0f, -10f);
        var vcam = vcamGo.GetComponent<CinemachineCamera>();
        vcam.Lens = new LensSettings { OrthographicSize = 270f / 16f / 2f, NearClipPlane = 0.3f, FarClipPlane = 1000f, ModeOverride = LensSettings.OverrideModes.Orthographic };

        var composer = vcamGo.GetComponent<CinemachinePositionComposer>();
        composer.CameraDistance = 10f;
        composer.TargetOffset = new Vector3(0f, 2f, 0f); // Look a little above Pip's feet.
        composer.Damping = new Vector3(0.4f, 0.3f, 0f);

        var confiner = vcamGo.GetComponent<CinemachineConfiner2D>();
        confiner.BoundingShape2D = bounds;

        B.SetField(cameraRig, "pipCamera", vcam);
        B.SetField(cameraRig, "confiner", confiner);
    }

    // ------------------------------------------------------------------ Scenes without Pip

    /// <summary>A scene with no Pip: a plain camera, LevelInfo (no Pip, no name card) and a canvas.</summary>
    private static GameObject BeginMenuScene(string displayName, out Scene scene)
    {
        scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Main Camera", typeof(Camera));
        cam.tag = "MainCamera";
        cam.transform.position = new Vector3(0f, 0f, -10f);
        Camera c = cam.GetComponent<Camera>();
        c.orthographic = true;
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = new Color(0.16f, 0.12f, 0.22f);

        var info = new GameObject("LevelInfo").AddComponent<LevelInfo>();
        var so = new SerializedObject(info);
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("spawnPlayer").boolValue = false;
        so.FindProperty("showNameCard").boolValue = false;
        so.FindProperty("lockAbilitiesUntilEarned").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject canvas = B.CreateCanvas("Canvas", null, sortingOrder: 0);
        canvas.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static string BuildTrainMap(bool overwrite)
    {
        string path = $"{LevelsFolder}/TrainMap.unity";
        if (File.Exists(path) && !overwrite)
            return null;

        GameObject canvas = BeginMenuScene("Train", out Scene scene);
        var map = canvas.AddComponent<TrainMap>();

        TMP_Text title = B.CreateText("Title", canvas.transform, "ALL ABOARD!\nWhere to?", 16f, TextAlignmentOptions.Center);
        B.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(400f, 40f));
        title.color = new Color(1f, 0.85f, 0.3f);

        RectTransform column = B.CreateEmpty("Stations", canvas.transform);
        B.Place(column, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(160f, 40f));
        var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        (string name, string scene, string spawn)[] stations =
        {
            ("Tumbleton", "Tumbleton", "FromTrain"),
            ("Wonderwick", "Wonderwick_Hub", "FromTrain"),
        };

        var so = new SerializedObject(map);
        SerializedProperty list = so.FindProperty("stations");
        list.arraySize = stations.Length;
        for (int i = 0; i < stations.Length; i++)
        {
            Button button = B.CreateButton($"Station_{stations[i].name}", column, stations[i].name, null);
            UnityEventTools.AddIntPersistentListener(button.onClick, map.Travel, i);
            SerializedProperty item = list.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("displayName").stringValue = stations[i].name;
            item.FindPropertyRelative("destination.sceneName").stringValue = stations[i].scene;
            item.FindPropertyRelative("destination.spawnPointId").stringValue = stations[i].spawn;
            item.FindPropertyRelative("button").objectReferenceValue = button;
            item.FindPropertyRelative("label").objectReferenceValue = button.GetComponentInChildren<TMP_Text>();
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, path);
        return "TrainMap";
    }

    private static string BuildEndOfDemo(bool overwrite)
    {
        string path = $"{LevelsFolder}/EndOfDemo.unity";
        if (File.Exists(path) && !overwrite)
            return null;

        GameObject canvas = BeginMenuScene("End of Demo", out Scene scene);
        TMP_Text text = B.CreateText("ToBeContinued", canvas.transform, "TO BE CONTINUED...", 16f, TextAlignmentOptions.Center);
        B.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(400f, 20f));
        TMP_Text note = B.CreateText("PlaceholderNote", canvas.transform, "(placeholder: credits come in Part 8)",
            PlaceholderArt.FontSize, TextAlignmentOptions.Center);
        B.Place(note.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(400f, 10f));
        note.color = new Color(0.6f, 0.6f, 0.65f);

        EditorSceneManager.SaveScene(scene, path);
        return "EndOfDemo";
    }

    // ------------------------------------------------------------------ Labels

    /// <summary>A small world-space sign (pixel font) above something, for finding your way in greyboxes.</summary>
    private static void Label(Transform parent, string text, float height)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, height, 0f);
        var tmp = go.GetComponent<TextMeshPro>();
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = 4f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.color = new Color(1f, 1f, 1f, 0.85f);
        tmp.sortingOrder = 30;
        ((RectTransform)go.transform).sizeDelta = new Vector2(8f, 1f);
    }

    // ------------------------------------------------------------------ Test scene

    private static void BuildTestScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Main Camera", typeof(Camera));
        cam.tag = "MainCamera";
        cam.GetComponent<Camera>().orthographic = true;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        new GameObject("Part5 Test Driver").AddComponent<LevelSystemTest>();
        EditorSceneManager.SaveScene(scene, TestScenePath);
        Part1Setup.AddScenesToBuildProfile(TestScenePath);
    }
}
