using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using P6 = Part6Setup;

/// <summary>
/// EDITOR-ONLY: the story additions requested after the first playtest. Run by
/// Tools > Clown Who Cried > Combat > Run Everything (CombatSetup), or on its own:
/// Tools > Clown Who Cried > Story > Run Everything. Everything is skipped where already done.
///
///   1. An AudioListener on the camera of EVERY scene (none had one: the game was silent).
///   2. Prologue abilities are taught in the prologue: Officer Honk gives Dash (P2),
///      Balloon Betty gives the Star Coins (P3), Old Tumbles the mallet (P4, as before).
///   3. The doctor's office (Tumbleton_DoctorOffice): after P4, task P5 "Doctor's Visit" starts;
///      the doctor explains the tear and teaches Tear Burst; the station opens only after it.
///
/// All dialogue is PLACEHOLDER text: edit it in Assets/Data/Dialogue/Prologue.
/// </summary>
public static class StorySetup
{
    private const string LevelsFolder = "Assets/Scenes/Levels";
    private const string TaskFolder = "Assets/Resources/Tasks/Prologue";
    private const string DialogueFolder = "Assets/Data/Dialogue/Prologue";
    private const string OfficeScene = "Tumbleton_DoctorOffice";
    private const string OfficePath = LevelsFolder + "/" + OfficeScene + ".unity";
    private const string DoctorName = "Dr. Mellow";

#if CWC_SETUP_TOOLS
    [MenuItem("Tools/Clown Who Cried/Story/Run Everything")]
#endif
    public static void RunEverything()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        AddAudioListeners();
        TeachAbilitiesInPrologue();
        BuildDoctorsOffice();
        AssetDatabase.SaveAssets();
        Debug.Log("StorySetup: done.");
    }

    // ------------------------------------------------------------------ 1. Audio listeners

    /// <summary>Every scene in Assets/Scenes gets exactly one AudioListener, on its main camera.</summary>
    public static void AddAudioListeners()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (Object.FindAnyObjectByType<AudioListener>(FindObjectsInactive.Include) != null)
                continue;

            Camera cam = null;
            foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
                if (cam == null || c.CompareTag("MainCamera"))
                    cam = c;
            if (cam == null)
            {
                Debug.LogWarning($"StorySetup: {path} has no camera; the runtime AudioListenerGuard will cover it.");
                continue;
            }

            cam.gameObject.AddComponent<AudioListener>();
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"StorySetup: AudioListener added to {path}.");
        }
    }

    // ------------------------------------------------------------------ 2. Prologue abilities

    private static void TeachAbilitiesInPrologue()
    {
        DialogueSpeaker guard = P6.Asset<DialogueSpeaker>(DialogueFolder, "Speaker_OfficerHonk");
        DialogueSpeaker vendor = P6.Asset<DialogueSpeaker>(DialogueFolder, "Speaker_BalloonBetty");

        // P2: Officer Honk teaches Dash.
        AddUnlock("Task_P2_HonkStreet", Ability.Dash, onStart: true);
        AddLine("Dialogue_P2_Guard_Ask", guard,
            "Here's a trick every brave clown knows: DASH! A quick zip, and those cars won't touch you.");
        // P3: Balloon Betty hands over the Star Coins.
        AddUnlock("Task_P3_BalloonWall", Ability.Coins, onStart: true);
        AddLine("Dialogue_P3_Vendor_Ask", vendor,
            "Here, take my lucky Star Coins! Throw them, and they always come back to you.");
        // P4: the mallet was already given by Old Tumbles (unlockOnStart Mallet).
    }

    private static void AddUnlock(string taskAsset, Ability ability, bool onStart)
    {
        TaskData task = P6.Asset<TaskData>(TaskFolder, taskAsset);
        if (task == null)
        {
            Debug.LogError($"StorySetup: {taskAsset} is missing (run Part 6 first).");
            return;
        }
        List<Ability> list = onStart ? task.unlockOnStart : task.unlockOnComplete;
        if (list.Contains(ability))
            return;
        list.Add(ability);
        EditorUtility.SetDirty(task);
    }

    /// <summary>Adds a line at the end of a conversation, once (skipped if that exact text is already there).</summary>
    private static void AddLine(string dialogueAsset, DialogueSpeaker speaker, string text)
    {
        DialogueData dialogue = P6.Asset<DialogueData>(DialogueFolder, dialogueAsset);
        if (dialogue == null || dialogue.lines.Exists(l => l.text == text))
            return;
        dialogue.lines.Add(new DialogueData.Line { speaker = speaker, text = text });
        EditorUtility.SetDirty(dialogue);
    }

    // ------------------------------------------------------------------ 3. The doctor's office

    private static void BuildDoctorsOffice()
    {
        Part1Setup.EnsureFolder(TaskFolder);
        Part1Setup.EnsureFolder(DialogueFolder);

        // ---- Data
        DialogueSpeaker pip = P6.Asset<DialogueSpeaker>(DialogueFolder, "Speaker_Pip");
        DialogueSpeaker tumbles = P6.Asset<DialogueSpeaker>(DialogueFolder, "Speaker_OldTumbles");
        DialogueSpeaker doctor = P6.CreateIfMissing<DialogueSpeaker>(DialogueFolder, "Speaker_DrMellow",
            s => { s.displayName = DoctorName; s.nameColor = new Color(0.6f, 0.95f, 0.8f); });

        DialogueData visit = P6.CreateIfMissing<DialogueData>(DialogueFolder, "Dialogue_P5_Doctor", d => Lines(d,
            (doctor, "Ah, Pip! Old Tumbles told me about your... leak. Hop up on the table."),
            (null, "(The doctor peers at the tear through an enormous magnifying glass.)"),
            (doctor, "Hmm. Salty. Warm. Completely real."),
            (pip, "Is it bad? Clowns in Tumbleton don't cry."),
            (doctor, "Crying isn't an illness, Pip. It's a feeling finding its way out."),
            (doctor, "And when a lot of feelings come out all at once... they can be very powerful."),
            (null, "(Something warm gathers behind Pip's eyes. TEAR BURST learned! When the Tear Gauge is full, press Tear Burst.)"),
            (doctor, "Now go and catch that train. Wonderwick is waiting.")));
        DialogueData closed = P6.CreateIfMissing<DialogueData>(DialogueFolder, "Dialogue_P5_DoctorClosed", d => Lines(d,
            (null, $"(A sign on the door: \"{DoctorName} - back after your Reflex Check!\")")));

        SignalObjective objective = P6.CreateIfMissing<SignalObjective>(TaskFolder, "Objective_P5_Doctor", o =>
        {
            o.signalKey = "p5_doctor";
            o.requiredCount = 1;
            o.description = $"See {DoctorName} (next to the station)";
        });
        TaskData p5 = P6.CreateIfMissing<TaskData>(TaskFolder, "Task_P5_DoctorVisit", t =>
        {
            t.id = "p5_doctor_visit";
            t.title = "Doctor's Visit";
            t.description = "Old Tumbles thinks someone should look at that tear.";
            t.city = TaskData.City.Tumbleton;
            t.boardOrder = 5;
            t.objectives = new List<TaskObjective> { objective };
            t.unlockOnComplete = new List<Ability> { Ability.TearBurst };
        });
        TaskData p4 = P6.Asset<TaskData>(TaskFolder, "Task_P4_ReflexCheck");
        AddLine("Dialogue_P4_Tumbles_Done", tumbles,
            $"But that tear of yours... see {DoctorName} before you ride the train. Her office is by the station.");
        AssetDatabase.SaveAssets();

        // NOTE: opening a scene unloads assets nothing points at yet, so each step below loads
        // its assets again AFTER opening its scene (otherwise they'd be saved as empty fields).
        BuildOfficeScene();
        AddOfficeToTumbleton();
        Part1Setup.AddScenesToBuildProfile(OfficePath);
    }

    private static DialogueData VisitDialogue => P6.Asset<DialogueData>(DialogueFolder, "Dialogue_P5_Doctor");
    private static DialogueData ClosedDialogue => P6.Asset<DialogueData>(DialogueFolder, "Dialogue_P5_DoctorClosed");
    private static TaskData P4 => P6.Asset<TaskData>(TaskFolder, "Task_P4_ReflexCheck");
    private static TaskData P5 => P6.Asset<TaskData>(TaskFolder, "Task_P5_DoctorVisit");

    private static void Lines(DialogueData d, params (DialogueSpeaker who, string text)[] lines)
    {
        foreach ((DialogueSpeaker who, string text) in lines)
            d.lines.Add(new DialogueData.Line { speaker = who, text = text });
    }

    /// <summary>The office: a copy of Pip's house layout, emptied, with the doctor and some furniture.</summary>
    private static void BuildOfficeScene()
    {
        if (File.Exists(OfficePath))
        {
            RepairOffice(); // Already built (maybe edited by hand): only fill in empty links.
            return;
        }
        if (!AssetDatabase.CopyAsset($"{LevelsFolder}/Tumbleton_PipHouse.unity", OfficePath))
        {
            Debug.LogError("StorySetup: couldn't copy Tumbleton_PipHouse to make the office.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(OfficePath, OpenSceneMode.Single);

        // Remove everything that belongs to Pip's house.
        foreach (string name in new[] { "Prologue (Part 6)", "Cutscenes (Part 8)", "Checkpoints (Combat)" })
        {
            GameObject go = GameObject.Find(name);
            if (go != null)
                Object.DestroyImmediate(go);
        }

        // Spawn points: only "FromTown" (by the door), and it's the default.
        foreach (SpawnPoint point in Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include))
        {
            var so = new SerializedObject(point);
            string id = so.FindProperty("id").stringValue;
            if (id != "FromTown")
            {
                Object.DestroyImmediate(point.gameObject);
                continue;
            }
            so.FindProperty("isDefault").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            Object.FindAnyObjectByType<LevelInfo>().transform.position = point.transform.position;
        }

        var info = Object.FindAnyObjectByType<LevelInfo>();
        var infoSo = new SerializedObject(info);
        infoSo.FindProperty("displayName").stringValue = $"{DoctorName}'s Office";
        infoSo.ApplyModifiedPropertiesWithoutUndo();

        // The front door goes back to town, always open.
        Door door = GameObject.Find("FrontDoor").GetComponent<Door>();
        var doorSo = new SerializedObject(door);
        doorSo.FindProperty("destination.sceneName").stringValue = "Tumbleton";
        doorSo.FindProperty("destination.spawnPointId").stringValue = "FromDoctor";
        doorSo.FindProperty("requirement.requiredFlag").stringValue = "";
        doorSo.FindProperty("requirement.lockedDialogue").objectReferenceValue = null;
        doorSo.ApplyModifiedPropertiesWithoutUndo();

        // The doctor (an Inspectable: talk to finish P5) and some placeholder furniture.
        var root = new GameObject("Doctor's Office (Combat)").transform;
        var doctor = new GameObject("NPC_DrMellow");
        doctor.transform.SetParent(root);
        doctor.transform.position = new Vector3(12f, 0f, 0f);
        P6.Box(doctor.transform, "Coat", new Vector2(0f, 0.75f), new Vector2(1f, 1.5f), new Color(0.95f, 0.97f, 1f), 3);
        P6.Box(doctor.transform, "Head", new Vector2(0f, 1.75f), new Vector2(0.7f, 0.6f), new Color(1f, 0.85f, 0.7f), 3);
        var range = doctor.AddComponent<BoxCollider2D>();
        range.isTrigger = true;
        range.size = new Vector2(2f, 2.5f);
        range.offset = new Vector2(0f, 1.25f);
        var talk = doctor.AddComponent<Inspectable>();
        var talkSo = new SerializedObject(talk);
        talkSo.FindProperty("dialogue").objectReferenceValue = VisitDialogue;
        talkSo.FindProperty("signalKey").stringValue = "p5_doctor";
        talkSo.FindProperty("prompt").objectReferenceValue = P6.Prompt(doctor.transform, 2.6f);
        talkSo.ApplyModifiedPropertiesWithoutUndo();

        P6.Box(root, "ExamTable", new Vector2(16f, 0.4f), new Vector2(3f, 0.8f), new Color(0.55f, 0.75f, 0.8f), 1);
        P6.Box(root, "EyeChart", new Vector2(7f, 2.2f), new Vector2(1.2f, 1.6f), new Color(0.95f, 0.95f, 0.9f), 1);
        P6.Box(root, "Cabinet", new Vector2(21f, 1f), new Vector2(1.5f, 2f), new Color(0.7f, 0.55f, 0.4f), 1);

        EditorSceneManager.SaveScene(scene);
        Debug.Log($"StorySetup: built {OfficePath}.");
    }

    /// <summary>Fills the doctor's conversation if it's empty (e.g. from an earlier, broken run).</summary>
    private static void RepairOffice()
    {
        Scene scene = EditorSceneManager.OpenScene(OfficePath, OpenSceneMode.Single);
        GameObject doctor = GameObject.Find("NPC_DrMellow");
        if (doctor == null)
            return;
        var so = new SerializedObject(doctor.GetComponent<Inspectable>());
        if (so.FindProperty("dialogue").objectReferenceValue != null)
            return;
        so.FindProperty("dialogue").objectReferenceValue = VisitDialogue;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("StorySetup: repaired the doctor's conversation link.");
    }

    /// <summary>Tumbleton: the doctor's door, a spawn point outside it, P4 -> P5, and the station needing P5.</summary>
    private static void AddOfficeToTumbleton()
    {
        Scene scene = EditorSceneManager.OpenScene($"{LevelsFolder}/Tumbleton.unity", OpenSceneMode.Single);
        TaskData p4 = P4, p5 = P5;        // Loaded AFTER opening the scene (see BuildDoctorsOffice).
        DialogueData closed = ClosedDialogue;
        const string rootName = "Doctor's Office (Combat)";
        if (GameObject.Find(rootName) != null)
        {
            RepairTumbleton(scene, p4, p5, closed);
            return;
        }
        var root = new GameObject(rootName).transform;

        // The door (x 98: past the pots, before the station gate). Closed until P4 is done.
        var door = new GameObject("DoctorDoor", typeof(BoxCollider2D));
        door.transform.SetParent(root);
        door.transform.position = new Vector3(98f, 0f, 0f);
        var range = door.GetComponent<BoxCollider2D>();
        range.isTrigger = true;
        range.size = new Vector2(2f, 2.5f);
        range.offset = new Vector2(0f, 1.25f);
        var art = new GameObject("Sprite", typeof(SpriteRenderer));
        art.transform.SetParent(door.transform, false);
        art.GetComponent<SpriteRenderer>().sprite = PlaceholderArt.DoorSprite;
        art.GetComponent<SpriteRenderer>().color = new Color(0.75f, 1f, 0.85f); // Minty: the doctor's.
        art.GetComponent<SpriteRenderer>().sortingOrder = 1;
        var exit = door.AddComponent<Door>();
        var so = new SerializedObject(exit);
        so.FindProperty("destination.sceneName").stringValue = OfficeScene;
        so.FindProperty("destination.spawnPointId").stringValue = "FromTown";
        so.FindProperty("requirement.requiredFlag").stringValue = p4.DoneFlag;
        so.FindProperty("requirement.lockedDialogue").objectReferenceValue = closed;
        so.FindProperty("prompt").objectReferenceValue = P6.Prompt(door.transform, 2.6f);
        so.ApplyModifiedPropertiesWithoutUndo();

        Part1Setup.AddSpawnPoint("FromDoctor", new Vector2(99.5f, 0f), false);
        GameObject.Find("SpawnPoint_FromDoctor").transform.SetParent(root);

        // P4 done -> P5 starts.
        var chain = new GameObject("TaskChain_P4_to_P5").AddComponent<TaskChain>();
        chain.transform.SetParent(root);
        so = new SerializedObject(chain);
        so.FindProperty("after").objectReferenceValue = p4;
        so.FindProperty("next").objectReferenceValue = p5;
        so.ApplyModifiedPropertiesWithoutUndo();

        // The station now needs the doctor's visit too.
        TaskGate gate = P6.FindNamed<TaskGate>("TaskGate_Station");
        if (gate != null)
        {
            StationNeeds(gate, p5);
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("StorySetup: Tumbleton has the doctor's door, P4 -> P5, and the station needs P5.");
    }

    /// <summary>The station gate needs P5: removes empty entries, adds P5 once.</summary>
    private static void StationNeeds(TaskGate gate, TaskData p5)
    {
        var so = new SerializedObject(gate);
        SerializedProperty tasks = so.FindProperty("requiredTasks");
        bool has = false;
        for (int i = tasks.arraySize - 1; i >= 0; i--)
        {
            Object task = tasks.GetArrayElementAtIndex(i).objectReferenceValue;
            if (task == null)
                tasks.DeleteArrayElementAtIndex(i); // An empty entry would keep the gate shut forever.
            else
                has |= task == p5;
        }
        if (!has)
        {
            tasks.arraySize++;
            tasks.GetArrayElementAtIndex(tasks.arraySize - 1).objectReferenceValue = p5;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        gate.name = "TaskGate_Station (needs P1-P5)";
    }

    /// <summary>Fills empty links left by an earlier run (chain, door's locked line, gate).</summary>
    private static void RepairTumbleton(Scene scene, TaskData p4, TaskData p5, DialogueData closed)
    {
        TaskChain chain = Object.FindAnyObjectByType<TaskChain>(FindObjectsInactive.Include);
        if (chain != null)
        {
            var so = new SerializedObject(chain);
            if (so.FindProperty("after").objectReferenceValue == null)
                so.FindProperty("after").objectReferenceValue = p4;
            if (so.FindProperty("next").objectReferenceValue == null)
                so.FindProperty("next").objectReferenceValue = p5;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        GameObject doorGo = GameObject.Find("DoctorDoor");
        if (doorGo != null)
        {
            var so = new SerializedObject(doorGo.GetComponent<Door>());
            if (so.FindProperty("requirement.lockedDialogue").objectReferenceValue == null)
                so.FindProperty("requirement.lockedDialogue").objectReferenceValue = closed;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        TaskGate gate = P6.FindNamed<TaskGate>("TaskGate_Station");
        if (gate != null)
            StationNeeds(gate, p5);

        EditorSceneManager.SaveScene(scene);
        Debug.Log("StorySetup: repaired Tumbleton's doctor links.");
    }
}
