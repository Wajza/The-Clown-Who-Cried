using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Plays a cutscene ONCE in the whole game (remembered in the save with "Seen Flag"):
///   On Level Loaded   as soon as Pip arrives in the level (e.g. the opening in Pip's house)
///   On Enter          when Pip walks into this object's trigger collider
///
/// Optional Required Flag: only play once that flag is set.
///
/// Setup: an object with this script and the cutscene's PlayableDirector assigned (Play On Awake
/// OFF). For "On Enter", also a trigger Collider2D.
/// </summary>
public class CutsceneTrigger : MonoBehaviour
{
    public enum When { OnLevelLoaded, OnEnter }

    [SerializeField] private PlayableDirector cutscene;
    [SerializeField] private When when = When.OnLevelLoaded;
    [SerializeField, Tooltip("Saved once played, so it never plays again, e.g. 'cs_opening_seen'.")]
    private string seenFlag;
    [SerializeField, Tooltip("Optional: only play once this flag is set.")]
    private string requiredFlag;

    public bool HasBeenSeen => GameManager.Instance != null && GameManager.Instance.Save.HasFlag(seenFlag);

    private void OnEnable() => GameEvents.SceneLoaded += OnSceneLoaded;
    private void OnDisable() => GameEvents.SceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(string sceneName, string spawnPointId)
    {
        if (when == When.OnLevelLoaded)
            TryPlay();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (when == When.OnEnter && other.GetComponentInParent<PlayerController>() != null)
            TryPlay();
    }

    private void TryPlay()
    {
        GameManager gm = GameManager.Instance;
        CutscenePlayer player = CutscenePlayer.Instance;
        if (gm == null || player == null || cutscene == null || HasBeenSeen)
            return;
        if (!string.IsNullOrEmpty(requiredFlag) && !gm.Save.HasFlag(requiredFlag))
            return;

        if (player.Play(cutscene))
            gm.Save.SetFlag(seenFlag); // Saved with the next save (and set now, so it can't double-play).
    }
}
