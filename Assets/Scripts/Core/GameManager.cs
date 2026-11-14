using UnityEngine;

/// <summary>
/// The "brain" that lives for the whole game. It knows:
///   - the current GameState (Title, Playing, Dialogue...),
///   - the current SaveData (what Pip has done so far),
///   - how to start a New Game, Continue, save, and go back to the title.
///
/// HOW IT STAYS ALIVE
/// It sits on the GameSystems prefab together with SceneLoader, ScreenFader, PlayerSaveBridge
/// and PlayerDeathHandler. GameBootstrap creates that prefab once when the game starts, and
/// DontDestroyOnLoad keeps it alive through every scene change. Don't put the prefab in scenes.
///
/// HOW THE SAVE DATA FLOWS
///   - New Game:  Save = fresh SaveData, then load the first level.
///   - Continue:  Save = data read from disk, then load the saved scene at the saved spawn point.
///   - While playing, other systems change Save (fragments, checkpoints, tasks, flags...).
///   - SaveGame() writes Save to disk. It's called on checkpoints and when entering a new scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector fields

    [Header("Scenes")]
    [SerializeField, Tooltip("Name of the title screen scene.")]
    private string titleSceneName = "Title";

    [SerializeField, Tooltip("Scene that New Game starts in.")]
    private string firstSceneName = "Tumbleton";

    [SerializeField, Tooltip("Spawn point id New Game uses in that scene. Empty = the scene's default spawn point.")]
    private string firstSpawnPointId = "";

    [Header("Saving")]
    [SerializeField, Tooltip("Save automatically every time a new scene is entered through SceneLoader.")]
    private bool saveOnSceneEnter = true;

    // ------------------------------------------------------------------ Public API

    /// <summary>The one GameManager. Null only before GameBootstrap has run.</summary>
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.Playing;

    /// <summary>The save data of the game in progress. Never null.</summary>
    public SaveData Save { get; private set; } = new SaveData();

    public bool IsOnTitleScreen => SceneLoader.CurrentSceneName == titleSceneName;

    // ------------------------------------------------------------------ Private state

    // True between SceneLoadStarted and SceneLoaded. Lets us tell a real scene change (save it)
    // apart from pressing Play in the Editor (don't overwrite the save with a test run).
    private bool _changingScene;

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        // Only one GameManager may exist. If another one is already alive, remove this copy.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Survive scene changes (with everything else on the prefab).
    }

    private void OnEnable()
    {
        GameEvents.SceneLoadStarted += OnSceneLoadStarted;
        GameEvents.SceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        GameEvents.SceneLoadStarted -= OnSceneLoadStarted;
        GameEvents.SceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ------------------------------------------------------------------ Game state

    /// <summary>Changes the state and tells everyone (GameEvents.GameStateChanged).</summary>
    public void SetState(GameState newState)
    {
        if (newState == State)
            return;

        State = newState;
        GameEvents.RaiseGameStateChanged(newState);
    }

    // ------------------------------------------------------------------ Starting and quitting

    /// <summary>Scene that New Game starts in.</summary>
    public string FirstSceneName => firstSceneName;

    /// <summary>
    /// Starts a brand-new game. The old save file is only replaced once the game saves.
    /// Returns false (and changes nothing) if the first scene isn't in the Build Profile scene list.
    /// </summary>
    public bool NewGame()
    {
        if (!Application.CanStreamedLevelBeLoaded(firstSceneName))
        {
            Debug.LogError($"GameManager: New Game can't start: scene '{firstSceneName}' is not in the Build Profile scene list.", this);
            return false;
        }

        Save = new SaveData();
        SceneLoader.Instance.LoadScene(firstSceneName, firstSpawnPointId);
        return true;
    }

    /// <summary>Loads the save file and continues from it. Returns false if there is no usable save.</summary>
    public bool ContinueGame()
    {
        SaveData loaded = SaveSystem.Read();
        if (loaded == null || string.IsNullOrEmpty(loaded.sceneName))
            return false;

        Save = loaded;
        SceneLoader.Instance.LoadScene(Save.sceneName, Save.spawnPointId);
        return true;
    }

    /// <summary>Goes back to the title screen (e.g. from the pause menu). Does not save by itself.</summary>
    public void ReturnToTitle()
    {
        PlayerInputLock.ClearAll(); // Drop any leftover dialogue/cutscene locks.
        SceneLoader.Instance.LoadScene(titleSceneName);
    }

    // ------------------------------------------------------------------ Saving

    /// <summary>Writes the current save data to disk. Ignored on the title screen.</summary>
    public void SaveGame()
    {
        if (IsOnTitleScreen)
            return;

        Save.sceneName = SceneLoader.CurrentSceneName;
        if (SaveSystem.Write(Save))
            GameEvents.RaiseGameSaved();
    }

    /// <summary>
    /// Makes a SpawnPoint/Checkpoint in the current scene Pip's respawn point (and the place
    /// Continue starts from). Called by Checkpoint.
    /// </summary>
    public void SetRespawnPoint(string spawnPointId, bool saveNow)
    {
        Save.sceneName = SceneLoader.CurrentSceneName;
        Save.spawnPointId = spawnPointId ?? "";

        if (saveNow)
            SaveGame();
    }

    // ------------------------------------------------------------------ Event handlers

    private void OnSceneLoadStarted(string sceneName)
    {
        _changingScene = true;
        SetState(GameState.Loading);
    }

    private void OnSceneLoaded(string sceneName, string spawnPointId)
    {
        bool isTitle = sceneName == titleSceneName;
        SetState(isTitle ? GameState.Title : GameState.Playing);

        bool wasRealSceneChange = _changingScene;
        _changingScene = false;

        if (isTitle)
            return;

        // Remember where Pip came in, so dying (with no checkpoint yet) or Continue puts her here.
        Save.sceneName = sceneName;
        Save.spawnPointId = spawnPointId ?? "";

        if (saveOnSceneEnter && wasRealSceneChange)
            SaveGame();
    }

    // ------------------------------------------------------------------ Reset

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;
}
