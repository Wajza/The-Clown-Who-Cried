using UnityEngine;

/// <summary>
/// The Blue Sight meter (how much Blue Sight is left; bright while it's switched on).
/// Listens to my teammate's PlayerBlueSight: MeterChanged(charge 0..1) and ActiveChanged(bool).
///
/// Hidden when Pip has no PlayerBlueSight, or while it's disabled (before W2 gives Blue Sight;
/// see PlayerAbilities in Part 5).
/// </summary>
public class HudBlueSight : PlayerHudElement
{
    [SerializeField] private HudBar bar;

    public float ShownFill => bar.Fill;

    private PlayerBlueSight _blueSight;

    protected override bool Bind(PlayerController pip)
    {
        _blueSight = pip.GetComponent<PlayerBlueSight>();
        if (_blueSight == null)
            return false;

        _blueSight.MeterChanged += OnMeterChanged;
        _blueSight.ActiveChanged += OnActiveChanged;
        OnMeterChanged(_blueSight.Charge);
        OnActiveChanged(_blueSight.IsActive);
        return _blueSight.enabled;
    }

    protected override void Unbind()
    {
        if (_blueSight != null)
        {
            _blueSight.MeterChanged -= OnMeterChanged;
            _blueSight.ActiveChanged -= OnActiveChanged;
        }
        _blueSight = null;
    }

    private void OnMeterChanged(float charge) => bar.SetFill(charge);
    private void OnActiveChanged(bool active) => bar.SetHighlight(active);
}
