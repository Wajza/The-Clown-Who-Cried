using UnityEngine;

/// <summary>
/// Makes a sprite blink as an attack warning, or flash when hit. Used by the boss, his copies,
/// trapdoors, cannons and bombs, so every warning in the fight looks the same.
///
/// Call Warn(seconds) and wait that long before the attack happens.
/// </summary>
public class SpriteFlasher : MonoBehaviour
{
    [SerializeField, Tooltip("Sprite to tint. Found automatically if empty.")]
    private SpriteRenderer target;
    [SerializeField] private Color warningColor = new Color(1f, 0.9f, 0.3f);
    [SerializeField] private Color hitColor = new Color(1f, 0.45f, 0.45f);
    [SerializeField, Min(0.02f), Tooltip("Seconds between warning blinks.")]
    private float blinkInterval = 0.08f;
    [SerializeField, Tooltip("Optional object (e.g. a '!' sign or a danger zone) shown while warning.")]
    private GameObject warningSign;

    public bool IsWarning => Time.time < _warnUntil;

    private Color _baseColor = Color.white;
    private float _warnUntil = float.NegativeInfinity;
    private float _hitUntil = float.NegativeInfinity;

    private void Awake()
    {
        if (target == null)
            target = GetComponentInChildren<SpriteRenderer>();
        if (target != null)
            _baseColor = target.color;
        if (warningSign != null)
            warningSign.SetActive(false);
    }

    /// <summary>Blink as a warning for 'seconds'.</summary>
    public void Warn(float seconds) => _warnUntil = Time.time + seconds;

    /// <summary>Flash the hit color for 'seconds'.</summary>
    public void Hit(float seconds) => _hitUntil = Time.time + seconds;

    /// <summary>Stop any warning or flash immediately.</summary>
    public void Stop()
    {
        _warnUntil = _hitUntil = float.NegativeInfinity;
        Update();
    }

    private void Update()
    {
        bool warning = IsWarning;
        if (warningSign != null && warningSign.activeSelf != warning)
            warningSign.SetActive(warning);

        if (target == null)
            return;

        if (Time.time < _hitUntil)
            target.color = hitColor;
        else if (warning)
            target.color = Mathf.FloorToInt(Time.time / blinkInterval) % 2 == 0 ? warningColor : _baseColor;
        else
            target.color = _baseColor;
    }
}
