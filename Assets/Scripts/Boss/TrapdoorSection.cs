using UnityEngine;

/// <summary>
/// One section of the arena floor that can drop open. TrapdoorFloor decides when.
/// Open = colliders off (Pip falls through into the pit below) and the open sprite (or nothing).
///
/// Setup: Ground layer, a BoxCollider2D, a SpriteRenderer and a SpriteFlasher (for the warning
/// shake/blink). UnsafeGround is added automatically so Pip never respawns on a trapdoor.
/// </summary>
[RequireComponent(typeof(Collider2D), typeof(UnsafeGround))]
public class TrapdoorSection : MonoBehaviour
{
    [SerializeField, Tooltip("Sprite to swap. Found automatically if empty.")]
    private SpriteRenderer spriteRenderer;
    [SerializeField, Tooltip("Optional. Empty = keep the current sprite.")]
    private Sprite closedSprite;
    [SerializeField, Tooltip("Optional. Empty = hide the sprite while open.")]
    private Sprite openSprite;

    public bool IsOpen { get; private set; }

    private Collider2D[] _colliders;
    private SpriteFlasher _flasher;

    private void Awake()
    {
        _colliders = GetComponents<Collider2D>();
        _flasher = GetComponent<SpriteFlasher>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>Blinks as a warning that this section is about to open.</summary>
    public void Warn(float seconds)
    {
        if (_flasher != null)
            _flasher.Warn(seconds);
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;

        foreach (Collider2D col in _colliders)
            col.enabled = !open;

        if (spriteRenderer == null)
            return;

        if (open)
        {
            if (openSprite != null)
                spriteRenderer.sprite = openSprite;
            else
                spriteRenderer.enabled = false;
        }
        else
        {
            spriteRenderer.enabled = true;
            if (closedSprite != null)
                spriteRenderer.sprite = closedSprite;
        }
    }
}
