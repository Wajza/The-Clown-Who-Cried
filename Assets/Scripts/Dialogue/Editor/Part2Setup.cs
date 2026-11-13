using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// EDITOR-ONLY tool for the Part 2 (Dialogue) setup.
///
/// Menu: Tools > Clown Who Cried > Part 2 > ...
///   1. "Import TMP Essentials": TextMeshPro's default font and settings (Assets/TextMesh Pro).
///      Needed once per project, before the dialogue box can show text.
///   2. "Run Everything":
///        - rebuilds the GameSystems prefab (now with SystemInput, InteractionInput,
///          DialogueRunner and the DialogueCanvas),
///        - creates TEST dialogue assets in Assets/Data/Dialogue/Test,
///        - builds Assets/Scenes/Tests/DialogueTest (stand-in Pip, a test Mayor and Kid),
///        - adds the scene to the Build Profile scene list.
///
/// The test dialogue is only for checking the system. Write the real conversations as new
/// assets (Create > Clown Who Cried > Dialogue) in Assets/Data/Dialogue.
/// </summary>
public static class Part2Setup
{
    private const string TmpEssentialsPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
    private const string TestDataFolder = "Assets/Data/Dialogue/Test";
    private const string ScenePath = Part1Setup.TestScenesFolder + "/DialogueTest.unity";

    // ------------------------------------------------------------------ TextMeshPro

    [MenuItem("Tools/Clown Who Cried/Part 2/1. Import TMP Essentials")]
    public static void ImportTmpEssentials()
    {
        if (TmpEssentialsPresent())
        {
            Debug.Log("Part2Setup: TMP Essential Resources are already in the project.");
            return;
        }
        AssetDatabase.ImportPackage(TmpEssentialsPackage, interactive: false);
    }

    /// <summary>Command line: import TMP Essentials, then close Unity (0 = ok, 1 = failed).</summary>
    public static void ImportTmpEssentialsFromCommandLine()
    {
        if (TmpEssentialsPresent())
        {
            EditorApplication.Exit(0);
            return;
        }

        AssetDatabase.importPackageCompleted += _ => EditorApplication.Exit(0);
        AssetDatabase.importPackageFailed += (_, error) =>
        {
            Debug.LogError($"Part2Setup: TMP import failed: {error}");
            EditorApplication.Exit(1);
        };
        AssetDatabase.ImportPackage(TmpEssentialsPackage, interactive: false);
    }

    private static bool TmpEssentialsPresent() => AssetDatabase.FindAssets("t:TMP_Settings").Length > 0;

    // ------------------------------------------------------------------ Everything else

    [MenuItem("Tools/Clown Who Cried/Part 2/2. Run Everything")]
    public static void RunEverything()
    {
        if (!TmpEssentialsPresent())
        {
            Debug.LogError("Part2Setup: run '1. Import TMP Essentials' first.");
            return;
        }

        GameSystemsPrefabBuilder.Build();
        BuildTestDialogue();
        BuildTestScene();
        Debug.Log("Part2Setup: done.");
    }

    /// <summary>Command line: set up, check for missing scripts, then play DialogueTest.</summary>
    public static void RunTestsFromCommandLine()
    {
        RunEverything();
        if (Part1Setup.CheckMissingScripts() > 0)
        {
            EditorApplication.Exit(1);
            return;
        }
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.EnterPlaymode();
    }

    // ------------------------------------------------------------------ Test dialogue assets

    private class TestDialogue
    {
        public DialogueData mayorFirst, mayorYes, mayorNo, mayorAgain, kid;
    }

    /// <summary>
    /// Loads the test conversations fresh from disk. Needed after opening a new scene: Unity
    /// unloads assets that nothing in the scene uses, and older references then point at nothing.
    /// </summary>
    private static TestDialogue LoadTestDialogue()
    {
        var d = new TestDialogue
        {
            mayorFirst = Load("Dialogue_Test_MayorFirst"),
            mayorYes = Load("Dialogue_Test_MayorYes"),
            mayorNo = Load("Dialogue_Test_MayorNo"),
            mayorAgain = Load("Dialogue_Test_MayorAgain"),
            kid = Load("Dialogue_Test_Kid"),
        };
        if (d.mayorFirst == null || d.mayorAgain == null || d.kid == null)
            Debug.LogError("Part2Setup: test dialogue assets could not be loaded.");
        return d;

        DialogueData Load(string name) =>
            AssetDatabase.LoadAssetAtPath<DialogueData>($"{TestDataFolder}/{name}.asset");
    }

    private static void BuildTestDialogue()
    {
        Part1Setup.EnsureFolder(TestDataFolder);
        Sprite square = Part1Setup.GetOrCreatePlaceholderSprite();

        DialogueSpeaker pip = LoadOrCreate<DialogueSpeaker>("Speaker_Pip");
        pip.displayName = "Pip";
        pip.nameColor = new Color(0.6f, 0.8f, 1f);
        pip.portrait = null; // Tests the "no portrait" layout.

        DialogueSpeaker mayor = LoadOrCreate<DialogueSpeaker>("Speaker_TestMayor");
        mayor.displayName = "Mayor (test)";
        mayor.nameColor = new Color(1f, 0.8f, 0.4f);
        mayor.portrait = square; // Placeholder face.

        var d = new TestDialogue
        {
            mayorFirst = LoadOrCreate<DialogueData>("Dialogue_Test_MayorFirst"),
            mayorYes = LoadOrCreate<DialogueData>("Dialogue_Test_MayorYes"),
            mayorNo = LoadOrCreate<DialogueData>("Dialogue_Test_MayorNo"),
            mayorAgain = LoadOrCreate<DialogueData>("Dialogue_Test_MayorAgain"),
            kid = LoadOrCreate<DialogueData>("Dialogue_Test_Kid"),
        };

        Fill(d.mayorFirst,
            Line(mayor, "Hello, Pip! This is a <color=#ffd34d>test</color> conversation."),
            Line(pip, "Hi! Can I help?"),
            Line(mayor, "Will you help me test the choices?"));
        d.mayorFirst.choices = new List<DialogueData.Choice>
        {
            new DialogueData.Choice { text = "Yes!", setFlag = "test_said_yes", next = d.mayorYes },
            new DialogueData.Choice { text = "Not now.", setFlag = "", next = d.mayorNo },
        };
        d.mayorFirst.setFlagWhenFinished = "test_met_mayor";

        Fill(d.mayorYes, Line(mayor, "Wonderful! Thank you."));
        Fill(d.mayorNo, Line(mayor, "Oh... maybe later, then."));
        Fill(d.mayorAgain, Line(mayor, "Nice to see you again, Pip."));
        Fill(d.kid, Line(null, "(The kid is too busy juggling to talk.)")); // Narration: no speaker.

        foreach (Object asset in new Object[] { pip, mayor, d.mayorFirst, d.mayorYes, d.mayorNo, d.mayorAgain, d.kid })
            EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    private static T LoadOrCreate<T>(string name) where T : ScriptableObject => LoadOrCreate<T>(TestDataFolder, name);

    /// <summary>Loads the asset if it exists (keeping its ID, so scenes stay linked), or creates it.</summary>
    internal static T LoadOrCreate<T>(string folder, string name) where T : ScriptableObject
    {
        string path = $"{folder}/{name}.asset";
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    private static DialogueData.Line Line(DialogueSpeaker speaker, string text) =>
        new DialogueData.Line { speaker = speaker, text = text };

    private static void Fill(DialogueData dialogue, params DialogueData.Line[] lines)
    {
        dialogue.lines = new List<DialogueData.Line>(lines);
        dialogue.choices = new List<DialogueData.Choice>();
        dialogue.setFlagWhenFinished = "";
    }

    // ------------------------------------------------------------------ Test scene

    private static void BuildTestScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Sprite square = Part1Setup.GetOrCreatePlaceholderSprite();
        TestDialogue d = LoadTestDialogue(); // After NewScene, so the references are valid.
        Part1Setup.BuildCommon(square); // Camera, floor, stand-in Pip at x = -8.
        Part1Setup.AddSpawnPoint("Start", new Vector2(-8f, Part1Setup.FloorY), isDefault: true);

        NpcTalk mayor = AddNpc("NPC_TestMayor", "mayor", 0f, new Color(0.9f, 0.6f, 0.3f), square, d.mayorFirst,
            new NpcTalk.ConditionalDialogue { requiredFlag = "test_met_mayor", dialogue = d.mayorAgain });
        NpcTalk kid = AddNpc("NPC_TestKid", "kid", 3f, new Color(0.5f, 0.8f, 0.5f), square, d.kid);

        var driver = new GameObject("Part2 Test Driver").AddComponent<DialogueSystemTest>();
        var so = new SerializedObject(driver);
        so.FindProperty("mayor").objectReferenceValue = mayor;
        so.FindProperty("kid").objectReferenceValue = kid;
        so.FindProperty("mayorFirst").objectReferenceValue = d.mayorFirst;
        so.FindProperty("mayorAgain").objectReferenceValue = d.mayorAgain;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        Part1Setup.AddScenesToBuildProfile(ScenePath);
        Debug.Log($"Part2Setup: built {ScenePath}");
    }

    /// <summary>
    /// An NPC standing on the floor: pivot at the feet, a 1 x 1.5 tile body, a talk range
    /// 5 tiles wide, and a yellow "!" prompt above the head.
    /// </summary>
    internal static NpcTalk AddNpc(string name, string npcId, float x, Color color, Sprite square,
        DialogueData defaultDialogue, params NpcTalk.ConditionalDialogue[] conditional)
    {
        var npc = new GameObject(name);
        npc.transform.position = new Vector3(x, Part1Setup.FloorY, 0f);

        var body = new GameObject("Body", typeof(SpriteRenderer));
        body.transform.SetParent(npc.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        body.transform.localScale = new Vector3(1f, 1.5f, 1f);
        var sr = body.GetComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = color;
        sr.sortingOrder = 5;

        var prompt = new GameObject("Prompt", typeof(SpriteRenderer));
        prompt.transform.SetParent(npc.transform, false);
        prompt.transform.localPosition = new Vector3(0f, 2f, 0f);
        prompt.transform.localScale = new Vector3(0.25f, 0.4f, 1f);
        var promptSprite = prompt.GetComponent<SpriteRenderer>();
        promptSprite.sprite = square;
        promptSprite.color = new Color(1f, 0.9f, 0.2f);
        promptSprite.sortingOrder = 20;

        var range = npc.AddComponent<BoxCollider2D>();
        range.isTrigger = true;
        range.size = new Vector2(5f, 2f);
        range.offset = new Vector2(0f, 1f);

        var talk = npc.AddComponent<NpcTalk>();
        var so = new SerializedObject(talk);
        so.FindProperty("npcId").stringValue = npcId;
        so.FindProperty("defaultDialogue").objectReferenceValue = defaultDialogue;
        so.FindProperty("prompt").objectReferenceValue = prompt; // Field from the Interactable base class.
        so.FindProperty("spriteToFlip").objectReferenceValue = sr;

        SerializedProperty list = so.FindProperty("conditionalDialogues");
        list.arraySize = conditional.Length;
        for (int i = 0; i < conditional.Length; i++)
        {
            SerializedProperty item = list.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("requiredFlag").stringValue = conditional[i].requiredFlag ?? "";
            item.FindPropertyRelative("blockedByFlag").stringValue = conditional[i].blockedByFlag ?? "";
            item.FindPropertyRelative("dialogue").objectReferenceValue = conditional[i].dialogue;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        prompt.SetActive(false);
        return talk;
    }
}
