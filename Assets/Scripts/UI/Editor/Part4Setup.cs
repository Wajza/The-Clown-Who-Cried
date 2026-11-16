using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using B = GameSystemsPrefabBuilder; // Shorter name for the shared UI helpers.

/// <summary>
/// EDITOR-ONLY tool for the Part 4 (UI + Journal) setup.
///
/// Menu: Tools > Clown Who Cried > Part 4 > ...
///   "Run Everything":
///     - placeholder art + pixel font (Assets/Art/Placeholders; only made if missing),
///     - rebuilds the GameSystems prefab (now with the HUD, menus, Journal and EventSystem),
///     - TEST journal pages (Assets/Resources/JournalPages/Test) and Journal cities for the test tasks,
///     - the Title scene (Assets/Scenes/Title) if it doesn't exist yet,
///     - Assets/Scenes/Tests/UITest,
///     - the Build Profile scene list, with Title FIRST (the game starts there).
///   "Rebuild Title Scene": throws the Title scene away and builds the placeholder one again.
/// </summary>
public static class Part4Setup
{
    private const string TitleScenePath = "Assets/Scenes/Title.unity";
    private const string TestScenePath = Part1Setup.TestScenesFolder + "/UITest.unity";
    private const string PageFolder = "Assets/Resources/JournalPages/Test";
    private const string TaskFolder = "Assets/Resources/Tasks/Test";
    private const float PickupX = 4f;

    [MenuItem("Tools/Clown Who Cried/Part 4/Run Everything")]
    public static void RunEverything()
    {
        GameSystemsPrefabBuilder.Build(); // Also makes the placeholder art.
        BuildTestData();
        if (!System.IO.File.Exists(TitleScenePath))
            BuildTitleScene();
        BuildTestScene();
        PutSceneFirst(TitleScenePath);
        Debug.Log("Part4Setup: done.");
    }

    [MenuItem("Tools/Clown Who Cried/Part 4/Rebuild Title Scene (overwrites it)")]
    public static void RebuildTitleScene()
    {
        if (EditorUtility.DisplayDialog("Rebuild Title Scene",
                "This replaces Assets/Scenes/Title.unity with the placeholder version. Continue?", "Rebuild", "Cancel"))
        {
            BuildTitleScene();
            PutSceneFirst(TitleScenePath);
        }
    }

    /// <summary>Command line: set up, check for missing scripts, then play UITest.</summary>
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

    // ------------------------------------------------------------------ Test data

    private static void BuildTestData()
    {
        Part1Setup.EnsureFolder(PageFolder);
        JournalPageData page1 = Page("JournalPage_Test_1", "test_page_1", 1, "A Test Page", "This page is only a test. It was found in UITest.");
        JournalPageData page2 = Page("JournalPage_Test_2", "test_page_2", 2, "Another Page", "Nobody should ever read this.");

        // The Part 3 test tasks: one per city, so the Journal's city boards can be checked.
        TaskData errand = AssetDatabase.LoadAssetAtPath<TaskData>($"{TaskFolder}/Task_Test_Errand.asset");
        TaskData kid = AssetDatabase.LoadAssetAtPath<TaskData>($"{TaskFolder}/Task_Test_Kid.asset");
        if (errand == null || kid == null)
            Debug.LogError("Part4Setup: run Part 3 > Run Everything first (the test tasks are missing).");
        else
        {
            errand.city = TaskData.City.Tumbleton;
            kid.city = TaskData.City.Wonderwick;
            errand.testOnly = kid.testOnly = true;
            EditorUtility.SetDirty(errand);
            EditorUtility.SetDirty(kid);
        }

        EditorUtility.SetDirty(page1);
        EditorUtility.SetDirty(page2);
        AssetDatabase.SaveAssets();
    }

    private static JournalPageData Page(string asset, string id, int order, string title, string text)
    {
        JournalPageData page = Part2Setup.LoadOrCreate<JournalPageData>(PageFolder, asset);
        page.id = id;
        page.order = order;
        page.title = title;
        page.text = text;
        page.testOnly = true;
        return page;
    }

    // ------------------------------------------------------------------ Title scene

    private static void BuildTitleScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cam = new GameObject("Main Camera", typeof(Camera));
        cam.tag = "MainCamera";
        cam.transform.position = new Vector3(0f, 0f, -10f);
        Camera c = cam.GetComponent<Camera>();
        c.orthographic = true;
        c.orthographicSize = 270f / 16f / 2f;
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = new Color(0.16f, 0.12f, 0.22f);

        GameObject canvas = B.CreateCanvas("TitleCanvas", null, sortingOrder: 0);
        canvas.AddComponent<GraphicRaycaster>();
        var title = canvas.AddComponent<TitleScreen>();

        // Placeholder logo: just text. The artist's logo can replace "Logo" (keep the name).
        TMP_Text logo = B.CreateText("Logo", canvas.transform, "THE CLOWN\nWHO CRIED", 16f, TextAlignmentOptions.Center);
        B.Place(logo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(400f, 40f));
        logo.color = new Color(1f, 0.85f, 0.3f);
        TMP_Text note = B.CreateText("PlaceholderNote", canvas.transform, "(placeholder title screen)",
            PlaceholderArt.FontSize, TextAlignmentOptions.Center);
        B.Place(note.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(400f, 10f));
        note.color = new Color(0.6f, 0.6f, 0.65f);

        RectTransform column = B.CreateEmpty("Buttons", canvas.transform);
        B.Place(column, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(120f, 4 * 18f));
        var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        Button newGame = B.CreateButton("NewGameButton", column, "New Game", title.OnNewGame);
        Button cont = B.CreateButton("ContinueButton", column, "Continue", title.OnContinue);
        Button settings = B.CreateButton("SettingsButton", column, "Settings", title.OnSettings);
        Button quit = B.CreateButton("QuitButton", column, "Quit", title.OnQuit);

        // "Start a new game?" box.
        Image confirm = B.CreateImage("ConfirmNewGamePanel", canvas.transform, new Color(0.1f, 0.08f, 0.15f, 0.97f));
        B.Place(confirm.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 70f));
        TMP_Text question = B.CreateText("Question", confirm.transform,
            "Start a new game?\nYour saved progress will be replaced.", PlaceholderArt.FontSize, TextAlignmentOptions.Center);
        B.Place(question.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(250f, 24f));
        Button yes = B.CreateButton("YesButton", confirm.transform, "Yes", title.OnConfirmNewGame);
        B.Place((RectTransform)yes.transform, new Vector2(0.5f, 0f), new Vector2(-45f, 10f), new Vector2(70f, 14f));
        Button no = B.CreateButton("NoButton", confirm.transform, "No", title.OnCancelNewGame);
        B.Place((RectTransform)no.transform, new Vector2(0.5f, 0f), new Vector2(45f, 10f), new Vector2(70f, 14f));
        confirm.gameObject.SetActive(false);

        var so = new SerializedObject(title);
        so.FindProperty("newGameButton").objectReferenceValue = newGame;
        so.FindProperty("continueButton").objectReferenceValue = cont;
        so.FindProperty("settingsButton").objectReferenceValue = settings;
        so.FindProperty("quitButton").objectReferenceValue = quit;
        so.FindProperty("confirmPanel").objectReferenceValue = confirm.gameObject;
        so.FindProperty("confirmYesButton").objectReferenceValue = yes;
        so.FindProperty("confirmNoButton").objectReferenceValue = no;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, TitleScenePath);
        Debug.Log($"Part4Setup: built {TitleScenePath}");
    }

    /// <summary>Adds the scene to the Build Profile list (if needed) and moves it to the top.</summary>
    private static void PutSceneFirst(string path)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == path);
        scenes.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
    }

    // ------------------------------------------------------------------ Test scene

    private static void BuildTestScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Sprite square = Part1Setup.GetOrCreatePlaceholderSprite();
        Part1Setup.BuildCommon(square); // Camera, floor, stand-in Pip at x = -8.
        Part1Setup.AddSpawnPoint("Start", new Vector2(-8f, Part1Setup.FloorY), isDefault: true);

        // Give the stand-in Pip my teammate's Tear Gauge and Blue Sight, so their HUD pieces show.
        // (No coin thrower: it needs a Star Coin prefab. The test checks the coins HUD hides instead.)
        GameObject pip = GameObject.Find("Pip (test stand-in)");
        pip.AddComponent<TearGauge>();
        pip.AddComponent<PlayerBlueSight>();

        // A torn page lying on the floor.
        var pickup = new GameObject("JournalPage_Test_1_Pickup", typeof(BoxCollider2D));
        pickup.transform.position = new Vector3(PickupX, Part1Setup.FloorY, 0f);
        var box = pickup.GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = Vector2.one;
        box.offset = new Vector2(0f, 0.5f);
        var sprite = new GameObject("Sprite", typeof(SpriteRenderer));
        sprite.transform.SetParent(pickup.transform, false);
        sprite.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        sprite.GetComponent<SpriteRenderer>().sprite = PlaceholderArt.PageSprite;
        sprite.GetComponent<SpriteRenderer>().sortingOrder = 5;

        JournalPageData page1 = AssetDatabase.LoadAssetAtPath<JournalPageData>($"{PageFolder}/JournalPage_Test_1.asset");
        var pagePickup = pickup.AddComponent<JournalPagePickup>();
        B.SetField(pagePickup, "page", page1);

        var boss = new GameObject("TestBoss").AddComponent<TestBossHealthSource>();

        var driver = new GameObject("Part4 Test Driver").AddComponent<UISystemTest>();
        var so = new SerializedObject(driver);
        so.FindProperty("page1").objectReferenceValue = page1;
        so.FindProperty("errand").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TaskData>($"{TaskFolder}/Task_Test_Errand.asset");
        so.FindProperty("kidTask").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TaskData>($"{TaskFolder}/Task_Test_Kid.asset");
        so.FindProperty("someDialogue").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<DialogueData>("Assets/Data/Dialogue/Test/Dialogue_Test_Kid.asset");
        so.FindProperty("boss").objectReferenceValue = boss;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, TestScenePath);
        Part1Setup.AddScenesToBuildProfile(TestScenePath);
        Debug.Log($"Part4Setup: built {TestScenePath}");
    }
}
