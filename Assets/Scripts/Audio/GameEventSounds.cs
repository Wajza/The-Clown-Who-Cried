using UnityEngine;

/// <summary>
/// UI sounds and little jingles for MY systems, all from GameEvents:
///   GamePausedChanged  -> pause / unpause
///   DialogueStarted    -> dialogue box opens
///   TaskStarted        -> "new task" jingle;  TaskCompleted -> "task done";  TaskFailed -> "task failed"
///   JournalPageFound   -> "page found" jingle
///
/// Setup: on the GameSystems prefab (the builder adds it).
/// </summary>
public class GameEventSounds : MonoBehaviour
{
    private void OnEnable()
    {
        GameEvents.GamePausedChanged += OnPaused;
        GameEvents.DialogueStarted += OnDialogue;
        GameEvents.TaskStarted += OnTaskStarted;
        GameEvents.TaskCompleted += OnTaskCompleted;
        GameEvents.TaskFailed += OnTaskFailed;
        GameEvents.JournalPageFound += OnPage;
    }

    private void OnDisable()
    {
        GameEvents.GamePausedChanged -= OnPaused;
        GameEvents.DialogueStarted -= OnDialogue;
        GameEvents.TaskStarted -= OnTaskStarted;
        GameEvents.TaskCompleted -= OnTaskCompleted;
        GameEvents.TaskFailed -= OnTaskFailed;
        GameEvents.JournalPageFound -= OnPage;
    }

    private static void OnPaused(bool paused) => AudioManager.Play(paused ? SoundIds.UiPause : SoundIds.UiUnpause);
    private static void OnDialogue(DialogueData dialogue) => AudioManager.Play(SoundIds.UiDialogue);
    private static void OnTaskStarted(TaskData task) => AudioManager.Play(SoundIds.TaskNew);
    private static void OnTaskCompleted(TaskData task) => AudioManager.Play(SoundIds.TaskDone);
    private static void OnTaskFailed(TaskData task) => AudioManager.Play(SoundIds.TaskFail);
    private static void OnPage(JournalPageData page) => AudioManager.Play(SoundIds.PageFound);
}
