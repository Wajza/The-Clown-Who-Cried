using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows the level's name ("Tumbleton") for a couple of seconds when Pip arrives, if the
/// level's LevelInfo has "Show Name Card" ticked. Not shown again when the same level loads
/// twice in a row (respawning never reloads the level anyway).
///
/// Setup: on the TaskCanvas in the GameSystems prefab (the builder adds it).
/// </summary>
public class LevelNameCard : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text nameText;
    [SerializeField, Min(0.1f)] private float holdSeconds = 2f;
    [SerializeField, Min(0.01f)] private float fadeSeconds = 0.4f;

    public bool Showing => _routine != null;
    public string ShownName => nameText.text;

    private Coroutine _routine;
    private string _lastLevel;

    private void Awake() => group.alpha = 0f;

    private void OnEnable() => GameEvents.SceneLoaded += OnSceneLoaded;

    private void OnDisable()
    {
        GameEvents.SceneLoaded -= OnSceneLoaded;
        _routine = null;
        group.alpha = 0f;
    }

    private void OnSceneLoaded(string sceneName, string spawnPointId)
    {
        LevelInfo level = LevelInfo.Current;
        if (level == null || !level.ShowNameCard || sceneName == _lastLevel)
        {
            _lastLevel = level != null ? sceneName : null;
            return;
        }
        _lastLevel = sceneName;

        if (_routine != null)
            StopCoroutine(_routine);
        nameText.text = level.DisplayName;
        _routine = StartCoroutine(Show());
    }

    private IEnumerator Show()
    {
        yield return Fade(0f, 1f);
        float end = Time.unscaledTime + holdSeconds;
        while (Time.unscaledTime < end)
            yield return null;
        yield return Fade(1f, 0f);
        _routine = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / fadeSeconds);
            yield return null;
        }
        group.alpha = to;
    }
}
