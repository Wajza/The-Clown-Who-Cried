using UnityEngine;

/// <summary>
/// A stall that takes one box of punchlines in W1 "Punchline Delivery". When Pip walks into it
/// while carrying a CarryableBox, the box is delivered (it disappears), the stall shows it's
/// served, and the task signal "w1_delivered" is sent (the task counts 5 of them).
///
/// Each stall takes ONE box per attempt. If the task fails (time ran out) or starts again, every
/// stall is ready again.
///
/// Setup: a GameObject with a stall sprite, a trigger BoxCollider2D (the counter Pip walks
/// into), this script, the W1 task, and optionally a "Served Marker" child (e.g. a green flag).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DeliveryStall : MonoBehaviour
{
    [SerializeField] private TaskData task;
    [SerializeField] private string signalKey = "w1_delivered";
    [SerializeField, Tooltip("Optional: shown once this stall got its box.")]
    private GameObject servedMarker;

    public bool Served { get; private set; }

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        SetServed(false);
        GameEvents.TaskStarted += OnTaskReset;
        GameEvents.TaskFailed += OnTaskReset;
    }

    private void OnDestroy()
    {
        GameEvents.TaskStarted -= OnTaskReset;
        GameEvents.TaskFailed -= OnTaskReset;
    }

    private void OnTaskReset(TaskData resetTask)
    {
        if (resetTask == task)
            SetServed(false);
    }

    // Stay (not just Enter): Pip may already stand here when she gets back with a box.
    private void OnTriggerStay2D(Collider2D other)
    {
        CarryableBox box = CarryableBox.Carried;
        if (box == null || Served || other.GetComponentInParent<PlayerController>() == null)
            return;
        if (TaskManager.Instance == null || !TaskManager.Instance.IsActive(task))
            return;

        box.Deliver();
        SetServed(true);
        GameEvents.RaiseTaskSignal(signalKey);
    }

    private void SetServed(bool served)
    {
        Served = served;
        if (servedMarker != null)
            servedMarker.SetActive(served);
    }
}
