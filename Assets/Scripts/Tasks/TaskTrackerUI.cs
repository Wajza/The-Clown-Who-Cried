using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows the player their tasks:
///   - TRACKER (top right): every running task's title and its current objective.
///     Only shown during normal play (hidden in dialogue, cutscenes, the title screen...)
///     and only when at least one task is running.
///   - BANNER (top centre): "New task: ...", "Task complete: ..." and "Task failed: ..." for a
///     couple of seconds.
///     If several arrive at once they're shown one after another.
///
/// It only LISTENS (GameEvents.TaskStarted / TaskProgressed / TaskCompleted, SceneLoaded,
/// GameStateChanged) and reads TaskManager. Nothing calls it.
///
/// Banner timing uses unscaled time, so it still fades while the game is paused.
///
/// Setup: on the TaskCanvas in the GameSystems prefab (the builder adds it).
/// </summary>
public class TaskTrackerUI : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector fields

    [Header("Tracker")]
    [SerializeField, Tooltip("The tracker box (switched on/off).")]
    private GameObject trackerPanel;
    [SerializeField] private TMP_Text trackerText;
    [SerializeField] private Color titleColor = new Color(1f, 0.85f, 0.3f);

    [Header("Banner")]
    [SerializeField, Tooltip("CanvasGroup on the banner, used to fade it.")]
    private CanvasGroup banner;
    [SerializeField] private TMP_Text bannerText;
    [SerializeField, Min(0.1f), Tooltip("How long each banner stays fully visible (seconds).")]
    private float bannerSeconds = 2.5f;
    [SerializeField, Min(0.01f)] private float bannerFadeSeconds = 0.25f;

    // ------------------------------------------------------------------ Public API (used by tests)

    public bool TrackerVisible => trackerPanel != null && trackerPanel.activeSelf;
    public string TrackerText => trackerText != null ? trackerText.text : "";
    public bool BannerShowing => _bannerRoutine != null;
    public string BannerText => bannerText != null ? bannerText.text : "";

    // ------------------------------------------------------------------ Private state

    private readonly Queue<string> _bannerQueue = new Queue<string>();
    private Coroutine _bannerRoutine;
    private readonly StringBuilder _sb = new StringBuilder();

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        trackerPanel.SetActive(false);
        banner.alpha = 0f;
    }

    private void OnEnable()
    {
        GameEvents.TaskStarted += OnTaskStarted;
        GameEvents.TaskProgressed += OnTaskProgressed;
        GameEvents.TaskCompleted += OnTaskCompleted;
        GameEvents.TaskFailed += OnTaskFailed;
        GameEvents.SceneLoaded += OnSceneLoaded;
        GameEvents.SceneLoadStarted += OnSceneLoadStarted;
        GameEvents.GameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.TaskStarted -= OnTaskStarted;
        GameEvents.TaskProgressed -= OnTaskProgressed;
        GameEvents.TaskCompleted -= OnTaskCompleted;
        GameEvents.TaskFailed -= OnTaskFailed;
        GameEvents.SceneLoaded -= OnSceneLoaded;
        GameEvents.SceneLoadStarted -= OnSceneLoadStarted;
        GameEvents.GameStateChanged -= OnGameStateChanged;
        _bannerRoutine = null; // Coroutines stop when disabled.
    }

    /// <summary>A scene change drops any banner on screen or waiting (e.g. quitting to the title).</summary>
    private void OnSceneLoadStarted(string sceneName)
    {
        if (_bannerRoutine != null)
            StopCoroutine(_bannerRoutine);
        _bannerRoutine = null;
        _bannerQueue.Clear();
        banner.alpha = 0f;
    }

    // ------------------------------------------------------------------ Event handlers

    private void OnTaskStarted(TaskData task)
    {
        ShowBanner($"New task: {task.title}");
        Refresh();
    }

    private void OnTaskProgressed(TaskData task) => Refresh();

    private void OnTaskCompleted(TaskData task)
    {
        ShowBanner($"Task complete: {task.title}");
        Refresh();
    }

    private void OnTaskFailed(TaskData task)
    {
        ShowBanner($"Task failed: {task.title}");
        Refresh();
    }

    private void OnSceneLoaded(string sceneName, string spawnPointId) => Refresh();
    private void OnGameStateChanged(GameState state) => Refresh();

    // A timed step (W1) shows a clock: redraw a few times a second while one runs.
    private float _nextClockRefresh;

    private void Update()
    {
        if (TaskManager.Instance != null && TaskManager.Instance.AnyTimedStep && Time.unscaledTime >= _nextClockRefresh)
        {
            _nextClockRefresh = Time.unscaledTime + 0.25f;
            Refresh();
        }
    }

    // ------------------------------------------------------------------ Tracker

    /// <summary>Rebuilds the tracker text and decides whether it's shown.</summary>
    public void Refresh()
    {
        TaskManager tasks = TaskManager.Instance;
        bool playing = GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;
        bool any = tasks != null && tasks.ActiveTasks.Count > 0;

        trackerPanel.SetActive(playing && any);
        if (!any)
        {
            trackerText.text = "";
            return;
        }

        // One block per task:   <title in colour>
        //                       - current objective
        string colour = ColorUtility.ToHtmlStringRGB(titleColor);
        _sb.Clear();
        foreach (TaskData task in tasks.ActiveTasks)
        {
            if (_sb.Length > 0)
                _sb.Append('\n');
            _sb.Append($"<color=#{colour}>{task.title}</color>");

            string objective = tasks.GetObjectiveText(task); // e.g. "Pop the balloons (3/8)"
            if (!string.IsNullOrEmpty(objective))
                _sb.Append($"\n- {objective}");
        }
        trackerText.text = _sb.ToString();
    }

    // ------------------------------------------------------------------ Banner

    private void ShowBanner(string message)
    {
        _bannerQueue.Enqueue(message);
        if (_bannerRoutine == null && isActiveAndEnabled)
            _bannerRoutine = StartCoroutine(PlayBanners());
    }

    private IEnumerator PlayBanners()
    {
        while (_bannerQueue.Count > 0)
        {
            bannerText.text = _bannerQueue.Dequeue();
            yield return Fade(0f, 1f);

            float end = Time.unscaledTime + bannerSeconds;
            while (Time.unscaledTime < end)
                yield return null;

            yield return Fade(1f, 0f);
        }
        _bannerRoutine = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < bannerFadeSeconds; t += Time.unscaledDeltaTime)
        {
            banner.alpha = Mathf.Lerp(from, to, t / bannerFadeSeconds);
            yield return null;
        }
        banner.alpha = to;
    }
}
