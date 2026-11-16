using TMPro;
using UnityEngine;

/// <summary>
/// The boss bar at the bottom of the screen: name, health bar and phase number.
/// Shown only while BossHealthSource.Active says the fight is on; hidden otherwise.
///
/// It doesn't know about any particular boss. For Chortle, put a ChortleHealthSource in the arena.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [SerializeField, Tooltip("What to show/hide.")]
    private GameObject content;
    [SerializeField] private HudBar bar;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text phaseText;

    // For tests.
    public bool Visible => content.activeSelf;
    public float ShownFill => bar.Fill;
    public string ShownName => nameText.text;
    public string ShownPhase => phaseText.text;

    private BossHealthSource _source;

    private void OnEnable()
    {
        BossHealthSource.ActiveChanged += Relink;
        Relink();
    }

    private void OnDisable()
    {
        BossHealthSource.ActiveChanged -= Relink;
        if (_source != null)
            _source.Changed -= Refresh;
        _source = null;
    }

    private void Relink()
    {
        if (_source != null)
            _source.Changed -= Refresh;

        _source = BossHealthSource.Active;
        if (_source != null)
            _source.Changed += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        bool show = _source != null && _source.IsFighting;
        content.SetActive(show);
        if (!show)
            return;

        nameText.text = _source.BossName;
        bar.SetFill(_source.MaxHealth > 0f ? _source.Health / _source.MaxHealth : 0f);
        phaseText.text = _source.Phase > 0 ? $"Phase {_source.Phase}" : "";
    }
}
