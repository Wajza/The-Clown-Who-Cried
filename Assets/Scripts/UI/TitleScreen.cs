using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The title screen's buttons. Lives in the Title scene (on its canvas).
///
///   New Game   starts from the first level (GameManager.NewGame). If a save exists, it asks
///              "Start a new game?" first, because the old save will be replaced.
///   Continue   loads the save (GameManager.ContinueGame). Greyed out when there's no save.
///   Settings   opens the shared Settings screen (MenuController). When it closes, the
///              Settings button is selected again.
///   Quit       closes the game (does nothing in the Editor).
///
/// Every button's On Click is set in the Inspector to the methods below.
/// </summary>
public class TitleScreen : MonoBehaviour
{
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("New Game confirmation")]
    [SerializeField, Tooltip("Shown when New Game would replace a save.")]
    private GameObject confirmPanel;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    // For tests.
    public Button NewGameButton => newGameButton;
    public Button ContinueButton => continueButton;
    public Button SettingsButton => settingsButton;
    public Button ConfirmYesButton => confirmYesButton;
    public Button ConfirmNoButton => confirmNoButton;
    public bool ConfirmVisible => confirmPanel.activeSelf;

    // ------------------------------------------------------------------ Unity lifecycle

    private void OnEnable()
    {
        if (MenuController.Instance != null)
            MenuController.Instance.ScreenChanged += OnMenuScreenChanged;
    }

    private void OnDisable()
    {
        if (MenuController.Instance != null)
            MenuController.Instance.ScreenChanged -= OnMenuScreenChanged;
    }

    private void Start()
    {
        // MenuController is created by GameBootstrap after this scene's OnEnable ran, so try again.
        OnDisable();
        OnEnable();

        confirmPanel.SetActive(false);
        continueButton.interactable = SaveSystem.HasSave;
        Select(continueButton.interactable ? continueButton : newGameButton);
    }

    // ------------------------------------------------------------------ Buttons

    public void OnNewGame()
    {
        if (SaveSystem.HasSave)
        {
            confirmPanel.SetActive(true);
            Select(confirmNoButton); // Safer default.
            return;
        }
        StartNewGame();
    }

    public void OnConfirmNewGame()
    {
        confirmPanel.SetActive(false);
        StartNewGame();
    }

    public void OnCancelNewGame()
    {
        confirmPanel.SetActive(false);
        Select(newGameButton);
    }

    public void OnContinue()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ContinueGame();
    }

    public void OnSettings()
    {
        if (MenuController.Instance != null)
            MenuController.Instance.OpenSettings();
    }

    public void OnQuit()
    {
        Debug.Log("TitleScreen: Quit (closes the game in a build; does nothing in the Editor).");
        Application.Quit();
    }

    // ------------------------------------------------------------------ Helpers

    private void StartNewGame()
    {
        if (GameManager.Instance != null && !GameManager.Instance.NewGame())
            Select(newGameButton); // First scene missing: stay here (the error is in the Console).
    }

    private void OnMenuScreenChanged(MenuController.Screen screen)
    {
        if (screen == MenuController.Screen.None)
            Select(settingsButton);
    }

    private static void Select(Button button)
    {
        if (EventSystem.current != null && button != null)
            EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
}
