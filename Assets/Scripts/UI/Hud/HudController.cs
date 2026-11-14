using UnityEngine;

/// <summary>
/// Shows or hides the whole HUD. The HUD is visible when Pip exists and the game is in normal
/// play, paused, in dialogue or loading. It's hidden on the title screen (no Pip) and during
/// cutscenes, so cutscenes look clean.
///
/// The pieces (HudStars, HudTearGauge...) look after their own contents.
///
/// Setup: on the HudCanvas in the GameSystems prefab, with the "Root" child assigned.
/// </summary>
public class HudController : MonoBehaviour
{
    [SerializeField, Tooltip("Parent of every HUD piece.")]
    private GameObject root;

    public bool Visible => root.activeSelf;

    private void LateUpdate()
    {
        GameManager gm = GameManager.Instance;
        bool show = PlayerController.Current != null && gm != null &&
                    gm.State != GameState.Title && gm.State != GameState.Cutscene;
        if (root.activeSelf != show)
            root.SetActive(show);
    }
}
