using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// EDITOR-ONLY tool that does the Part 1 setup for you (it lives in an "Editor" folder, so it is
/// never included in the built game).
///
/// Menu: Tools > Clown Who Cried > Part 1 > ...
///   - "Build GameSystems Prefab": Assets/Resources/GameSystems.prefab with GameManager,
///     SceneLoader, PlayerSaveBridge, PlayerDeathHandler and the Fader canvas.
///   - "Build Test Scenes": Assets/Scenes/Tests/CoreTest_A and CoreTest_B, each with ground,
///     a stand-in Pip, spawn points and a checkpoint, and adds them to the Build Profile scene list.
///   - "Run Everything": both of the above.
///
/// Running it again is safe: it overwrites what it made before and never touches other assets.
///
/// The test scenes use a STAND-IN Pip made from my teammate's components (PlayerController,
/// PlayerHealth, PlayerStarFragments) with a plain square sprite, because the real Pip prefab
/// isn't in the project yet. The project also has no "Ground"/"Player" layers yet, so the
/// stand-in Pip sits on the built-in "Ignore Raycast" layer and treats "Default" as ground,
/// which stops her ground check from hitting her own collider.
/// </summary>
public static class Part1Setup
{
    // ------------------------------------------------------------------ Paths

    private const string ResourcesFolder = "Assets/Resources";
    private const string PrefabPath = ResourcesFolder + "/GameSystems.prefab";
    private const string TestScenesFolder = "Assets/Scenes/Tests";
    private const string SceneAPath = TestScenesFolder + "/CoreTest_A.unity";
    private const string SceneBPath = TestScenesFolder + "/CoreTest_B.unity";
    private const string PlaceholderSpritePath = TestScenesFolder + "/Placeholder_White.png";
    private const string PlayerControlsPath = "Assets/Scripts/Player/PlayerControls.inputactions";

    // Layers that already exist in every Unity project (see the class comment).
    private const int DefaultLayer = 0;
    private const int IgnoreRaycastLayer = 2;

    // ------------------------------------------------------------------ Menu items

    [MenuItem("Tools/Clown Who Cried/Part 1/Run Everything")]
    public static void RunEverything()
    {
        BuildGameSystemsPrefab();
        BuildTestScenes();
        Debug.Log("Part1Setup: done.");
    }

    [MenuItem("Tools/Clown Who Cried/Part 1/Build GameSystems Prefab")]
    public static void BuildGameSystemsPrefab()
    {
        EnsureFolder(ResourcesFolder);

        // 1. Root object with the four core components.
        var root = new GameObject("GameSystems");
        root.AddComponent<GameManager>();
        root.AddComponent<SceneLoader>();
        root.AddComponent<PlayerSaveBridge>();
        root.AddComponent<PlayerDeathHandler>();

        // 2. Fader: a Canvas drawn on top of everything, with a CanvasGroup + ScreenFader.
        var fader = new GameObject("Fader", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        fader.transform.SetParent(root.transform, false);
        Canvas canvas = fader.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // Above every other UI.
        fader.AddComponent<GraphicRaycaster>(); // Lets the black screen block clicks while visible.
        fader.AddComponent<ScreenFader>();

        // 3. The black Image, stretched over the whole screen.
        var black = new GameObject("Black", typeof(RectTransform), typeof(Image));
        black.transform.SetParent(fader.transform, false);
        var rect = black.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        black.GetComponent<Image>().color = Color.black;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log($"Part1Setup: saved {PrefabPath}");
    }

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
        AddSpawnPoint("Start", new Vector2(-8f, -1f), isDefault: true);
        AddSpawnPoint("FromB", new Vector2(8f, -1f), isDefault: false);
        AddCheckpoint("CoreTest_A_CP1", new Vector2(2f, -1f), square);
        new GameObject("Part1 Test Driver").AddComponent<CoreSystemsTest>();
        EditorSceneManager.SaveScene(a, SceneAPath);

        // Scene B: a second level to test scene loading.
        Scene b = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildCommon(square);
        AddSpawnPoint("Start", new Vector2(-8f, -1f), isDefault: true);
        AddSpawnPoint("FromA", new Vector2(-3f, -1f), isDefault: false);
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
        EditorSceneManager.OpenScene(SceneAPath);
        EditorApplication.EnterPlaymode();
    }

    // ------------------------------------------------------------------ Scene pieces

    private static void BuildCommon(Sprite square)
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
        ground.layer = DefaultLayer;
        ground.transform.position = new Vector3(0f, -2f, 0f);
        ground.transform.localScale = new Vector3(40f, 1f, 1f);
        ground.GetComponent<SpriteRenderer>().sprite = square;
        ground.GetComponent<SpriteRenderer>().color = new Color(0.35f, 0.3f, 0.3f);
        ground.GetComponent<BoxCollider2D>().size = Vector2.one;

        BuildStandInPip(square);
    }

    private static void BuildStandInPip(Sprite square)
    {
        var pip = new GameObject("Pip (test stand-in)");
        pip.layer = IgnoreRaycastLayer;
        pip.transform.position = new Vector3(-8f, -1f, 0f);

        var sr = pip.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = new Color(1f, 0.85f, 0.9f);
        sr.sortingOrder = 10;

        var col = pip.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.8f, 1f);

        // Feet marker for PlayerController's ground check.
        var feet = new GameObject("GroundCheck");
        feet.transform.SetParent(pip.transform, false);
        feet.transform.localPosition = new Vector3(0f, -0.5f, 0f);

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
        so.FindProperty("groundLayer").intValue = 1 << DefaultLayer;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddSpawnPoint(string id, Vector2 position, bool isDefault)
    {
        var go = new GameObject($"SpawnPoint_{id}");
        go.transform.position = position;
        SetSpawnFields(go.AddComponent<SpawnPoint>(), id, isDefault);
    }

    private static void AddCheckpoint(string id, Vector2 position, Sprite square)
    {
        var go = new GameObject($"Checkpoint_{id}");
        go.transform.position = position; // Same height as Pip's pivot when she stands here.

        // The stand-in Pip's pivot is her centre, 0.5 tiles above the floor, so the trigger
        // and the post start 0.5 tiles below this object's position.
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1f, 2f);
        box.offset = new Vector2(0f, 0.5f);

        // A thin post so it's visible.
        var post = new GameObject("Post", typeof(SpriteRenderer));
        post.transform.SetParent(go.transform, false);
        post.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        post.transform.localScale = new Vector3(0.25f, 2f, 1f);
        var sr = post.GetComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = new Color(0.6f, 0.6f, 0.6f);

        var checkpoint = go.AddComponent<Checkpoint>();
        SetSpawnFields(checkpoint, id, false);
        var so = new SerializedObject(checkpoint);
        so.FindProperty("spriteRenderer").objectReferenceValue = sr;
        so.ApplyModifiedPropertiesWithoutUndo();
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
    private static Sprite GetOrCreatePlaceholderSprite()
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
    private static void AddScenesToBuildProfile(params string[] paths)
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

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
