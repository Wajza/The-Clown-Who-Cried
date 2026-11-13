using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Shared base for level objects that change when Blue Sight turns on or off.
/// It finds every SpriteRenderer, Tilemap and Collider2D on this object and its children, then:
///   - fades their opacity to the value the subclass asks for,
///   - switches colliders on/off (solid or not) straight away.
/// Subclasses (BlueSightOnly, FakeInBlueSight) only say WHAT the object should look like.
/// Works on single sprites and on whole Tilemaps.
/// </summary>
public abstract class BlueSightReactive : MonoBehaviour
{
    [SerializeField, Min(0f), Tooltip("Seconds to fade between looks.")]
    private float fadeTime = 0.2f;

    /// <summary>Opacity multiplier (0..1) the object should have, with Blue Sight on or off.</summary>
    protected abstract float AlphaWhen(bool blueSightOn);

    /// <summary>Whether the object's colliders should be on (solid), with Blue Sight on or off.</summary>
    protected abstract bool SolidWhen(bool blueSightOn);

    private SpriteRenderer[] _sprites;
    private Tilemap[] _tilemaps;
    private Renderer[] _tilemapRenderers;
    private Collider2D[] _colliders;
    private Color[] _spriteColors;   // Original colors, so we only change opacity.
    private Color[] _tilemapColors;

    private float _alpha;
    private float _targetAlpha;

    protected virtual void Awake()
    {
        _sprites = GetComponentsInChildren<SpriteRenderer>(true);
        _tilemaps = GetComponentsInChildren<Tilemap>(true);
        _colliders = GetComponentsInChildren<Collider2D>(true);

        _spriteColors = new Color[_sprites.Length];
        for (int i = 0; i < _sprites.Length; i++)
            _spriteColors[i] = _sprites[i].color;

        _tilemapColors = new Color[_tilemaps.Length];
        _tilemapRenderers = new Renderer[_tilemaps.Length];
        for (int i = 0; i < _tilemaps.Length; i++)
        {
            _tilemapColors[i] = _tilemaps[i].color;
            _tilemapRenderers[i] = _tilemaps[i].GetComponent<TilemapRenderer>();
        }
    }

    protected virtual void OnEnable()
    {
        BlueSight.Changed += OnBlueSightChanged;

        // Snap straight to the correct look (no fade) when the level loads.
        Apply(BlueSight.IsActive);
        _alpha = _targetAlpha;
        SetAlpha(_alpha);
    }

    protected virtual void OnDisable()
    {
        BlueSight.Changed -= OnBlueSightChanged;
    }

    private void OnBlueSightChanged(bool on) => Apply(on);

    private void Apply(bool on)
    {
        _targetAlpha = AlphaWhen(on);

        // Colliders change instantly so gameplay is never "half solid".
        bool solid = SolidWhen(on);
        foreach (Collider2D col in _colliders)
            col.enabled = solid;
    }

    private void Update()
    {
        if (Mathf.Approximately(_alpha, _targetAlpha))
            return;

        float step = fadeTime > 0f ? Time.deltaTime / fadeTime : 1f;
        _alpha = Mathf.MoveTowards(_alpha, _targetAlpha, step);
        SetAlpha(_alpha);
    }

    private void SetAlpha(float alpha)
    {
        bool visible = alpha > 0.001f; // Fully hidden renderers are switched off (no wasted drawing).

        for (int i = 0; i < _sprites.Length; i++)
        {
            Color c = _spriteColors[i];
            c.a *= alpha;
            _sprites[i].color = c;
            _sprites[i].enabled = visible;
        }

        for (int i = 0; i < _tilemaps.Length; i++)
        {
            Color c = _tilemapColors[i];
            c.a *= alpha;
            _tilemaps[i].color = c;
            if (_tilemapRenderers[i] != null)
                _tilemapRenderers[i].enabled = visible;
        }
    }
}
