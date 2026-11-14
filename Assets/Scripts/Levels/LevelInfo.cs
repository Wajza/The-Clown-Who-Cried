using UnityEngine;

/// <summary>
/// One per level scene. Says what the level is and sets it up when it loads.
///
///   Display Name     shown on the name card when Pip arrives ("Tumbleton").
///   City             which city the level belongs to (the Journal opens on its Task Board).
///   Music Id         the music to play (used by the AudioManager in Part 9).
///   Show Name Card   show the name for a moment when the level starts.
///   Lock Abilities Until Earned   Pip's mallet/Blue Sight/Juggle Shot only work once earned
///                    (see PlayerAbilities). Untick for test levels.
///   Spawn Player     if the scene has no Pip, create one (GameSettings > Player Prefab) here.
///                    SceneLoader then moves her to the right spawn point.
///   Camera Bounds    the area the camera may show (a BoxCollider2D or PolygonCollider2D,
///                    usually around the whole level). CameraZones can switch to smaller ones.
///
/// Setup: an empty GameObject called "LevelInfo" in every level, at the default spawn point.
/// </summary>
[DefaultExecutionOrder(-100)] // Before other scripts, so Pip exists when they wake up.
public class LevelInfo : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector fields

    [SerializeField] private string displayName = "Level";
    [SerializeField] private TaskData.City city = TaskData.City.Tumbleton;
    [SerializeField, Tooltip("Id of the music track for this level (Part 9).")]
    private string musicId = "";
    [SerializeField] private bool showNameCard = true;
    [SerializeField] private bool lockAbilitiesUntilEarned = true;
    [SerializeField] private bool spawnPlayer = true;
    [SerializeField, Tooltip("The camera stays inside this shape. Must be a BoxCollider2D or PolygonCollider2D (trigger).")]
    private Collider2D cameraBounds;

    // ------------------------------------------------------------------ Public API

    /// <summary>The LevelInfo of the loaded level, or null (title screen, test scenes...).</summary>
    public static LevelInfo Current { get; private set; }

    public string DisplayName => displayName;
    public TaskData.City City => city;
    public string MusicId => musicId;
    public bool ShowNameCard => showNameCard;
    public bool LockAbilitiesUntilEarned => lockAbilitiesUntilEarned;
    public Collider2D CameraBounds => cameraBounds;

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        Current = this;

        if (spawnPlayer && !SceneHasPip())
            SpawnPip();
    }

    /// <summary>
    /// Is there a Pip in THIS scene? (Not PlayerController.Current: while scenes change, the old
    /// level's Pip may still exist, and a Pip placed in this scene may not have woken up yet.)
    /// </summary>
    private bool SceneHasPip()
    {
        foreach (PlayerController pip in FindObjectsByType<PlayerController>(FindObjectsInactive.Include))
            if (pip.gameObject.scene == gameObject.scene)
                return true;
        return false;
    }

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }

    private void SpawnPip()
    {
        GameSettings settings = GameSettings.Instance;
        if (settings == null || settings.playerPrefab == null)
        {
            Debug.LogError("LevelInfo: no Player Prefab in Assets/Resources/GameSettings, so there's no Pip.", this);
            return;
        }

        PlayerController pip = Instantiate(settings.playerPrefab, transform.position, Quaternion.identity);
        pip.name = "Pip";

        // While loading, the OLD scene is still the "active" one, and new objects go there. Move Pip
        // into this level's scene, or she'd be destroyed together with the old level.
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(pip.gameObject, gameObject.scene);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Current = null;
}
