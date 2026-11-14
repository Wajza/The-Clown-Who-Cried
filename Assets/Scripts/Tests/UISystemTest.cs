using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// An automatic check of Part 4 (UI + Journal). It lives in the UITest scene:
/// the stand-in Pip (with my teammate's Tear Gauge and Blue Sight), a torn page on the floor,
/// and a pretend boss (TestBossHealthSource).
///
/// It checks the HUD, the boss bar, the pause menu, the Journal (Task Boards, torn pages,
/// fragments), Settings, Quit to Title and Continue from the title screen.
///
/// Menu buttons are pressed by invoking their On Click, which runs exactly what the Inspector
/// has wired up. Your Settings are put back at the end. Results: Logs/Part4TestResults.txt.
///
/// While the game is paused (time scale 0) the test only waits with "yield return null" or
/// real-time waits, because WaitForSeconds would never finish.
/// </summary>
public class UISystemTest : SystemTestBase
{
    private const string FragmentId = "ui_test_fragment";

    [SerializeField] private JournalPageData page1;
    [SerializeField] private TaskData errand;
    [SerializeField] private TaskData kidTask;
    [SerializeField] private DialogueData someDialogue;
    [SerializeField] private TestBossHealthSource boss;

    protected override string ResultsFileName => "Part4TestResults.txt";
    protected override string LogTag => "Part4Test";

    private string _sceneLoaded;
    private int _pausedEvents, _resumedEvents;
    private JournalPageData _pageFound;

    // Settings to put back afterwards.
    private float _music, _sfx, _shakeStrength;
    private bool _shake, _hitPause;

    // ------------------------------------------------------------------ Events the test watches

    private void OnEnable()
    {
        GameEvents.SceneLoaded += OnSceneLoaded;
        GameEvents.GamePausedChanged += OnPausedChanged;
        GameEvents.JournalPageFound += OnPageFound;
    }

    private void OnDisable()
    {
        GameEvents.SceneLoaded -= OnSceneLoaded;
        GameEvents.GamePausedChanged -= OnPausedChanged;
        GameEvents.JournalPageFound -= OnPageFound;
    }

    private void OnSceneLoaded(string scene, string spawn) => _sceneLoaded = scene;
    private void OnPausedChanged(bool paused) { if (paused) _pausedEvents++; else _resumedEvents++; }
    private void OnPageFound(JournalPageData page) => _pageFound = page;

    private static MenuController Menu => MenuController.Instance;
    private static GameState State => GameManager.Instance.State;

    // ------------------------------------------------------------------ Test flow

    protected override IEnumerator RunTest()
    {
        bool linked = page1 != null && errand != null && kidTask != null && someDialogue != null && boss != null;
        Check("Test driver references are assigned", linked);
        if (!linked)
            yield break;

        JournalPages.IncludeTestContent = true; // Show the test pages/tasks in the Journal.
        RememberSettings();

        yield return WaitUntil(() => GameManager.Instance != null && _sceneLoaded != null, 5f, "first SceneLoaded");
        yield return null;
        yield return null; // HUD pieces link to Pip in Update.

        yield return CheckHud();
        yield return CheckBossBar();
        yield return CheckPauseMenu();
        yield return CheckJournal();
        yield return CheckPagePickup();
        yield return CheckSettings();
        yield return CheckTitleAndContinue();

        RestoreSettings();
    }

    // ------------------------------------------------------------------ 1. HUD

    private IEnumerator CheckHud()
    {
        var hud = FindAnyObjectByType<HudController>();
        var stars = FindAnyObjectByType<HudStars>();
        var tear = FindAnyObjectByType<HudTearGauge>();
        var blue = FindAnyObjectByType<HudBlueSight>();
        var coins = FindAnyObjectByType<HudCoins>();
        var fragments = FindAnyObjectByType<HudFragments>();
        Check("HUD pieces exist", hud && stars && tear && blue && coins && fragments);
        Check("HUD visible while playing", hud.Visible);

        PlayerHealth health = PlayerController.Current.GetComponent<PlayerHealth>();
        TearGauge gauge = PlayerController.Current.GetComponent<TearGauge>();
        Check("Stars: one icon per max star, all full",
            stars.ContentVisible && stars.ShownMax == health.MaxStars && stars.ShownFull == health.CurrentStars);
        Check("Tear Gauge shown and matches", tear.ContentVisible && Mathf.Approximately(tear.ShownFill, (float)gauge.Value / gauge.MaxValue));
        Check("Blue Sight meter shown and full", blue.ContentVisible && Mathf.Approximately(blue.ShownFill, 1f));
        Check("Coins hidden: this Pip has no coin thrower", !coins.ContentVisible);
        Check("Fragments shown: x 0", fragments.ContentVisible && fragments.ShownText == "x 0");

        // Lose a star: the star row and the Tear Gauge (which fills when Pip is hurt) update.
        int before = health.CurrentStars;
        health.TakeHazardDamage();
        yield return null;
        Check("Losing a star empties one icon", stars.ShownFull == before - 1 && stars.ShownFull == health.CurrentStars);
        Check("Tear Gauge follows the gauge", Mathf.Approximately(tear.ShownFill, (float)gauge.Value / gauge.MaxValue));
        gauge.Add(gauge.MaxValue);
        yield return null;
        Check("Full Tear Gauge = full bar", Mathf.Approximately(tear.ShownFill, 1f));

        PlayerStarFragments.Current.Collect(FragmentId);
        yield return null;
        Check("Collecting a fragment: x 1", fragments.ShownText == "x 1");

        yield return WaitUntil(() => !PlayerInputLock.IsLocked, 5f, "Pip free after the hazard hit");
        yield return new WaitForSeconds(0.3f);
    }

    // ------------------------------------------------------------------ 2. Boss bar

    private IEnumerator CheckBossBar()
    {
        var bar = FindAnyObjectByType<BossHealthBar>();
        Check("Boss bar hidden before the fight", bar != null && !bar.Visible);

        boss.StartFight("Test Boss", 100f);
        yield return null;
        Check("Boss bar shown when the fight starts", bar.Visible && bar.ShownName == "Test Boss" && Mathf.Approximately(bar.ShownFill, 1f));
        Check("Phase shown", bar.ShownPhase == "Phase 1");

        boss.Damage(25f);
        boss.SetPhase(2);
        yield return null;
        Check("Damage shrinks the bar", Mathf.Approximately(bar.ShownFill, 0.75f));
        Check("Phase 2 shown", bar.ShownPhase == "Phase 2");

        boss.Defeat();
        yield return null;
        Check("Boss bar hidden after the fight", !bar.Visible);
    }

    // ------------------------------------------------------------------ 3. Pause menu

    private IEnumerator CheckPauseMenu()
    {
        Check("MenuController exists, nothing open", Menu != null && Menu.Current == MenuController.Screen.None && !Menu.IsPaused);

        Menu.OpenPause();
        yield return null;
        Check("Pause: menu open", Menu.Current == MenuController.Screen.Pause && Menu.IsPaused);
        Check("Pause: time stopped", Time.timeScale == 0f);
        Check("Pause: state Paused, Pip locked", State == GameState.Paused && PlayerInputLock.IsLocked);
        Check("Pause: GamePausedChanged(true)", _pausedEvents == 1);
        Check("Pause: Resume is selected", EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == Menu.ResumeButton.gameObject);
        Check("HUD still visible while paused", FindAnyObjectByType<HudController>().Visible);

        Menu.ResumeButton.onClick.Invoke();
        yield return null;
        Check("Resume: closed and running", Menu.Current == MenuController.Screen.None && !Menu.IsPaused && Time.timeScale == 1f);
        Check("Resume: Playing, Pip free", State == GameState.Playing && !PlayerInputLock.IsLocked);
        Check("Resume: GamePausedChanged(false)", _resumedEvents == 1);

        // No pausing in the middle of a conversation.
        Runner.StartDialogue(someDialogue);
        Menu.OpenPause();
        Check("Can't pause during dialogue", !Menu.IsPaused && Menu.Current == MenuController.Screen.None);
        yield return FinishConversation();
    }

    // ------------------------------------------------------------------ 4. Journal

    private IEnumerator CheckJournal()
    {
        // One running task in Tumbleton, one finished task in Wonderwick.
        TaskManager.Instance.StartTask(errand);
        TaskManager.Instance.StartTask(kidTask);
        TaskManager.Instance.CompleteTask(kidTask);
        yield return null;

        Menu.OpenJournal();
        yield return null;
        JournalUI journal = Menu.Journal;
        Check("Journal opens from gameplay and pauses", Menu.Current == MenuController.Screen.Journal && Menu.IsPaused && Time.timeScale == 0f);
        Check("Opens on the first board (Tumbleton)", journal.Tab == 0 && journal.PageTitle.Contains("Tumbleton"));
        Check("Running task listed with [>]", journal.ListText.Contains("[>] The Mayor's Errand"));
        Check("Details show the current objective", journal.DetailText.Contains("Now: Talk to the baker"));
        Check("Header shows fragments and next star",
            journal.FragmentsText.Contains("Star Fragments: 1") && journal.FragmentsText.Contains("Next star: 1/"));

        journal.NextTab();
        Check("Next page: Wonderwick board", journal.PageTitle.Contains("Wonderwick"));
        Check("Finished task listed with [x]", journal.ListText.Contains("[x] Say Hi") && journal.DetailText.Contains("Done!"));

        // The real pages in Resources/JournalPages count too, so compare with the actual total.
        int totalPages = JournalPages.All.Count;
        journal.NextTab();
        Check($"Next page: Torn Pages, none of {totalPages} found", journal.OnPagesTab && journal.PageTitle.Contains($"0/{totalPages}"));
        Check("Missing pages show as torn out", journal.ListText.Contains("Page 1: (torn out)") && journal.ListText.Contains("Page 2: (torn out)"));
        journal.MoveSelection(+1);
        Check("Selecting moves down the list", journal.SelectedIndex == 1);

        journal.NextTab();
        Check("Pages wrap around to the first board", journal.Tab == 0);
        journal.PreviousTab();
        Check("...and back the other way", journal.OnPagesTab);

        Menu.CloseAll();
        yield return null;
        Check("Closing the Journal resumes", !Menu.IsPaused && Time.timeScale == 1f && State == GameState.Playing);

        // From the pause menu: Journal button, then Back twice.
        Menu.OpenPause();
        Menu.JournalButton.onClick.Invoke();
        yield return null;
        Check("Pause > Journal", Menu.Current == MenuController.Screen.Journal && Menu.IsPaused);
        Menu.Back();
        Check("Back from the Journal returns to the pause menu", Menu.Current == MenuController.Screen.Pause && Menu.IsPaused);
        Menu.Back();
        yield return null;
        Check("Back from the pause menu resumes", Menu.Current == MenuController.Screen.None && !Menu.IsPaused);
    }

    // ------------------------------------------------------------------ 5. Torn page pickup

    private IEnumerator CheckPagePickup()
    {
        JournalPagePickup pickup = FindAnyObjectByType<JournalPagePickup>();
        Check("Page pickup in the scene", pickup != null && pickup.gameObject.activeSelf);

        yield return MovePipTo(pickup.transform.position.x);
        yield return WaitUntil(() => _pageFound != null, 3f, "page pickup");
        Check("JournalPageFound raised with the page", _pageFound == page1);
        Check("Page saved", GameManager.Instance.Save.journalPageIds.Contains("test_page_1"));
        Check("Page written to disk", SaveSystem.Read()?.journalPageIds.Contains("test_page_1") == true);
        Check("Pickup gone", !pickup.gameObject.activeSelf);

        Menu.OpenJournal();
        yield return null;
        Menu.Journal.SetTab(Menu.Journal.TabCount - 1);
        int totalPages = JournalPages.All.Count;
        Check($"Journal: 1 of {totalPages} pages found", Menu.Journal.PageTitle.Contains($"1/{totalPages}"));
        Check("Found page listed by title", Menu.Journal.ListText.Contains("Page 1: A Test Page"));

        // Highlight the test page (real pages may come before it in the list), then read it.
        int index = 0;
        for (int i = 0; i < totalPages; i++)
            if (JournalPages.All[i] == page1)
                index = i;
        for (int i = 0; i < index; i++)
            Menu.Journal.MoveSelection(+1);
        Check("Found page can be read", Menu.Journal.DetailText.Contains("This page is only a test."));
        Menu.CloseAll();
        yield return null;
    }

    // ------------------------------------------------------------------ 6. Settings

    private IEnumerator CheckSettings()
    {
        Menu.OpenPause();
        Menu.SettingsButton.onClick.Invoke();
        yield return null;
        SettingsMenu settings = Menu.Settings;
        Check("Pause > Settings", Menu.Current == MenuController.Screen.Settings && Menu.IsPaused);
        Check("Settings: music slider selected", EventSystem.current.currentSelectedGameObject == settings.MusicSlider.gameObject);
        Check("Settings show the saved values", Mathf.Approximately(settings.MusicSlider.value, VolumeSettings.Music) &&
            settings.ScreenShakeToggle.isOn == GameFeelSettings.ScreenShakeEnabled);

        settings.MusicSlider.value = 0.3f;
        settings.SfxSlider.value = 0.55f;
        Check("Music slider sets the music volume", Mathf.Approximately(VolumeSettings.Music, 0.3f));
        Check("Sound slider sets the sound volume", Mathf.Approximately(VolumeSettings.Sfx, 0.55f));
        Check("Volume kept in PlayerPrefs", Mathf.Approximately(PlayerPrefs.GetFloat("volume.music"), 0.3f));

        bool shakeBefore = GameFeelSettings.ScreenShakeEnabled;
        settings.ScreenShakeToggle.isOn = !shakeBefore;
        Check("Screen shake toggle changes my teammate's setting", GameFeelSettings.ScreenShakeEnabled == !shakeBefore);
        Check("Shake amount only usable with shake on", settings.ScreenShakeStrengthSlider.interactable == !shakeBefore);
        settings.HitPauseToggle.isOn = !_hitPause;
        Check("Hit pause toggle changes my teammate's setting", GameFeelSettings.HitPauseEnabled == !_hitPause);

        Menu.Back();
        Check("Back from Settings returns to the pause menu", Menu.Current == MenuController.Screen.Pause);
        Menu.CloseAll();
        yield return null;
        RestoreSettings();
    }

    // ------------------------------------------------------------------ 7. Title screen and Continue

    private IEnumerator CheckTitleAndContinue()
    {
        string testScene = SceneLoader.CurrentSceneName;

        Menu.OpenPause();
        Menu.QuitToTitleButton.onClick.Invoke();
        yield return WaitUntil(() => _sceneLoaded == "Title" && !SceneLoader.Instance.IsLoading, 10f, "Title");
        yield return null;
        Check("Quit to Title: on the title screen", SceneLoader.CurrentSceneName == "Title" && State == GameState.Title);
        Check("Quit to Title: not paused, time running", !Menu.IsPaused && Time.timeScale == 1f);
        Check("HUD hidden on the title screen", !FindAnyObjectByType<HudController>().Visible);
        Menu.OpenPause();
        Check("Can't pause on the title screen", !Menu.IsPaused);

        TitleScreen title = FindAnyObjectByType<TitleScreen>();
        Check("TitleScreen found", title != null);
        if (title == null)
            yield break;
        Check("Continue available (a save exists)", title.ContinueButton.interactable);
        Check("Continue is selected", EventSystem.current.currentSelectedGameObject == title.ContinueButton.gameObject);

        title.SettingsButton.onClick.Invoke();
        yield return null;
        Check("Title > Settings opens without pausing", Menu.Current == MenuController.Screen.Settings && !Menu.IsPaused);
        Menu.Back();
        yield return null;
        Check("Closing Settings selects the title's Settings button",
            Menu.Current == MenuController.Screen.None && EventSystem.current.currentSelectedGameObject == title.SettingsButton.gameObject);

        title.NewGameButton.onClick.Invoke();
        Check("New Game asks first when a save exists", title.ConfirmVisible &&
            EventSystem.current.currentSelectedGameObject == title.ConfirmNoButton.gameObject);
        title.ConfirmNoButton.onClick.Invoke();
        Check("'No' closes the question", !title.ConfirmVisible);

        _sceneLoaded = null;
        title.ContinueButton.onClick.Invoke();
        yield return WaitUntil(() => _sceneLoaded == testScene && !SceneLoader.Instance.IsLoading, 10f, "Continue");
        yield return null;
        yield return null;
        Check("Continue: back in the saved scene, playing", SceneLoader.CurrentSceneName == testScene && State == GameState.Playing);
        Check("Continue: HUD visible again", FindAnyObjectByType<HudController>().Visible);
        Check("Continue: page still found, pickup gone",
            JournalPages.IsFound(page1) && FindAnyObjectByType<JournalPagePickup>() == null);
        Check("Continue: fragment count restored", FindAnyObjectByType<HudFragments>().ShownText == "x 1");
    }

    // ------------------------------------------------------------------ Settings backup

    private void RememberSettings()
    {
        _music = VolumeSettings.Music;
        _sfx = VolumeSettings.Sfx;
        _shake = GameFeelSettings.ScreenShakeEnabled;
        _shakeStrength = GameFeelSettings.ScreenShakeStrength;
        _hitPause = GameFeelSettings.HitPauseEnabled;
    }

    private void RestoreSettings()
    {
        VolumeSettings.Music = _music;
        VolumeSettings.Sfx = _sfx;
        GameFeelSettings.ScreenShakeEnabled = _shake;
        GameFeelSettings.ScreenShakeStrength = _shakeStrength;
        GameFeelSettings.HitPauseEnabled = _hitPause;
    }
}
