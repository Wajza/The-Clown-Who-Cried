using UnityEngine;

/// <summary>
/// Walk into it to go to another scene: the edge of a street, a path, a tunnel.
/// (For doors you open with Interact, use Door instead.)
///
/// When Pip touches the trigger:
///   - if the Requirement is met, the Destination loads (SceneLoader fades and places Pip at
///     the destination's spawn point);
///   - if not, the Locked Dialogue plays (if set), and Pip stays.
///
/// Touches while a scene is still loading are ignored, so if Pip arrives standing inside an exit
/// she has to step out and back in to use it. Still, put each SpawnPoint a little away from exits.
///
/// Setup: a GameObject with a trigger BoxCollider2D (e.g. 1 tile wide, 3 tall, at the edge of
/// the level) and this script.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LevelExit : MonoBehaviour
{
    [SerializeField] private ExitDestination destination = new ExitDestination();
    [SerializeField] private ExitRequirement requirement = new ExitRequirement();

    public ExitDestination Destination => destination;
    public ExitRequirement Requirement => requirement;

    private void Awake() => GetComponent<Collider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        // Only during normal play (not while loading, in dialogue, or with a menu open).
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
            return;

        if (requirement.IsMet)
            destination.Go();
        else
            requirement.SayLocked();
    }
}
