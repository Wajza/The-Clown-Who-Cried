using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads scenes behind a fade and puts Pip at the right spawn point.
///
/// RULE: every scene change goes through SceneLoader.Instance.LoadScene(...), never straight
/// through Unity's SceneManager. That way the fade, the input lock, Pip's placement and the
/// SceneLoaded event always happen, in the same order:
///
///   1. Lock Pip's input and raise GameEvents.SceneLoadStarted.
///   2. Fade to black.
///   3. Load the new scene (async, so the game doesn't freeze).
///   4. Move Pip to the requested SpawnPoint (or the scene's default one).
///   5. Raise GameEvents.SceneLoaded(scene, spawnId). Other systems set themselves up here.
///   6. Fade back in, then unlock Pip.
///
/// The very first scene (the one Play starts in) is not "loaded" by us, so Start() raises
/// SceneLoaded for it. That means you can press Play in any level and everything still works.
///
/// Setup: on the GameSystems prefab. Every scene must be in File > Build Profiles > Scene List.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector fields

    [SerializeField, Min(0f), Tooltip("Seconds to fade to black before loading.")]
    private float fadeOutTime = 0.4f;

    [SerializeField, Min(0f), Tooltip("Seconds to fade back in after loading.")]
    private float fadeInTime = 0.4f;

    // ------------------------------------------------------------------ Public API

    public static SceneLoader Instance { get; private set; }

    /// <summary>True from the start of a load until the fade-in has finished.</summary>
    public bool IsLoading { get; private set; }

    /// <summary>Name of the scene that is open right now.</summary>
    public static string CurrentSceneName => SceneManager.GetActiveScene().name;

    /// <summary>
    /// Loads a scene. spawnPointId is the SpawnPoint (or Checkpoint) id Pip should appear at.
    /// Leave it out to use the scene's default spawn point.
    /// Example: SceneLoader.Instance.LoadScene("Wonderwick_Hub", "FromTrain");
    /// </summary>
    public void LoadScene(string sceneName, string spawnPointId = null)
    {
        if (IsLoading)
        {
            Debug.LogWarning($"SceneLoader: already loading a scene, ignored request for '{sceneName}'.", this);
            return;
        }

        // Catch typos and scenes missing from the Build Profile with a clear message.
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"SceneLoader: scene '{sceneName}' not found. Is it in File > Build Profiles > Scene List?", this);
            return;
        }

        StartCoroutine(LoadRoutine(sceneName, spawnPointId));
    }

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        // The scene Play started in. Leave Pip where you placed her in the scene (handy for testing).
        GameEvents.RaiseSceneLoaded(CurrentSceneName, null);
    }

    // ------------------------------------------------------------------ Loading

    private IEnumerator LoadRoutine(string sceneName, string spawnPointId)
    {
        IsLoading = true;
        PlayerInputLock.Lock(this);
        GameEvents.RaiseSceneLoadStarted(sceneName);

        ScreenFader fader = ScreenFader.Instance;
        if (fader != null)
            yield return fader.FadeOut(fadeOutTime);

        // In case we left from the pause menu (timeScale 0) or during a hit pause.
        Time.timeScale = 1f;

        // Load the new scene. LoadSceneAsync replaces the old scene by default.
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        while (!load.isDone)
            yield return null;

        PlacePlayer(spawnPointId);

        // The new Pip's PlayerController switched the "Player" map on by itself (in OnEnable),
        // so re-apply our lock. Pip stays frozen until the fade-in is done.
        PlayerInputLock.Refresh();

        GameEvents.RaiseSceneLoaded(sceneName, spawnPointId);

        if (fader != null)
            yield return fader.FadeIn(fadeInTime);

        PlayerInputLock.Unlock(this);
        IsLoading = false;
    }

    /// <summary>Moves Pip to the requested spawn point, or the default one if it's missing.</summary>
    private void PlacePlayer(string spawnPointId)
    {
        PlayerController player = PlayerController.Current;
        if (player == null)
            return; // Scenes without Pip (title screen, train map) are fine.

        SpawnPoint point = SpawnPoint.Find(spawnPointId);
        if (point == null)
        {
            if (!string.IsNullOrEmpty(spawnPointId))
                Debug.LogWarning($"SceneLoader: no spawn point '{spawnPointId}' in this scene. Using the default one.", this);
            point = SpawnPoint.FindDefault();
        }

        if (point != null)
            player.Teleport(point.Position, 0f); // My teammate's method: moves Pip and stops all motion.
    }
}
