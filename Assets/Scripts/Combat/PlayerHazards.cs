using System;
using UnityEngine;

/// <summary>
/// Spikes and pits. Put it on Pip next to PlayerController and PlayerHealth.
///
/// - Touching anything tagged "Spikes" (solid collider or trigger) or a trigger tagged "Pit"
///   costs 1 star and puts Pip back on the last safe ground she stood on.
/// - "Safe ground" is recorded every physics step while Pip stands on the Ground layer with
///   BOTH edges of her feet supported, so she is never put back hanging over a ledge.
///   (Spikes should be on their own layer, not Ground, so standing on them is never "safe".)
/// - If the hazard takes her last star, she is not moved: OnDeath handles that.
/// </summary>
[RequireComponent(typeof(PlayerController), typeof(PlayerHealth))]
public class PlayerHazards : MonoBehaviour
{
    [Header("Tags")]
    [SerializeField, Tooltip("Tag on spike objects. Must exist in Tags and Layers.")]
    private string spikesTag = "Spikes";
    [SerializeField, Tooltip("Tag on pit triggers. Must exist in Tags and Layers.")]
    private string pitTag = "Pit";

    [Header("Safe Ground")]
    [SerializeField, Min(0f), Tooltip("Half the width checked under Pip's feet (tiles). Both edges must be on ground.")]
    private float safeCheckHalfWidth = 0.35f;
    [SerializeField, Min(0.01f), Tooltip("How far below the feet the safe-ground rays look (tiles).")]
    private float safeCheckDepth = 0.2f;

    [Header("Respawn")]
    [SerializeField, Min(0f), Tooltip("Seconds Pip can't move after being put back on safe ground.")]
    private float respawnLockTime = 0.3f;
    [SerializeField, Min(0f), Tooltip("Seconds after a hazard hit during which other hazard contacts are ignored.")]
    private float hazardCooldown = 0.2f;

    /// <summary>Where Pip will be put back after a hazard.</summary>
    public Vector2 LastSafePosition { get; private set; }

    /// <summary>Fired after a hazard hit and respawn (for a fade, sound, camera snap...).</summary>
    public event Action HazardHit;

    private PlayerController _controller;
    private PlayerHealth _health;
    private float _ignoreHazardsUntil = float.NegativeInfinity;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _health = GetComponent<PlayerHealth>();
        LastSafePosition = transform.position;
    }

    private void FixedUpdate()
    {
        if (_controller.IsGrounded && !_controller.ControlsLocked && IsFullySupported())
            LastSafePosition = transform.position;
    }

    /// <summary>Both edges of the feet have Ground directly below them.</summary>
    private bool IsFullySupported()
    {
        Vector2 feet = transform.position;
        Vector2 up = new Vector2(0f, 0.05f); // Start rays just above the feet so they don't begin inside the floor.
        Vector2 left = feet + new Vector2(-safeCheckHalfWidth, 0f) + up;
        Vector2 right = feet + new Vector2(safeCheckHalfWidth, 0f) + up;
        float distance = safeCheckDepth + up.y;

        return IsSafeHit(Physics2D.Raycast(left, Vector2.down, distance, _controller.GroundLayer))
            && IsSafeHit(Physics2D.Raycast(right, Vector2.down, distance, _controller.GroundLayer));
    }

    /// <summary>The ray hit ground, and that ground isn't marked as able to disappear (trapdoors).</summary>
    private static bool IsSafeHit(RaycastHit2D hit)
    {
        return hit.collider != null && hit.collider.GetComponentInParent<UnsafeGround>() == null;
    }

    // Solid spikes report collisions; trigger spikes and pits report triggers.
    // "Stay" catches the case where Pip is already touching a hazard when the cooldown ends.
    private void OnCollisionEnter2D(Collision2D collision) => CheckHazard(collision.collider);
    private void OnCollisionStay2D(Collision2D collision) => CheckHazard(collision.collider);
    private void OnTriggerEnter2D(Collider2D other) => CheckHazard(other);
    private void OnTriggerStay2D(Collider2D other) => CheckHazard(other);

    private void CheckHazard(Collider2D other)
    {
        if (_health.IsDead || Time.time < _ignoreHazardsUntil)
            return;

        if (other.CompareTag(spikesTag) || other.CompareTag(pitTag))
            HitHazard();
    }

    private void HitHazard()
    {
        _ignoreHazardsUntil = Time.time + hazardCooldown;
        _health.TakeHazardDamage();

        if (_health.IsDead)
            return; // Out of stars: the death/respawn system takes over.

        _controller.Teleport(LastSafePosition, respawnLockTime);
        HazardHit?.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        // Shows the last safe position (green) and the two safe-ground rays.
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)LastSafePosition : transform.position, 0.2f);

        Vector3 feet = transform.position;
        Gizmos.DrawLine(feet + new Vector3(-safeCheckHalfWidth, 0.05f), feet + new Vector3(-safeCheckHalfWidth, -safeCheckDepth));
        Gizmos.DrawLine(feet + new Vector3(safeCheckHalfWidth, 0.05f), feet + new Vector3(safeCheckHalfWidth, -safeCheckDepth));
    }
}
