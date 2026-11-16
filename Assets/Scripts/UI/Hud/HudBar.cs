using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A simple horizontal bar (an Image set to "Filled") used by the Tear Gauge, Blue Sight meter
/// and boss health bar. Keeps the HUD scripts short: they just call SetFill(0..1).
///
/// Art: give the Fill Image any sprite (Placeholder_White for now); keep Image Type = Filled,
/// Fill Method = Horizontal.
/// </summary>
public class HudBar : MonoBehaviour
{
    [SerializeField] private Image fill;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField, Tooltip("Colour used when Highlight is on (e.g. gauge full, Blue Sight active).")]
    private Color highlightColor = Color.yellow;

    public float Fill => fill.fillAmount;

    public void SetFill(float amount01) => fill.fillAmount = Mathf.Clamp01(amount01);

    public void SetHighlight(bool on) => fill.color = on ? highlightColor : normalColor;

    /// <summary>Used by the prefab builder to give each bar its colours.</summary>
    public void SetColors(Color normal, Color highlight)
    {
        normalColor = normal;
        highlightColor = highlight;
        fill.color = normal;
    }
}
