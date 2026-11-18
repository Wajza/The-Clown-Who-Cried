using UnityEngine;

/// <summary>
/// PLACEHOLDER swing effect: shows a see-through box exactly where the mallet's hitbox is, while
/// it can hit. Without it a swing is invisible (there's no swing animation yet), which makes
/// the mallet look like it does nothing. Replace with the artist's swing animation later.
///
/// Setup: on Pip (next to PlayerMallet), with a child SpriteRenderer (a 1x1 white square)
/// assigned as Swing Sprite. It must NOT be inside Pip's Visual child (squash and stretch).
/// </summary>
[RequireComponent(typeof(PlayerMallet))]
public class MalletSwingVisual : MonoBehaviour
{
    [SerializeField, Tooltip("Child sprite (1x1 tile) stretched over the active hitbox.")]
    private SpriteRenderer swingSprite;
    [SerializeField] private Color color = new Color(1f, 1f, 0.85f, 0.45f);

    private PlayerMallet _mallet;

    private void Awake()
    {
        _mallet = GetComponent<PlayerMallet>();
        if (swingSprite != null)
            swingSprite.enabled = false;
    }

    private void LateUpdate()
    {
        if (swingSprite == null)
            return;

        bool active = _mallet.TryGetActiveHitbox(out Vector2 center, out Vector2 size);
        swingSprite.enabled = active;
        if (!active)
            return;

        swingSprite.color = color;
        swingSprite.transform.position = new Vector3(center.x, center.y, transform.position.z);
        swingSprite.transform.localScale = new Vector3(size.x, size.y, 1f); // 1x1 sprite -> hitbox size.
    }
}
