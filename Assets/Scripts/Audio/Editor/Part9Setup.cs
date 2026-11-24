using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// EDITOR-ONLY tool for Part 9 (audio).
///
/// Menu: Tools > Clown Who Cried > Part 9 > Run Everything
///   1. Placeholder sounds and music loops (Assets/Audio/Placeholders; only made if missing).
///   2. The SoundLibrary (Assets/Resources/Audio/SoundLibrary): an entry for every id in SoundIds
///      that doesn't have one yet, pointing at its placeholder. Existing entries are never changed.
///   3. Rebuilds the GameSystems prefab (now with the Audio object).
///   4. Each level's LevelInfo > Music Id, only where it's still EMPTY:
///        Pip's House, Tumbleton -> music_tumbleton   Train Map -> music_train
///        Wonderwick hub + W1-W3  -> music_wonderwick  W4 Audition -> music_audition
///        End of Demo -> music_end      (Chortle's arena: no level music; boss music during the fight)
///   5. Assets/Scenes/Tests/AudioTest + driver.
/// </summary>
public static class Part9Setup
{
    private const string TestScenePath = Part1Setup.TestScenesFolder + "/AudioTest.unity";

    private static readonly Dictionary<string, string> LevelMusic = new Dictionary<string, string>
    {
        { "Tumbleton_PipHouse", SoundIds.MusicTumbleton },
        { "Tumbleton", SoundIds.MusicTumbleton },
        { "TrainMap", SoundIds.MusicTrain },
        { "Wonderwick_Hub", SoundIds.MusicWonderwick },
        { "Wonderwick_W1_Delivery", SoundIds.MusicWonderwick },
        { "Wonderwick_W2_MirrorMaze", SoundIds.MusicWonderwick },
        { "Wonderwick_W3_Coaster", SoundIds.MusicWonderwick },
        { "Wonderwick_W4_Audition", SoundIds.MusicAudition },
        { "EndOfDemo", SoundIds.MusicEnd },
    };

#if CWC_SETUP_TOOLS
    [MenuItem("Tools/Clown Who Cried/Part 9/Run Everything")]
#endif
    public static void RunEverything()
    {
        PlaceholderAudio.EnsureAll();
        BuildLibrary();
        GameSystemsPrefabBuilder.Build();
        SetLevelMusic();
        BuildTestScene();
        Debug.Log("Part9Setup: done.");
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

    // ------------------------------------------------------------------ Library

    private static void BuildLibrary()
    {
        string path = GameSystemsPrefabBuilder.SoundLibraryPath;
        Part1Setup.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(path);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<SoundLibrary>();
            AssetDatabase.CreateAsset(library, path);
        }

        // Sounds that play very often get a little random pitch, so they don't sound robotic.
        var varied = new HashSet<string> { SoundIds.Jump, SoundIds.Swing, SoundIds.Hit, SoundIds.CoinThrow,
            SoundIds.Break, SoundIds.Pickup, SoundIds.UiDialogue };

        foreach (string id in SoundIds.AllMusic)
            AddIfMissing(library, id, 0.55f, 0f);
        foreach (string id in SoundIds.AllSfx)
            AddIfMissing(library, id, 0.8f, varied.Contains(id) ? 0.08f : 0f);

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
    }

    private static void AddIfMissing(SoundLibrary library, string id, float volume, float pitchVariation)
    {
        if (library.entries.Exists(e => e.id == id))
            return; // Already there (maybe with real audio): never touched.
        library.entries.Add(new SoundLibrary.Entry
        {
            id = id,
            clips = new List<AudioClip> { PlaceholderAudio.Load(id) },
            volume = volume,
            pitchVariation = pitchVariation,
        });
    }

    // ------------------------------------------------------------------ Levels

    private static void SetLevelMusic()
    {
        foreach (KeyValuePair<string, string> pair in LevelMusic)
        {
            string path = $"{Part5Setup.LevelsFolder}/{pair.Key}.unity";
            if (!File.Exists(path))
                continue;
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var info = Object.FindAnyObjectByType<LevelInfo>();
            if (info == null)
                continue;
            var so = new SerializedObject(info);
            SerializedProperty music = so.FindProperty("musicId");
            if (!string.IsNullOrEmpty(music.stringValue))
                continue; // Chosen already (maybe by hand): leave it.
            music.stringValue = pair.Value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }
    }

    // ------------------------------------------------------------------ Test scene

    private static void BuildTestScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Part1Setup.BuildCommon(Part1Setup.GetOrCreatePlaceholderSprite()); // Camera, floor, stand-in Pip.
        Part1Setup.AddSpawnPoint("Start", new Vector2(-8f, Part1Setup.FloorY), isDefault: true);
        GameObject.Find("Pip (test stand-in)").AddComponent<TearGauge>(); // So "tear full" can be tested.

        var driver = new GameObject("Part9 Test Driver").AddComponent<AudioSystemTest>();
        var so = new SerializedObject(driver);
        so.FindProperty("testTask").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<TaskData>("Assets/Resources/Tasks/Test/Task_Test_Kid.asset");
        so.FindProperty("testPage").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<JournalPageData>("Assets/Resources/JournalPages/Test/JournalPage_Test_2.asset");
        so.FindProperty("timeout").floatValue = 180f;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, TestScenePath);
        Part1Setup.AddScenesToBuildProfile(TestScenePath);
    }
}
