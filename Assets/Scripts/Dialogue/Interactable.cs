using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for anything Pip can use with the Interact button (F): NPCs now, signs and doors
/// later. It is "abstract": you never add Interactable itself, only a script built on it
/// (like NpcTalk) that says what Interact() does.
///
/// HOW IT WORKS
///   - The object has a trigger collider: its "talk range".
///   - While Pip is inside the range, the object is in the static InRange list.
///   - InteractionInput picks the CLOSEST usable one, shows its prompt (e.g. a "!" bubble), and
///     calls Interact() when F is pressed. So with two NPCs side by side, only one reacts.
///
/// Pip may have several colliders (body, mallet hitbox...). The object counts how many of
/// Pip's colliders are inside, and only says "out of range" once all of them have left.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public abstract class Interactable : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector fields

    [SerializeField, Tooltip("Optional child shown above the object when Pip can use it (e.g. a '!' bubble).")]
    private GameObject prompt;

    // ------------------------------------------------------------------ Public API

    /// <summary>Every interactable Pip is standing in range of right now.</summary>
    public static IReadOnlyList<Interactable> InRange => s_inRange;

    /// <summary>Override to say "not right now" (e.g. an NPC with nothing to say).</summary>
    public virtual bool CanInteract => true;

    /// <summary>What happens when the player presses Interact here.</summary>
    public abstract void Interact();

    /// <summary>Shows or hides the prompt. Called by InteractionInput.</summary>
    public void SetPromptVisible(bool visible)
    {
        if (prompt != null)
            prompt.SetActive(visible);
    }

    public bool PromptVisible => prompt != null && prompt.activeSelf;

    // ------------------------------------------------------------------ Private state

    private static readonly List<Interactable> s_inRange = new List<Interactable>();
    private readonly HashSet<Collider2D> _playerCollidersInside = new HashSet<Collider2D>();

    // ------------------------------------------------------------------ Unity lifecycle

    protected virtual void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true; // A range, not a wall.
        SetPromptVisible(false);
    }

    protected virtual void OnDisable()
    {
        _playerCollidersInside.Clear();
        s_inRange.Remove(this);
        SetPromptVisible(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        _playerCollidersInside.Add(other);
        if (!s_inRange.Contains(this))
            s_inRange.Add(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!_playerCollidersInside.Remove(other))
            return;

        if (_playerCollidersInside.Count == 0)
            s_inRange.Remove(this);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => s_inRange.Clear();
}
