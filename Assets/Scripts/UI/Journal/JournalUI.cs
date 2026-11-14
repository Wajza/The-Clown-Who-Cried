using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Pip's Journal. MenuController opens it (Tab / gamepad Select, or "Journal" in the pause menu).
///
/// PAGES OF THE JOURNAL (PageLeft / PageRight: A/D, arrows, LB/RB)
///   - One Task Board per city (Tumbleton, Wonderwick): every task of that city
///       [x] done      [>] in progress (plus its current objective)      [ ] not started yet
///   - Torn Pages: every page; found ones can be read, the rest show as "(torn out)".
/// W/S or Up/Down picks an entry in the list; the right side shows its details.
/// The header always shows Pip's Star Fragments and progress toward the next Smile Star.
///
/// Where the information comes from (it never stores its own copy):
///   tasks       TaskManager (AllTasks, IsActive, IsCompleted, GetCurrentObjective)
///   pages       JournalPages (All, IsFound)
///   fragments   my teammate's PlayerStarFragments.Current (FragmentCount, FragmentsTowardNextStar)
///
/// Setup: on the Journal panel inside the MenuCanvas (the builder adds it).
/// </summary>
public class JournalUI : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector fields

    [SerializeField] private TMP_Text pageTitleText;
    [SerializeField] private TMP_Text fragmentsText;
    [SerializeField] private TMP_Text listText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color doneColor = new Color(0.55f, 0.85f, 0.55f);
    [SerializeField] private Color dimColor = new Color(0.55f, 0.55f, 0.6f);

    // ------------------------------------------------------------------ Public API (also used by tests)

    /// <summary>The page of the Journal on show: 0..N-1 are the city Task Boards, N is Torn Pages.</summary>
    public int Tab { get; private set; }

    public int TabCount => CityCount + 1;
    public bool OnPagesTab => Tab == CityCount;
    public int SelectedIndex { get; private set; }

    public string PageTitle => pageTitleText.text;
    public string ListText => listText.text;
    public string DetailText => detailText.text;
    public string FragmentsText => fragmentsText.text;

    public void NextTab() => SetTab((Tab + 1) % TabCount);
    public void PreviousTab() => SetTab((Tab + TabCount - 1) % TabCount);

    public void SetTab(int tab)
    {
        Tab = Mathf.Clamp(tab, 0, TabCount - 1);
        SelectedIndex = 0;
        Refresh();
    }

    /// <summary>Moves the highlight in the list: -1 = up, +1 = down (wraps around).</summary>
    public void MoveSelection(int direction)
    {
        int count = OnPagesTab ? JournalPages.All.Count : TasksOnBoard((TaskData.City)Tab).Count;
        if (count == 0)
            return;
        SelectedIndex = (SelectedIndex + direction + count) % count;
        Refresh();
    }

    // ------------------------------------------------------------------ Private state

    private static readonly int CityCount = Enum.GetValues(typeof(TaskData.City)).Length;
    private readonly StringBuilder _list = new StringBuilder();
    private readonly StringBuilder _detail = new StringBuilder();

    // ------------------------------------------------------------------ Unity lifecycle

    private void OnEnable()
    {
        // Open on the board of the city Pip is in (by scene name), else the first one.
        Tab = GuessCurrentCityTab();
        SelectedIndex = 0;
        Refresh();
    }

    private void Update()
    {
        // These keys are read here (not by the EventSystem), and only while the Journal is open.
        if (SystemInput.WasPressed(SystemInput.PageRight)) NextTab();
        if (SystemInput.WasPressed(SystemInput.PageLeft)) PreviousTab();
        if (SystemInput.WasPressed(SystemInput.ChoiceDown)) MoveSelection(+1);
        if (SystemInput.WasPressed(SystemInput.ChoiceUp)) MoveSelection(-1);
    }

    // ------------------------------------------------------------------ Drawing

    /// <summary>Rebuilds every text from the current game state.</summary>
    public void Refresh()
    {
        ShowFragments();
        _list.Clear();
        _detail.Clear();

        if (OnPagesTab)
            DrawPages();
        else
            DrawTaskBoard((TaskData.City)Tab);

        listText.text = _list.ToString();
        detailText.text = _detail.ToString();
    }

    private void ShowFragments()
    {
        PlayerStarFragments fragments = PlayerStarFragments.Current;
        fragmentsText.text = fragments == null
            ? ""
            : $"Star Fragments: {fragments.FragmentCount}   Next star: {fragments.FragmentsTowardNextStar}/{fragments.FragmentsPerStar}";
    }

    private void DrawTaskBoard(TaskData.City city)
    {
        pageTitleText.text = $"< {city} Task Board >";
        List<TaskData> tasks = TasksOnBoard(city);
        if (tasks.Count == 0)
        {
            _list.Append(Colour("No tasks here yet.", dimColor));
            return;
        }

        TaskManager manager = TaskManager.Instance;
        for (int i = 0; i < tasks.Count; i++)
        {
            TaskData task = tasks[i];
            bool done = TaskManager.IsCompleted(task);
            bool active = manager != null && manager.IsActive(task);
            string box = done ? "[x]" : active ? "[>]" : "[ ]";
            AddListLine(i, $"{box} {task.title}", done ? doneColor : active ? Color.white : dimColor);

            if (i != SelectedIndex)
                continue;

            _detail.Append(task.title).Append("\n\n");
            if (!string.IsNullOrEmpty(task.description))
                _detail.Append(task.description).Append("\n\n");

            if (done)
                _detail.Append(Colour("Done!", doneColor));
            else if (active)
                _detail.Append("Now: ").Append(manager.GetCurrentObjective(task)?.description ?? "");
            else
                _detail.Append(Colour("Not started.", dimColor));
        }
    }

    private void DrawPages()
    {
        IReadOnlyList<JournalPageData> pages = JournalPages.All;
        pageTitleText.text = $"< Torn Pages  {JournalPages.FoundCount}/{pages.Count} >";
        if (pages.Count == 0)
        {
            _list.Append(Colour("No pages.", dimColor));
            return;
        }

        for (int i = 0; i < pages.Count; i++)
        {
            JournalPageData page = pages[i];
            bool found = JournalPages.IsFound(page);
            AddListLine(i, found ? $"Page {page.order}: {page.title}" : $"Page {page.order}: (torn out)",
                found ? Color.white : dimColor);

            if (i == SelectedIndex)
                _detail.Append(found ? page.text : Colour("This page is missing. Maybe it's somewhere out there...", dimColor));
        }
    }

    private void AddListLine(int index, string text, Color colour)
    {
        if (_list.Length > 0)
            _list.Append('\n');
        bool selected = index == SelectedIndex;
        _list.Append(Colour((selected ? "> " : "  ") + text, selected ? selectedColor : colour));
    }

    private static string Colour(string text, Color colour) =>
        $"<color=#{ColorUtility.ToHtmlStringRGB(colour)}>{text}</color>";

    // ------------------------------------------------------------------ Helpers

    /// <summary>The tasks listed on a city's board, sorted by Board Order then title.</summary>
    private static List<TaskData> TasksOnBoard(TaskData.City city)
    {
        var result = new List<TaskData>();
        if (TaskManager.Instance == null)
            return result;

        foreach (TaskData task in TaskManager.Instance.AllTasks)
            if (task.showOnTaskBoard && task.city == city && (!task.testOnly || JournalPages.IncludeTestContent))
                result.Add(task);

        result.Sort((a, b) => a.boardOrder != b.boardOrder
            ? a.boardOrder.CompareTo(b.boardOrder)
            : string.CompareOrdinal(a.title, b.title));
        return result;
    }

    /// <summary>If the scene name starts with a city's name (e.g. "Wonderwick_Hub"), open that board.</summary>
    private static int GuessCurrentCityTab()
    {
        string scene = SceneLoader.CurrentSceneName ?? "";
        foreach (TaskData.City city in Enum.GetValues(typeof(TaskData.City)))
            if (scene.StartsWith(city.ToString(), StringComparison.OrdinalIgnoreCase))
                return (int)city;
        return 0;
    }
}
