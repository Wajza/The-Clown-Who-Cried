using System;
using UnityEngine;

/// <summary>
/// One of Chortle's phase-2 copies. It looks exactly like him (the boss copies his current sprite
/// onto it every frame) but has NO shadow, so Blue Sight reveals which one is real.
///
/// Copies throw pies (the boss tells them when), take no damage, and pop when hit by anything,
/// including a Tear Burst.
///
/// Prefab: Enemy layer, SpriteRenderer, trigger Collider2D, SpriteFlasher, ContactDamage, this.
/// Do NOT give it the "Boss" tag or a shadow.
/// </summary>
[RequireComponent(typeof(SpriteFlasher))]
public class ChortleCopy : MonoBehaviour, IDamageable, IStunnable
{
    [SerializeField, Tooltip("Optional puff effect when the copy pops.")]
    private GameObject popEffectPrefab;

    public SpriteRenderer SpriteRenderer { get; private set; }
    public SpriteFlasher Flasher { get; private set; }
    public bool IsPopped { get; private set; }

    /// <summary>Fired when the copy pops (hit by Pip, or cleared by the boss).</summary>
    public event Action<ChortleCopy> Popped;

    private void Awake()
    {
        SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        Flasher = GetComponent<SpriteFlasher>();
    }

    // Any hit or stun pops a copy instantly.
    public void TakeDamage(DamageInfo info) => Pop();
    public void Stun(float duration) => Pop();

    public void Pop()
    {
        if (IsPopped)
            return;

        IsPopped = true;
        if (popEffectPrefab != null)
            Instantiate(popEffectPrefab, transform.position + Vector3.up, Quaternion.identity);

        Popped?.Invoke(this);
        Destroy(gameObject);
    }
}
