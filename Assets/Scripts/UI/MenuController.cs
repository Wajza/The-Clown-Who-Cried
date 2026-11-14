using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Opens and closes the menu screens and pauses the game while they're open.
///
/// SCREENS
///   Pause     Resume / Journal / Settings / Quit to Title
///   Journal   Task Board, torn pages, Star Fragments (JournalUI)
///   Settings  volumes and game-feel options (SettingsMenu)
///
/// BUTTONS (SystemInput)
///   Pause (Esc / Start):     gameplay -> pause menu; pause menu -> resume; other screens -> back
///   Journal (Tab / Select):  gameplay -> journal;    journal -> close everything
///   Back (Esc / Backspace / gamepad East): one screen back (closing the last one resumes)
///
/// PAUSING
/// While a screen is open in gameplay: Time.timeScale = 0, GameState.Paused, Pip locked
/// (PlayerInputLock), and GameEvents.GamePausedChanged(true). Closing undoes all of it.
/// Pausing is only allowed during normal play: not in dialogue, cutscenes, loading, the title
/// screen, or while Pip is locked for another reason (e.g. the death/respawn fade).
///
/// On the title screen, TitleScreen opens Settings through OpenSettings(); that doesn't pause.
///
/// Every button's On Click is set in the Inspector (the GameSystems prefab), pointing at the
/// public methods below, so new art can be dropped onto the buttons without touching code.
///
/// Setup: on the MenuCanvas in the GameSystems prefab (the builder adds it).
/// </summary>
public class MenuController : MonoBehaviour
{
    public enum Screen { None, Pause, Journal, Settings }

    // ------------------------------------------------------------------ Inspector fields

    [Header("Pause menu")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField, Tooltip("Selected when the pause menu opens.")]
    private Button resumeButton;
    [SerializeField] private Button journalButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitToTitleButton;

    [Header("Other screens")]
    [SerializeField] private JournalUI journal;
    [SerializeField] private SettingsMenu settings;

    // ------------------------------------------------------------------ Public API

    public static MenuController Instance { get; private set; }

    /// <summary>The screen on show, or None.</summary>
    public Screen Current { get; private set; } = Screen.None;

    /// <summary>True while the game is paused by a menu.</summary>
    public bool IsPaused { get; private set; }

    /// <summary>Fired whenever the screen changes (TitleScreen uses it to re-select its buttons).</summary>
    public event Action<Screen> ScreenChanged;

    public Button ResumeButton => resumeButton;
    public Button JournalButton => journalButton;
    public Button SettingsButton => settingsButton;
    public Button QuitToTitleButton => quitToTitleButton;
    public JournalUI Journal => journal;
    public SettingsMenu Settings => settings;

    /// <summary>True when a menu may pause the game right now.</summary>
    public static bool CanPause
    {
        get
        {
            GameManager gm = GameManager.Instance;
            return gm != null && gm.State == GameState.Playing && !PlayerInputLock.IsLocked
                   && PlayerController.Current != null;
        }
    }

    /// <summary>Opens the pause menu (pausing the game). Ignored when pausing isn't allowed.</summary>
    public void OpenPause()
    {
        if (!IsPaused && !TryPause())
            return;
        Show(Screen.Pause, remember: false);
    }

    /// <summary>Opens the Journal: from gameplay (pausing it) or from the pause menu.</summary>
    public void OpenJournal()
    {
        if (!IsPaused && !TryPause())
            return;
        Show(Screen.Journal, remember: Current != Screen.None);
    }

    /// <summary>Opens Settings: from the pause menu, or from the title screen (no pausing there).</summary>
    public void OpenSettings() => Show(Screen.Settings, remember: Current != Screen.None);

    /// <summary>One screen back. Leaving the last screen closes the menus (and resumes).</summary>
    public void Back()
    {
        if (_history.Count > 0)
            Show(_history.Pop(), remember: false);
        else
            CloseAll();
    }

    /// <summary>Closes every screen and resumes the game. The Resume button calls this.</summary>
    public void CloseAll()
    {
        _history.Clear();
        Show(Screen.None, remember: false);
        if (IsPaused)
            Unpause();
    }

    /// <summary>The "Quit to Title" button. Progress is kept from the last save (checkpoint/scene/task).</summary>
    public void QuitToTitle()
    {
        CloseAll();
        if (GameManager.Instance != null)
            GameManager.Instance.ReturnToTitle();
    }

    // ------------------------------------------------------------------ Private state

    private readonly Stack<Screen> _history = new Stack<Screen>();

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        Instance = this;
        Show(Screen.None, remember: false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable() => GameEvents.SceneLoadStarted += OnSceneLoadStarted;
    private void OnDisable() => GameEvents.SceneLoadStarted -= OnSceneLoadStarted;

    // A scene change (e.g. Continue from the title) never leaves a menu open.
    private void OnSceneLoadStarted(string sceneName) => CloseAll();

    private void Update()
    {
        // Keep the game frozen. Something else (e.g. my teammate's hit pause ending) might have
        // set the time scale back to 1 while the menu was open.
        if (IsPaused && Time.timeScale != 0f)
            Time.timeScale = 0f;

        if (SystemInput.WasPressed(SystemInput.Pause))
        {
            if (Current == Screen.None) OpenPause();
            else if (Current == Screen.Pause) CloseAll();
            else Back();
        }
        else if (SystemInput.WasPressed(SystemInput.Journal))
        {
            if (Current == Screen.None) OpenJournal();
            else if (Current == Screen.Journal) CloseAll();
        }
        else if (Current != Screen.None && SystemInput.WasPressed(SystemInput.Back))
        {
            Back();
        }
    }

    // ------------------------------------------------------------------ Screens

    private void Show(Screen screen, bool remember)
    {
        if (remember && Current != Screen.None && Current != screen)
            _history.Push(Current);

        Current = screen;
        pausePanel.SetActive(screen == Screen.Pause);
        journal.gameObject.SetActive(screen == Screen.Journal);
        settings.gameObject.SetActive(screen == Screen.Settings);

        // Tell the EventSystem which button is highlighted, so keyboard/gamepad work at once.
        GameObject select = screen switch
        {
            Screen.Pause => resumeButton.gameObject,
            Screen.Settings => settings.FirstSelectable,
            _ => null, // The Journal reads the keys itself.
        };
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(select);

        ScreenChanged?.Invoke(screen);
    }

    // ------------------------------------------------------------------ Pausing

    private bool TryPause()
    {
        if (!CanPause)
            return false;

        IsPaused = true;
        Time.timeScale = 0f;
        GameManager.Instance.SetState(GameState.Paused);
        PlayerInputLock.Lock(this);
        GameEvents.RaiseGamePausedChanged(true);
        return true;
    }

    private void Unpause()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Paused)
            GameManager.Instance.SetState(GameState.Playing);
        PlayerInputLock.Unlock(this);
        GameEvents.RaiseGamePausedChanged(false);
    }

    // ------------------------------------------------------------------ Reset

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;
}
