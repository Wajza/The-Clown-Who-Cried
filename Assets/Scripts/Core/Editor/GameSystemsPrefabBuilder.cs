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
///   └─ TaskCanvas               task tracker (top right) and "New task" banner (TaskTrackerUI)   (Part 3)
///
/// UI sizes are in "game pixels": the canvases scale a 480 x 270 layout up to the real screen,
/// the same way the Pixel Perfect Camera scales the game (x4 at 1920 x 1080).
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

        var root = new GameObject("GameSystems");
        AddCore(root);      // Part 1
        AddDialogue(root);  // Part 2
        AddTasks(root);     // Part 3

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
        TMP_Text nameText = CreateText("Name", panel.transform, "Name", 11f, TextAlignmentOptions.TopLeft);
        nameText.fontStyle = FontStyles.Bold;
        SetRect(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -18f), new Vector2(-8f, -4f));

        TMP_Text body = CreateText("Body", panel.transform, "", 10f, TextAlignmentOptions.TopLeft);
        SetRect(body.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 6f), new Vector2(-16f, -20f));

        // "Press to continue" marker, bottom right.
        TMP_Text arrow = CreateText("ContinueArrow", panel.transform, "v", 10f, TextAlignmentOptions.Center);
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
            choiceTexts[i] = CreateText($"Choice{i + 1}", choices.transform, "Answer", 10f, TextAlignmentOptions.Left);
            choiceTexts[i].gameObject.AddComponent<LayoutElement>().preferredHeight = 12f;
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

        // ---- Tracker: top right, 150 px wide, as tall as its text needs.
        Image panel = CreateImage("Tracker", canvas.transform, boxColor);
        RectTransform p = panel.rectTransform;
        p.anchorMin = p.anchorMax = p.pivot = new Vector2(1f, 1f);
        p.anchoredPosition = new Vector2(-6f, -6f);
        p.sizeDelta = new Vector2(150f, 20f);
        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(5, 5, 3, 4);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text trackerText = CreateText("Text", panel.transform, "", 9f, TextAlignmentOptions.TopLeft);

        // ---- Banner: top centre, fades in and out.
        Image banner = CreateImage("Banner", canvas.transform, boxColor);
        RectTransform b = banner.rectTransform;
        b.anchorMin = b.anchorMax = b.pivot = new Vector2(0.5f, 1f);
        b.anchoredPosition = new Vector2(0f, -30f);
        b.sizeDelta = new Vector2(200f, 18f);
        var bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
        bannerGroup.alpha = 0f;
        bannerGroup.blocksRaycasts = false;

        TMP_Text bannerText = CreateText("Text", banner.transform, "", 10f, TextAlignmentOptions.Center);
        bannerText.fontStyle = FontStyles.Bold;
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

    // ------------------------------------------------------------------ UI helpers

    /// <summary>A Screen Space - Overlay canvas that scales a 480 x 270 layout to the screen.</summary>
    private static GameObject CreateCanvas(string name, Transform parent, int sortingOrder)
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

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();

        if (TMP_Settings.defaultFontAsset == null)
            Debug.LogError("GameSystemsPrefabBuilder: no TextMeshPro font. Run Tools > Clown Who Cried > Part 2 > Import TMP Essentials first.");
        tmp.font = TMP_Settings.defaultFontAsset;

        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void Stretch(RectTransform rect) => SetRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    /// <summary>Sets a private [SerializeField], the same way dragging it in the Inspector does.</summary>
    private static void SetField(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
