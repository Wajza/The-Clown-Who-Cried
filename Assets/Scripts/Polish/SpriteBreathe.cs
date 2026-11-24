using UnityEngine;

/// <summary>
/// A gentle "breathing" squash on a character's sprite, so idle poses that only have two drawings
/// still feel alive and smooth between frames. The sprite's pivot should be at the feet (the
/// Demo Polish tool sets that), so the character stretches upward and the feet stay planted.
///
/// Put it on the object that has the SpriteRenderer (NOT on Pip's "Visual", which
/// PlayerSquashStretch already scales). Only touches localScale, multiplying the scale it had.
/// </summary>
public class SpriteBreathe : MonoBehaviour
{
    [SerializeField, Range(0f, 0.08f), Tooltip("How much taller the sprite gets at the top of a breath (0.02 = 2%).")]
    private float amount = 0.02f;
    [SerializeField, Min(0.2f), Tooltip("Seconds for one full breath.")]
    private float period = 2.4f;
    [SerializeField, Tooltip("Fade the breathing out while the character moves (uses the Rigidbody2D above this object).")]
    private bool onlyWhenStill = true;
    [SerializeField, Min(0f), Tooltip("Faster than this (tiles per second) counts as moving.")]
    private float stillSpeed = 0.2f;

    private Vector3 _baseScale;
    private Rigidbody2D _body;
    private float _phase;
    private float _weight = 1f;

    private void Awake()
    {
        _baseScale = transform.localScale;
        _body = GetComponentInParent<Rigidbody2D>();
        _phase = Random.value * Mathf.PI * 2f; // Characters side by side don't breathe in sync.
    }

    private void OnDisable()
    {
        transform.localScale = _baseScale;
    }

    private void LateUpdate()
    {
        float target = 1f;
        if (onlyWhenStill && _body != null && _body.linearVelocity.sqrMagnitude > stillSpeed * stillSpeed)
            target = 0f;
        _weight = Mathf.MoveTowards(_weight, target, Time.deltaTime * 4f);

        float s = Mathf.Sin(Time.time * (Mathf.PI * 2f / period) + _phase) * amount * _weight;
        transform.localScale = new Vector3(_baseScale.x * (1f - s * 0.5f), _baseScale.y * (1f + s), _baseScale.z);
    }
}
