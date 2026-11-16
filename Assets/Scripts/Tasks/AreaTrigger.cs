using UnityEngine;

/// <summary>
/// An invisible area that sends a task signal every time Pip walks into it, e.g. "p1_outside"
/// just outside Pip's house, or "p2_crossed" on the far side of Honk Street.
/// A SignalObjective with the same key counts it (only while that step is the current one).
///
/// Optional: also set a save flag the first time (e.g. for dialogue or doors to react).
///
/// Setup: a GameObject with a trigger BoxCollider2D covering the area, and this script.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AreaTrigger : MonoBehaviour
{
    [SerializeField, Tooltip("Task signal sent when Pip enters, e.g. 'p2_crossed'.")]
    private string signalKey;

    [SerializeField, Tooltip("Optional flag set when Pip enters. Empty = none.")]
    private string setFlag;

    public string SignalKey => signalKey;

    private void Awake() => GetComponent<Collider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only Pip's main body (not her mallet hitbox and other child colliders).
        Rigidbody2D body = other.attachedRigidbody;
        if (body == null || other.gameObject != body.gameObject || body.GetComponent<PlayerController>() == null)
            return;

        if (!string.IsNullOrEmpty(setFlag) && GameManager.Instance != null)
            GameManager.Instance.Save.SetFlag(setFlag);
        GameEvents.RaiseTaskSignal(signalKey);
    }
}
