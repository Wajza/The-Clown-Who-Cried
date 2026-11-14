using UnityEngine;

/// <summary>
/// The Blue Sight meter (how much Blue Sight is left; bright while it's switched on).
/// Listens to my teammate's PlayerBlueSight: MeterChanged(charge 0..1) and ActiveChanged(bool).
///
/// Hidden when Pip has no PlayerBlueSight, or until she has earned Blue Sight (W2; see
/// PlayerAbilities). It appears by itself the moment the ability is unlocked.
/// </summary>
public class HudBlueSight : PlayerHudElement
{
    [SerializeField] private HudBar bar;

    public float ShownFill => bar.Fill;

    private PlayerBlueSight _blueSight;

    protected override void OnEnable()
    {
        base.OnEnable();
        PlayerAbilities.Changed += OnAbilitiesChanged;
    }

    protected override void OnDisable()
    {
        PlayerAbilities.Changed -= OnAbilitiesChanged;
        base.OnDisable();
    }

    protected override bool Bind(PlayerController pip)
    {
        _blueSight = pip.GetComponent<PlayerBlueSight>();
        if (_blueSight == null)
            return false;

        _blueSight.MeterChanged += OnMeterChanged;
        _blueSight.ActiveChanged += OnActiveChanged;
        OnMeterChanged(_blueSight.Charge);
        OnActiveChanged(_blueSight.IsActive);
        return PlayerAbilities.IsUnlocked(Ability.BlueSight);
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

    // Unlocked (or a new level that locks/unlocks abilities): link again to show/hide.
    private void OnAbilitiesChanged() => Relink();

    private void OnMeterChanged(float charge) => bar.SetFill(charge);
    private void OnActiveChanged(bool active) => bar.SetHighlight(active);
}
