using UnityEngine;

/// <summary>
/// Makes a fake see-through while Blue Sight is on. On Chortle's phase-2 copies: with Blue Sight the
/// copies fade to ghosts and the real Chortle stays solid, so you can tell which one to hit.
/// Without Blue Sight everyone looks the same. The copies can still be hit (only the look changes).
///
/// Runs after everything else each frame, so it also wins over the warning blink colours.
/// Setup: on the copy prefab's root.
/// </summary>
[DefaultExecutionOrder(10000)]
public class FadeInBlueSight : MonoBehaviour
{
    [SerializeField, Range(0f, 1f), Tooltip("Opacity while Blue Sight is on (0 = invisible, 1 = solid).")]
    private float blueSightOpacity = 0.25f;
    [SerializeField, Min(0f), Tooltip("Seconds to fade in or out.")]
    private float fadeTime = 0.2f;

    private SpriteRenderer[] _sprites;
    private float[] _alphas;
    private float _amount = 1f;

    private void Awake()
    {
        _sprites = GetComponentsInChildren<SpriteRenderer>(true);
        _alphas = new float[_sprites.Length];
        for (int i = 0; i < _sprites.Length; i++)
            _alphas[i] = _sprites[i].color.a;
        _amount = BlueSight.IsActive ? blueSightOpacity : 1f;
    }

    private void LateUpdate()
    {
        float target = BlueSight.IsActive ? blueSightOpacity : 1f;
        _amount = fadeTime > 0f ? Mathf.MoveTowards(_amount, target, Time.deltaTime / fadeTime) : target;

        for (int i = 0; i < _sprites.Length; i++)
        {
            if (_sprites[i] == null)
                continue;
            Color c = _sprites[i].color;
            c.a = _alphas[i] * _amount;
            _sprites[i].color = c;
        }
    }
}
