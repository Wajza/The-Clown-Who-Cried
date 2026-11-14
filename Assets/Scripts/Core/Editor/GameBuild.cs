using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// EDITOR-ONLY. Builds the game for Windows or Mac.
///
/// Menu: Tools > Clown Who Cried > Build > Windows (64-bit) / Mac
/// Output: Builds/Windows/TheClownWhoCried.exe, Builds/Mac/TheClownWhoCried.app
/// (the Builds folder is in .gitignore, so builds are never committed).
///
/// WHAT GOES IN
///   - Only the real game scenes (Game Scenes below): the title screen and the levels.
///     Test scenes and my teammate's SampleScene stay out, whatever the Build Profile list says.
///   - Everything in Resources EXCEPT the test-only folders (Test Content below). Unity always
///     ships every Resources folder, so those are moved out for the build and moved straight
///     back afterwards (even if the build fails). If Unity ever crashed mid-build, the next build
///     (or the menu item "Restore Test Content") puts them back from Assets/_BuildExcluded.
///
/// MAC: needs "Mac Build Support (Mono)" added to this Unity version in Unity Hub
/// (Installs > the gear > Add modules). Windows support is already installed.
/// </summary>
public static class GameBuild
{
    public const string WindowsPath = "Builds/Windows/TheClownWhoCried.exe";
    public const string MacPath = "Builds/Mac/TheClownWhoCried.app";
    private const string ExcludedFolder = "Assets/_BuildExcluded";

    /// <summary>The scenes in the game, in order (the first one is where the game starts).</summary>
    public static readonly string[] GameScenes =
    {
        "Assets/Scenes/Title.unity",
        "Assets/Scenes/Levels/Tumbleton_PipHouse.unity",
        "Assets/Scenes/Levels/Tumbleton.unity",
        "Assets/Scenes/Levels/TrainMap.unity",
        "Assets/Scenes/Levels/Wonderwick_Hub.unity",
        "Assets/Scenes/Levels/Wonderwick_W1_Delivery.unity",
        "Assets/Scenes/Levels/Wonderwick_W2_MirrorMaze.unity",
        "Assets/Scenes/Levels/Wonderwick_W3_Coaster.unity",
        "Assets/Scenes/Levels/Wonderwick_W4_Audition.unity",
        "Assets/Scenes/Levels/ChortleArena.unity",
        "Assets/Scenes/Levels/EndOfDemo.unity",
    };

    /// <summary>Resources folders only the automatic tests use (moved out during a build).</summary>
    private static readonly string[] TestContent =
    {
        "Assets/Resources/Tasks/Test",
        "Assets/Resources/JournalPages/Test",
    };

    [MenuItem("Tools/Clown Who Cried/Build/Windows (64-bit)")]
    public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, WindowsPath);

    [MenuItem("Tools/Clown Who Cried/Build/Mac")]
    public static void BuildMac() => Build(BuildTarget.StandaloneOSX, MacPath);

    [MenuItem("Tools/Clown Who Cried/Build/Restore Test Content (after a crash)")]
    public static void RestoreTestContent() => MoveTestContent(back: true);

    /// <summary>Command line: build for Windows, exit 0 on success, 1 on failure.</summary>
    public static void BuildWindowsFromCommandLine() =>
        EditorApplication.Exit(Build(BuildTarget.StandaloneWindows64, WindowsPath) ? 0 : 1);

    public static void BuildMacFromCommandLine() =>
        EditorApplication.Exit(Build(BuildTarget.StandaloneOSX, MacPath) ? 0 : 1);

    // ------------------------------------------------------------------ Building

    public static bool Build(BuildTarget target, string path)
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
        {
            Debug.LogError($"GameBuild: {target} support isn't installed. In Unity Hub: Installs > (gear) > Add modules > " +
                           (target == BuildTarget.StandaloneOSX ? "Mac Build Support (Mono)." : "Windows Build Support."));
            return false;
        }

        var scenes = new List<string>();
        foreach (string scene in GameScenes)
        {
            if (File.Exists(scene))
                scenes.Add(scene);
            else
                Debug.LogWarning($"GameBuild: {scene} is missing; building without it.");
        }

        MoveTestContent(back: true); // Recover from an interrupted earlier build, if needed.
        MoveTestContent(back: false);
        BuildReport report;
        try
        {
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = path,
                target = target,
                options = BuildOptions.None,
            });
        }
        finally
        {
            MoveTestContent(back: true);
        }

        BuildSummary summary = report.summary;
        bool ok = summary.result == BuildResult.Succeeded;
        if (ok)
            Debug.Log($"GameBuild: {target} build OK: {path} ({summary.totalSize / (1024f * 1024f):0.0} MB, " +
                      $"{scenes.Count} scenes, {summary.totalTime.TotalSeconds:0} s).");
        else
            Debug.LogError($"GameBuild: {target} build {summary.result} with {summary.totalErrors} error(s). See the Console.");
        return ok;
    }

    /// <summary>Moves the test-only Resources folders out of Resources (back = false) or back in.</summary>
    private static void MoveTestContent(bool back)
    {
        foreach (string folder in TestContent)
        {
            string parked = $"{ExcludedFolder}/{folder.Replace("Assets/Resources/", "").Replace('/', '_')}";
            string from = back ? parked : folder;
            string to = back ? folder : parked;
            if (!AssetDatabase.IsValidFolder(from))
                continue;
            if (!back)
                Part1Setup.EnsureFolder(ExcludedFolder);
            string error = AssetDatabase.MoveAsset(from, to); // Keeps every asset's ID (GUID).
            if (!string.IsNullOrEmpty(error))
                Debug.LogError($"GameBuild: could not move {from} -> {to}: {error}");
        }

        if (back && AssetDatabase.IsValidFolder(ExcludedFolder) && AssetDatabase.FindAssets("", new[] { ExcludedFolder }).Length == 0)
            AssetDatabase.DeleteAsset(ExcludedFolder);
        AssetDatabase.SaveAssets();
    }
}
