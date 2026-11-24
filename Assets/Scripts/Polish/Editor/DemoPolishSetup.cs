using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Demo polish pass: Tools > Clown Who Cried > Demo Polish > Run Everything.
/// Safe to run more than once. What it does:
///   1. Character art: puts every animation frame's pivot on the character's feet (Pip, Wobbler,
///      Pie Slinger, Dr. Mellow), so poses no longer jump around between frames and animations.
///   2. Animators: Pip's jump shows the right pose for rising/falling instead of looping, walk speed
///      follows her movement, coin throws play the throw pose; Pie Slinger starts in its idle
///      (it started in a missing "Wobbler Idle" state).
///   3. Tiles: the artist's ground blocks replace the placeholder tiles in every level, and the
///      platform art replaces the plain ledge boxes. In rooms whose background already shows the
///      floor (Pip's house, the doctor's office, the Tumbleton road) the tiles are collision only.
///   4. Rooms: the bedroom and the doctor's office show their whole background, unstretched, on one
///      screen. The mirror in the bedroom art is the one Pip inspects; Dr. Mellow is talked to where
///      he stands; the extra placeholder furniture in the office is gone (it's in the background).
///   5. Prefabs: Pip's real colours (no pink tint), no placeholder swing box; enemies stand on the
///      ground, play hop / throw / alert poses; "!" bubbles for prompts and warnings.
/// </summary>
public static class DemoPolishSetup
{
    private const string Menu = "Tools/Clown Who Cried/Demo Polish/";
    private const string LevelsFolder = "Assets/Scenes/Levels/";
    private const string TileAssetsFolder = "Assets/Art/Tiles/Tile Assets";
    private const string FillTexturePath = "Assets/Art/Tiles/Ground_Fill.png";
    private const string BubblePath = "Assets/Art/UI/Prompt_Bubble.png";
    private const string PlaceholderWhiteGuid = "04aa9a54ba3715e469d245c3116a524b";
    private const string PipController = "Assets/Art/Pip Animation/Walk/Sprite.controller";
    private const string PieController = "Assets/Sprite.controller";
    private const string WobblerController = "Assets/Art/Enemies/Wobbler Animation/Idle/Sprite.controller";

    private static readonly string[] LevelScenes =
    {
        "Tumbleton_PipHouse", "Tumbleton_DoctorOffice", "Tumbleton", "ChortleArena",
        "Wonderwick_Hub", "Wonderwick_W1_Delivery", "Wonderwick_W2_MirrorMaze",
        "Wonderwick_W3_Coaster", "Wonderwick_W4_Audition",
    };

    // Scenes whose background art already draws the floor: tiles are kept for collision only.
    private static readonly HashSet<string> TilesHidden = new HashSet<string>
    {
        "Tumbleton_PipHouse", "Tumbleton_DoctorOffice", "Tumbleton",
    };

    // Scenes whose end walls are invisible (the background frames the room).
    private static readonly HashSet<string> WallsHidden = new HashSet<string> { "ChortleArena" };

    private static readonly string[] GroundTextures =
    {
        "Assets/Art/Tiles/Ground_01.png", "Assets/Art/Tiles/Ground_02.png", "Assets/Art/Tiles/Ground_03.png",
        "Assets/Art/Tiles/Ground_Star.png", "Assets/Art/Tiles/Ground_Right_Edge.png",
    };

    private static readonly string[] PlatformTextures =
    {
        "Assets/Art/Tiles/Platform_Brick.png", "Assets/Art/Tiles/Platform_Short.png",
        "Assets/Art/Tiles/Platform_Double_Star.png", "Assets/Art/Tiles/Platform_Star.png",
        "Assets/Art/Tiles/Platform_Stone.png",
    };

    private static readonly StringBuilder Report = new StringBuilder();

    // Measured opaque art of each frame (pixels), used to place things above characters' heads.
    private struct Feet
    {
        public float X;   // centre of the feet (pixels from the left)
        public int Y;     // lowest opaque row (pixels from the bottom)
        public int Top;   // highest opaque row (pixels from the bottom)
        public int W, H;
    }

    private static readonly Dictionary<string, Feet> Measured = new Dictionary<string, Feet>();

    // ------------------------------------------------------------------ Menu

#if CWC_SETUP_TOOLS
    [MenuItem(Menu + "Run Everything", priority = 0)]
#endif
    public static void RunEverything()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        string previousScene = SceneManager.GetActiveScene().path;
        Report.Clear();
        try
        {
            FixCharacterPivots();
            FixAnimators();
            CreateArtAssets();
            FixPrefabs();
            FixLevels();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(previousScene) && File.Exists(previousScene))
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
            WriteReport();
        }
    }

#if CWC_SETUP_TOOLS
    [MenuItem(Menu + "1. Character Pivots + Animators", priority = 20)]
#endif
    public static void RunCharacters()
    {
        Report.Clear();
        FixCharacterPivots();
        FixAnimators();
        AssetDatabase.SaveAssets();
        WriteReport();
    }

#if CWC_SETUP_TOOLS
    [MenuItem(Menu + "2. Prefabs (Pip, enemies)", priority = 21)]
#endif
    public static void RunPrefabs()
    {
        Report.Clear();
        CreateArtAssets();
        FixPrefabs();
        AssetDatabase.SaveAssets();
        WriteReport();
    }

#if CWC_SETUP_TOOLS
    [MenuItem(Menu + "3. Levels (tiles, platforms, rooms)", priority = 22)]
#endif
    public static void RunLevels()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        string previousScene = SceneManager.GetActiveScene().path;
        Report.Clear();
        try
        {
            CreateArtAssets();
            FixLevels();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(previousScene) && File.Exists(previousScene))
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
            WriteReport();
        }
    }

    /// <summary>
    /// ONLY the boss room's newest changes, nothing else (no other scene, prefab or art is touched,
    /// and the rest of the arena is left as it is):
    ///   - the rubber-chicken cannons are hidden and show a "!" before each chicken,
    ///   - Chortle's copies turn see-through while Blue Sight is on.
    /// </summary>
#if CWC_SETUP_TOOLS
    [MenuItem(Menu + "Boss Room Only (cannons + Blue Sight)", priority = 40)]
#endif
    public static void RunBossRoomOnly()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        string previousScene = SceneManager.GetActiveScene().path;
        Report.Clear();
        try
        {
            _bubble = AssetDatabase.LoadAssetAtPath<Sprite>(BubblePath);
            if (_bubble == null)
            {
                Log("Missing " + BubblePath + " (run Demo Polish once first)");
                return;
            }

            // Only the component on the copy prefab; its size, art and everything else stay as they are.
            EditPrefab("Assets/Prefabs/Boss/ChortleCopy.prefab", root =>
            {
                if (root.GetComponent<FadeInBlueSight>() == null)
                    root.AddComponent<FadeInBlueSight>();
                Log("ChortleCopy prefab: see-through while Blue Sight is on");
            });

            string path = LevelsFolder + "ChortleArena.unity";
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            HideChickenCannons(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(previousScene) && File.Exists(previousScene))
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
            WriteReport();
        }
    }

    /// <summary>
    /// ONLY Pip's throw and dash frames (nothing else is touched), lined up with her idle pose:
    ///   - throw: her FRONT shoe stays exactly where it is when she stands (the back foot steps
    ///     behind her and she leans into the throw),
    ///   - dash: her body (the star on her hat) stays put and her shoes stay on the ground.
    /// Shoes are found from dark pixels only, so dust clouds and speed lines don't count.
    /// </summary>
#if CWC_SETUP_TOOLS
    [MenuItem(Menu + "Pip Throw + Dash Feet Only", priority = 43)]
#endif
    public static void RunThrowDashFeetOnly()
    {
        Report.Clear();
        PlantLikeIdle(IdleFrame, AnchorX.FrontToe, "Assets/Art/Pip Animation/Attack");
        PlantLikeIdle(IdleFrame, AnchorX.HatStar, "Assets/Art/Pip Animation/Dash");
        WriteReport();
    }

    private const string IdleFrame = "Assets/Art/Pip Animation/Idle/Pip Idle_0000.png";

    private enum AnchorX { FrontToe, HatStar }

    /// <summary>
    /// Gives every frame in these folders the same pivot, relative to her shoes, as the idle frame:
    /// height from the bottom of her shoes (always on the ground), left/right from her front toe or
    /// from the star on her hat. A frame drawn the same as idle gets idle's exact pivot.
    /// </summary>
    private static void PlantLikeIdle(string idleFrame, AnchorX anchor, params string[] folders)
    {
        var idleImporter = AssetImporter.GetAtPath(idleFrame) as TextureImporter;
        if (idleImporter == null)
            return;
        var idleSettings = new TextureImporterSettings();
        idleImporter.ReadTextureSettings(idleSettings);
        Vector2 idlePoint = AnchorPoint(idleFrame, anchor, out int iw, out int ih);
        // Pivot relative to the anchor point, in pixels: the same in every frame.
        Vector2 offset = new Vector2(idleSettings.spritePivot.x * iw, idleSettings.spritePivot.y * ih) - idlePoint;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string folder in folders)
            {
                string[] files = Directory.GetFiles(folder, "*.png").Select(f => f.Replace(Path.DirectorySeparatorChar, '/')).OrderBy(f => f).ToArray();
                foreach (string file in files)
                {
                    Vector2 pivotPx = AnchorPoint(file, anchor, out int w, out int h) + offset;
                    SetSpriteSettings(file, new Vector2(pivotPx.x / w, pivotPx.y / h), null, null);
                }
                Log($"Pivots: {folder} ({files.Length} frames) lined up with her idle pose " +
                    (anchor == AnchorX.FrontToe ? "(front shoe planted)" : "(body still, shoes on the ground)"));
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
    }

    /// <summary>(x, y) in pixels from the bottom-left: y = bottom of her shoes, x = front toe or hat star.</summary>
    private static Vector2 AnchorPoint(string path, AnchorX anchor, out int w, out int h)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(path));
        w = tex.width;
        h = tex.height;
        Color32[] px = tex.GetPixels32();
        Object.DestroyImmediate(tex);
        int W = w, H = h;

        // Shoes and outlines are dark; dust clouds and speed lines are light.
        bool Dark(int x, int y)
        {
            Color32 c = px[y * W + x];
            return c.a > 200 && (c.r * 0.299f + c.g * 0.587f + c.b * 0.114f) < 110f;
        }

        int bottom = 0;
        for (int y = 0; y < H; y++)
        {
            int n = 0;
            for (int x = 0; x < W; x++)
                if (Dark(x, y)) n++;
            if (n >= 12) { bottom = y; break; }
        }

        if (anchor == AnchorX.FrontToe)
        {
            // The tip of the front shoe: the 98th percentile of the dark pixels in the shoe band
            // (the art faces right), so a stray dot can't move it.
            var xs = new List<int>();
            int band = Mathf.RoundToInt(H * 0.06f);
            for (int y = bottom; y < Mathf.Min(H, bottom + band); y++)
                for (int x = 0; x < W; x++)
                    if (Dark(x, y)) xs.Add(x);
            xs.Sort();
            float toe = xs.Count > 0 ? xs[Mathf.Min(xs.Count - 1, (int)(xs.Count * 0.98f))] : W * 0.5f;
            return new Vector2(toe, bottom);
        }

        // The yellow star on top of her hat: yellow pixels near the very top of the drawing.
        int top = H - 1;
        for (int y = H - 1; y >= 0; y--)
        {
            int n = 0;
            for (int x = 0; x < W; x++)
                if (px[y * W + x].a > 200) n++;
            if (n >= 6) { top = y; break; }
        }
        double sum = 0;
        int count = 0;
        for (int y = Mathf.Max(0, top - Mathf.RoundToInt(H * 0.12f)); y <= top; y++)
            for (int x = 0; x < W; x++)
            {
                Color32 c = px[y * W + x];
                if (c.a > 200 && c.r > 200 && c.g > 160 && c.b < 150) { sum += x + 0.5; count++; }
            }
        return new Vector2(count > 0 ? (float)(sum / count) : W * 0.5f, bottom);
    }

    /// <summary>
    /// ONLY the Pie Slinger's throw pose (nothing else is touched). His idle frames face right, but the
    /// throw drawing faces left, so he turned away from Pip every time he threw. This makes a mirrored
    /// copy of the throw drawing ("... (faces right).png", same settings, pivot mirrored) and points the
    /// Pieslingger_Attack clip at it. The original drawing is left as it is.
    /// </summary>
#if CWC_SETUP_TOOLS
    [MenuItem(Menu + "Pie Slinger Throw Facing Only", priority = 44)]
#endif
    public static void RunPieSlingerFacingOnly()
    {
        Report.Clear();
        const string original = "Assets/Art/Enemies/Pie Slinger Animations/Attack/Pie thrower attack.png";
        const string mirrored = "Assets/Art/Enemies/Pie Slinger Animations/Attack/Pie thrower attack (faces right).png";
        const string clipPath = "Assets/Art/Enemies/Pie Slinger Animations/Attack/Pieslingger_Attack.anim";

        var srcImporter = AssetImporter.GetAtPath(original) as TextureImporter;
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (srcImporter == null || clip == null)
        {
            Log("Pie Slinger throw drawing or clip not found, nothing changed");
            WriteReport();
            return;
        }

        // 1. Mirrored copy of the drawing.
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(original));
        int w = tex.width, h = tex.height;
        Color32[] src = tex.GetPixels32();
        var dst = new Color32[src.Length];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                dst[y * w + x] = src[y * w + (w - 1 - x)];
        tex.SetPixels32(dst);
        File.WriteAllBytes(mirrored, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(mirrored, ImportAssetOptions.ForceUpdate);

        // 2. Same import settings as the original, with the pivot mirrored.
        var srcSettings = new TextureImporterSettings();
        srcImporter.ReadTextureSettings(srcSettings);
        var dstImporter = (TextureImporter)AssetImporter.GetAtPath(mirrored);
        dstImporter.SetTextureSettings(srcSettings);
        dstImporter.textureType = srcImporter.textureType;
        dstImporter.spriteImportMode = SpriteImportMode.Single;
        dstImporter.spritePixelsPerUnit = srcImporter.spritePixelsPerUnit;
        dstImporter.textureCompression = srcImporter.textureCompression;
        var s = new TextureImporterSettings();
        dstImporter.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = new Vector2(1f - srcSettings.spritePivot.x, srcSettings.spritePivot.y);
        dstImporter.SetTextureSettings(s);
        dstImporter.SaveAndReimport();
        Sprite faced = AssetDatabase.LoadAssetAtPath<Sprite>(mirrored);

        // 3. The throw clip shows the right-facing copy.
        int swapped = 0;
        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            for (int i = 0; i < keys.Length; i++)
                if (keys[i].value is Sprite sp && AssetDatabase.GetAssetPath(sp) == original)
                {
                    keys[i].value = faced;
                    swapped++;
                }
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        }
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        Log($"Pie Slinger: throw drawing mirrored to face right like his other frames ({swapped} keys in Pieslingger_Attack now use it)");
        WriteReport();
    }

    /// <summary>
    /// ONLY the Wonderwick Hub's camera (nothing else in it or anywhere else is touched): it follows
    /// Pip but never shows outside the level. If the Hub has background pictures (sprites from
    /// Assets/Art/Background), the camera limits are also kept inside those pictures, so running this
    /// again after adding a background fits the camera to it.
    /// </summary>
#if CWC_SETUP_TOOLS
    [MenuItem(Menu + "Wonderwick Hub Camera Only", priority = 41)]
#endif
    public static void RunHubCameraOnly() => RunCameraInsideLevelOnly("Wonderwick_Hub");

    /// <summary>The same as the Hub's, for the four Wonderwick task levels (only their cameras).</summary>
#if CWC_SETUP_TOOLS
    [MenuItem(Menu + "Wonderwick Tasks Camera Only (W1-W4)", priority = 42)]
#endif
    public static void RunTasksCameraOnly() => RunCameraInsideLevelOnly(
        "Wonderwick_W1_Delivery", "Wonderwick_W2_MirrorMaze", "Wonderwick_W3_Coaster", "Wonderwick_W4_Audition");

    private static void RunCameraInsideLevelOnly(params string[] sceneNames)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        string previousScene = SceneManager.GetActiveScene().path;
        Report.Clear();
        try
        {
            foreach (string name in sceneNames)
            {
                string path = LevelsFolder + name + ".unity";
                if (!File.Exists(path))
                {
                    Log("Missing scene " + path);
                    continue;
                }
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                if (CameraInsideLevel(scene))
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(previousScene) && File.Exists(previousScene))
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
            WriteReport();
        }
    }

    /// <summary>Camera follows Pip but stays inside the level's camera area (and its background pictures).</summary>
    private static bool CameraInsideLevel(Scene scene)
    {
        {
            GameObject boundsGo = Root(scene, "CameraBounds");
            var box = boundsGo != null ? boundsGo.GetComponent<BoxCollider2D>() : null;
            if (box == null)
            {
                Log($"{scene.name}: no CameraBounds found, nothing changed");
                return false;
            }

            // Fit inside any background pictures (if there are some).
            bool any = false;
            Bounds pics = default;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    string art = sr.sprite != null ? AssetDatabase.GetAssetPath(sr.sprite) : "";
                    if (!art.StartsWith("Assets/Art/Background/"))
                        continue;
                    if (!any) { pics = sr.bounds; any = true; }
                    else pics.Encapsulate(sr.bounds);
                }
            Vector2 pos = boundsGo.transform.position;
            float xMin = pos.x + box.offset.x - box.size.x * 0.5f, xMax = pos.x + box.offset.x + box.size.x * 0.5f;
            float yMin = pos.y + box.offset.y - box.size.y * 0.5f, yMax = pos.y + box.offset.y + box.size.y * 0.5f;
            if (any)
            {
                xMin = Mathf.Max(xMin, pics.min.x); xMax = Mathf.Min(xMax, pics.max.x);
                yMin = Mathf.Max(yMin, pics.min.y); yMax = Mathf.Min(yMax, pics.max.y);
                box.offset = new Vector2((xMin + xMax) * 0.5f - pos.x, (yMin + yMax) * 0.5f - pos.y);
                box.size = new Vector2(xMax - xMin, yMax - yMin);
                if (box.size.y < 14f)
                    Log($"{scene.name}: WARNING the background is only {box.size.y:0.0} tall; the camera view is 14, " +
                        "so a little past it can show. Make the picture taller to hide it.");
            }

            if (boundsGo.GetComponent<KeepCameraInBounds>() == null)
                boundsGo.AddComponent<KeepCameraInBounds>();
            GameObject rigGo = Root(scene, "CameraRig");
            if (rigGo != null)
                foreach (var confiner in rigGo.GetComponentsInChildren<CinemachineConfiner2D>(true))
                {
                    confiner.enabled = true;
                    confiner.Damping = 0f;
                    confiner.SlowingDistance = 0f;
                    EditorUtility.SetDirty(confiner);
                }
            Log($"{scene.name}: camera follows Pip but stays inside x {xMin:0.00}-{xMax:0.00}, y {yMin:0.00}-{yMax:0.00}" +
                (any ? " (the background pictures)" : " (the level area; no background picture here yet)"));
            return true;
        }
    }

    private static void Log(string line)
    {
        Report.AppendLine(line);
        Debug.Log("[DemoPolish] " + line);
    }

    private static void WriteReport()
    {
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/DemoPolish.log", Report.ToString());
        Debug.Log("[DemoPolish] DONE. Report: Logs/DemoPolish.log");
    }

    // ------------------------------------------------------------------ 1. Pivots

    private static void FixCharacterPivots()
    {
        // (folder, per-frame x?, per-frame y?, fixed pivot). Per-frame removes movement baked into a
        // clip (the jump's rise, the hop); everything else keeps ONE feet point for the whole clip, so
        // a mallet head or dust cloud below the feet in one frame can't make the character bob.
        var folders = new List<(string folder, bool perFrameX, bool perFrameY, Vector2? fixedPivot)>
        {
            ("Assets/Art/Pip Animation/Idle", false, false, null),
            ("Assets/Art/Pip Animation/Walk", false, false, null),
            ("Assets/Art/Pip Animation/Jump", false, true, null),
            // Shoes measured by hand: the mallet head and the smash dust reach below and beside her feet.
            ("Assets/Art/Pip Animation/Mallet", false, false, new Vector2(0.48f, 0.056f)),
            ("Assets/Art/Pip Animation/Dash", false, false, null),
            ("Assets/Art/Pip Animation/Attack", false, false, null),
            ("Assets/Art/Enemies/Wobbler Animation/Idle", false, false, null),
            ("Assets/Art/Enemies/Wobbler Animation/Hopping", true, true, null),
            ("Assets/Art/Enemies/Wobbler Animation/Alert", true, true, null),
            ("Assets/Art/Enemies/Pie Slinger Animations/Idle", false, false, null),
            ("Assets/Art/Enemies/Pie Slinger Animations/Attack", true, true, null),
            ("Assets/Art/Doctor/Idle", false, false, null),
        };

        var pivots = new Dictionary<string, Vector2>();
        int done = 0;
        foreach (var (folder, perFrameX, perFrameY, fixedPivot) in folders)
        {
            EditorUtility.DisplayProgressBar("Demo Polish", "Measuring " + folder, done++ / (float)folders.Count);
            if (!Directory.Exists(folder))
            {
                Log("Skipped missing folder " + folder);
                continue;
            }
            string[] files = Directory.GetFiles(folder, "*.png").Select(f => f.Replace('\\', '/')).OrderBy(f => f).ToArray();
            if (files.Length == 0)
                continue;

            var feet = files.Select(Measure).ToArray();
            for (int i = 0; i < files.Length; i++)
                Measured[files[i]] = feet[i];

            float[] xs = feet.Select(f => f.X / f.W).OrderBy(v => v).ToArray();
            float[] ys = feet.Select(f => f.Y / (float)f.H).OrderBy(v => v).ToArray();
            float medianX = xs[xs.Length / 2];
            float medianY = ys[ys.Length / 2];
            for (int i = 0; i < files.Length; i++)
            {
                float x = perFrameX ? feet[i].X / feet[i].W : medianX;
                float y = perFrameY ? feet[i].Y / (float)feet[i].H : medianY;
                pivots[files[i]] = fixedPivot ?? new Vector2(x, y);
            }
            Log(fixedPivot.HasValue
                ? $"Pivots: {folder} ({files.Length} frames) fixed at {fixedPivot.Value}"
                : $"Pivots: {folder} ({files.Length} frames) feet x {(perFrameX ? "per frame" : medianX.ToString("0.000"))}, " +
                  $"y {(perFrameY ? "per frame" : medianY.ToString("0.000"))}");
        }

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var kv in pivots)
                SetSpriteSettings(kv.Key, kv.Value, null, null);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        // Throw and dash: shoes exactly where they are when she stands still (the dash dust doesn't count).
        PlantLikeIdle(IdleFrame, AnchorX.FrontToe, "Assets/Art/Pip Animation/Attack");
        PlantLikeIdle(IdleFrame, AnchorX.HatStar, "Assets/Art/Pip Animation/Dash");
    }

    private static Feet MeasureCached(string path)
    {
        if (!Measured.TryGetValue(path, out Feet f))
            Measured[path] = f = Measure(path);
        return f;
    }

    /// <summary>Finds the lowest opaque row (feet) and the centre of the feet in a PNG.</summary>
    private static Feet Measure(string path)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(path));
        int w = tex.width, h = tex.height;
        Color32[] px = tex.GetPixels32(); // Row 0 is the bottom.
        Object.DestroyImmediate(tex);

        const byte Solid = 128;
        const int MinRun = 6; // Ignores stray dots.
        int CountRow(int y)
        {
            int n = 0;
            for (int x = 0; x < w; x++)
                if (px[y * w + x].a > Solid) n++;
            return n;
        }

        int bottom = 0, top = h - 1;
        for (int y = 0; y < h; y++)
            if (CountRow(y) >= MinRun) { bottom = y; break; }
        for (int y = h - 1; y >= 0; y--)
            if (CountRow(y) >= MinRun) { top = y; break; }

        int band = Mathf.Max(6, Mathf.RoundToInt((top - bottom) * 0.1f));
        double sum = 0;
        int count = 0;
        for (int y = bottom; y <= Mathf.Min(h - 1, bottom + band); y++)
            for (int x = 0; x < w; x++)
                if (px[y * w + x].a > Solid) { sum += x + 0.5; count++; }

        return new Feet { X = count > 0 ? (float)(sum / count) : w * 0.5f, Y = bottom, Top = top, W = w, H = h };
    }

    private static void SetSpriteSettings(string path, Vector2? pivot, float? ppu, SpriteMeshType? mesh)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;
        var s = new TextureImporterSettings();
        importer.ReadTextureSettings(s);
        if (pivot.HasValue)
        {
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot = pivot.Value;
        }
        if (ppu.HasValue)
            s.spritePixelsPerUnit = ppu.Value;
        if (mesh.HasValue)
            s.spriteMeshType = mesh.Value;
        importer.SetTextureSettings(s);
        importer.SaveAndReimport();
    }

    // ------------------------------------------------------------------ 2. Animators

    private static void FixAnimators()
    {
        FixPipController();

        var pie = AssetDatabase.LoadAssetAtPath<AnimatorController>(PieController);
        if (pie != null)
        {
            var sm = pie.layers[0].stateMachine;
            AnimatorState idle = FindState(sm, "Pieslingger_Idle");
            foreach (var child in sm.states.ToArray())
                if (child.state.motion == null)
                {
                    Log($"Pie Slinger controller: removed state '{child.state.name}' (its clip is missing)");
                    sm.RemoveState(child.state);
                }
            if (idle != null)
                sm.defaultState = idle;
            EditorUtility.SetDirty(pie);
            Log("Pie Slinger controller: starts in Pieslingger_Idle");
        }

        var wob = AssetDatabase.LoadAssetAtPath<AnimatorController>(WobblerController);
        if (wob != null)
        {
            var sm = wob.layers[0].stateMachine;
            AnimatorState idle = FindState(sm, "WhoopeeWobbler_Idle");
            if (idle != null)
                sm.defaultState = idle;
            EditorUtility.SetDirty(wob);
            Log("Wobbler controller: starts in WhoopeeWobbler_Idle");
        }
    }

    private static void FixPipController()
    {
        var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(PipController);
        if (c == null)
        {
            Log("Pip controller not found at " + PipController);
            return;
        }

        EnsureParam(c, "WalkSpeed", AnimatorControllerParameterType.Float, 1f);
        EnsureParam(c, "Throw", AnimatorControllerParameterType.Trigger, 0f);
        EnsureParam(c, "AirPose", AnimatorControllerParameterType.Float, 0.6f);

        var sm = c.layers[0].stateMachine;
        AnimatorState idle = FindState(sm, "Pip_Idle");
        AnimatorState walk = FindState(sm, "Pip_Walk");
        AnimatorState jump = FindState(sm, "Pip_Jump");
        AnimatorState mallet = FindState(sm, "Pip_Mallet");
        AnimatorState dash = FindState(sm, "Pip_Dash");
        AnimatorState thr = FindState(sm, "Pip_Throw") ?? FindState(sm, "New Animation");
        if (idle == null || walk == null || jump == null)
        {
            Log("Pip controller: Idle/Walk/Jump state missing, left alone");
            return;
        }

        // Jump: pick the drawing from AirPose instead of looping the clip.
        jump.timeParameterActive = true;
        jump.timeParameter = "AirPose";
        SetLoop(jump.motion as AnimationClip, false);

        // Walk: feet speed follows Pip's speed.
        walk.speedParameterActive = true;
        walk.speedParameter = "WalkSpeed";

        // Landing / finishing while running goes straight to Walk (no one-frame Idle flash).
        AddFirstTransition(jump, walk, false, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        });

        // Mallet: the drawing follows the swing's own timing (SwingPhase), so the smash shows exactly
        // while the hitbox is on, and the state lasts exactly as long as the swing (Swinging).
        if (mallet != null)
        {
            EnsureParam(c, "SwingPhase", AnimatorControllerParameterType.Float, 0.3f);
            EnsureParam(c, "Swinging", AnimatorControllerParameterType.Bool, 0f);
            SetLoop(mallet.motion as AnimationClip, false);
            mallet.speedParameterActive = false;
            mallet.speed = 1f;
            mallet.timeParameterActive = true;
            mallet.timeParameter = "SwingPhase";
            foreach (var old in mallet.transitions.ToArray())
                mallet.RemoveTransition(old);
            var toWalk = mallet.AddTransition(walk);
            toWalk.hasExitTime = false;
            toWalk.duration = 0f;
            toWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "Swinging");
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            var toIdle = mallet.AddTransition(idle);
            toIdle.hasExitTime = false;
            toIdle.duration = 0f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Swinging");
        }

        if (dash != null)
            AddFirstTransition(dash, walk, false, t =>
            {
                t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dashing");
                t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            });

        // Coin throw: the artist's "Attack" clip (idle pose, then the throw). Start at the throw.
        // The throw pose starts at 0.6 of the clip; at half speed it shows for about 0.3 s.
        if (thr != null)
        {
            thr.name = "Pip_Throw";
            thr.speed = 0.5f;
            SetLoop(thr.motion as AnimationClip, false);
            if (!sm.anyStateTransitions.Any(t => t.destinationState == thr))
            {
                var t = sm.AddAnyStateTransition(thr);
                t.AddCondition(AnimatorConditionMode.If, 0f, "Throw");
                t.hasExitTime = false;
                t.duration = 0f;
                t.canTransitionToSelf = true;
            }
            foreach (var t in sm.anyStateTransitions.Where(t => t.destinationState == thr))
                t.offset = 0.6f;
            if (!thr.transitions.Any(t => t.destinationState == idle))
            {
                var back = thr.AddTransition(idle);
                back.hasExitTime = true;
                back.exitTime = 1f;
                back.duration = 0f;
            }
            AddFirstTransition(thr, walk, true, t => t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed"));
        }

        EditorUtility.SetDirty(c);
        Log("Pip controller: jump poses follow her rise/fall, walk speed follows movement, coin throw plays Pip_Throw");
    }

    private static AnimatorState FindState(AnimatorStateMachine sm, string name)
    {
        return sm.states.Select(s => s.state).FirstOrDefault(s => s.name == name);
    }

    private static void EnsureParam(AnimatorController c, string name, AnimatorControllerParameterType type, float def)
    {
        if (c.parameters.Any(p => p.name == name))
            return;
        c.AddParameter(new AnimatorControllerParameter { name = name, type = type, defaultFloat = def });
    }

    private static void AddFirstTransition(AnimatorState from, AnimatorState to, bool exitTime,
                                           System.Action<AnimatorStateTransition> conditions)
    {
        if (from.transitions.Any(t => t.destinationState == to))
            return;
        var t = from.AddTransition(to);
        t.hasExitTime = exitTime;
        t.exitTime = 1f;
        t.duration = 0f;
        conditions(t);
        from.transitions = new[] { t }.Concat(from.transitions.Where(x => x != t)).ToArray();
    }

    private static void SetLoop(AnimationClip clip, bool loop)
    {
        if (clip == null)
            return;
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        if (s.loopTime == loop)
            return;
        s.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, s);
        EditorUtility.SetDirty(clip);
    }

    // ------------------------------------------------------------------ 3. Art assets (tiles, bubble)

    private static Tile _top01, _top02, _top03, _topStar, _edge, _fill;
    private static Sprite[] _groundSprites;
    private static Sprite[] _platformSprites;
    private static Sprite _bubble;
    private static Sprite _white;

    private static void CreateArtAssets()
    {
        _white = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(PlaceholderWhiteGuid));

        // Ground blocks: 1 tile tall (pixels per unit = image height), full-rect so they can tile.
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string path in GroundTextures)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null)
                    continue;
                SetSpriteSettings(path, new Vector2(0.5f, 0.5f), tex.height, SpriteMeshType.FullRect);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        if (!File.Exists(FillTexturePath))
        {
            // Solid earth under the top row, in the colour of the blocks' stones.
            Color fill = SampleFillColour("Assets/Art/Tiles/Ground_01.png");
            WritePng(FillTexturePath, 8, 8, (x, y) => fill);
            Log($"Created {FillTexturePath} (colour {ColorUtility.ToHtmlStringRGB(fill)})");
        }
        ImportSprite(FillTexturePath, 8, FilterMode.Point);

        if (!File.Exists(BubblePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BubblePath));
            WritePng(BubblePath, 64, 64, BubblePixel);
            Log("Created " + BubblePath);
        }
        ImportSprite(BubblePath, 64, FilterMode.Bilinear);
        _bubble = AssetDatabase.LoadAssetAtPath<Sprite>(BubblePath);

        _groundSprites = GroundTextures.Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p)).ToArray();
        _platformSprites = PlatformTextures.Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p)).Where(s => s != null).ToArray();

        if (!AssetDatabase.IsValidFolder(TileAssetsFolder))
            AssetDatabase.CreateFolder("Assets/Art/Tiles", "Tile Assets");
        _top01 = MakeTile("Tile_Ground_01", _groundSprites[0]);
        _top02 = MakeTile("Tile_Ground_02", _groundSprites[1]);
        _top03 = MakeTile("Tile_Ground_03", _groundSprites[2]);
        _topStar = MakeTile("Tile_Ground_Star", _groundSprites[3]);
        _edge = MakeTile("Tile_Ground_Edge", _groundSprites[4]);
        _fill = MakeTile("Tile_Ground_Fill", AssetDatabase.LoadAssetAtPath<Sprite>(FillTexturePath));
        AssetDatabase.SaveAssets();
    }

    private static Tile MakeTile(string name, Sprite sprite)
    {
        string path = $"{TileAssetsFolder}/{name}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.sprite = sprite;
        tile.color = Color.white;
        tile.flags = TileFlags.None;              // Lets a level flip edge tiles / hide wall tiles.
        tile.colliderType = Tile.ColliderType.Grid; // Square collision: no bumps at the rounded corners.
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static Color SampleFillColour(string path)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(path));
        Color sum = Color.black;
        int n = 0;
        for (int y = (int)(tex.height * 0.15f); y < tex.height * 0.45f; y++)
            for (int x = (int)(tex.width * 0.2f); x < tex.width * 0.8f; x++)
            {
                Color c = tex.GetPixel(x, y);
                if (c.a < 0.9f) continue;
                sum += c;
                n++;
            }
        Object.DestroyImmediate(tex);
        if (n == 0)
            return new Color(0.42f, 0.26f, 0.37f);
        Color avg = sum / n;
        return new Color(avg.r * 0.85f, avg.g * 0.85f, avg.b * 0.85f, 1f);
    }

    private static void WritePng(string path, int w, int h, System.Func<int, int, Color> pixel)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, pixel(x, y));
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    private static void ImportSprite(string path, float ppu, FilterMode filter)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = filter;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var s = new TextureImporterSettings();
        importer.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(s);
        importer.SaveAndReimport();
    }

    // A round speech bubble with a "!" (64 x 64), drawn with 4x4 supersampling for smooth edges.
    private static Color BubblePixel(int px, int py)
    {
        var outline = new Color(0.24f, 0.11f, 0.2f, 1f);
        var paper = new Color(1f, 0.95f, 0.86f, 1f);
        Color acc = Color.clear;
        const int N = 4;
        for (int sy = 0; sy < N; sy++)
            for (int sx = 0; sx < N; sx++)
            {
                var p = new Vector2(px + (sx + 0.5f) / N, py + (sy + 0.5f) / N);
                Color c = Color.clear;
                bool outer = Vector2.Distance(p, new Vector2(32f, 37f)) <= 24f ||
                             InTriangle(p, new Vector2(23f, 22f), new Vector2(37f, 18f), new Vector2(20f, 5f));
                bool inner = Vector2.Distance(p, new Vector2(32f, 37f)) <= 20.5f ||
                             InTriangle(p, new Vector2(25.5f, 22f), new Vector2(34f, 19.5f), new Vector2(23f, 10f));
                bool bar = (p.x >= 29f && p.x <= 35f && p.y >= 35f && p.y <= 49f) ||
                           Vector2.Distance(p, new Vector2(32f, 49f)) <= 3f;
                bool dot = Vector2.Distance(p, new Vector2(32f, 28.5f)) <= 3.4f;
                if (outer) c = outline;
                if (inner) c = paper;
                if (inner && (bar || dot)) c = outline;
                acc += c;
            }
        return acc / (N * N);
    }

    private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float Sign(Vector2 p1, Vector2 p2, Vector2 p3) => (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
        bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(neg && pos);
    }

    // ------------------------------------------------------------------ 4. Prefabs

    private const float ChortleScale = 0.65f;

    private static void FixPrefabs()
    {
        EditPrefab("Assets/Prefabs/Player/Pip.prefab", root =>
        {
            Transform sprite = root.transform.Find("Visual/Sprite");
            if (sprite != null)
            {
                var sr = sprite.GetComponent<SpriteRenderer>();
                sr.color = Color.white; // Was the pink placeholder tint.
                AddBreathe(sprite.gameObject, 0.015f, true);
            }
            var swing = root.GetComponent<MalletSwingVisual>();
            if (swing != null)
                swing.enabled = false; // The mallet animation shows the swing now (the box was a placeholder).
            Log("Pip prefab: real colours, no placeholder swing box, idle breathing");
        });

        EditPrefab("Assets/Prefabs/Enemies/WhoopeeWobbler.prefab", root =>
        {
            Sprite alert = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Enemies/Wobbler Animation/Alert/Wobbler Alert.png");
            SetupEnemy(root, 0.9f, "Assets/Art/Enemies/Wobbler Animation/Idle/Wobbler Idle_0000.png",
                       "WhoopeeWobbler_Idle", "WhoopeeWobbler_Hop", "", alert, 0.03f);
        });

        // Chortle was about 7 tiles tall (3x Pip, twice his hitbox). Now about 4.6: a big boss that fits his hitbox.
        // His phase-2 copies get the same size (they copy his drawing every frame, but had scale 1).
        foreach (string path in new[] { "Assets/Prefabs/Boss/Chortle.prefab", "Assets/Prefabs/Boss/ChortleCopy.prefab" })
            EditPrefab(path, root =>
            {
                Transform sprite = root.transform.Find("Sprite");
                if (sprite == null)
                    return;
                sprite.localScale = new Vector3(ChortleScale, ChortleScale, 1f);
                const string frame = "Assets/Art/Enemies/Chortle Animation/Idle/Jester Idle_0000.png";
                float headTop = 4.6f;
                var art = AssetDatabase.LoadAssetAtPath<Sprite>(frame);
                if (art != null)
                {
                    Feet f = MeasureCached(frame);
                    headTop = (f.Top - f.Y) / art.pixelsPerUnit * ChortleScale;
                }
                Transform sign = root.transform.Find("WarningSign");
                if (sign != null)
                    MakeBubble(sign, headTop + 0.5f, new Color(1f, 0.55f, 0.45f, 1f), 0.9f);

                // Blue Sight: the copies turn see-through, the real Chortle stays solid.
                if (root.GetComponent<ChortleCopy>() != null && root.GetComponent<FadeInBlueSight>() == null)
                {
                    root.AddComponent<FadeInBlueSight>();
                    Log("ChortleCopy prefab: see-through while Blue Sight is on");
                }
                Log($"{root.name} prefab: sprite scale {ChortleScale} (about {headTop:0.0} tiles tall), warning bubble above the head");
            });

        EditPrefab("Assets/Prefabs/Enemies/PieSlinger.prefab", root =>
        {
            SetupEnemy(root, 0.8f, "Assets/Art/Enemies/Pie Slinger Animations/Idle/Pie thrower animation idle f_0000.png",
                       "Pieslingger_Idle", "", "Pieslingger_Attack", null, 0.02f);
        });
    }

    private static void EditPrefab(string path, System.Action<GameObject> edit)
    {
        if (!File.Exists(path))
        {
            Log("Missing prefab " + path);
            return;
        }
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void SetupEnemy(GameObject root, float scale, string firstFrame, string idle, string move,
                                   string attack, Sprite alert, float breathe)
    {
        Transform sprite = root.transform.Find("Sprite");
        if (sprite == null)
            return;

        // The frames' pivots are on the feet now, so the sprite sits on the enemy's origin (its feet).
        sprite.localPosition = Vector3.zero;
        sprite.localScale = new Vector3(scale, scale, 1f); // Uniform: no more stretched art.
        var sr = sprite.GetComponent<SpriteRenderer>();
        var frame = AssetDatabase.LoadAssetAtPath<Sprite>(firstFrame);
        if (frame != null)
            sr.sprite = frame; // Shows the real art in the editor and on the first frame.

        var anim = root.GetComponent<EnemySpriteAnimator>() ?? root.AddComponent<EnemySpriteAnimator>();
        var so = new SerializedObject(anim);
        so.FindProperty("idleState").stringValue = idle;
        so.FindProperty("moveState").stringValue = move;
        so.FindProperty("attackState").stringValue = attack;
        so.FindProperty("alertSprite").objectReferenceValue = alert;
        so.ApplyModifiedPropertiesWithoutUndo();

        AddBreathe(sprite.gameObject, breathe, true);

        // "!" bubble just above the head.
        float headTop = 1.5f;
        if (frame != null)
        {
            Feet f = MeasureCached(firstFrame);
            headTop = (f.Top - f.Y) / frame.pixelsPerUnit * scale;
        }
        Transform sign = root.transform.Find("WarningSign");
        if (sign != null)
            MakeBubble(sign, headTop + 0.5f, new Color(1f, 0.55f, 0.45f, 1f), 0.75f);

        Log($"{root.name} prefab: feet on the ground, uniform scale {scale}, plays {idle}/{move}/{attack}" +
            (alert != null ? " + alert pose" : "") + $", warning bubble at {headTop + 0.5f:0.00}");
    }

    private static void AddBreathe(GameObject go, float amount, bool onlyWhenStill)
    {
        var b = go.GetComponent<SpriteBreathe>() ?? go.AddComponent<SpriteBreathe>();
        var so = new SerializedObject(b);
        so.FindProperty("amount").floatValue = amount;
        so.FindProperty("onlyWhenStill").boolValue = onlyWhenStill;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void MakeBubble(Transform t, float y, Color tint, float size)
    {
        var sr = t.GetComponent<SpriteRenderer>();
        if (sr == null || _bubble == null)
            return;
        sr.sprite = _bubble;
        sr.color = tint;
        t.localScale = new Vector3(size, size, 1f);
        t.localPosition = new Vector3(t.localPosition.x, y, t.localPosition.z);
    }

    // ------------------------------------------------------------------ 5. Levels

    private static int _platformIndex;

    private static void FixLevels()
    {
        _platformIndex = 0;
        for (int i = 0; i < LevelScenes.Length; i++)
        {
            string name = LevelScenes[i];
            string path = LevelsFolder + name + ".unity";
            if (!File.Exists(path))
            {
                Log("Missing scene " + path);
                continue;
            }
            EditorUtility.DisplayProgressBar("Demo Polish", "Level " + name, i / (float)LevelScenes.Length);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            SetZoom(scene, name);
            FixCamera(scene, name);
            SwapTiles(scene, name);
            SwapPlatforms(scene, name);
            SwapPrompts(scene);

            if (name == "Tumbleton_PipHouse")
                FixBedroom(scene);
            else if (name == "Tumbleton_DoctorOffice")
                FixDoctorOffice(scene);
            else if (name == "ChortleArena")
                FixArena(scene);
            else if (name == "Tumbleton")
                FixTumbletonBackground(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // Scenes without a Pip camera: only the pixel-perfect camera goes.
        foreach (string name in new[] { "TrainMap", "EndOfDemo" })
        {
            string path = LevelsFolder + name + ".unity";
            if (!File.Exists(path))
                continue;
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (RemovePixelPerfect(scene) > 0)
            {
                Log($"{name}: pixel-perfect camera removed");
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }

    /// <summary>
    /// The levels were built for 16-pixels-per-tile placeholder art with a Pixel Perfect Camera
    /// (480 x 270). With the real, high-resolution art it fought everything: it re-picked the zoom
    /// from the window size (ignoring the level's zoom), so the camera bounds were wrong and it showed
    /// past the art, it added its own bars/cropping, and it snapped the camera to a pixel grid (jitter).
    /// Removed. Also: no sticky slow-down near the level edges, a small dead zone so tiny steps and
    /// jumps don't wobble the view, a little look-ahead, and a night sky colour that matches the art.
    /// </summary>
    private static void FixCamera(Scene scene, string sceneName)
    {
        int removed = RemovePixelPerfect(scene);

        GameObject rigGo = Root(scene, "CameraRig");
        if (rigGo == null)
            return;
        // The camera always follows Pip (same settings PipCameraFollow applies at runtime).
        foreach (var confiner in rigGo.GetComponentsInChildren<CinemachineConfiner2D>(true))
        {
            confiner.enabled = false;
            EditorUtility.SetDirty(confiner);
        }
        foreach (var composer in rigGo.GetComponentsInChildren<CinemachinePositionComposer>(true))
        {
            composer.Damping = new Vector3(0.12f, 0.2f, 0f);
            var comp = composer.Composition;
            comp.ScreenPosition = Vector2.zero;
            comp.DeadZone.Enabled = false;
            comp.HardLimits.Enabled = true;
            comp.HardLimits.Size = new Vector2(0.3f, 0.4f);
            comp.HardLimits.Offset = Vector2.zero;
            composer.Composition = comp;
            var look = composer.Lookahead;
            look.Enabled = false;
            composer.Lookahead = look;
            EditorUtility.SetDirty(composer);
        }
        foreach (Camera cam in rigGo.GetComponentsInChildren<Camera>(true))
        {
            cam.backgroundColor = new Color(0.16f, 0.1f, 0.18f, 1f);
            EditorUtility.SetDirty(cam);
        }
        Log($"{sceneName}: camera fixed ({removed} pixel-perfect parts removed, smooth follow, no sticky edges, night sky colour)");
    }

    private static int RemovePixelPerfect(Scene scene)
    {
        int removed = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null)
                    continue;
                string type = mb.GetType().Name;
                if (type == "PixelPerfectCamera" || type == "CinemachinePixelPerfect")
                {
                    Object.DestroyImmediate(mb);
                    removed++;
                }
            }
        return removed;
    }

    /// <summary>Camera zoom (orthographic size) used in every level, so characters look the same size everywhere.</summary>
    private const float LevelZoom = 7f;

    private static void SetZoom(Scene scene, string sceneName)
    {
        GameObject rigGo = Root(scene, "CameraRig");
        var rig = rigGo != null ? rigGo.GetComponent<CameraRig>() : null;
        if (rig == null || rig.PipCamera == null)
            return;
        var lens = rig.PipCamera.Lens;
        float before = lens.OrthographicSize;
        lens.OrthographicSize = LevelZoom;
        rig.PipCamera.Lens = lens;
        EditorUtility.SetDirty(rig.PipCamera);
        foreach (Camera cam in rigGo.GetComponentsInChildren<Camera>(true))
        {
            cam.orthographicSize = LevelZoom;
            EditorUtility.SetDirty(cam);
        }
        if (!Mathf.Approximately(before, LevelZoom))
            Log($"{sceneName}: camera zoom {before} -> {LevelZoom}");
    }

    /// <summary>
    /// Replaces a door with a walk-through exit at the right edge (same destination and lock):
    /// Pip leaves by walking right. A silent exit doesn't say anything while it's locked.
    /// </summary>
    private static void MakeRightExit(Scene scene, GameObject go, float x, bool silent)
    {
        var door = go.GetComponent<Door>();
        var exit = go.GetComponent<LevelExit>() ?? go.AddComponent<LevelExit>();
        if (door != null)
        {
            var from = new SerializedObject(door);
            var to = new SerializedObject(exit);
            to.CopyFromSerializedProperty(from.FindProperty("destination"));
            to.CopyFromSerializedProperty(from.FindProperty("requirement"));
            to.ApplyModifiedPropertiesWithoutUndo();
            Object.DestroyImmediate(door);
        }
        for (int i = go.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(go.transform.GetChild(i).gameObject); // Door sprite, label, prompt.

        if (silent)
        {
            var so = new SerializedObject(exit);
            so.FindProperty("requirement.lockedDialogue").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        go.transform.position = new Vector3(x, 0f, 0f);
        var box = go.GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1f, 4f);
        box.offset = new Vector2(0f, 2f);
        Log($"{scene.name}: '{go.name}' is now a walk-out exit at the right edge (x {x:0.00})" +
            (silent ? ", silent while locked" : ""));
    }

    private static GameObject Root(Scene scene, string name)
    {
        return scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);
    }

    private static void SwapTiles(Scene scene, string sceneName)
    {
        GameObject grid = Root(scene, "Grid");
        Tilemap map = grid != null ? grid.transform.Find("Ground")?.GetComponent<Tilemap>() : null;
        if (map == null)
            return;

        // Which cells are what (from the placeholder tiles the levels were built with).
        var tops = new List<Vector3Int>();
        var fills = new List<Vector3Int>();
        var walls = new List<Vector3Int>();
        foreach (Vector3Int pos in map.cellBounds.allPositionsWithin)
        {
            TileBase t = map.GetTile(pos);
            if (t == null) continue;
            if (t.name == "Placeholder_Tile_GroundTop") tops.Add(pos);
            else if (t.name == "Placeholder_Tile_Ground") fills.Add(pos);
            else if (t.name == "Placeholder_Tile_Wall") walls.Add(pos);
        }

        if (tops.Count + fills.Count + walls.Count > 0)
        {
            var topSet = new HashSet<Vector3Int>(tops);
            foreach (Vector3Int pos in tops)
            {
                bool leftOpen = map.GetTile(pos + Vector3Int.left) == null;
                bool rightOpen = map.GetTile(pos + Vector3Int.right) == null;
                Tile tile;
                Matrix4x4 m = Matrix4x4.identity;
                if (rightOpen && !leftOpen) tile = _edge;
                else if (leftOpen && !rightOpen) { tile = _edge; m = Matrix4x4.Scale(new Vector3(-1f, 1f, 1f)); }
                else
                {
                    int k = Mathf.Abs(pos.x * 7 + 3) % 10; // Same pattern every time, not random.
                    tile = k < 3 ? _top01 : k < 6 ? _top02 : k < 9 ? _top03 : _topStar;
                }
                map.SetTile(pos, tile);
                map.SetTileFlags(pos, TileFlags.None);
                map.SetTransformMatrix(pos, m);
            }
            foreach (Vector3Int pos in fills)
                map.SetTile(pos, _fill);
            foreach (Vector3Int pos in walls)
            {
                map.SetTile(pos, _fill);
                if (WallsHidden.Contains(sceneName))
                {
                    map.SetTileFlags(pos, TileFlags.None);
                    map.SetColor(pos, new Color(1f, 1f, 1f, 0f));
                }
            }
            Log($"{sceneName}: tiles -> real art ({tops.Count} top blocks, {fills.Count} earth, {walls.Count} wall)" +
                (WallsHidden.Contains(sceneName) ? ", walls invisible" : ""));
        }

        var renderer = map.GetComponent<TilemapRenderer>();
        if (renderer != null && TilesHidden.Contains(sceneName) && renderer.enabled)
        {
            renderer.enabled = false;
            Log($"{sceneName}: tiles are collision only (the background art is the floor)");
        }
    }

    private static void SwapPlatforms(Scene scene, string sceneName)
    {
        if (_platformSprites == null || _platformSprites.Length == 0)
            return;
        int count = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.sprite != _white || sr.name != "Sprite" || sr.transform.parent == null)
                    continue;
                Transform owner = sr.transform.parent;

                // Ledges / gondolas: a thin solid box collider on the parent.
                var box = owner.GetComponent<BoxCollider2D>();
                if (box == null || box.isTrigger || box.size.y > 0.6f || box.size.x < 2.5f) // Not the 2-wide jack-in-the-box lids.
                    continue;

                Sprite art = _platformSprites[_platformIndex++ % _platformSprites.Length];
                float targetW = box.size.x + 0.3f;
                const float targetH = 0.9f;
                Vector3 artSize = art.bounds.size;
                sr.sprite = art;
                sr.drawMode = SpriteDrawMode.Simple;
                sr.transform.localScale = new Vector3(targetW / artSize.x, targetH / artSize.y, 1f);
                float topOfCollider = box.offset.y + box.size.y * 0.5f;
                sr.transform.localPosition = new Vector3(box.offset.x, topOfCollider + 0.05f - targetH * 0.5f, 0f);
                if (owner.GetComponent<BlueSightOnly>() == null)
                    sr.color = Color.white;
                count++;
            }
        if (count > 0)
            Log($"{sceneName}: {count} platforms -> real platform art");
    }

    private static void SwapPrompts(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.name == "Prompt" && sr.sprite == _white)
                    MakeBubble(sr.transform, sr.transform.localPosition.y + 0.2f, Color.white, 0.8f);
    }

    // ---- Rooms

    /// <summary>
    /// Shows the room's whole background on one screen, unstretched: the picture is exactly as tall as
    /// the camera view, its bottom on the camera's lowest point, its left side at leftX. The camera
    /// bounds become the picture, and an invisible wall stops Pip at the right side.
    /// Returns the background renderer (null if missing).
    /// </summary>
    private static SpriteRenderer FitRoom(Scene scene, string backgroundName, out float rightLimit)
    {
        rightLimit = 0f;
        GameObject bgGo = Root(scene, backgroundName);
        GameObject boundsGo = Root(scene, "CameraBounds");
        GameObject rigGo = Root(scene, "CameraRig");
        if (bgGo == null || boundsGo == null)
        {
            Log($"{scene.name}: background '{backgroundName}' or CameraBounds missing, room left alone");
            return null;
        }

        float ortho = 8.4375f;
        var rig = rigGo != null ? rigGo.GetComponent<CameraRig>() : null;
        if (rig != null && rig.PipCamera != null)
            ortho = rig.PipCamera.Lens.OrthographicSize;

        var sr = bgGo.GetComponent<SpriteRenderer>();
        Bounds b = sr.sprite.bounds; // At scale 1, around the pivot.
        float s = 2f * ortho / b.size.y;
        float w = b.size.x * s, h = b.size.y * s;
        const float LeftX = -1f;   // The room's left wall tiles end at x = 0.
        const float BottomY = -3f; // The level's lowest point (the camera never goes lower).
        Vector2 centre = new Vector2(LeftX + w * 0.5f, BottomY + h * 0.5f);

        bgGo.transform.localScale = new Vector3(s, s, 1f);
        bgGo.transform.position = new Vector3(centre.x - b.center.x * s, centre.y - b.center.y * s, 0f);

        var box = boundsGo.GetComponent<BoxCollider2D>();
        boundsGo.transform.position = Vector3.zero;
        box.offset = centre;
        box.size = new Vector2(w, h);

        // Invisible wall: Pip stops 1 tile before the right edge (like the left wall).
        rightLimit = LeftX + w - 1f;
        GameObject grid = Root(scene, "Grid");
        Transform parent = grid != null ? grid.transform : null;
        Transform wall = parent != null ? parent.Find("RoomWall_Right") : null;
        if (wall == null)
        {
            wall = new GameObject("RoomWall_Right").transform;
            wall.SetParent(parent, false);
            wall.gameObject.AddComponent<BoxCollider2D>();
        }
        Transform ground = parent != null ? parent.Find("Ground") : null;
        if (ground != null)
            wall.gameObject.layer = ground.gameObject.layer;
        wall.position = new Vector3(rightLimit + 0.5f, 10f, 0f);
        wall.GetComponent<BoxCollider2D>().size = new Vector2(1f, 40f);

        // No door: Pip leaves by walking out of the right side. She arrives a few steps in from it
        // (arriving inside an exit would need a step out and back in).
        GameObject frontDoor = Root(scene, "FrontDoor");
        if (frontDoor != null)
            MakeRightExit(scene, frontDoor, rightLimit - 0.5f, false);
        GameObject arrival = Root(scene, "SpawnPoint_FromTown");
        if (arrival != null)
            arrival.transform.position = new Vector3(rightLimit - 3f, 0f, 0f);

        // The grey checkpoint post is a placeholder; inside a room it just looks like a stick.
        GameObject checkpoints = Root(scene, "Checkpoints (Combat)");
        if (checkpoints != null)
            foreach (SpriteRenderer post in checkpoints.GetComponentsInChildren<SpriteRenderer>(true))
                if (post.name == "Post")
                    post.enabled = false;

        Log($"{scene.name}: background fits the screen ({w:0.00} x {h:0.00}, uniform scale {s:0.000}), " +
            $"camera bounds = picture, right wall at x {rightLimit:0.00}");
        return sr;
    }

    /// <summary>World position of a pixel of a background picture (pixels from the top-left).</summary>
    private static Vector3 ImageToWorld(SpriteRenderer sr, float px, float pyFromTop)
    {
        Sprite sp = sr.sprite;
        float ppu = sp.pixelsPerUnit;
        float texH = sp.texture.height;
        Vector2 local = new Vector2(
            (px - sp.rect.x - sp.pivot.x) / ppu,
            (texH - pyFromTop - sp.rect.y - sp.pivot.y) / ppu);
        return sr.transform.TransformPoint(local);
    }

    private static void FixBedroom(Scene scene)
    {
        SpriteRenderer bg = FitRoom(scene, "bedroom", out _);
        if (bg == null)
            return;

        // The camera shows the whole bedroom and stays still (FitRoom made the bounds = the picture).
        GameObject boundsGo = Root(scene, "CameraBounds");
        if (boundsGo != null)
            MakeFixedCamera(scene, boundsGo.GetComponent<BoxCollider2D>());

        // The mirror Pip looks into is the one painted on the wall (above the vanity).
        GameObject prologue = Root(scene, "Prologue (Part 6)");
        Transform mirror = prologue != null ? prologue.transform.Find("Mirror") : null;
        if (mirror == null)
        {
            Log("Bedroom: Mirror not found");
            return;
        }
        Vector3 painted = ImageToWorld(bg, 1180f, 365f);
        mirror.position = new Vector3(painted.x, 0f, 0f);
        Transform placeholder = mirror.Find("Sprite");
        if (placeholder != null)
            Object.DestroyImmediate(placeholder.gameObject);
        var range = mirror.GetComponent<BoxCollider2D>();
        range.size = new Vector2(3f, 3f);
        range.offset = new Vector2(0f, 1.5f);
        Transform prompt = mirror.Find("Prompt");
        if (prompt != null)
            prompt.localPosition = new Vector3(0f, 3.1f, 0f);
        Log($"Bedroom: Mirror uses the painted mirror at x {painted.x:0.00} (placeholder box removed)");
    }

    private static void FixDoctorOffice(Scene scene)
    {
        SpriteRenderer bg = FitRoom(scene, "nursery_0", out _);

        // The camera shows the whole office and stays still (FitRoom made the bounds = the picture).
        GameObject boundsGo = Root(scene, "CameraBounds");
        if (bg != null && boundsGo != null)
            MakeFixedCamera(scene, boundsGo.GetComponent<BoxCollider2D>());

        GameObject office = Root(scene, "Doctor's Office (Combat)");
        if (office == null)
            return;

        // Already drawn in the background picture.
        foreach (string n in new[] { "Cabinet", "ExamTable", "EyeChart" })
        {
            Transform t = office.transform.Find(n);
            if (t != null)
            {
                Object.DestroyImmediate(t.gameObject);
                Log($"Doctor's office: removed placeholder '{n}'");
            }
        }

        Transform doc = office.transform.Find("NPC_DrMellow");
        if (doc == null)
            return;
        foreach (string n in new[] { "Head", "Coat" })
        {
            Transform t = doc.Find(n);
            if (t != null)
                Object.DestroyImmediate(t.gameObject);
        }

        // Dr. Mellow stands in front of the medicine cabinet, and you talk to him where he stands.
        float x = bg != null ? ImageToWorld(bg, 720f, 800f).x : 12f;
        doc.position = new Vector3(x, 0f, 0f);
        Transform visual = doc.Find("DoctorVisual");
        float height = 4f;
        if (visual != null)
        {
            visual.localPosition = Vector3.zero;
            visual.localScale = new Vector3(1.1f, 1.1f, 1.1f); // An adult about 1.35x Pip's height (was 1.85x).
            var sr = visual.GetComponent<SpriteRenderer>();
            sr.sortingOrder = 5; // Behind Pip (10), in front of the room.
            const string frame = "Assets/Art/Doctor/Idle/Doctor Idle_0000.png";
            if (sr.sprite != null && File.Exists(frame))
            {
                Feet f = MeasureCached(frame);
                height = (f.Top - f.Y) / sr.sprite.pixelsPerUnit * visual.localScale.y;
            }
            AddBreathe(visual.gameObject, 0.015f, false);
        }
        var range = doc.GetComponent<BoxCollider2D>();
        range.size = new Vector2(2.6f, 3f);
        range.offset = new Vector2(0f, 1.5f);
        Transform prompt = doc.Find("Prompt");
        if (prompt != null)
            prompt.localPosition = new Vector3(0f, height + 0.5f, 0f);
        Log($"Doctor's office: Dr. Mellow at x {x:0.00}, talk range and prompt on him (head at {height:0.00})");
    }

    private static void FixArena(Scene scene)
    {
        // A second, smaller copy of the boss background was left in the scene.
        GameObject copy = Root(scene, "background boss_0");
        if (copy != null)
        {
            Object.DestroyImmediate(copy);
            Log("ChortleArena: removed the duplicate background 'background boss_0'");
        }
        GameObject bg = Root(scene, "background boss");
        if (bg != null)
        {
            Vector3 s = bg.transform.localScale;
            float u = Mathf.Max(s.x, s.y);
            bg.transform.localScale = new Vector3(u, u, 1f);

            // Fixed camera that shows exactly the whole picture (this arena only).
            Bounds pic = bg.GetComponent<SpriteRenderer>().bounds;
            GameObject boundsGo = Root(scene, "CameraBounds");
            if (boundsGo != null)
            {
                var box = boundsGo.GetComponent<BoxCollider2D>();
                boundsGo.transform.position = Vector3.zero;
                box.offset = pic.center;
                box.size = pic.size;
                MakeFixedCamera(scene, box);
            }
        }

        // Leave on the right after the fight (the wall tiles stop Pip at x 32 until then). Silent,
        // so Chortle's "Leaving already?" line can't interrupt the fight at the right edge.
        GameObject flow = Root(scene, "Boss (Part 8)");
        Transform exit = flow != null ? flow.transform.Find("ExitToHub") : null;
        if (exit != null)
            MakeRightExit(scene, exit.gameObject, 31.5f, true);

        FixArenaFloorAndBoss(scene);
        HideChickenCannons(scene);
    }

    /// <summary>
    /// No visible cannons: the launchers stay (same timing and chickens), their body is hidden, and a
    /// "!" bubble pops up where the chicken will come from during the warning, then the chicken flies.
    /// </summary>
    private static void HideChickenCannons(Scene scene)
    {
        GameObject arena = Root(scene, "Arena (Combat)");
        if (arena == null)
            return;
        foreach (RubberChickenCannon cannon in arena.GetComponentsInChildren<RubberChickenCannon>(true))
        {
            Transform body = cannon.transform.Find("Sprite");
            if (body != null && body.GetComponent<SpriteRenderer>() != null)
                body.GetComponent<SpriteRenderer>().enabled = false;

            Transform muzzle = cannon.transform.Find("Muzzle");
            Transform bubbleT = cannon.transform.Find("WarningBubble");
            if (bubbleT == null)
            {
                bubbleT = new GameObject("WarningBubble", typeof(SpriteRenderer)).transform;
                bubbleT.SetParent(cannon.transform, false);
            }
            Vector3 at = muzzle != null ? muzzle.localPosition : Vector3.zero;
            bubbleT.localPosition = at + new Vector3(0f, 0.9f, 0f);
            bubbleT.localScale = new Vector3(0.9f, 0.9f, 1f);
            var sr = bubbleT.GetComponent<SpriteRenderer>();
            sr.sprite = _bubble;
            sr.color = new Color(1f, 0.55f, 0.45f, 1f);
            sr.sortingOrder = 20;
            sr.enabled = false;

            var warn = cannon.GetComponent<ThrowWarningBubble>() ?? cannon.gameObject.AddComponent<ThrowWarningBubble>();
            var so = new SerializedObject(warn);
            so.FindProperty("bubble").objectReferenceValue = sr;
            so.FindProperty("flasher").objectReferenceValue = cannon.GetComponent<SpriteFlasher>();
            so.ApplyModifiedPropertiesWithoutUndo();
            Log($"ChortleArena: '{cannon.name}' hidden; a '!' shows where its chicken comes from before each throw");
        }
    }

    /// <summary>
    /// The arena is a solid floor plus the three platforms: no trapdoors, no pit. Chortle starts in
    /// the middle of the room and teleports anywhere in it, platforms included; his phase-2 copies
    /// are scattered all over the room too.
    /// </summary>
    private static void FixArenaFloorAndBoss(Scene scene)
    {
        GameObject arena = Root(scene, "Arena (Combat)");
        if (arena == null)
            return;

        foreach (string n in new[] { "TrapdoorFloor", "Pit" })
        {
            Transform t = arena.transform.Find(n);
            if (t != null)
            {
                Object.DestroyImmediate(t.gameObject);
                Log($"ChortleArena: removed '{n}'");
            }
        }

        // Close the floor where the pit was: earth below, a row of blocks on top (edges only at the ends).
        GameObject grid = Root(scene, "Grid");
        Tilemap map = grid != null ? grid.transform.Find("Ground")?.GetComponent<Tilemap>() : null;
        if (map != null)
        {
            BoundsInt b = map.cellBounds;
            int filled = 0;
            for (int x = b.xMin; x < b.xMax; x++)
            {
                for (int y = -3; y <= -2; y++)
                    if (map.GetTile(new Vector3Int(x, y, 0)) == null) { map.SetTile(new Vector3Int(x, y, 0), _fill); filled++; }
                var top = new Vector3Int(x, -1, 0);
                if (map.GetTile(top) == null) filled++;
                bool end = x == b.xMin || x == b.xMax - 1;
                int k = Mathf.Abs(x * 7 + 3) % 10;
                Tile tile = end ? _edge : k < 3 ? _top01 : k < 6 ? _top02 : k < 9 ? _top03 : _topStar;
                map.SetTile(top, tile);
                map.SetTileFlags(top, TileFlags.None);
                map.SetTransformMatrix(top, x == b.xMin ? Matrix4x4.Scale(new Vector3(-1f, 1f, 1f)) : Matrix4x4.identity);
            }
            if (filled > 0)
                Log($"ChortleArena: floor closed where the pit was ({filled} tiles)");
        }

        // Spots on the floor all across the room and on top of each platform (pivot = feet).
        var spots = new List<(string name, Vector2 pos)>
        {
            ("Floor_A", new Vector2(7f, 0f)), ("Floor_B", new Vector2(16f, 0f)),
            ("Floor_C", new Vector2(23f, 0f)), ("Floor_D", new Vector2(28.5f, 0f)),
        };
        foreach (string p in new[] { "Platform_Left", "Platform_Top", "Platform_Right" })
        {
            Transform plat = arena.transform.Find(p);
            var box = plat != null ? plat.GetComponent<BoxCollider2D>() : null;
            if (box == null)
                continue;
            Vector2 c = (Vector2)plat.position + box.offset;
            spots.Add(("On_" + p, new Vector2(c.x, c.y + box.size.y * 0.5f)));
        }

        Transform[] stage = MakePoints(arena.transform, "StagePoints", "StagePoint_", spots);
        Transform[] clones = MakePoints(arena.transform, "ClonePoints", "ClonePoint_", spots);

        // The Chortle in this scene: in the middle of the room, using the new spots.
        ChortleBoss boss = Object.FindObjectsByType<ChortleBoss>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(x => x.gameObject.scene == scene);
        if (boss == null)
            return;
        boss.transform.position = new Vector3(16f, 0f, 0f);
        var so = new SerializedObject(boss);
        SetArray(so.FindProperty("stagePoints"), stage);
        SetArray(so.FindProperty("clonePoints"), clones);
        so.ApplyModifiedPropertiesWithoutUndo();
        Log($"ChortleArena: Chortle starts in the middle (x 16); {stage.Length} teleport spots and {clones.Length} " +
            "clone spots all over the room, platforms included");
    }

    /// <summary>
    /// The camera shows exactly this area (the room's background picture) and stays still:
    /// FixedCameraRoom zooms to fit it and keeps the confiner on, with the area as its limits.
    /// </summary>
    private static void MakeFixedCamera(Scene scene, BoxCollider2D area)
    {
        GameObject go = area.gameObject;
        var room = go.GetComponent<FixedCameraRoom>() ?? go.AddComponent<FixedCameraRoom>();
        var so = new SerializedObject(room);
        so.FindProperty("area").objectReferenceValue = area;
        so.ApplyModifiedPropertiesWithoutUndo();

        float zoom = FixedCameraRoom.ZoomFor(area.size);
        GameObject rigGo = Root(scene, "CameraRig");
        var rig = rigGo != null ? rigGo.GetComponent<CameraRig>() : null;
        if (rig != null && rig.PipCamera != null)
        {
            var lens = rig.PipCamera.Lens;
            lens.OrthographicSize = zoom;
            rig.PipCamera.Lens = lens;
            EditorUtility.SetDirty(rig.PipCamera);
            var confiner = rig.PipCamera.GetComponent<CinemachineConfiner2D>();
            if (confiner != null)
            {
                confiner.enabled = true;
                EditorUtility.SetDirty(confiner);
            }
            foreach (Camera cam in rigGo.GetComponentsInChildren<Camera>(true))
            {
                cam.orthographicSize = zoom;
                EditorUtility.SetDirty(cam);
            }
        }
        Log($"{scene.name}: fixed camera showing the whole picture ({area.size.x:0.00} x {area.size.y:0.00}, zoom {zoom:0.00})");
    }

    private static Transform[] MakePoints(Transform parent, string groupName, string prefix, List<(string name, Vector2 pos)> spots)
    {
        Transform group = parent.Find(groupName);
        if (group == null)
        {
            group = new GameObject(groupName).transform;
            group.SetParent(parent, false);
        }
        for (int i = group.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(group.GetChild(i).gameObject);
        var result = new Transform[spots.Count];
        for (int i = 0; i < spots.Count; i++)
        {
            var t = new GameObject(prefix + spots[i].name).transform;
            t.SetParent(group, false);
            t.position = spots[i].pos;
            result[i] = t;
        }
        return result;
    }

    private static void SetArray(SerializedProperty array, Transform[] values)
    {
        if (array == null)
            return;
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    /// <summary>
    /// The road picture was a little shorter than the camera view at the new zoom: make it uniform
    /// and tall enough (bottom kept where it was, so the road stays under Pip), and keep the camera
    /// inside it vertically.
    /// </summary>
    private static void FixTumbletonBackground(Scene scene)
    {
        float top = float.MinValue, bottom = float.MaxValue;
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (!go.name.StartsWith("rode"))
                continue;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
                continue;
            float oldBottom = sr.bounds.min.y;
            float spriteH = sr.sprite.bounds.size.y;
            float s = Mathf.Max(go.transform.localScale.x, go.transform.localScale.y, (2f * LevelZoom + 0.5f) / spriteH);
            go.transform.localScale = new Vector3(s, s, 1f);
            go.transform.position += new Vector3(0f, oldBottom - sr.bounds.min.y, 0f);
            top = Mathf.Max(top, sr.bounds.max.y);
            bottom = Mathf.Min(bottom, sr.bounds.min.y);
        }
        if (top == float.MinValue)
            return;

        float left = float.MaxValue, right = float.MinValue;
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name.StartsWith("rode") && go.GetComponent<SpriteRenderer>() != null)
            {
                Bounds b = go.GetComponent<SpriteRenderer>().bounds;
                left = Mathf.Min(left, b.min.x);
                right = Mathf.Max(right, b.max.x);
            }

        float yMin = Mathf.Max(bottom, -3f), yMax = top;
        foreach (string n in new[] { "CameraBounds", "CameraZone_TownSquare" })
        {
            GameObject go = Root(scene, n);
            var box = go != null ? go.GetComponent<BoxCollider2D>() : null;
            if (box == null)
                continue;
            // Never wider than the road pictures either.
            Vector2 pos = go.transform.position;
            float xMin = Mathf.Max(pos.x + box.offset.x - box.size.x * 0.5f, left);
            float xMax = Mathf.Min(pos.x + box.offset.x + box.size.x * 0.5f, right);
            box.offset = new Vector2((xMin + xMax) * 0.5f - pos.x, (yMin + yMax) * 0.5f - pos.y);
            box.size = new Vector2(xMax - xMin, yMax - yMin);
        }

        // The camera follows Pip, but stops at the edges of the road pictures.
        GameObject levelBounds = Root(scene, "CameraBounds");
        if (levelBounds != null && levelBounds.GetComponent<KeepCameraInBounds>() == null)
            levelBounds.AddComponent<KeepCameraInBounds>();
        GameObject rigGo = Root(scene, "CameraRig");
        if (rigGo != null)
            foreach (var confiner in rigGo.GetComponentsInChildren<CinemachineConfiner2D>(true))
            {
                confiner.enabled = true;
                confiner.Damping = 0f;
                confiner.SlowingDistance = 0f;
                EditorUtility.SetDirty(confiner);
            }
        Log($"Tumbleton: camera follows Pip but stays on the road pictures (x {left:0.00}-{right:0.00}, y {yMin:0.00}-{yMax:0.00})");
    }
}
