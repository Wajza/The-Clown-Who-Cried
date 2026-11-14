using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The buttons MY systems use (talking, dialogue, menus, journal). They live in their
/// own asset, Assets/Settings/SystemControls.inputactions, separate from my teammate's
/// PlayerControls, so locking Pip's movement never switches these off.
///
/// Keys (change them by double-clicking the asset):
///   Interact    F            / gamepad East (B / Circle)   - E is taken by Tear Burst
///   Advance     Space, Enter, F, left click / gamepad South or East
///   ChoiceUp    W, Up arrow  / d-pad up, stick up
///   ChoiceDown  S, Down arrow/ d-pad down, stick down
///   Pause       Escape       / gamepad Start                 (open/close the pause menu)
///   Journal     Tab          / gamepad Select (View)         (open/close the Journal)
///   Back        Escape, Backspace / gamepad East            (in menus: go back one screen)
///   PageLeft    A, Left arrow / LB, d-pad left              (Journal: previous page)
///   PageRight   D, Right arrow/ RB, d-pad right             (Journal: next page)
///
/// Menu buttons (Resume, sliders...) are moved between and pressed by Unity's EventSystem, which
/// has its own default keys: arrows/WASD/d-pad/stick to move, Enter/Space/gamepad South to press.
///
/// Other scripts read them like this:
///   if (SystemInput.WasPressed(SystemInput.Interact)) { ... }
///
/// Setup: on the GameSystems prefab, with the SystemControls asset assigned.
/// </summary>
public class SystemInput : MonoBehaviour
{
    [SerializeField, Tooltip("Assets/Settings/SystemControls.inputactions. Needs a 'Systems' map.")]
    private InputActionAsset controls;

    // ------------------------------------------------------------------ Public API

    public static InputAction Interact { get; private set; }
    public static InputAction Advance { get; private set; }
    public static InputAction ChoiceUp { get; private set; }
    public static InputAction ChoiceDown { get; private set; }
    public static InputAction Pause { get; private set; }
    public static InputAction Journal { get; private set; }
    public static InputAction Back { get; private set; }
    public static InputAction PageLeft { get; private set; }
    public static InputAction PageRight { get; private set; }

    /// <summary>True only on the frame the button went down. Safe to call with a null action.</summary>
    public static bool WasPressed(InputAction action) => action != null && action.WasPressedThisFrame();

    // ------------------------------------------------------------------ Unity lifecycle

    private InputActionMap _map;

    private void OnEnable()
    {
        if (controls == null)
        {
            Debug.LogError("SystemInput: assign the SystemControls Input Actions asset.", this);
            return;
        }

        _map = controls.FindActionMap("Systems", throwIfNotFound: true);
        Interact = _map.FindAction("Interact", true);
        Advance = _map.FindAction("Advance", true);
        ChoiceUp = _map.FindAction("ChoiceUp", true);
        ChoiceDown = _map.FindAction("ChoiceDown", true);
        Pause = _map.FindAction("Pause", true);
        Journal = _map.FindAction("Journal", true);
        Back = _map.FindAction("Back", true);
        PageLeft = _map.FindAction("PageLeft", true);
        PageRight = _map.FindAction("PageRight", true);
        _map.Enable();
    }

    private void OnDisable()
    {
        _map?.Disable();
        Interact = Advance = ChoiceUp = ChoiceDown = null;
        Pause = Journal = Back = PageLeft = PageRight = null;
    }
}
