using System;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Plays cutscenes. A cutscene is a Timeline (a PlayableDirector in the level, Play On Awake OFF)
/// and can contain my DialogueTrack (conversations that pause the timeline until they're read),
/// activation tracks (show/hide things), animation tracks, and so on.
///
/// WHILE A CUTSCENE PLAYS
///   - GameState.Cutscene (the HUD and task tracker hide; no pause menu), Pip is locked,
///   - GameEvents.CutsceneStarted(name) ... CutsceneEnded(name).
///   - Pause (Esc / gamepad Start) SKIPS it: any open conversation closes and the timeline jumps
///     to its end, so everything it shows/hides ends up in its final state.
///
/// Anything that must happen AFTER the cutscene (start the boss fight, load the next scene)
/// goes in Play's onFinished; it runs whether the cutscene was watched or skipped.
///
/// Setup: on the GameSystems prefab (the builder adds it). Levels start cutscenes with a
/// CutsceneTrigger or from scripts like BossFightFlow.
/// </summary>
public class CutscenePlayer : MonoBehaviour
{
    public static CutscenePlayer Instance { get; private set; }

    /// <summary>True while Skip() is running (DialogueClips use it to not start conversations).</summary>
    public static bool IsSkipping { get; private set; }

    public bool IsPlaying => _director != null;
    public PlayableDirector Current => _director;

    /// <summary>Plays a cutscene. Returns false if one is already playing.</summary>
    public bool Play(PlayableDirector director, Action onFinished = null)
    {
        if (director == null || IsPlaying)
            return false;

        _director = director;
        _onFinished = onFinished;
        _name = director.playableAsset != null ? director.playableAsset.name : director.name;

        GameManager gm = GameManager.Instance;
        _stateBefore = gm != null ? gm.State : GameState.Playing;
        if (gm != null)
            gm.SetState(GameState.Cutscene);
        PlayerInputLock.Lock(this);
        GameEvents.RaiseCutsceneStarted(_name);

        director.extrapolationMode = DirectorWrapMode.None; // Stop (and fire 'stopped') at the end.
        director.stopped += OnDirectorStopped;
        director.time = 0;
        director.Play();
        return true;
    }

    /// <summary>Jumps to the end (Pause button). Conversations are closed.</summary>
    public void Skip()
    {
        if (!IsPlaying)
            return;

        IsSkipping = true;
        PlayableDirector director = _director;
        if (DialogueRunner.Instance != null)
            DialogueRunner.Instance.StopDialogue();

        // Show the final frame (so activation tracks etc. end in the right state), then stop.
        if (director.playableGraph.IsValid())
            director.playableGraph.GetRootPlayable(0).SetSpeed(1);
        director.time = director.duration;
        director.Evaluate();
        director.Stop(); // -> OnDirectorStopped
        IsSkipping = false;
    }

    // ------------------------------------------------------------------ Private state

    private PlayableDirector _director;
    private Action _onFinished;
    private string _name;
    private GameState _stateBefore;
    private int _startFrame;

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable() => GameEvents.SceneLoadStarted += OnSceneLoadStarted;
    private void OnDisable() => GameEvents.SceneLoadStarted -= OnSceneLoadStarted;

    private void Update()
    {
        if (IsPlaying && SystemInput.WasPressed(SystemInput.Pause))
            Skip();

        // A director in an unloaded scene is gone: tidy up.
        if (IsPlaying && _director == null)
            Finish(runCallback: false);
    }

    // Leaving the level mid-cutscene (shouldn't happen, but never leave Pip locked).
    private void OnSceneLoadStarted(string sceneName)
    {
        if (IsPlaying)
            Finish(runCallback: false);
    }

    // ------------------------------------------------------------------ Ending

    private void OnDirectorStopped(PlayableDirector director)
    {
        if (director == _director)
            Finish(runCallback: true);
    }

    private void Finish(bool runCallback)
    {
        if (_director != null)
            _director.stopped -= OnDirectorStopped;
        _director = null;

        PlayerInputLock.Unlock(this);
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.State == GameState.Cutscene)
            gm.SetState(_stateBefore == GameState.Cutscene ? GameState.Playing : _stateBefore);
        GameEvents.RaiseCutsceneEnded(_name);

        Action callback = _onFinished;
        _onFinished = null;
        if (runCallback)
            callback?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        IsSkipping = false;
    }
}
