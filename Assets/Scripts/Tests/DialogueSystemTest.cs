using System.Collections;
using UnityEngine;

/// <summary>
/// An automatic check of Part 2 (Dialogue). It lives in the DialogueTest scene, which has a test
/// Mayor (with choices and a "talk again" conversation) and a test Kid (narration, no speaker).
///
/// Press Play in DialogueTest and watch Pip walk up to the NPCs, talk, pick an answer and so on.
/// Results go to the Console and Logs/Part2TestResults.txt (machinery in SystemTestBase).
///
/// The test presses buttons by calling the same methods the buttons call
/// (InteractionInput.TryInteract, DialogueRunner.RequestAdvance / RequestChoiceMove), and
/// separately checks that the real keys are bound in SystemControls.
/// </summary>
public class DialogueSystemTest : SystemTestBase
{
    private const string TestScene = "DialogueTest";

    [SerializeField] private NpcTalk mayor;
    [SerializeField] private NpcTalk kid;
    [SerializeField] private DialogueData mayorFirst;
    [SerializeField] private DialogueData mayorAgain;

    protected override string ResultsFileName => "Part2TestResults.txt";
    protected override string LogTag => "Part2Test";

    private DialogueData _lastStarted;
    private DialogueData _lastEnded;
    private string _lastNpcTalkedTo;
    private int _npcTalkCount;
    private bool _sceneLoaded;

    // ------------------------------------------------------------------ Events the test watches

    private void OnEnable()
    {
        GameEvents.DialogueStarted += OnStarted;
        GameEvents.DialogueEnded += OnEnded;
        GameEvents.NpcTalkedTo += OnNpcTalkedTo;
        GameEvents.SceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        GameEvents.DialogueStarted -= OnStarted;
        GameEvents.DialogueEnded -= OnEnded;
        GameEvents.NpcTalkedTo -= OnNpcTalkedTo;
        GameEvents.SceneLoaded -= OnSceneLoaded;
    }

    private void OnStarted(DialogueData d) => _lastStarted = d;
    private void OnEnded(DialogueData d) => _lastEnded = d;
    private void OnNpcTalkedTo(string id) { _lastNpcTalkedTo = id; _npcTalkCount++; }
    private void OnSceneLoaded(string scene, string spawn) => _sceneLoaded = true;

    private static DialogueRunner Runner => DialogueRunner.Instance;
    private static DialogueUI UI => DialogueRunner.Instance.UI;

    // ------------------------------------------------------------------ Test flow

    protected override IEnumerator RunTest()
    {
        // 0. The scene's links to the test NPCs and conversations must be set, otherwise
        //    comparisons like "null == null" would pass by accident.
        bool linked = mayor != null && kid != null && mayorFirst != null && mayorAgain != null;
        Check("Test driver references are assigned", linked);
        if (!linked)
            yield break;

        // 1. Everything exists and the keys are bound.
        yield return WaitUntil(() => GameManager.Instance != null && _sceneLoaded, 5f, "first SceneLoaded");
        yield return null; // Let InteractionInput run its first Update.

        Check("SystemInput actions ready", SystemInput.Interact != null && SystemInput.Advance != null);
        Check("Interact is bound to F", HasBinding(SystemInput.Interact, "<Keyboard>/f"));
        Check("Interact is bound to gamepad East", HasBinding(SystemInput.Interact, "<Gamepad>/buttonEast"));
        Check("Advance is bound to Space", HasBinding(SystemInput.Advance, "<Keyboard>/space"));
        Check("Interact is NOT E (Tear Burst)", !HasBinding(SystemInput.Interact, "<Keyboard>/e"));
        Check("DialogueRunner, UI and InteractionInput exist",
            Runner != null && UI != null && InteractionInput.Instance != null);
        Check("Dialogue box hidden at start", !UI.IsVisible);

        InteractionInput interaction = InteractionInput.Instance;

        // 2. Range and "closest wins".
        Check("Far away: no target", interaction.Target == null);
        Check("Far away: no prompts", !mayor.PromptVisible && !kid.PromptVisible);

        yield return MovePipTo(mayor.transform.position.x + 1.8f); // Kid is 1.2 away, Mayor 1.8.
        Check("Between both: the closer Kid is the target", interaction.Target == kid);
        Check("Only the Kid's prompt shows", kid.PromptVisible && !mayor.PromptVisible);

        yield return MovePipTo(mayor.transform.position.x + 1.2f); // Mayor 1.2, Kid 1.8.
        Check("Closer to the Mayor: Mayor is the target", interaction.Target == mayor);
        Check("Only the Mayor's prompt shows", mayor.PromptVisible && !kid.PromptVisible);

        // 3. Talk to the Mayor: first conversation.
        Check("Mayor picks the first-meeting dialogue", mayor.PickDialogue() == mayorFirst);
        Check("Interact starts a conversation", interaction.TryInteract());
        Check("Dialogue running", Runner.IsRunning);
        Check("GameState is Dialogue", GameManager.Instance.State == GameState.Dialogue);
        Check("Pip is locked", PlayerInputLock.IsLocked && !PlayerController.Current.InputActions.FindActionMap("Player").enabled);
        Check("Box visible", UI.IsVisible);
        Check("DialogueStarted raised with the Mayor's dialogue", _lastStarted == mayorFirst);
        Check("Speaker name shown", UI.SpeakerName == "Mayor (test)");

        yield return null;
        Check("Prompt hidden while talking", !mayor.PromptVisible && interaction.Target == null);
        Check("Line is being typed", Runner.IsTyping && UI.VisibleCharacters < UI.CharacterCount);

        int before = UI.VisibleCharacters;
        yield return new WaitForSecondsRealtime(0.25f);
        Check("Typewriter adds letters over time", UI.VisibleCharacters > before);

        Runner.RequestAdvance(); // While typing: show the whole line.
        yield return null;
        Check("Advance while typing completes the line",
            !Runner.IsTyping && UI.VisibleCharacters == UI.CharacterCount && UI.SpeakerName == "Mayor (test)");

        Runner.RequestAdvance(); // Fully typed: next line.
        yield return null;
        Check("Advance again goes to the next line (Pip speaks)", UI.SpeakerName == "Pip");

        yield return CompleteAndAdvance(); // -> line 3 (Mayor asks the question)
        Check("Third line is the Mayor's question", UI.SpeakerName == "Mayor (test)");
        yield return CompleteAndAdvance(); // -> choices appear
        Check("Choices shown after the last line", UI.ChoicesVisible && Runner.IsWaitingForChoice);
        Check("First answer selected", UI.SelectedChoice == 0);

        // 4. Choices: move and wrap around.
        Runner.RequestChoiceMove(+1); yield return null;
        Check("Down selects answer 2", UI.SelectedChoice == 1);
        Runner.RequestChoiceMove(+1); yield return null;
        Check("Down from the last answer wraps to the first", UI.SelectedChoice == 0);
        Runner.RequestChoiceMove(-1); yield return null;
        Check("Up from the first answer wraps to the last", UI.SelectedChoice == 1);
        Runner.RequestChoiceMove(-1); yield return null;

        Runner.RequestAdvance(); // Pick "Yes!"
        yield return null;
        SaveData save = GameManager.Instance.Save;
        Check("Picking 'Yes!' set its flag", save.HasFlag("test_said_yes"));
        Check("'Set Flag When Finished' was set", save.HasFlag("test_met_mayor"));
        Check("Choices hidden after picking", !UI.ChoicesVisible);
        Check("The answer's 'next' conversation plays", Runner.IsRunning && UI.SpeakerName == "Mayor (test)");

        yield return FinishConversation();

        // 5. After the conversation.
        Check("Dialogue finished", !Runner.IsRunning);
        Check("Box hidden", !UI.IsVisible);
        Check("GameState back to Playing", GameManager.Instance.State == GameState.Playing);
        Check("Pip unlocked", !PlayerInputLock.IsLocked && PlayerController.Current.InputActions.FindActionMap("Player").enabled);
        Check("DialogueEnded raised with the started dialogue", _lastEnded == mayorFirst);
        Check("NpcTalkedTo('mayor') raised once", _lastNpcTalkedTo == "mayor" && _npcTalkCount == 1);
        yield return null;
        Check("Prompt back after the conversation", mayor.PromptVisible && interaction.Target == mayor);

        // 6. Talk again: the flag picks the other conversation.
        Check("Mayor now picks the 'again' dialogue", mayor.PickDialogue() == mayorAgain);
        Check("Second talk starts", interaction.TryInteract());
        Check("DialogueStarted raised with the 'again' dialogue", _lastStarted == mayorAgain);
        yield return FinishConversation();
        Check("NpcTalkedTo raised again", _npcTalkCount == 2);

        // 7. Narration (a line with no speaker) from the Kid.
        yield return MovePipTo(kid.transform.position.x - 0.5f);
        Check("Kid is the target", interaction.Target == kid);
        interaction.TryInteract();
        Check("Narration line has no speaker name", Runner.IsRunning && UI.SpeakerName == "");
        yield return FinishConversation();
        Check("NpcTalkedTo('kid')", _lastNpcTalkedTo == "kid");

        // 8. Can't interact while Pip is locked (e.g. a cutscene).
        var cutscene = new object();
        PlayerInputLock.Lock(cutscene);
        Check("Interact ignored while locked", !interaction.TryInteract() && !Runner.IsRunning);
        PlayerInputLock.Unlock(cutscene);

        // 9. Only one conversation at a time.
        Check("StartDialogue works", Runner.StartDialogue(mayorAgain));
        Check("A second StartDialogue is refused", !Runner.StartDialogue(mayorFirst));

        // 10. A scene change closes the conversation cleanly.
        _sceneLoaded = false;
        SceneLoader.Instance.LoadScene(TestScene);
        Check("Loading a scene stops the dialogue", !Runner.IsRunning && !UI.IsVisible);
        yield return WaitUntil(() => _sceneLoaded && !SceneLoader.Instance.IsLoading, 10f, "scene reload");
        Check("After loading: Pip unlocked and Playing",
            !PlayerInputLock.IsLocked && GameManager.Instance.State == GameState.Playing);
    }

    // ------------------------------------------------------------------ Steps

    private IEnumerator MovePipTo(float x)
    {
        PlayerController.Current.Teleport(new Vector2(x, Part1FloorY), 0f);
        yield return new WaitForSeconds(0.15f); // A few physics steps for the triggers.
        yield return null;                      // One Update for InteractionInput.
    }

    /// <summary>Finish typing the current line (if needed), then move on.</summary>
    private IEnumerator CompleteAndAdvance()
    {
        if (Runner.IsTyping)
        {
            Runner.RequestAdvance();
            yield return null;
        }
        Runner.RequestAdvance();
        yield return null;
    }

    /// <summary>Press Advance until the conversation is over (picking the first answer if asked).</summary>
    private IEnumerator FinishConversation()
    {
        for (int i = 0; i < 40 && Runner.IsRunning; i++)
            yield return CompleteAndAdvance();
        Check("Conversation could be finished", !Runner.IsRunning);
    }

    private static bool HasBinding(UnityEngine.InputSystem.InputAction action, string path)
    {
        foreach (var binding in action.bindings)
            if (binding.path == path)
                return true;
        return false;
    }

    // Height of the floor in the test scenes (Part1Setup.FloorY is editor-only).
    private const float Part1FloorY = -1.5f;
}
