using UnityEngine;

/// <summary>
/// A checkpoint (e.g. a balloon post Pip touches). After touching it, Pip respawns here when she
/// runs out of Smile Stars, and the game saves.
///
/// HOW IT WORKS
/// Checkpoint inherits from SpawnPoint, so it has an id and can be found with SpawnPoint.Find().
/// When Pip walks into its trigger, it tells GameManager "this is the new respawn point".
/// GameManager stores the id in the save data and writes the save file.
///
/// Setup: a GameObject with a Collider2D set to "Is Trigger" and this script. Its position is
/// where Pip reappears. Pip's pivot is at her feet, so put it ON THE FLOOR, and shape the trigger
/// to stand up from there (e.g. offset y = 1, size 1 x 2). Optional: a SpriteRenderer plus an
/// "on" sprite, so the player can see it light up.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : SpawnPoint
{
    // ------------------------------------------------------------------ Inspector fields

    [Header("Checkpoint")]
    [SerializeField, Tooltip("Save the game to disk when Pip touches this.")]
    private bool saveOnReach = true;

    [SerializeField, Tooltip("Optional: sprite renderer that changes when the checkpoint is reached.")]
    private SpriteRenderer spriteRenderer;

    [SerializeField, Tooltip("Optional: sprite shown once the checkpoint is active.")]
    private Sprite activeSprite;

    // ------------------------------------------------------------------ Private state

    private Sprite _inactiveSprite;

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true; // Pip walks through it, not into it.
        if (spriteRenderer != null)
            _inactiveSprite = spriteRenderer.sprite;
    }

    protected override void OnEnable()
    {
        base.OnEnable(); // Register as a spawn point.
        GameEvents.CheckpointReached += OnAnyCheckpointReached;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GameEvents.CheckpointReached -= OnAnyCheckpointReached;
    }

    private void Start()
    {
        // Show the "on" sprite if the save already points at this checkpoint (e.g. after Continue).
        GameManager gm = GameManager.Instance;
        SetVisualActive(gm != null && gm.Save.spawnPointId == Id);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Same check my teammate uses: is the thing that entered part of Pip?
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Save.spawnPointId == Id)
            return; // Already the active checkpoint: don't save again every time Pip walks past.

        gm.SetRespawnPoint(Id, saveOnReach);
        GameEvents.RaiseCheckpointReached(Id);
    }

    // ------------------------------------------------------------------ Visuals

    // Only the newest checkpoint looks "on"; older ones switch back off.
    private void OnAnyCheckpointReached(string reachedId) => SetVisualActive(reachedId == Id);

    private void SetVisualActive(bool active)
    {
        if (spriteRenderer == null || activeSprite == null)
            return;
        spriteRenderer.sprite = active ? activeSprite : _inactiveSprite;
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(0.5f, 1f, 0f));
    }
}
