using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// EDITOR-ONLY tool for the Part 3 (Tasks) setup.
///
/// Menu: Tools > Clown Who Cried > Part 3 > Run Everything
///   - rebuilds the GameSystems prefab (now with TaskManager and the TaskCanvas),
///   - creates TEST tasks in Assets/Resources/Tasks/Test and their conversations in
///     Assets/Data/Dialogue/TaskTest,
///   - builds Assets/Scenes/Tests/TaskTest (stand-in Pip, a Mayor who gives an errand,
///     a Baker, a Kid and a gate that opens when both test tasks are done),
///   - adds the scene to the Build Profile scene list.
///
/// The test tasks only check the system. Make the real ones (Create > Clown Who Cried > Tasks)
/// in Assets/Resources/Tasks.
/// </summary>
public static class Part3Setup
{
    private const string TaskFolder = "Assets/Resources/Tasks/Test";
    private const string DialogueFolder = "Assets/Data/Dialogue/TaskTest";
    private const string ScenePath = Part1Setup.TestScenesFolder + "/TaskTest.unity";

    // Where things stand in the test scene (x positions; everyone is on the floor).
    private const float MayorX = -3f, BakerX = 1f, KidX = 4f, GateX = 9f;

    [MenuItem("Tools/Clown Who Cried/Part 3/Run Everything")]
    public static void RunEverything()
    {
        GameSystemsPrefabBuilder.Build();
        BuildTestData();
        BuildTestScene();
        Debug.Log("Part3Setup: done.");
    }

    /// <summary>Command line: set up, check for missing scripts, then play TaskTest.</summary>
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

    // ------------------------------------------------------------------ Test data

    /// <summary>Every test asset the scene needs, loaded fresh from disk.</summary>
    private class TestData
    {
        public TaskData errand, kidTask;
        public DialogueData mayorAsk, mayorWaiting, mayorThanks, baker, kid;
    }

    private static TestData LoadTestData() => new TestData
    {
        errand = Task("Task_Test_Errand"),
        kidTask = Task("Task_Test_Kid"),
        mayorAsk = Dialogue("Dialogue_TaskTest_MayorAsk"),
        mayorWaiting = Dialogue("Dialogue_TaskTest_MayorWaiting"),
        mayorThanks = Dialogue("Dialogue_TaskTest_MayorThanks"),
        baker = Dialogue("Dialogue_TaskTest_Baker"),
        kid = Dialogue("Dialogue_TaskTest_Kid"),
    };

    private static TaskData Task(string name) => Part2Setup.LoadOrCreate<TaskData>(TaskFolder, name);
    private static DialogueData Dialogue(string name) => Part2Setup.LoadOrCreate<DialogueData>(DialogueFolder, name);

    private static void BuildTestData()
    {
        Part1Setup.EnsureFolder(TaskFolder);
        Part1Setup.EnsureFolder(DialogueFolder);
        var changed = new List<Object>();

        // ---- Speakers.
        DialogueSpeaker mayor = Speaker("Speaker_TaskTestMayor", "Mayor (test)", new Color(1f, 0.8f, 0.4f));
        DialogueSpeaker baker = Speaker("Speaker_TaskTestBaker", "Baker (test)", new Color(1f, 0.6f, 0.7f));

        // ---- Conversations.
        TestData d = LoadTestData();
        DialogueData accept = Dialogue("Dialogue_TaskTest_MayorAccept");
        DialogueData decline = Dialogue("Dialogue_TaskTest_MayorDecline");

        Fill(d.mayorAsk, mayor, "Pip! Could you run an errand to the baker for me?");
        d.mayorAsk.choices = new List<DialogueData.Choice>
        {
            new DialogueData.Choice { text = "Sure!", setFlag = "test_errand_accepted", next = accept },
            new DialogueData.Choice { text = "Not now.", setFlag = "", next = decline },
        };
        Fill(accept, mayor, "Thank you! Go and see the baker.");
        Fill(decline, mayor, "Alright. Ask me later.");
        Fill(d.mayorWaiting, mayor, "Have you been to the baker yet?");
        Fill(d.mayorThanks, mayor, "Thanks for your help, Pip!");
        Fill(d.baker, baker, "Fresh bread! Tell the mayor it's ready.");
        Fill(d.kid, null, "(The kid waves at you.)");
        changed.AddRange(new Object[] { d.mayorAsk, accept, decline, d.mayorWaiting, d.mayorThanks, d.baker, d.kid });

        // ---- Objectives and tasks.
        TalkToNpcObjective talkBaker = Objective("Objective_Test_TalkBaker", "baker", "Talk to the baker");
        TalkToNpcObjective reportMayor = Objective("Objective_Test_ReportMayor", "mayor", "Report back to the mayor");
        TalkToNpcObjective talkKid = Objective("Objective_Test_TalkKid", "kid", "Say hi to the kid");

        SetTask(d.errand, "test_errand", "The Mayor's Errand", "The mayor needs something from the baker.",
            "test_errand_reward", talkBaker, reportMayor);
        SetTask(d.kidTask, "test_kid", "Say Hi", "Be friendly.", "", talkKid);
        changed.AddRange(new Object[] { mayor, baker, talkBaker, reportMayor, talkKid, d.errand, d.kidTask });

        foreach (Object asset in changed)
            EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    private static DialogueSpeaker Speaker(string asset, string displayName, Color color)
    {
        DialogueSpeaker s = Part2Setup.LoadOrCreate<DialogueSpeaker>(DialogueFolder, asset);
        s.displayName = displayName;
        s.nameColor = color;
        s.portrait = null;
        return s;
    }

    private static void Fill(DialogueData dialogue, DialogueSpeaker speaker, string text)
    {
        dialogue.lines = new List<DialogueData.Line> { new DialogueData.Line { speaker = speaker, text = text } };
        dialogue.choices = new List<DialogueData.Choice>();
        dialogue.setFlagWhenFinished = "";
    }

    private static TalkToNpcObjective Objective(string asset, string npcId, string description)
    {
        var o = Part2Setup.LoadOrCreate<TalkToNpcObjective>(TaskFolder, asset);
        o.npcId = npcId;
        o.description = description;
        return o;
    }

    private static void SetTask(TaskData task, string id, string title, string description,
        string flagWhenComplete, params TaskObjective[] objectives)
    {
        task.id = id;
        task.title = title;
        task.description = description;
        task.setFlagWhenComplete = flagWhenComplete;
        task.objectives = new List<TaskObjective>(objectives);
        task.testOnly = true; // Kept out of the real Journal.
    }

    // ------------------------------------------------------------------ Test scene

    private static void BuildTestScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Sprite square = Part1Setup.GetOrCreatePlaceholderSprite();
        TestData d = LoadTestData(); // After NewScene, so the references are valid.
        Part1Setup.BuildCommon(square); // Camera, floor, stand-in Pip at x = -8.
        Part1Setup.AddSpawnPoint("Start", new Vector2(-8f, Part1Setup.FloorY), isDefault: true);

        // Mayor: asks first, then waits, then thanks (most specific condition at the top).
        NpcTalk mayor = Part2Setup.AddNpc("NPC_TestMayor", "mayor", MayorX, new Color(0.9f, 0.6f, 0.3f), square, d.mayorAsk,
            new NpcTalk.ConditionalDialogue { requiredFlag = d.errand.DoneFlag, dialogue = d.mayorThanks },
            new NpcTalk.ConditionalDialogue { requiredFlag = d.errand.ActiveFlag, dialogue = d.mayorWaiting });
        var giver = mayor.gameObject.AddComponent<TaskGiver>();
        var so = new SerializedObject(giver);
        so.FindProperty("task").objectReferenceValue = d.errand;
        so.FindProperty("requiredFlag").stringValue = "test_errand_accepted";
        so.ApplyModifiedPropertiesWithoutUndo();

        Part2Setup.AddNpc("NPC_TestBaker", "baker", BakerX, new Color(0.9f, 0.5f, 0.6f), square, d.baker);
        Part2Setup.AddNpc("NPC_TestKid", "kid", KidX, new Color(0.5f, 0.8f, 0.5f), square, d.kid);

        AddGate(square, d.errand, d.kidTask);

        var driver = new GameObject("Part3 Test Driver").AddComponent<TaskSystemTest>();
        so = new SerializedObject(driver);
        so.FindProperty("errand").objectReferenceValue = d.errand;
        so.FindProperty("kidTask").objectReferenceValue = d.kidTask;
        so.FindProperty("mayorThanks").objectReferenceValue = d.mayorThanks;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        Part1Setup.AddScenesToBuildProfile(ScenePath);
        Debug.Log($"Part3Setup: built {ScenePath}");
    }

    /// <summary>A gate on the floor: a solid red wall while closed, a green strip once open.</summary>
    private static void AddGate(Sprite square, params TaskData[] required)
    {
        var gate = new GameObject("TaskGate_Test");
        gate.transform.position = new Vector3(GateX, Part1Setup.FloorY, 0f);

        var wall = new GameObject("Wall", typeof(SpriteRenderer), typeof(BoxCollider2D));
        wall.transform.SetParent(gate.transform, false);
        wall.transform.localPosition = new Vector3(0f, 1.5f, 0f); // 3 tall, standing on the floor.
        wall.transform.localScale = new Vector3(1f, 3f, 1f);
        var wallSprite = wall.GetComponent<SpriteRenderer>();
        wallSprite.sprite = square;
        wallSprite.color = new Color(0.8f, 0.25f, 0.25f);
        wall.GetComponent<BoxCollider2D>().size = Vector2.one;

        var open = new GameObject("OpenMarker", typeof(SpriteRenderer));
        open.transform.SetParent(gate.transform, false);
        open.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        open.transform.localScale = new Vector3(1f, 0.2f, 1f);
        var openSprite = open.GetComponent<SpriteRenderer>();
        openSprite.sprite = square;
        openSprite.color = new Color(0.3f, 0.9f, 0.4f);
        open.SetActive(false);

        var taskGate = gate.AddComponent<TaskGate>();
        var so = new SerializedObject(taskGate);
        SerializedProperty tasks = so.FindProperty("requiredTasks");
        tasks.arraySize = required.Length;
        for (int i = 0; i < required.Length; i++)
            tasks.GetArrayElementAtIndex(i).objectReferenceValue = required[i];
        SerializedProperty closed = so.FindProperty("activeWhileClosed");
        closed.arraySize = 1;
        closed.GetArrayElementAtIndex(0).objectReferenceValue = wall;
        SerializedProperty opened = so.FindProperty("activeWhenOpen");
        opened.arraySize = 1;
        opened.GetArrayElementAtIndex(0).objectReferenceValue = open;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
