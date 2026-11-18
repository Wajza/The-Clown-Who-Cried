using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using B = GameSystemsPrefabBuilder;
using P6 = Part6Setup;

/// <summary>
/// EDITOR-ONLY tool for Part 8: cutscenes, the Chortle encounter and the End of Demo.
///
/// Menu: Tools > Clown Who Cried > Part 8 > Run Everything
///   1. Data (only CREATED if missing; your edits are never overwritten):
///        Assets/Data/Dialogue/Cutscenes            Chortle's speaker + the cutscene conversations
///        Assets/Cutscenes                           Timeline_Opening, Timeline_ChortleIntro, Timeline_ChortleOutro
///        Assets/Resources/JournalPages              JournalPage_02_JestersTear (the reward page)
///   2. Levels (each only once: skipped if its Part 8 object exists):
///        Tumbleton_PipHouse   "Cutscenes (Part 8)": the opening cutscene, played once on New Game
///        ChortleArena         "Boss (Part 8)": PlaceholderBoss, intro/outro, BossFightFlow, exit door
///        EndOfDemo            "End Of Demo (Part 8)": credits + EndOfDemoSequence; no autosave
///   3. Assets/Scenes/Tests/CutsceneTest + driver.
///
/// To edit a cutscene: select its PlayableDirector in the level and open Window > Sequencing >
/// Timeline. Dialogue clips sit on the "Dialogue" track; add Activation/Animation tracks freely.
/// </summary>
public static class Part8Setup
{
    private const string DialogueFolder = "Assets/Data/Dialogue/Cutscenes";
    private const string TimelineFolder = "Assets/Cutscenes";
    private const string PageFolder = "Assets/Resources/JournalPages";
    private const string TestScenePath = Part1Setup.TestScenesFolder + "/CutsceneTest.unity";

    [MenuItem("Tools/Clown Who Cried/Part 8/Run Everything")]
    public static void RunEverything()
    {
        GameSystemsPrefabBuilder.Build();
        BuildData();
        AddOpening();
        AddBoss();
        AddEndOfDemo();
        BuildTestScene();
        Debug.Log("Part8Setup: done.");
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

    private static TimelineAsset Timeline(string name) =>
        AssetDatabase.LoadAssetAtPath<TimelineAsset>($"{TimelineFolder}/{name}.playable");

    private static JournalPageData RewardPage() => P6.Asset<JournalPageData>(PageFolder, "JournalPage_02_JestersTear");

    private static void BuildData()
    {
        Part1Setup.EnsureFolder(DialogueFolder);
        Part1Setup.EnsureFolder(TimelineFolder);

        DialogueSpeaker pip = P6.Asset<DialogueSpeaker>("Assets/Data/Dialogue/Prologue", "Speaker_Pip");
        DialogueSpeaker chortle = P6.CreateIfMissing<DialogueSpeaker>(DialogueFolder, "Speaker_Chortle",
            s => { s.displayName = "Grand Jester Chortle"; s.nameColor = new Color(0.85f, 0.5f, 1f); });

        // ---- Opening (Pip's house, New Game)
        DialogueData opening1 = Talk("Dialogue_CS_Opening_1",
            (null, "Tumbleton. The happiest town in the whole wide world."),
            (null, "Everyone here smiles. All day. Every day."));
        DialogueData opening2 = Talk("Dialogue_CS_Opening_2",
            (pip, "Mmm... morning already? Something feels... wrong with me."));

        // ---- Chortle intro (Laugh Tower)
        DialogueData intro1 = Talk("Dialogue_CS_ChortleIntro_1",
            (chortle, "Welcome to the Laugh Tower, little crybaby!"),
            (chortle, "In Wonderwick nobody cries. Let's turn that frown upside down... PERMANENTLY!"));
        DialogueData intro2 = Talk("Dialogue_CS_ChortleIntro_2",
            (pip, "I'm not afraid of you!"));

        // ---- Chortle outro
        DialogueData outro1 = Talk("Dialogue_CS_ChortleOutro_1",
            (null, "(Chortle's mask cracks. Behind it... a single tear.)"),
            (chortle, "I... I haven't cried in a hundred years."));
        DialogueData outro2 = Talk("Dialogue_CS_ChortleOutro_2",
            (pip, "It's okay. Tears just mean you feel something."),
            (null, "(A torn page flutters down from the top of the tower. Pip feels a little stronger.)"));

        MakeTimeline("Timeline_Opening", opening1, opening2);
        MakeTimeline("Timeline_ChortleIntro", intro1, intro2);
        MakeTimeline("Timeline_ChortleOutro", outro1, outro2);

        P6.CreateIfMissing<JournalPageData>(PageFolder, "JournalPage_02_JestersTear", page =>
        {
            page.id = "page_2";
            page.order = 2;
            page.title = "The Jester's Tear";
            page.text = "Even the Grand Jester can cry. Maybe a tear isn't something to fix... " +
                        "maybe it's something to understand. (Placeholder text: write the real page here.)";
        });

        AssetDatabase.SaveAssets();

        DialogueData Talk(string asset, params (DialogueSpeaker who, string text)[] lines) =>
            P6.CreateIfMissing<DialogueData>(DialogueFolder, asset, d =>
            {
                foreach ((DialogueSpeaker who, string text) in lines)
                    d.lines.Add(new DialogueData.Line { speaker = who, text = text });
            });
    }

    /// <summary>A Timeline with one Dialogue track: one short clip per conversation, 1 s apart.</summary>
    private static void MakeTimeline(string name, params DialogueData[] conversations)
    {
        if (Timeline(name) != null)
            return; // Already exists (maybe edited by hand): leave it.

        var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, $"{TimelineFolder}/{name}.playable");
        var track = timeline.CreateTrack<DialogueTrack>(null, "Dialogue");
        double time = 0.5;
        foreach (DialogueData conversation in conversations)
        {
            TimelineClip clip = track.CreateClip<DialogueClip>();
            clip.start = time;
            clip.duration = 0.5;
            clip.displayName = conversation.name.Replace("Dialogue_CS_", "");
            ((DialogueClip)clip.asset).dialogue = conversation;
            EditorUtility.SetDirty(clip.asset);
            time += 1.0;
        }
        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();
    }

    // ------------------------------------------------------------------ Levels

    private static Scene? Open(string scene, string rootName, out Transform root)
    {
        root = null;
        string path = $"{Part5Setup.LevelsFolder}/{scene}.unity";
        if (!File.Exists(path))
        {
            Debug.LogError($"Part8Setup: {path} is missing. Run Part 5 > Run Everything first.");
            return null;
        }
        Scene opened = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        if (GameObject.Find(rootName) != null)
        {
            Debug.Log($"Part8Setup: {scene} already has '{rootName}'; left unchanged.");
            return null;
        }
        root = new GameObject(rootName).transform;
        return opened;
    }

    private static PlayableDirector Director(Transform root, string name, TimelineAsset timeline)
    {
        var go = new GameObject(name, typeof(PlayableDirector));
        go.transform.SetParent(root);
        var director = go.GetComponent<PlayableDirector>();
        director.playableAsset = timeline;
        director.playOnAwake = false;
        director.extrapolationMode = DirectorWrapMode.None;
        return director;
    }

    private static void AddOpening()
    {
        Scene? scene = Open("Tumbleton_PipHouse", "Cutscenes (Part 8)", out Transform root);
        if (scene == null)
            return;

        PlayableDirector director = Director(root, "Cutscene_Opening", Timeline("Timeline_Opening"));
        var trigger = director.gameObject.AddComponent<CutsceneTrigger>();
        var so = new SerializedObject(trigger);
        so.FindProperty("cutscene").objectReferenceValue = director;
        so.FindProperty("seenFlag").stringValue = "cs_opening_seen";
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene.Value);
    }

    private static void AddBoss()
    {
        Scene? scene = Open("ChortleArena", "Boss (Part 8)", out Transform root);
        if (scene == null)
            return;
        float bossX = P6.TakeMarker("Chortle (Part 8)", 24f);

        // PLACEHOLDER boss until my teammate's Chortle prefab exists (see ChortleFight).
        var bossGo = new GameObject("PlaceholderBoss (replace with Chortle)", typeof(BoxCollider2D));
        bossGo.transform.SetParent(root);
        bossGo.transform.position = new Vector3(bossX, 0f, 0f);
        bossGo.layer = Part1Setup.RequireLayer("Enemy");
        var hitbox = bossGo.GetComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        hitbox.size = new Vector2(3f, 4f);
        hitbox.offset = new Vector2(0f, 2f);
        GameObject body = P6.Box(bossGo.transform, "Sprite", new Vector2(0f, 2f), new Vector2(3f, 4f), new Color(0.6f, 0.3f, 0.8f), 5);
        P6.Box(bossGo.transform, "Mask", new Vector2(0f, 3f), new Vector2(1.8f, 1f), Color.white, 6);
        var boss = bossGo.AddComponent<PlaceholderBoss>();
        B.SetField(boss, "sprite", body.GetComponent<SpriteRenderer>());

        PlayableDirector intro = Director(root, "Cutscene_ChortleIntro", Timeline("Timeline_ChortleIntro"));
        PlayableDirector outro = Director(root, "Cutscene_ChortleOutro", Timeline("Timeline_ChortleOutro"));

        var flow = new GameObject("BossFightFlow").AddComponent<BossFightFlow>();
        flow.transform.SetParent(root);
        var so = new SerializedObject(flow);
        so.FindProperty("boss").objectReferenceValue = boss;
        so.FindProperty("intro").objectReferenceValue = intro;
        so.FindProperty("outro").objectReferenceValue = outro;
        so.FindProperty("rewardPage").objectReferenceValue = RewardPage();
        so.ApplyModifiedPropertiesWithoutUndo();

        // The way back out: locked until Chortle is beaten.
        DialogueData locked = P6.CreateIfMissing<DialogueData>(DialogueFolder, "Dialogue_Arena_DoorLocked", d =>
            d.lines.Add(new DialogueData.Line
            {
                speaker = P6.Asset<DialogueSpeaker>(DialogueFolder, "Speaker_Chortle"),
                text = "Leaving already? The show has only just begun!",
            }));
        var door = new GameObject("ExitToHub", typeof(BoxCollider2D));
        door.transform.SetParent(root);
        door.transform.position = new Vector3(1.5f, 0f, 0f);
        var range = door.GetComponent<BoxCollider2D>();
        range.isTrigger = true;
        range.size = new Vector2(2f, 2.5f);
        range.offset = new Vector2(0f, 1.25f);
        var art = new GameObject("Sprite", typeof(SpriteRenderer));
        art.transform.SetParent(door.transform, false);
        art.GetComponent<SpriteRenderer>().sprite = PlaceholderArt.DoorSprite;
        art.GetComponent<SpriteRenderer>().sortingOrder = 1;
        var exit = door.AddComponent<Door>();
        so = new SerializedObject(exit);
        so.FindProperty("destination.sceneName").stringValue = "Wonderwick_Hub";
        so.FindProperty("destination.spawnPointId").stringValue = "FromTower";
        so.FindProperty("requirement.requiredFlag").stringValue = BossFightFlow.DefeatedFlag;
        so.FindProperty("requirement.lockedDialogue").objectReferenceValue = locked;
        so.FindProperty("prompt").objectReferenceValue = P6.Prompt(door.transform, 2.6f);
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene.Value);
    }

    private static void AddEndOfDemo()
    {
        Scene? scene = Open("EndOfDemo", "End Of Demo (Part 8)", out Transform root);
        if (scene == null)
            return;

        // Continue must never start on this screen.
        var info = Object.FindAnyObjectByType<LevelInfo>();
        var so = new SerializedObject(info);
        so.FindProperty("saveOnEnter").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject note = GameObject.Find("PlaceholderNote");
        if (note != null)
            Object.DestroyImmediate(note);

        Transform canvas = GameObject.Find("Canvas").transform;
        GameObject title = GameObject.Find("ToBeContinued");
        var group = title.AddComponent<CanvasGroup>();

        TMP_Text credits = B.CreateText("Credits", canvas,
            "THE CLOWN WHO CRIED\n(demo)\n\n\n" +
            "GAME SYSTEMS, LEVELS, UI,\nDIALOGUE, CUTSCENES, AUDIO\n[your name]\n\n" +
            "PLAYER, COMBAT, ENEMIES, BOSS\n[your teammate's name]\n\n" +
            "ART\n[artist's name]\n\n" +
            "FONT\nPress Start 2P (SIL Open Font License)\n\n" +
            "MADE WITH\nUnity\n\n\n" +
            "Thank you for playing!",
            PlaceholderArt.FontSize, TextAlignmentOptions.Top);
        credits.rectTransform.anchorMin = credits.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        credits.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        credits.rectTransform.sizeDelta = new Vector2(400f, 300f);
        credits.lineSpacing = 40f;

        var sequence = canvas.gameObject.AddComponent<EndOfDemoSequence>();
        so = new SerializedObject(sequence);
        so.FindProperty("toBeContinued").objectReferenceValue = group;
        so.FindProperty("credits").objectReferenceValue = credits.rectTransform;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene.Value);
    }

    // ------------------------------------------------------------------ Test scene

    private static void BuildTestScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Main Camera", typeof(Camera));
        cam.tag = "MainCamera";
        cam.GetComponent<Camera>().orthographic = true;
        cam.transform.position = new Vector3(0f, 0f, -10f);

        var driver = new GameObject("Part8 Test Driver").AddComponent<CutsceneTest>();
        var so = new SerializedObject(driver);
        so.FindProperty("rewardPage").objectReferenceValue = RewardPage();
        so.FindProperty("timeout").floatValue = 240f;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, TestScenePath);
        Part1Setup.AddScenesToBuildProfile(TestScenePath);
    }
}
