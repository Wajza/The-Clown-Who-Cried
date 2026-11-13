using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A named place where Pip can appear in a scene: where she arrives through a door, steps off
/// the train, or comes back after dying.
///
/// HOW IT WORKS
/// Every SpawnPoint adds itself to a static list (keyed by its id) while it is enabled, so any
/// script can find one by name without searching the scene:
///   SpawnPoint point = SpawnPoint.Find("FromTrain");
///
/// SceneLoader uses this to place Pip after loading a scene. Checkpoint is a SpawnPoint too
/// (it inherits from this class), so a save can say "respawn at checkpoint X" the same way.
///
/// Setup: an empty GameObject where Pip's pivot should stand. Give it an id that is unique
/// within its scene. Tick "Is Default" on the one Pip should use when no id is given.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector fields

    [SerializeField, Tooltip("Unique name inside this scene, e.g. 'FromTrain' or 'TownSquare'.")]
    private string id;

    [SerializeField, Tooltip("Used when a scene is loaded without a spawn id (e.g. New Game). One per scene.")]
    private bool isDefault;

    // ------------------------------------------------------------------ Public API

    public string Id => id;
    public bool IsDefault => isDefault;

    /// <summary>Where Pip is placed.</summary>
    public Vector2 Position => transform.position;

    /// <summary>Finds an enabled spawn point by id in the loaded scene(s). Returns null if none.</summary>
    public static SpawnPoint Find(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId))
            return null;
        s_all.TryGetValue(spawnId, out SpawnPoint point);
        return point;
    }

    /// <summary>The spawn point marked "Is Default", or null if there isn't one.</summary>
    public static SpawnPoint FindDefault()
    {
        foreach (SpawnPoint point in s_all.Values)
        {
            if (point.isDefault)
                return point;
        }
        return null;
    }

    // ------------------------------------------------------------------ Registry

    private static readonly Dictionary<string, SpawnPoint> s_all = new Dictionary<string, SpawnPoint>();

    // 'protected virtual' so Checkpoint can add to these and still call the originals with base.OnEnable().
    protected virtual void OnEnable()
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning($"{GetType().Name} '{name}' has no id, so it can't be found.", this);
            return;
        }

        if (s_all.TryGetValue(id, out SpawnPoint other) && other != this)
            Debug.LogWarning($"Two spawn points use the id '{id}' ('{other.name}' and '{name}'). Ids must be unique.", this);

        s_all[id] = this;
    }

    protected virtual void OnDisable()
    {
        // Only remove the entry if it is still us (a duplicate might have replaced it).
        if (!string.IsNullOrEmpty(id) && s_all.TryGetValue(id, out SpawnPoint current) && current == this)
            s_all.Remove(id);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => s_all.Clear();

    // ------------------------------------------------------------------ Editor helpers

    protected virtual void OnDrawGizmos()
    {
        // A small arrow-like marker so spawn points are easy to see in the Scene view.
        Gizmos.color = isDefault ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.25f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up);
    }
}
