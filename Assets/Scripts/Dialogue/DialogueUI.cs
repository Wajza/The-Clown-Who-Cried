using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The text box on screen. It only DISPLAYS things; DialogueRunner decides what to show and when.
///
/// Parts (all assigned in the Inspector, inside the GameSystems prefab):
///   - Panel:          the whole box (switched on/off).
///   - Portrait:       speaker's face (hidden when the speaker has none).
///   - Name / Body:    TextMeshPro texts.
///   - Continue Arrow: little marker shown when the line is fully typed.
///   - Choice Texts:   up to 3 answer slots; the selected one gets "> " and a highlight colour.
///
/// TYPEWRITER TRICK
/// The whole line is put into the text at once, but TextMeshPro's maxVisibleCharacters hides the
/// letters that aren't "typed" yet. Raising it one by one types the line. This keeps word-wrap
/// stable (words don't jump to the next line half-typed) and works with rich text tags.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector fields

    [SerializeField] private GameObject panel;
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField, Tooltip("Shown when the line is fully typed and waiting for the player.")]
    private GameObject continueArrow;
    [SerializeField, Min(0f), Tooltip("How far (UI pixels) the name and text move right when a portrait is shown.")]
    private float portraitIndent = 62f;

    [Header("Choices")]
    [SerializeField, Tooltip("Box around the answers (switched on/off).")]
    private GameObject choicesPanel;
    [SerializeField, Tooltip("One text per answer slot. Conversations can have up to this many choices.")]
    private TMP_Text[] choiceTexts;
    [SerializeField] private Color choiceColor = new Color(0.8f, 0.8f, 0.8f);
    [SerializeField] private Color selectedChoiceColor = new Color(1f, 0.85f, 0.3f);

    // ------------------------------------------------------------------ Public API

    public bool IsVisible => panel != null && panel.activeSelf;
    public bool ChoicesVisible => choicesPanel != null && choicesPanel.activeSelf;

    /// <summary>Index of the highlighted answer, or -1 if no choices are shown.</summary>
    public int SelectedChoice { get; private set; } = -1;

    public string SpeakerName => nameText != null ? nameText.text : "";

    /// <summary>Number of letters in the current line (not counting rich text tags).</summary>
    public int CharacterCount => bodyText.textInfo.characterCount;

    /// <summary>How many letters are typed so far.</summary>
    public int VisibleCharacters
    {
        get => bodyText.maxVisibleCharacters;
        set => bodyText.maxVisibleCharacters = value;
    }

    public int MaxChoices => choiceTexts != null ? choiceTexts.Length : 0;

    // ------------------------------------------------------------------ Private state

    private readonly List<string> _answers = new List<string>(); // The answers currently shown.

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake() => Hide();

    // ------------------------------------------------------------------ Showing and hiding

    public void Show() => panel.SetActive(true);

    public void Hide()
    {
        panel.SetActive(false);
        HideChoices();
    }

    /// <summary>Puts a new line in the box with 0 letters visible. DialogueRunner then types it.</summary>
    public void ShowLine(DialogueSpeaker speaker, string text)
    {
        // Name and portrait. No speaker = narration: no name, no face.
        nameText.text = speaker != null ? speaker.displayName : "";
        nameText.color = speaker != null ? speaker.nameColor : Color.white;

        bool hasPortrait = speaker != null && speaker.portrait != null;
        portrait.gameObject.SetActive(hasPortrait);
        if (hasPortrait)
            portrait.sprite = speaker.portrait;

        // Text starts after the portrait, or at the left edge when there's none.
        var indent = new Vector4(hasPortrait ? portraitIndent : 0f, 0f, 0f, 0f); // (left, top, right, bottom)
        nameText.margin = indent;
        bodyText.margin = indent;

        bodyText.text = text;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate(); // Work out the letters now, so CharacterCount is right straight away.

        SetContinueArrow(false);
    }

    public void SetContinueArrow(bool visible)
    {
        if (continueArrow != null)
            continueArrow.SetActive(visible);
    }

    // ------------------------------------------------------------------ Choices

    /// <summary>Shows the answers (up to MaxChoices) with the first one selected.</summary>
    public void ShowChoices(IReadOnlyList<string> answers)
    {
        _answers.Clear();
        _answers.AddRange(answers);

        choicesPanel.SetActive(true);
        for (int i = 0; i < choiceTexts.Length; i++)
            choiceTexts[i].gameObject.SetActive(i < _answers.Count);

        SetSelectedChoice(0);
    }

    /// <summary>Highlights one answer: "> " in front and the highlight colour.</summary>
    public void SetSelectedChoice(int index)
    {
        SelectedChoice = index;
        for (int i = 0; i < _answers.Count && i < choiceTexts.Length; i++)
        {
            bool selected = i == index;
            choiceTexts[i].text = selected ? "> " + _answers[i] : _answers[i];
            choiceTexts[i].color = selected ? selectedChoiceColor : choiceColor;
        }
    }

    public void HideChoices()
    {
        if (choicesPanel != null)
            choicesPanel.SetActive(false);
        SelectedChoice = -1;
    }
}
