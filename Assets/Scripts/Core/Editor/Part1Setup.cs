using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// EDITOR-ONLY tool that does the Part 1 setup for you (it lives in an "Editor" folder, so it is
/// never included in the built game).
///
/// Menu: Tools > Clown Who Cried > Part 1 > ...
///   - "Build GameSystems Prefab": rebuilds Assets/Resources/GameSystems.prefab with the systems
///     of EVERY part so far (see GameSystemsPrefabBuilder).
///   - "Build Test Scenes": Assets/Scenes/Tests/CoreTest_A and CoreTest_B, each with ground,
///     a stand-in Pip, spawn points and a checkpoint, and adds them to the Build Profile scene list.
///   - "Run Everything": both of the above.
///
/// Running it again is safe: it overwrites what it made before and never touches other assets.
///
/// The test scenes use a STAND-IN Pip made from my teammate's components (PlayerController,
/// PlayerHealth, PlayerStarFragments) with a plain square sprite, because the real Pip prefab
/// isn't in the project yet. Like the real Pip, her pivot is at her feet (bottom centre), she is
/// on the "Player" layer, and the floor is on the "Ground" layer (layers my teammate added).
///
/// "Check Missing Scripts" scans the GameSystems prefab and every scene in Assets/Scenes for
/// components whose script can't be found (shown as "Missing Script" in the Inspector). That
/// happens when a script's .meta file ID changes, e.g. after a merge.
/// </summary>
public static class Part1Setup
{
    // ------------------------------------------------------------------ Paths

    internal const string ResourcesFolder = "Assets/Resources";
    internal const string PrefabPath = ResourcesFolder + "/GameSystems.prefab";
    internal const string TestScenesFolder = "Assets/Scenes/Tests";
    private const string SceneAPath = TestScenesFolder + "/CoreTest_A.unity";
    private const string SceneBPath = TestScenesFolder + "/CoreTest_B.unity";
    private const string PlaceholderSpritePath = TestScenesFolder + "/Placeholder_White.png";
    private const string PlayerControlsPath = "Assets/Scripts/Player/PlayerControls.inputactions";

    // Layer names from ProjectSettings/TagManager.asset (added by my teammate).
    private const string GroundLayerName = "Ground";
    private const string PlayerLayerName = "Player";

    // Pip's feet stand on the floor's top surface, which is at this height in the test scenes.
    internal const float FloorY = -1.5f;

    // ------------------------------------------------------------------ Menu items

    [MenuItem("Tools/Clown Who Cried/Part 1/Run Everything")]
    public static void RunEverything()
    {
        BuildGameSystemsPrefab();
        BuildTestScenes();
        Debug.Log("Part1Setup: done.");
    }

    /// <summary>Builds the whole GameSystems prefab (every part's systems, not only Part 1's).</summary>
    [MenuItem("Tools/Clown Who Cried/Part 1/Build GameSystems Prefab")]
    public static void BuildGameSystemsPrefab() => GameSystemsPrefabBuilder.Build();

    [MenuItem("Tools/Clown Who Cried/Part 1/Build Test Scenes")]
    public static void BuildTestScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return; // The user cancelled.

        EnsureFolder(TestScenesFolder);
        Sprite square = GetOrCreatePlaceholderSprite();

        // Scene A: Pip starts on the left, a checkpoint in the middle, the test driver.
        Scene a = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildCommon(square);
        AddSpawnPoint("Start", new Vector2(-8f, FloorY), isDefault: true);
        AddSpawnPoint("FromB", new Vector2(8f, FloorY), isDefault: false);
        AddCheckpoint("CoreTest_A_CP1", new Vector2(2f, FloorY), square);
        new GameObject("Part1 Test Driver").AddComponent<CoreSystemsTest>();
        EditorSceneManager.SaveScene(a, SceneAPath);

        // Scene B: a second level to test scene loading.
        Scene b = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildCommon(square);
        AddSpawnPoint("Start", new Vector2(-8f, FloorY), isDefault: true);
        AddSpawnPoint("FromA", new Vector2(-3f, FloorY), isDefault: false);
        EditorSceneManager.SaveScene(b, SceneBPath);

        AddScenesToBuildProfile(SceneAPath, SceneBPath);
        EditorSceneManager.OpenScene(SceneAPath);
        Debug.Log("Part1Setup: test scenes built and added to the scene list.");
    }

    /// <summary>
    /// Used from the command line: builds everything, opens CoreTest_A and presses Play.
    /// CoreSystemsTest then runs the checks and closes Unity with exit code 0 (pass) or 1 (fail).
    /// </summary>
    public static void RunTestsFromCommandLine()
    {
        RunEverything();
        if (CheckMissingScripts() > 0)
        {
            EditorApplication.Exit(1); // Don't bother testing broken scenes.
            return;
        }
        EditorSceneManager.OpenScene(SceneAPath);
        EditorApplication.EnterPlaymode();
    }

    /// <summary>Command line: only scan for missing scripts, exit 0 if none, 1 if any.</summary>
    public static void CheckMissingScriptsFromCommandLine()
    {
        EditorApplication.Exit(CheckMissingScripts() == 0 ? 0 : 1);
    }

    // ------------------------------------------------------------------ Missing script check

    /// <summary>
    /// Counts components with a missing script in the GameSystems prefab and in every scene
    /// under Assets/Scenes. Prints one line per asset and returns the total.
    /// </summary>
    [MenuItem("Tools/Clown Who Cried/Part 1/Check Missing Scripts")]
    public static int CheckMissingScripts()
    {
        int total = 0;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
            Debug.LogError($"[MissingScripts] {PrefabPath} not found.");
        else
            total += ReportMissing(PrefabPath, new[] { prefab });

        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            total += ReportMissing(path, scene.GetRootGameObjects());
        }

        Debug.Log($"[MissingScripts] TOTAL missing scripts: {total}");
        return total;
    }

    private static int ReportMissing(string assetPath, GameObject[] roots)
    {
        int missing = 0;
        foreach (GameObject root in roots)
        {
            // Check the root and every child.
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (count > 0)
                    Debug.LogError($"[MissingScripts] {assetPath}: '{t.name}' has {count} missing script(s).");
                missing += count;
            }
        }
        Debug.Log($"[MissingScripts] {assetPath}: {missing} missing");
        return missing;
    }

    // ------------------------------------------------------------------ Scene pieces

    /// <summary>Camera, floor and stand-in Pip: the start of every test scene.</summary>
    internal static void BuildCommon(Sprite square)
    {
        // Camera sized for 480x270 at 16 pixels per unit: half the height = 270 / 16 / 2.
        var cam = new GameObject("Main Camera", typeof(Camera));
        cam.tag = "MainCamera";
        cam.transform.position = new Vector3(0f, 0f, -10f);
        Camera c = cam.GetComponent<Camera>();
        c.orthographic = true;
        c.orthographicSize = 270f / 16f / 2f;
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = new Color(0.55f, 0.6f, 0.75f);

        // Floor: 40 tiles wide, 1 tall, top surface at y = -1.5.
        var ground = new GameObject("Ground (test)", typeof(SpriteRenderer), typeof(BoxCollider2D));
        ground.layer = RequireLayer(GroundLayerName);
        ground.transform.position = new Vector3(0f, FloorY - 0.5f, 0f); // 1 tall, so the top is FloorY.
        ground.transform.localScale = new Vector3(40f, 1f, 1f);
        ground.GetComponent<SpriteRenderer>().sprite = square;
        ground.GetComponent<SpriteRenderer>().color = new Color(0.35f, 0.3f, 0.3f);
        ground.GetComponent<BoxCollider2D>().size = Vector2.one;

        BuildStandInPip(square);
    }

    internal static GameObject BuildStandInPip(Sprite square)
    {
        // The root's position is Pip's FEET (bottom centre), like the real Pip. Everything
        // visible/solid is shifted up by half her height (0.5 tiles) so it sits above the pivot.
        var pip = new GameObject("Pip (test stand-in)");
        pip.layer = RequireLayer(PlayerLayerName);
        pip.transform.position = new Vector3(-8f, FloorY, 0f);

        var body = new GameObject("Body", typeof(SpriteRenderer));
        body.layer = pip.layer;
        body.transform.SetParent(pip.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        var sr = body.GetComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = new Color(1f, 0.85f, 0.9f);
        sr.sortingOrder = 10;

        var col = pip.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.8f, 1f);
        col.offset = new Vector2(0f, 0.5f);

        // Feet marker for PlayerController's ground check: right at the pivot.
        var feet = new GameObject("GroundCheck");
        feet.transform.SetParent(pip.transform, false);
        feet.transform.localPosition = Vector3.zero;

        // My teammate's components. RequireComponent adds the Rigidbody2D automatically.
        var controller = pip.AddComponent<PlayerController>();
        pip.AddComponent<PlayerHealth>();
        pip.AddComponent<PlayerStarFragments>();
        pip.GetComponent<Rigidbody2D>().interpolation = RigidbodyInterpolation2D.Interpolate;

        // Their Inspector fields are private, so set them the way the Inspector does.
        var so = new SerializedObject(controller);
        so.FindProperty("inputActions").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<InputActionAsset>(PlayerControlsPath);
        so.FindProperty("groundCheck").objectReferenceValue = feet.transform;
        so.FindProperty("groundLayer").intValue = 1 << RequireLayer(GroundLayerName);
        so.ApplyModifiedPropertiesWithoutUndo();
        return pip;
    }

    internal static void AddSpawnPoint(string id, Vector2 position, bool isDefault)
    {
        var go = new GameObject($"SpawnPoint_{id}");
        go.transform.position = position;
        SetSpawnFields(go.AddComponent<SpawnPoint>(), id, isDefault);
    }

    internal static GameObject AddCheckpoint(string id, Vector2 position, Sprite square)
    {
        var go = new GameObject($"Checkpoint_{id}");
        go.transform.position = position; // On the floor: Pip's feet (her pivot) land here.

        // Trigger 1 tile wide and 2 tall, standing on the floor.
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1f, 2f);
        box.offset = new Vector2(0f, 1f);

        // A thin post so it's visible.
        var post = new GameObject("Post", typeof(SpriteRenderer));
        post.transform.SetParent(go.transform, false);
        post.transform.localPosition = new Vector3(0f, 1f, 0f);
        post.transform.localScale = new Vector3(0.25f, 2f, 1f);
        var sr = post.GetComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = new Color(0.6f, 0.6f, 0.6f);

        var checkpoint = go.AddComponent<Checkpoint>();
        SetSpawnFields(checkpoint, id, false);
        var so = new SerializedObject(checkpoint);
        so.FindProperty("spriteRenderer").objectReferenceValue = sr;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    private static void SetSpawnFields(SpawnPoint point, string id, bool isDefault)
    {
        var so = new SerializedObject(point);
        so.FindProperty("id").stringValue = id;
        so.FindProperty("isDefault").boolValue = isDefault;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------ Helpers

    /// <summary>A 16x16 white sprite (1 tile), so stand-in objects are visible.</summary>
    internal static Sprite GetOrCreatePlaceholderSprite()
    {
        if (!File.Exists(PlaceholderSpritePath))
        {
            var tex = new Texture2D(16, 16);
            var pixels = new Color32[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            File.WriteAllBytes(PlaceholderSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(PlaceholderSpritePath);
        }

        // Pixel-art import settings: Sprite, 16 PPU, no blur, no compression.
        var importer = (TextureImporter)AssetImporter.GetAtPath(PlaceholderSpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
    }

    /// <summary>Adds scenes to File > Build Profiles > Scene List, keeping the ones already there.</summary>
    internal static void AddScenesToBuildProfile(params string[] paths)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (string path in paths)
        {
            if (!scenes.Exists(s => s.path == path))
                scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets(); // Write the scene list to ProjectSettings/EditorBuildSettings.asset now.
    }

    /// <summary>Layer index by name. Logs an error (and falls back to Default) if it doesn't exist.</summary>
    internal static int RequireLayer(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            Debug.LogError($"Part1Setup: layer '{layerName}' is missing. Add it in Project Settings > Tags and Layers.");
            return 0;
        }
        return layer;
    }

    internal static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
