using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Star Coins ready to throw: one icon per coin, bright when it's in Pip's hand, dim while it's
/// flying or coming back.
/// Listens to my teammate's PlayerCoinThrower.CoinCountChanged(available, max).
/// While the coin DAMAGE BOOST is on (DamageBoostChanged), the ready coins glow in Boost Color. Hidden if Pip
/// has no PlayerCoinThrower.
///
/// Art: swap the sprite on the "Coin Template" Image (Placeholder_UI_Coin for now).
/// </summary>
public class HudCoins : PlayerHudElement
{
    [SerializeField, Tooltip("A disabled Image used as the pattern for every coin icon.")]
    private Image coinTemplate;
    [SerializeField] private Color readyColor = new Color(1f, 0.8f, 0.3f);
    [SerializeField] private Color usedColor = new Color(0.3f, 0.27f, 0.3f);
    [SerializeField, Tooltip("Ready coins while the damage boost is active.")]
    private Color boostColor = new Color(1f, 0.45f, 0.75f);

    public int ShownMax { get; private set; }
    public int ShownReady { get; private set; }
    public bool ShowingBoost { get; private set; }

    private readonly List<Image> _icons = new List<Image>();
    private PlayerCoinThrower _thrower;

    protected override bool Bind(PlayerController pip)
    {
        _thrower = pip.GetComponent<PlayerCoinThrower>();
        if (_thrower == null)
            return false;

        _thrower.CoinCountChanged += OnCoinCountChanged;
        _thrower.DamageBoostChanged += OnBoostChanged;
        ShowingBoost = _thrower.DamageBoostActive;
        OnCoinCountChanged(_thrower.AvailableCoins, _thrower.MaxCoins);
        return true;
    }

    protected override void Unbind()
    {
        if (_thrower != null)
        {
            _thrower.CoinCountChanged -= OnCoinCountChanged;
            _thrower.DamageBoostChanged -= OnBoostChanged;
        }
        _thrower = null;
    }

    private void OnBoostChanged(bool active)
    {
        ShowingBoost = active;
        OnCoinCountChanged(ShownReady, ShownMax); // Recolour.
    }

    private void OnCoinCountChanged(int available, int max)
    {
        while (_icons.Count < max)
        {
            Image icon = Instantiate(coinTemplate, coinTemplate.transform.parent);
            icon.name = $"Coin{_icons.Count + 1}";
            _icons.Add(icon);
        }

        for (int i = 0; i < _icons.Count; i++)
        {
            _icons[i].gameObject.SetActive(i < max);
            _icons[i].color = i < available ? (ShowingBoost ? boostColor : readyColor) : usedColor;
        }
        ShownMax = max;
        ShownReady = available;
    }
}
