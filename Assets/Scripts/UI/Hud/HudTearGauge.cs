using UnityEngine;

/// <summary>
/// The Tear Gauge bar (fills up until Tear Burst is ready; glows when full).
/// Listens to my teammate's TearGauge.GaugeChanged(value, max). Hidden if Pip has no TearGauge.
/// </summary>
public class HudTearGauge : PlayerHudElement
{
    [SerializeField] private HudBar bar;

    public float ShownFill => bar.Fill;

    private TearGauge _gauge;

    protected override bool Bind(PlayerController pip)
    {
        _gauge = pip.GetComponent<TearGauge>();
        if (_gauge == null)
            return false;

        _gauge.GaugeChanged += OnGaugeChanged;
        OnGaugeChanged(_gauge.Value, _gauge.MaxValue);
        return true;
    }

    protected override void Unbind()
    {
        if (_gauge != null)
            _gauge.GaugeChanged -= OnGaugeChanged;
        _gauge = null;
    }

    private void OnGaugeChanged(int value, int max)
    {
        bar.SetFill(max > 0 ? (float)value / max : 0f);
        bar.SetHighlight(max > 0 && value >= max);
    }
}
