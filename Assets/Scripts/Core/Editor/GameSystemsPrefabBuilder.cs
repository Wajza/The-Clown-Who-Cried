using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// EDITOR-ONLY. Builds Assets/Resources/GameSystems.prefab from scratch, with the systems of
/// every part so far. Each part has its own section below, so running any part's setup rebuilds
/// the complete prefab and never drops what earlier parts added.
///
/// What ends up in the prefab:
///   GameSystems                 GameManager, SceneLoader, PlayerSaveBridge, PlayerDeathHandler   (Part 1)
///                               SystemInput, InteractionInput, DialogueRunner                   (Part 2)
///                               TaskManager                                                      (Part 3)
///   ├─ Fader                    full-screen black fade, drawn above everything                   (Part 1)
///   ├─ DialogueCanvas           the dialogue box (DialogueUI)                                    (Part 2)
///   ├─ TaskCanvas               task tracker (top right) and "New task" banner (TaskTrackerUI)   (Part 3)
///   ├─ HudCanvas                stars, tear gauge, Blue Sight, coins, fragments, boss bar        (Part 4)
///   ├─ MenuCanvas               pause menu, Journal, Settings (MenuController)                   (Part 4)
///   └─ EventSystem              lets keyboard/gamepad/mouse use menu buttons                     (Part 4)
///
/// Drawing order (canvas sort order): HUD 40 < tasks 50 < dialogue 100 < menus 200 < fader 1000.
///
/// UI sizes are in "game pixels": the canvases scale a 480 x 270 layout up to the real screen,
/// the same way the Pixel Perfect Camera scales the game (x4 at 1920 x 1080).
///
/// ART: every picture is a placeholder from Assets/Art/Placeholders (see PlaceholderArt), and
/// all text uses TextMeshPro's Default Font Asset (the placeholder pixel font, 8 px). Objects
/// are named after what they show (e.g. "StarTemplate", "TearGaugeBar") so the artist can find them.
///
/// Tip: if you change the prefab by hand, rebuilding will overwrite those changes. Change the
/// numbers here instead, or stop using the builder once the layout is final.
/// </summary>
public static class GameSystemsPrefabBuilder
{
    private const string SystemControlsPath = "Assets/Settings/SystemControls.inputactions";
    private static readonly Vector2 ReferenceResolution = new Vector2(480f, 270f);

    [MenuItem("Tools/Clown Who Cried/Rebuild GameSystems Prefab")]
    public static void Build()
    {
        Part1Setup.EnsureFolder(Part1Setup.ResourcesFolder);
        PlaceholderArt.EnsureAll(); // Placeholder sprites + pixel font (only made if missing).

        var root = new GameObject("GameSystems");
        AddCore(root);      // Part 1
        AddDialogue(root);  // Part 2
        AddTasks(root);     // Part 3
        AddHud(root);       // Part 4
        AddMenus(root);     // Part 4

        PrefabUtility.SaveAsPrefabAsset(root, Part1Setup.PrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log($"GameSystemsPrefabBuilder: saved {Part1Setup.PrefabPath}");
    }

    // ------------------------------------------------------------------ Part 1: Core

    private static void AddCore(GameObject root)
    {
        root.AddComponent<GameManager>();
        root.AddComponent<SceneLoader>();
        root.AddComponent<PlayerSaveBridge>();
        root.AddComponent<PlayerDeathHandler>();

        // Fader: a canvas above everything, with a CanvasGroup + ScreenFader and a black Image.
        GameObject fader = CreateCanvas("Fader", root.transform, sortingOrder: 1000);
        fader.AddComponent<CanvasGroup>();
        fader.AddComponent<GraphicRaycaster>(); // Lets the black screen block clicks while visible.
        fader.AddComponent<ScreenFader>();

        Image black = CreateImage("Black", fader.transform, Color.black);
        Stretch(black.rectTransform);
    }

    // ------------------------------------------------------------------ Part 2: Dialogue

    private static void AddDialogue(GameObject root)
    {
        // Input for my systems (separate from my teammate's PlayerControls).
        var systemInput = root.AddComponent<SystemInput>();
        SetField(systemInput, "controls", AssetDatabase.LoadAssetAtPath<InputActionAsset>(SystemControlsPath));

        root.AddComponent<InteractionInput>();
        var runner = root.AddComponent<DialogueRunner>();

        // ---- Canvas (below the fader, so fades cover the dialogue box too).
        GameObject canvas = CreateCanvas("DialogueCanvas", root.transform, sortingOrder: 100);
        var ui = canvas.AddComponent<DialogueUI>();

        // ---- Box along the bottom of the screen: 8 px from the sides, 6 px up, 70 px tall.
        Image panel = CreateImage("Panel", canvas.transform, new Color(0.08f, 0.06f, 0.12f, 0.92f));
        RectTransform p = panel.rectTransform;
        p.anchorMin = new Vector2(0f, 0f);
        p.anchorMax = new Vector2(1f, 0f);
        p.pivot = new Vector2(0.5f, 0f);
        p.offsetMin = new Vector2(8f, 6f);
        p.offsetMax = new Vector2(-8f, 76f);

        // Portrait: 56 x 56 on the left.
        Image portrait = CreateImage("Portrait", panel.transform, Color.white);
        portrait.preserveAspect = true;
        RectTransform pr = portrait.rectTransform;
        pr.anchorMin = pr.anchorMax = new Vector2(0f, 0.5f);
        pr.pivot = new Vector2(0f, 0.5f);
        pr.anchoredPosition = new Vector2(6f, 0f);
        pr.sizeDelta = new Vector2(56f, 56f);

        // Name along the top, body text under it. (DialogueUI indents both when there's a portrait.)
        TMP_Text nameText = CreateText("Name", panel.transform, "Name", PlaceholderArt.FontSize, TextAlignmentOptions.TopLeft);
        SetRect(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -18f), new Vector2(-8f, -4f));

        TMP_Text body = CreateText("Body", panel.transform, "", PlaceholderArt.FontSize, TextAlignmentOptions.TopLeft);
        SetRect(body.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 6f), new Vector2(-16f, -20f));

        // "Press to continue" marker, bottom right.
        TMP_Text arrow = CreateText("ContinueArrow", panel.transform, "v", PlaceholderArt.FontSize, TextAlignmentOptions.Center);
        RectTransform ar = arrow.rectTransform;
        ar.anchorMin = ar.anchorMax = new Vector2(1f, 0f);
        ar.pivot = new Vector2(1f, 0f);
        ar.anchoredPosition = new Vector2(-5f, 3f);
        ar.sizeDelta = new Vector2(10f, 10f);

        // ---- Choices: a small box above the right end of the dialogue box, 3 answer slots.
        Image choices = CreateImage("Choices", canvas.transform, new Color(0.08f, 0.06f, 0.12f, 0.92f));
        RectTransform cr = choices.rectTransform;
        cr.anchorMin = cr.anchorMax = new Vector2(1f, 0f);
        cr.pivot = new Vector2(1f, 0f);
        cr.anchoredPosition = new Vector2(-8f, 80f);
        cr.sizeDelta = new Vector2(150f, 46f);
        var layout = choices.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 4, 4);
        layout.spacing = 1f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;

        var choiceTexts = new TMP_Text[3];
        for (int i = 0; i < choiceTexts.Length; i++)
        {
            choiceTexts[i] = CreateText($"Choice{i + 1}", choices.transform, "Answer", PlaceholderArt.FontSize, TextAlignmentOptions.Left);
            choiceTexts[i].gameObject.AddComponent<LayoutElement>().preferredHeight = 11f;
        }

        // ---- Wire everything into DialogueUI and DialogueRunner.
        var so = new SerializedObject(ui);
        so.FindProperty("panel").objectReferenceValue = panel.gameObject;
        so.FindProperty("portrait").objectReferenceValue = portrait;
        so.FindProperty("nameText").objectReferenceValue = nameText;
        so.FindProperty("bodyText").objectReferenceValue = body;
        so.FindProperty("continueArrow").objectReferenceValue = arrow.gameObject;
        so.FindProperty("choicesPanel").objectReferenceValue = choices.gameObject;
        SerializedProperty slots = so.FindProperty("choiceTexts");
        slots.arraySize = choiceTexts.Length;
        for (int i = 0; i < choiceTexts.Length; i++)
            slots.GetArrayElementAtIndex(i).objectReferenceValue = choiceTexts[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        SetField(runner, "ui", ui);

        // Hidden until a conversation starts (DialogueUI.Awake also hides it).
        panel.gameObject.SetActive(false);
        choices.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ Part 3: Tasks

    private static void AddTasks(GameObject root)
    {
        root.AddComponent<TaskManager>();

        // ---- Canvas below the dialogue box (sort order 50), so a conversation covers it.
        GameObject canvas = CreateCanvas("TaskCanvas", root.transform, sortingOrder: 50);
        var tracker = canvas.AddComponent<TaskTrackerUI>();
        Color boxColor = new Color(0.08f, 0.06f, 0.12f, 0.8f);

        // ---- Tracker: top right, 190 px wide, as tall as its text needs.
        Image panel = CreateImage("Tracker", canvas.transform, boxColor);
        RectTransform p = panel.rectTransform;
        p.anchorMin = p.anchorMax = p.pivot = new Vector2(1f, 1f);
        p.anchoredPosition = new Vector2(-6f, -6f);
        p.sizeDelta = new Vector2(190f, 20f);
        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(5, 5, 3, 4);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text trackerText = CreateText("Text", panel.transform, "", PlaceholderArt.FontSize, TextAlignmentOptions.TopLeft);

        // ---- Banner: top centre, fades in and out.
        Image banner = CreateImage("Banner", canvas.transform, boxColor);
        RectTransform b = banner.rectTransform;
        b.anchorMin = b.anchorMax = b.pivot = new Vector2(0.5f, 1f);
        b.anchoredPosition = new Vector2(0f, -80f); // Below the tracker, so they never overlap.
        b.sizeDelta = new Vector2(320f, 16f);
        var bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
        bannerGroup.alpha = 0f;
        bannerGroup.blocksRaycasts = false;

        TMP_Text bannerText = CreateText("Text", banner.transform, "", PlaceholderArt.FontSize, TextAlignmentOptions.Center);
        Stretch(bannerText.rectTransform);

        // ---- Wire into TaskTrackerUI.
        var so = new SerializedObject(tracker);
        so.FindProperty("trackerPanel").objectReferenceValue = panel.gameObject;
        so.FindProperty("trackerText").objectReferenceValue = trackerText;
        so.FindProperty("banner").objectReferenceValue = bannerGroup;
        so.FindProperty("bannerText").objectReferenceValue = bannerText;
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false); // Hidden until a task starts.
    }

    // ------------------------------------------------------------------ Part 4: HUD

    private static readonly Color HudBackColor = new Color(0.08f, 0.06f, 0.12f, 0.75f);

    private static void AddHud(GameObject root)
    {
        GameObject canvas = CreateCanvas("HudCanvas", root.transform, sortingOrder: 40);
        var hud = canvas.AddComponent<HudController>();
        RectTransform hudRoot = CreateEmpty("Root", canvas.transform);
        Stretch(hudRoot);
        SetField(hud, "root", hudRoot.gameObject);

        // Top-left column, 6 px from the corner. Each piece = a holder with the script (always
        // active, so it can notice a new Pip) + a "Content" child it shows/hides.

        // ---- Smile Stars: a row of star icons, then the shield icon.
        RectTransform starsContent = CreateHudPiece<HudStars>("HudStars", hudRoot, 6f, out HudStars stars);
        AddRowLayout(starsContent);
        Image starTemplate = CreateIcon("StarTemplate", starsContent, PlaceholderArt.StarSprite, Color.white);
        starTemplate.gameObject.SetActive(false);
        Image shield = CreateIcon("ShieldIcon", starsContent, PlaceholderArt.ShieldSprite, new Color(0.5f, 0.8f, 1f));
        shield.gameObject.SetActive(false);
        SetField(stars, "starTemplate", starTemplate);
        SetField(stars, "shieldIcon", shield.gameObject);

        // ---- Tear Gauge and Blue Sight: a label and a bar each.
        RectTransform tearContent = CreateHudPiece<HudTearGauge>("HudTearGauge", hudRoot, 17f, out HudTearGauge tear);
        SetField(tear, "bar", CreateLabelledBar(tearContent, "TEAR", new Color(0.45f, 0.7f, 1f), new Color(1f, 1f, 1f)));

        RectTransform blueContent = CreateHudPiece<HudBlueSight>("HudBlueSight", hudRoot, 27f, out HudBlueSight blue);
        SetField(blue, "bar", CreateLabelledBar(blueContent, "BLUE", new Color(0.3f, 0.4f, 0.9f), new Color(0.5f, 0.9f, 1f)));

        // ---- Star Coins: a row of coin icons.
        RectTransform coinsContent = CreateHudPiece<HudCoins>("HudCoins", hudRoot, 37f, out HudCoins coins);
        AddRowLayout(coinsContent);
        Image coinTemplate = CreateIcon("CoinTemplate", coinsContent, PlaceholderArt.CoinSprite, Color.white);
        coinTemplate.gameObject.SetActive(false);
        SetField(coins, "coinTemplate", coinTemplate);

        // ---- Star Fragments: icon + "x 0".
        RectTransform fragContent = CreateHudPiece<HudFragments>("HudFragments", hudRoot, 47f, out HudFragments fragments);
        CreateIcon("FragmentIcon", fragContent, PlaceholderArt.FragmentSprite, new Color(0.8f, 0.7f, 1f));
        TMP_Text fragText = CreateText("Count", fragContent, "x 0", PlaceholderArt.FontSize, TextAlignmentOptions.Left);
        Place(fragText.rectTransform, new Vector2(0f, 1f), new Vector2(11f, 0f), new Vector2(60f, 8f));
        SetField(fragments, "countText", fragText);

        // ---- Boss bar: bottom centre (name + phase above a long bar).
        RectTransform bossHolder = CreateEmpty("BossHealthBar", hudRoot);
        Place(bossHolder, new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(240f, 20f));
        var boss = bossHolder.gameObject.AddComponent<BossHealthBar>();
        RectTransform bossContent = CreateEmpty("Content", bossHolder);
        Stretch(bossContent);
        TMP_Text bossName = CreateText("BossName", bossContent, "Boss", PlaceholderArt.FontSize, TextAlignmentOptions.Left);
        Place(bossName.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(180f, 9f));
        TMP_Text phase = CreateText("Phase", bossContent, "", PlaceholderArt.FontSize, TextAlignmentOptions.Right);
        Place(phase.rectTransform, new Vector2(1f, 1f), Vector2.zero, new Vector2(80f, 9f));
        HudBar bossBar = CreateBar("BossBar", bossContent, new Color(0.9f, 0.3f, 0.4f), Color.white);
        Place((RectTransform)bossBar.transform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(240f, 7f));
        SetField(boss, "content", bossContent.gameObject);
        SetField(boss, "bar", bossBar);
        SetField(boss, "nameText", bossName);
        SetField(boss, "phaseText", phase);
        bossContent.gameObject.SetActive(false);
    }

    /// <summary>A HUD piece at (6, -top) in the top-left: holder with script T + "Content" child.</summary>
    private static RectTransform CreateHudPiece<T>(string name, Transform parent, float top, out T script)
        where T : PlayerHudElement
    {
        RectTransform holder = CreateEmpty(name, parent);
        Place(holder, new Vector2(0f, 1f), new Vector2(6f, -top), new Vector2(120f, 8f));
        script = holder.gameObject.AddComponent<T>();

        RectTransform content = CreateEmpty("Content", holder);
        Stretch(content);
        SetField(script, "content", content.gameObject);
        return content;
    }

    private static void AddRowLayout(RectTransform row)
    {
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 2f;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleLeft;
    }

    private static Image CreateIcon(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        Place(image.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(8f, 8f)); // Icons are 8 x 8.
        return image;
    }

    /// <summary>"TEAR [=====    ]": a 32 px label, then a 60 x 5 bar.</summary>
    private static HudBar CreateLabelledBar(RectTransform content, string label, Color normal, Color highlight)
    {
        TMP_Text text = CreateText("Label", content, label, PlaceholderArt.FontSize, TextAlignmentOptions.Left);
        Place(text.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(34f, 8f));
        HudBar bar = CreateBar("Bar", content, normal, highlight);
        Place((RectTransform)bar.transform, new Vector2(0f, 1f), new Vector2(36f, -1.5f), new Vector2(60f, 5f));
        return bar;
    }

    /// <summary>A bar: dark background with a "Filled" Image on top (HudBar).</summary>
    private static HudBar CreateBar(string name, Transform parent, Color normal, Color highlight)
    {
        Image back = CreateImage(name, parent, HudBackColor);
        back.raycastTarget = false;
        Image fill = CreateImage("Fill", back.transform, normal);
        fill.sprite = PlaceholderArt.BarSprite;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.raycastTarget = false;
        Stretch(fill.rectTransform);

        var bar = back.gameObject.AddComponent<HudBar>();
        SetField(bar, "fill", fill);
        bar.SetColors(normal, highlight);
        return bar;
    }

    // ------------------------------------------------------------------ Part 4: Menus

    private static readonly Color MenuBoxColor = new Color(0.1f, 0.08f, 0.15f, 0.97f);

    private static void AddMenus(GameObject root)
    {
        GameObject canvas = CreateCanvas("MenuCanvas", root.transform, sortingOrder: 200);
        canvas.AddComponent<GraphicRaycaster>(); // Mouse clicks on buttons.
        var menu = canvas.AddComponent<MenuController>();

        // ---- Pause menu: dimmed screen, a box with 4 buttons.
        Image pause = CreateImage("PausePanel", canvas.transform, new Color(0f, 0f, 0f, 0.55f));
        Stretch(pause.rectTransform);
        Image pauseBox = CreateImage("Box", pause.transform, MenuBoxColor);
        Place(pauseBox.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 110f));
        TMP_Text pauseTitle = CreateText("Title", pauseBox.transform, "PAUSED", PlaceholderArt.FontSize, TextAlignmentOptions.Center);
        Place(pauseTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(140f, 10f));
        RectTransform buttons = CreateButtonColumn(pauseBox.transform, new Vector2(0f, -26f), 4);
        Button resume = CreateButton("ResumeButton", buttons, "Resume", menu.CloseAll);
        Button journalButton = CreateButton("JournalButton", buttons, "Journal", menu.OpenJournal);
        Button settingsButton = CreateButton("SettingsButton", buttons, "Settings", menu.OpenSettings);
        Button quit = CreateButton("QuitToTitleButton", buttons, "Quit to Title", menu.QuitToTitle);

        JournalUI journal = BuildJournal(canvas.transform);
        SettingsMenu settings = BuildSettings(canvas.transform, menu);

        var so = new SerializedObject(menu);
        so.FindProperty("pausePanel").objectReferenceValue = pause.gameObject;
        so.FindProperty("resumeButton").objectReferenceValue = resume;
        so.FindProperty("journalButton").objectReferenceValue = journalButton;
        so.FindProperty("settingsButton").objectReferenceValue = settingsButton;
        so.FindProperty("quitToTitleButton").objectReferenceValue = quit;
        so.FindProperty("journal").objectReferenceValue = journal;
        so.FindProperty("settings").objectReferenceValue = settings;
        so.ApplyModifiedPropertiesWithoutUndo();

        pause.gameObject.SetActive(false);
        journal.gameObject.SetActive(false);
        settings.gameObject.SetActive(false);

        // ---- One EventSystem for the whole game (keyboard/gamepad/mouse on menu buttons).
        // InputSystemUIInputModule uses the Input System's default UI keys when it has none set.
        var eventSystem = new GameObject("EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule),
            typeof(SingleEventSystem));
        eventSystem.transform.SetParent(root.transform, false);
    }

    private static JournalUI BuildJournal(Transform canvas)
    {
        // Covers the whole screen (opaque), so the game, HUD and banners don't show through.
        Image panel = CreateImage("JournalPanel", canvas, new Color(0.1f, 0.08f, 0.15f, 1f));
        Stretch(panel.rectTransform);
        var journal = panel.gameObject.AddComponent<JournalUI>();

        TMP_Text pageTitle = CreateText("PageTitle", panel.transform, "< Task Board >", PlaceholderArt.FontSize, TextAlignmentOptions.Center);
        SetRect(pageTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -16f), new Vector2(-8f, -6f));
        pageTitle.color = new Color(1f, 0.85f, 0.3f);

        TMP_Text fragmentsText = CreateText("Fragments", panel.transform, "", PlaceholderArt.FontSize, TextAlignmentOptions.Center);
        SetRect(fragmentsText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -30f), new Vector2(-8f, -20f));

        // List on the left 45%, details on the right.
        TMP_Text list = CreateText("List", panel.transform, "", PlaceholderArt.FontSize, TextAlignmentOptions.TopLeft);
        SetRect(list.rectTransform, new Vector2(0f, 0f), new Vector2(0.45f, 1f), new Vector2(10f, 22f), new Vector2(-4f, -40f));
        list.lineSpacing = 50f;
        TMP_Text detail = CreateText("Detail", panel.transform, "", PlaceholderArt.FontSize, TextAlignmentOptions.TopLeft);
        SetRect(detail.rectTransform, new Vector2(0.45f, 0f), new Vector2(1f, 1f), new Vector2(6f, 22f), new Vector2(-10f, -40f));
        detail.lineSpacing = 40f;

        TMP_Text hint = CreateText("Hint", panel.transform, "A/D: page   W/S: choose   Tab/Esc: close",
            PlaceholderArt.FontSize, TextAlignmentOptions.Center);
        SetRect(hint.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(8f, 6f), new Vector2(-8f, 16f));
        hint.color = new Color(0.6f, 0.6f, 0.65f);

        var so = new SerializedObject(journal);
        so.FindProperty("pageTitleText").objectReferenceValue = pageTitle;
        so.FindProperty("fragmentsText").objectReferenceValue = fragmentsText;
        so.FindProperty("listText").objectReferenceValue = list;
        so.FindProperty("detailText").objectReferenceValue = detail;
        so.ApplyModifiedPropertiesWithoutUndo();
        return journal;
    }

    private static SettingsMenu BuildSettings(Transform canvas, MenuController menu)
    {
        Image panel = CreateImage("SettingsPanel", canvas, MenuBoxColor);
        Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 150f));
        var settings = panel.gameObject.AddComponent<SettingsMenu>();

        TMP_Text title = CreateText("Title", panel.transform, "SETTINGS", PlaceholderArt.FontSize, TextAlignmentOptions.Center);
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(280f, 10f));

        // One row per setting: label | control | value.
        Slider music = SettingsSliderRow(panel.transform, "Music", 0, out TMP_Text musicValue);
        Slider sfx = SettingsSliderRow(panel.transform, "Sound", 1, out TMP_Text sfxValue);
        Toggle shake = SettingsToggleRow(panel.transform, "Screen shake", 2);
        Slider shakeStrength = SettingsSliderRow(panel.transform, "Shake amount", 3, out TMP_Text shakeValue);
        Toggle hitPause = SettingsToggleRow(panel.transform, "Hit pause", 4);

        Button back = CreateButton("BackButton", panel.transform, "Back", menu.Back);
        Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(80f, 14f));

        var so = new SerializedObject(settings);
        so.FindProperty("musicSlider").objectReferenceValue = music;
        so.FindProperty("sfxSlider").objectReferenceValue = sfx;
        so.FindProperty("screenShakeToggle").objectReferenceValue = shake;
        so.FindProperty("screenShakeStrengthSlider").objectReferenceValue = shakeStrength;
        so.FindProperty("hitPauseToggle").objectReferenceValue = hitPause;
        so.FindProperty("musicValue").objectReferenceValue = musicValue;
        so.FindProperty("sfxValue").objectReferenceValue = sfxValue;
        so.FindProperty("shakeStrengthValue").objectReferenceValue = shakeValue;
        so.ApplyModifiedPropertiesWithoutUndo();
        return settings;
    }

    private static float RowY(int row) => -26f - row * 18f;

    private static TMP_Text RowLabel(Transform panel, string label, int row)
    {
        TMP_Text text = CreateText(label.Replace(" ", "") + "Label", panel, label, PlaceholderArt.FontSize, TextAlignmentOptions.Left);
        Place(text.rectTransform, new Vector2(0f, 1f), new Vector2(12f, RowY(row)), new Vector2(120f, 10f));
        return text;
    }

    private static Slider SettingsSliderRow(Transform panel, string label, int row, out TMP_Text value)
    {
        RowLabel(panel, label, row);

        // Slider: background, a fill that grows from the left, and a handle.
        Image back = CreateImage(label.Replace(" ", "") + "Slider", panel, new Color(0.25f, 0.22f, 0.32f));
        Place(back.rectTransform, new Vector2(0f, 1f), new Vector2(136f, RowY(row) - 2f), new Vector2(100f, 6f));
        var slider = back.gameObject.AddComponent<Slider>();

        RectTransform fillArea = CreateEmpty("Fill Area", back.transform);
        Stretch(fillArea);
        Image fill = CreateImage("Fill", fillArea, new Color(1f, 0.8f, 0.35f));
        fill.raycastTarget = false;
        Stretch(fill.rectTransform); // The Slider sets the width; full height of the bar.
        RectTransform handleArea = CreateEmpty("Handle Slide Area", back.transform);
        Stretch(handleArea);
        Image handle = CreateImage("Handle", handleArea, Color.white);
        SetRect(handle.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(-2f, -2f), new Vector2(2f, 2f)); // 4 px wide, a bit taller than the bar.

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.colors = SelectableColors();

        value = CreateText(label.Replace(" ", "") + "Value", panel, "100%", PlaceholderArt.FontSize, TextAlignmentOptions.Right);
        Place(value.rectTransform, new Vector2(0f, 1f), new Vector2(244f, RowY(row)), new Vector2(44f, 10f));
        return slider;
    }

    private static Toggle SettingsToggleRow(Transform panel, string label, int row)
    {
        RowLabel(panel, label, row);

        // Toggle: a box with a check mark inside.
        Image box = CreateImage(label.Replace(" ", "") + "Toggle", panel, new Color(0.25f, 0.22f, 0.32f));
        Place(box.rectTransform, new Vector2(0f, 1f), new Vector2(136f, RowY(row)), new Vector2(10f, 10f));
        Image check = CreateImage("Checkmark", box.transform, new Color(1f, 0.8f, 0.35f));
        SetRect(check.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
        check.raycastTarget = false;

        var toggle = box.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = box;
        toggle.graphic = check;
        toggle.colors = SelectableColors();
        return toggle;
    }

    /// <summary>A column for menu buttons (each 120 x 14, 4 px apart), centred under a title.</summary>
    private static RectTransform CreateButtonColumn(Transform parent, Vector2 topCentre, int count)
    {
        RectTransform column = CreateEmpty("Buttons", parent);
        Place(column, new Vector2(0.5f, 1f), topCentre, new Vector2(120f, count * 18f));
        var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return column;
    }

    /// <summary>
    /// A menu button: Placeholder_UI_Button + a text. On Click is set to 'onClick' as a persistent
    /// (Inspector-visible) listener, so it survives art changes.
    /// </summary>
    internal static Button CreateButton(string name, Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = PlaceholderArt.ButtonSprite;
        image.type = Image.Type.Sliced;
        go.GetComponent<LayoutElement>().preferredHeight = 14f;
        ((RectTransform)go.transform).sizeDelta = new Vector2(120f, 14f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.colors = SelectableColors();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, onClick);

        TMP_Text text = CreateText("Label", go.transform, label, PlaceholderArt.FontSize, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        return button;
    }

    /// <summary>Dark purple normally; gold when highlighted/selected (keyboard or mouse).</summary>
    private static ColorBlock SelectableColors()
    {
        ColorBlock c = ColorBlock.defaultColorBlock;
        c.normalColor = new Color(0.25f, 0.2f, 0.35f);
        c.highlightedColor = new Color(0.85f, 0.6f, 0.2f);
        c.selectedColor = new Color(0.85f, 0.6f, 0.2f);
        c.pressedColor = new Color(1f, 0.8f, 0.4f);
        c.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        c.fadeDuration = 0f; // Instant: pixel games don't fade highlights (and time may be paused).
        return c;
    }

    // ------------------------------------------------------------------ UI helpers

    internal static RectTransform CreateEmpty(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    /// <summary>Anchors and pivots a rect at one point (0..1 of the parent), then offsets and sizes it.</summary>
    internal static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    /// <summary>A Screen Space - Overlay canvas that scales a 480 x 270 layout to the screen.</summary>
    internal static GameObject CreateCanvas(string name, Transform parent, int sortingOrder)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(parent, false);

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        canvas.pixelPerfect = true;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;
        return go;
    }

    internal static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = PlaceholderArt.PanelSprite; // White square; the artist can replace the PNG.
        image.type = Image.Type.Sliced;
        image.color = color;
        return image;
    }

    internal static TMP_Text CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();

        if (TMP_Settings.defaultFontAsset == null)
            Debug.LogError("GameSystemsPrefabBuilder: no TextMeshPro font. Run Tools > Clown Who Cried > Part 2 > Import TMP Essentials first.");
        // The Default Font Asset (Project Settings > TextMesh Pro) is the placeholder pixel font
        // after Part 4 (PlaceholderArt). Pixel fonts look sharpest at their own size (8) or 16.
        tmp.font = TMP_Settings.defaultFontAsset;

        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        tmp.lineSpacing = 25f; // The pixel font's lines touch otherwise (+25% of the size between lines).
        return tmp;
    }

    internal static void Stretch(RectTransform rect) => SetRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

    internal static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    /// <summary>Sets a private [SerializeField], the same way dragging it in the Inspector does.</summary>
    internal static void SetField(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
