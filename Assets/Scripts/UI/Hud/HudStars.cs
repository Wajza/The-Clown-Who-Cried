using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pip's Smile Stars (health) in the top-left corner: one icon per max star, filled or empty,
/// plus a shield icon while she has a shield.
///
/// Listens to my teammate's PlayerHealth: StarsChanged(current, max) and ShieldChanged(bool).
///
/// Art: the icons use the "Star Template" Image's sprite (Placeholder_UI_Star for now). Swap the
/// sprite on the template (or replace the PNG) and every star changes. Full/empty are tints.
/// </summary>
public class HudStars : PlayerHudElement
{
    [SerializeField, Tooltip("A disabled Image used as the pattern for every star icon.")]
    private Image starTemplate;
    [SerializeField] private Color fullColor = new Color(1f, 0.85f, 0.25f);
    [SerializeField] private Color emptyColor = new Color(0.25f, 0.22f, 0.3f);
    [SerializeField, Tooltip("Shown while Pip has a shield.")]
    private GameObject shieldIcon;

    /// <summary>For tests: how many icons are shown, and how many of those are full.</summary>
    public int ShownMax { get; private set; }
    public int ShownFull { get; private set; }
    public bool ShieldShown => shieldIcon != null && shieldIcon.activeSelf;

    private readonly List<Image> _icons = new List<Image>();
    private PlayerHealth _health;

    protected override bool Bind(PlayerController pip)
    {
        _health = pip.GetComponent<PlayerHealth>();
        if (_health == null)
            return false;

        _health.StarsChanged += OnStarsChanged;
        _health.ShieldChanged += OnShieldChanged;
        OnStarsChanged(_health.CurrentStars, _health.MaxStars);
        OnShieldChanged(_health.HasShield);
        return true;
    }

    protected override void Unbind()
    {
        if (_health != null)
        {
            _health.StarsChanged -= OnStarsChanged;
            _health.ShieldChanged -= OnShieldChanged;
        }
        _health = null;
    }

    private void OnStarsChanged(int current, int max)
    {
        // Make enough icons, copying the template.
        while (_icons.Count < max)
        {
            Image icon = Instantiate(starTemplate, starTemplate.transform.parent);
            icon.name = $"Star{_icons.Count + 1}";
            _icons.Add(icon);
        }

        for (int i = 0; i < _icons.Count; i++)
        {
            _icons[i].gameObject.SetActive(i < max);
            _icons[i].color = i < current ? fullColor : emptyColor;
        }

        ShownMax = max;
        ShownFull = Mathf.Clamp(current, 0, max);
        if (shieldIcon != null)
            shieldIcon.transform.SetAsLastSibling(); // Keep the shield after the stars.
    }

    private void OnShieldChanged(bool hasShield)
    {
        if (shieldIcon != null)
            shieldIcon.SetActive(hasShield);
    }
}
