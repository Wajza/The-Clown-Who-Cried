using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays conversations (DialogueData) in the DialogueUI box.
///
/// HOW IT WORKS
/// StartDialogue() runs one coroutine that goes through the conversation step by step:
///
///   1. Lock Pip (PlayerInputLock), set GameState.Dialogue, raise GameEvents.DialogueStarted.
///   2. For every line: type it out letter by letter.
///        - Pressing Advance while typing shows the whole line at once.
///        - Pressing Advance when it's fully typed goes to the next line.
///   3. If the conversation has choices: show them, ChoiceUp/ChoiceDown move the highlight,
///      Advance picks. The choice's flag is set and its "next" conversation plays.
///   4. Close the box, unlock Pip, put the old GameState back, call the optional onFinished
///      callback (NpcTalk uses it), then raise GameEvents.DialogueEnded.
///
/// Button presses are turned into "requests" (RequestAdvance, RequestChoiceMove) that the
/// coroutine picks up. Anything can make requests, not only the keyboard: tests do, and
/// cutscenes (Part 6) can too.
///
/// Typing uses unscaled time, so it works even if the game is paused.
///
/// Setup: on the GameSystems prefab, with the DialogueUI assigned.
/// </summary>
public class DialogueRunner : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector fields

    [SerializeField, Tooltip("The text box (DialogueCanvas in the GameSystems prefab).")]
    private DialogueUI ui;

    [SerializeField, Min(1f), Tooltip("Typing speed in letters per second.")]
    private float charactersPerSecond = 40f;

    // ------------------------------------------------------------------ Public API

    public static DialogueRunner Instance { get; private set; }

    /// <summary>True from StartDialogue until the box closes.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>True while a line is still being typed.</summary>
    public bool IsTyping { get; private set; }

    /// <summary>True while answers are on screen, waiting for a pick.</summary>
    public bool IsWaitingForChoice { get; private set; }

    /// <summary>The conversation that was started (stays the same through "next" conversations).</summary>
    public DialogueData CurrentDialogue { get; private set; }

    public DialogueUI UI => ui;

    /// <summary>
    /// Starts a conversation. Returns false if one is already running or data is empty.
    /// onFinished is called once the box has closed.
    /// </summary>
    public bool StartDialogue(DialogueData dialogue, Action onFinished = null)
    {
        if (IsRunning)
            return false;
        if (dialogue == null || dialogue.lines.Count == 0)
        {
            Debug.LogWarning("DialogueRunner: the dialogue is missing or has no lines.", this);
            return false;
        }

        _routine = StartCoroutine(RunDialogue(dialogue, onFinished));
        return true;
    }

    /// <summary>"The player pressed Advance": finish typing, go to the next line, or pick the choice.</summary>
    public void RequestAdvance() => _advanceRequested = true;

    /// <summary>Moves the choice highlight: -1 = up, +1 = down.</summary>
    public void RequestChoiceMove(int direction) => _choiceMove += direction;

    /// <summary>Closes the box immediately (e.g. when a new scene starts loading).</summary>
    public void StopDialogue()
    {
        if (!IsRunning)
            return;
        if (_routine != null)
            StopCoroutine(_routine);
        Finish(CurrentDialogue, null);
    }

    // ------------------------------------------------------------------ Private state

    private Coroutine _routine;
    private bool _advanceRequested;
    private int _choiceMove;
    private int _startFrame;          // Frame the dialogue began (see Update).
    private GameState _stateBefore;   // Put back when the dialogue ends.

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable() => GameEvents.SceneLoadStarted += OnSceneLoadStarted;
    private void OnDisable() => GameEvents.SceneLoadStarted -= OnSceneLoadStarted;

    private void OnSceneLoadStarted(string sceneName) => StopDialogue();

    private void Update()
    {
        if (!IsRunning)
            return;

        // Ignore buttons on the frame the dialogue started. Interact (F) is also an Advance key,
        // so without this the same press that opened the box would skip the first line.
        if (Time.frameCount == _startFrame)
            return;

        if (SystemInput.WasPressed(SystemInput.Advance))
            RequestAdvance();
        if (SystemInput.WasPressed(SystemInput.ChoiceUp))
            RequestChoiceMove(-1);
        if (SystemInput.WasPressed(SystemInput.ChoiceDown))
            RequestChoiceMove(+1);
    }

    // ------------------------------------------------------------------ The conversation

    private IEnumerator RunDialogue(DialogueData first, Action onFinished)
    {
        // 1. Set up.
        IsRunning = true;
        CurrentDialogue = first;
        _startFrame = Time.frameCount;
        PlayerInputLock.Lock(this);

        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            _stateBefore = gm.State; // Usually Playing, but could be Cutscene (Part 6).
            gm.SetState(GameState.Dialogue);
        }

        GameEvents.RaiseDialogueStarted(first);
        ui.Show();

        // 2 + 3. Lines, then choices. A choice can lead to another conversation, so loop.
        DialogueData current = first;
        while (current != null)
        {
            foreach (DialogueData.Line line in current.lines)
                yield return PlayLine(line);

            SetFlag(current.setFlagWhenFinished);

            DialogueData next = null;
            if (current.HasChoices)
            {
                int picked = 0;
                yield return AskChoice(current.choices, index => picked = index);

                DialogueData.Choice choice = current.choices[picked];
                SetFlag(choice.setFlag);
                next = choice.next;
            }
            current = next;
        }

        // 4. Clean up.
        Finish(first, onFinished);
    }

    private IEnumerator PlayLine(DialogueData.Line line)
    {
        ui.ShowLine(line.speaker, line.text);
        _advanceRequested = false; // Forget presses from before this line.

        // Type the letters. 'typed' counts up in fractions so slow speeds still work.
        IsTyping = true;
        int total = ui.CharacterCount;
        float typed = 0f;
        while (ui.VisibleCharacters < total)
        {
            if (_advanceRequested) // Player is impatient: show the whole line now.
            {
                _advanceRequested = false;
                break;
            }

            typed += charactersPerSecond * Time.unscaledDeltaTime;
            ui.VisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(typed));
            yield return null;
        }
        ui.VisibleCharacters = total;
        IsTyping = false;

        // Wait for Advance before moving on.
        ui.SetContinueArrow(true);
        while (!_advanceRequested)
            yield return null;
        _advanceRequested = false;
        ui.SetContinueArrow(false);
    }

    private IEnumerator AskChoice(List<DialogueData.Choice> choices, Action<int> onPicked)
    {
        int count = Mathf.Min(choices.Count, ui.MaxChoices);
        if (choices.Count > ui.MaxChoices)
            Debug.LogWarning($"DialogueRunner: only the first {ui.MaxChoices} choices fit in the box.", this);

        var answers = new List<string>();
        for (int i = 0; i < count; i++)
            answers.Add(choices[i].text);

        ui.ShowChoices(answers);
        IsWaitingForChoice = true;
        int selected = 0;
        _advanceRequested = false;
        _choiceMove = 0;

        while (!_advanceRequested)
        {
            if (_choiceMove != 0)
            {
                // Move up/down and wrap around: from the last answer, "down" goes to the first.
                selected = ((selected + Math.Sign(_choiceMove)) % count + count) % count;
                _choiceMove = 0;
                ui.SetSelectedChoice(selected);
            }
            yield return null;
        }

        _advanceRequested = false;
        IsWaitingForChoice = false;
        ui.HideChoices();
        onPicked(selected);
    }

    private void Finish(DialogueData first, Action onFinished)
    {
        ui.Hide();
        IsRunning = false;
        IsTyping = false;
        IsWaitingForChoice = false;
        CurrentDialogue = null;
        _routine = null;

        PlayerInputLock.Unlock(this);

        // Only put the old state back if nothing else changed it meanwhile (e.g. Loading).
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.State == GameState.Dialogue)
            gm.SetState(_stateBefore);

        // IsRunning is already false, so these listeners may start a new conversation.
        onFinished?.Invoke();
        GameEvents.RaiseDialogueEnded(first);
    }

    private static void SetFlag(string flag)
    {
        if (!string.IsNullOrEmpty(flag) && GameManager.Instance != null)
            GameManager.Instance.Save.SetFlag(flag);
    }
}
