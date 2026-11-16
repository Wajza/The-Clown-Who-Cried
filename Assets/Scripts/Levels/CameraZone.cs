using UnityEngine;

/// <summary>
/// A room or area with its own camera limits, e.g. a small shop inside Tumbleton or one
/// chamber of the Mirror Maze. While Pip is inside it, the camera stays within this shape
/// instead of the whole level's bounds (LevelInfo > Camera Bounds).
///
/// The same collider is both "is Pip in here?" and "the area the camera may show".
///
/// Setup: a GameObject with a trigger BoxCollider2D or PolygonCollider2D covering the room
/// (at least 30 x 17 tiles, the size of the screen, or the camera can't fit) and this script.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CameraZone : MonoBehaviour
{
    public Collider2D Bounds { get; private set; }

    private void Awake()
    {
        Bounds = GetComponent<Collider2D>();
        Bounds.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPipBody(other) && CameraRig.Current != null)
            CameraRig.Current.EnterZone(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPipBody(other) && CameraRig.Current != null)
            CameraRig.Current.ExitZone(this);
    }

    // Only Pip's main body counts (not her mallet hitbox and other child colliders).
    private static bool IsPipBody(Collider2D other) =>
        other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<PlayerController>() != null
        && other.gameObject == other.attachedRigidbody.gameObject;
}
