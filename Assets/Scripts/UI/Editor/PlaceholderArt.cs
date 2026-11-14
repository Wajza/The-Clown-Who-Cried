using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// EDITOR-ONLY. Makes the placeholder art my UI uses until the artist's real art arrives, in
/// Assets/Art/Placeholders:
///
///   Fonts/Placeholder_PixelFont.ttf        Press Start 2P (free, SIL Open Font License, see the
///                                          LICENSE file next to it). An 8 px pixel font.
///   Fonts/Placeholder_PixelFont TMP.asset  The TextMeshPro version of it (made from the .ttf).
///   UI/Placeholder_UI_Star.png ... etc.    Tiny 8 x 8 icons and a white panel/button square.
///
/// HOW THE ARTIST REPLACES THEM (no code changes)
///   - A sprite: overwrite the PNG with the real picture, KEEPING THE SAME FILE NAME. Unity keeps
///     the file's ID, so every Image using it updates. (Or drag a new sprite onto the Images.)
///   - The font: make a TMP font asset from the real font, then set it as the Default Font
///     Asset in Project Settings > TextMesh Pro, and rebuild the GameSystems prefab.
///
/// Files are only created when MISSING, so running the setup again never overwrites real art.
/// </summary>
public static class PlaceholderArt
{
    public const string Folder = "Assets/Art/Placeholders";
    public const string UiFolder = Folder + "/UI";
    public const string FontTtfPath = Folder + "/Fonts/Placeholder_PixelFont.ttf";
    public const string FontAssetPath = Folder + "/Fonts/Placeholder_PixelFont TMP.asset";

    /// <summary>The pixel font's native size. Use multiples of it (8, 16) for crisp text.</summary>
    public const float FontSize = 8f;

    // 8 x 8 icons, top row first. '#' = white pixel, '.' = transparent.
    private static readonly string[] Star =
    {
        "...##...", "...##...", "########", ".######.", "..####..", ".##..##.", "##....##", "........",
    };
    private static readonly string[] Coin =
    {
        "..####..", ".######.", "##.##.##", "########", "########", "##.##.##", ".######.", "..####..",
    };
    private static readonly string[] Fragment =
    {
        "...##...", "..####..", ".######.", "########", ".######.", "..####..", "...##...", "........",
    };
    private static readonly string[] Shield =
    {
        "########", "#......#", "#......#", "#......#", ".#....#.", ".#....#.", "..#..#..", "...##...",
    };
    private static readonly string[] Page =
    {
        "######..", "#....##.", "#.##..##", "#......#", "#.####.#", "#......#", "#.###..#", "########",
    };
    private static readonly string[] Square =
    {
        "########", "########", "########", "########", "########", "########", "########", "########",
    };

    public static Sprite StarSprite => LoadSprite("Placeholder_UI_Star");
    public static Sprite CoinSprite => LoadSprite("Placeholder_UI_Coin");
    public static Sprite FragmentSprite => LoadSprite("Placeholder_UI_Fragment");
    public static Sprite ShieldSprite => LoadSprite("Placeholder_UI_Shield");
    public static Sprite PageSprite => LoadSprite("Placeholder_UI_Page");
    /// <summary>White square for panels (9-sliced, 2 px border).</summary>
    public static Sprite PanelSprite => LoadSprite("Placeholder_UI_Panel");
    /// <summary>White square for buttons (9-sliced, 2 px border).</summary>
    public static Sprite ButtonSprite => LoadSprite("Placeholder_UI_Button");
    /// <summary>White square for bars (Image Type = Filled).</summary>
    public static Sprite BarSprite => LoadSprite("Placeholder_UI_Bar");

    public static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

    // ------------------------------------------------------------------ Level art (Part 5)

    public const string TileFolder = Folder + "/Tiles";
    public const string LevelFolder = Folder + "/Level";

    /// <summary>Ground tile (16 x 16): brown earth. For everything below the surface.</summary>
    public static UnityEngine.Tilemaps.Tile GroundTile => LoadTile("Placeholder_Tile_Ground");
    /// <summary>Ground surface tile (16 x 16): earth with grass on top. For the top row.</summary>
    public static UnityEngine.Tilemaps.Tile GroundTopTile => LoadTile("Placeholder_Tile_GroundTop");
    /// <summary>Wall tile (16 x 16): grey bricks.</summary>
    public static UnityEngine.Tilemaps.Tile WallTile => LoadTile("Placeholder_Tile_Wall");
    /// <summary>Door (16 x 32).</summary>
    public static Sprite DoorSprite => AssetDatabase.LoadAssetAtPath<Sprite>($"{LevelFolder}/Placeholder_Door.png");

    /// <summary>Creates the tile and door placeholders if missing (called by EnsureAll).</summary>
    private static void EnsureLevelArt()
    {
        Part1Setup.EnsureFolder(TileFolder);
        Part1Setup.EnsureFolder(LevelFolder);

        Color dirt = new Color(0.42f, 0.3f, 0.22f), dirtDark = new Color(0.34f, 0.24f, 0.18f), grass = new Color(0.55f, 0.7f, 0.4f);
        MakeTile("Placeholder_Tile_Ground", (x, y) => (x + y) % 5 == 0 ? dirtDark : dirt);
        MakeTile("Placeholder_Tile_GroundTop", (x, y) => y >= 13 ? grass : (x + y) % 5 == 0 ? dirtDark : dirt);

        Color brick = new Color(0.5f, 0.5f, 0.56f), mortar = new Color(0.38f, 0.38f, 0.44f);
        MakeTile("Placeholder_Tile_Wall", (x, y) => y % 8 == 0 || (x + (y / 8 % 2) * 8) % 16 == 0 ? mortar : brick);

        Color wood = new Color(0.55f, 0.35f, 0.2f), frame = new Color(0.3f, 0.18f, 0.1f), knob = new Color(1f, 0.85f, 0.3f);
        MakeSprite($"{LevelFolder}/Placeholder_Door.png", 16, 32, new Vector2(0.5f, 0f), // Pivot at the bottom.
            (x, y) => x == 0 || x == 15 || y == 31 ? frame : x == 12 && y == 14 ? knob : wood);
    }

    private static UnityEngine.Tilemaps.Tile LoadTile(string name) =>
        AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>($"{TileFolder}/{name}.asset");

    /// <summary>A 16 x 16 PNG plus a Tile asset using it (both only made if missing).</summary>
    private static void MakeTile(string name, System.Func<int, int, Color> pixel)
    {
        string png = $"{TileFolder}/{name}.png";
        MakeSprite(png, 16, 16, new Vector2(0.5f, 0.5f), pixel);

        string tilePath = $"{TileFolder}/{name}.asset";
        if (File.Exists(tilePath))
            return;
        var tile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
        tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(png);
        tile.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.Grid; // A full square for physics.
        AssetDatabase.CreateAsset(tile, tilePath);
    }

    private static void MakeSprite(string path, int w, int h, Vector2 pivot, System.Func<int, int, Color> pixel)
    {
        if (File.Exists(path))
            return; // Never overwrite (it may be the artist's real picture by now).

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, pixel(x, y)); // y = 0 is the bottom row.
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    /// <summary>Creates any missing placeholder files and makes the pixel font TMP's default.</summary>
    public static void EnsureAll()
    {
        Part1Setup.EnsureFolder(UiFolder);
        MakeIcon("Placeholder_UI_Star", Star, 0);
        MakeIcon("Placeholder_UI_Coin", Coin, 0);
        MakeIcon("Placeholder_UI_Fragment", Fragment, 0);
        MakeIcon("Placeholder_UI_Shield", Shield, 0);
        MakeIcon("Placeholder_UI_Page", Page, 0);
        MakeIcon("Placeholder_UI_Panel", Square, 2);
        MakeIcon("Placeholder_UI_Button", Square, 2);
        MakeIcon("Placeholder_UI_Bar", Square, 0);
        MakeFontAsset();
        MakeFontTheDefault();
        EnsureLevelArt();
    }

    // ------------------------------------------------------------------ Icons

    private static void MakeIcon(string name, string[] rows, int border)
    {
        string path = $"{UiFolder}/{name}.png";
        if (File.Exists(path))
            return; // Never overwrite (it may be the artist's real picture by now).

        int h = rows.Length, w = rows[0].Length;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, h - 1 - y, rows[y][x] == '#' ? Color.white : Color.clear); // Row 0 is the top.
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.spriteBorder = new Vector4(border, border, border, border);
        importer.SaveAndReimport();
    }

    private static Sprite LoadSprite(string name) =>
        AssetDatabase.LoadAssetAtPath<Sprite>($"{UiFolder}/{name}.png");

    // ------------------------------------------------------------------ Font

    private static void MakeFontAsset()
    {
        if (Font != null)
            return;

        var ttf = AssetDatabase.LoadAssetAtPath<UnityEngine.Font>(FontTtfPath);
        if (ttf == null)
        {
            Debug.LogError($"PlaceholderArt: {FontTtfPath} is missing.");
            return;
        }

        // Bitmap ("raster") rendering at the font's native 8 px: sharp pixels, no smoothing.
        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(ttf, (int)FontSize, 1, GlyphRenderMode.RASTER_HINTED,
            256, 256, AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: false);
        font.name = Path.GetFileNameWithoutExtension(FontAssetPath);
        AssetDatabase.CreateAsset(font, FontAssetPath);

        // The atlas texture and material must be saved inside the font asset.
        Texture2D atlas = font.atlasTexture;
        atlas.name = font.name + " Atlas";
        atlas.filterMode = FilterMode.Point;
        AssetDatabase.AddObjectToAsset(atlas, font);
        font.material.name = font.name + " Material";
        AssetDatabase.AddObjectToAsset(font.material, font);

        // Pre-fill every plain keyboard character.
        var ascii = new System.Text.StringBuilder();
        for (char c = ' '; c <= '~'; c++)
            ascii.Append(c);
        font.TryAddCharacters(ascii.ToString());

        // Letters the pixel font lacks (fancy quotes, accents...) fall back to TMP's standard font.
        TMP_FontAsset liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (liberation != null)
            font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { liberation };

        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
    }

    /// <summary>New TextMeshPro texts use the pixel font (Project Settings > TextMesh Pro).</summary>
    private static void MakeFontTheDefault()
    {
        TMP_FontAsset font = Font;
        string[] guids = AssetDatabase.FindAssets("t:TMP_Settings");
        if (font == null || guids.Length == 0)
            return;

        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(AssetDatabase.GUIDToAssetPath(guids[0]));
        var so = new SerializedObject(settings);
        SerializedProperty prop = so.FindProperty("m_defaultFontAsset");
        if (prop.objectReferenceValue == font)
            return;

        // Only replace TMP's own default (LiberationSans). If someone chose a real font, keep it.
        if (prop.objectReferenceValue != null && !prop.objectReferenceValue.name.StartsWith("LiberationSans"))
            return;

        prop.objectReferenceValue = font;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
    }
}
