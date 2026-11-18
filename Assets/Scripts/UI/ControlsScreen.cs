using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The Controls screen (Pause > Controls, or Settings > Controls on the title screen).
///
/// The button lists are read from the REAL input assets every time the screen opens, so they
/// always match the game: change a binding in PlayerControls.inputactions and this screen
/// updates by itself. Keyboard + mouse go in one column, gamepad in the other.
///
/// Setup: on the Controls panel inside the MenuCanvas (GameSystemsPrefabBuilder adds it).
/// MenuController shows and hides it; the Back button calls MenuController.Back().
/// </summary>
public class ControlsScreen : MonoBehaviour
{
    [SerializeField, Tooltip("Pip's actions (Assets/Scripts/Player/PlayerControls).")]
    private InputActionAsset playerControls;
    [SerializeField, Tooltip("Menu/dialogue actions (Assets/Settings/SystemControls).")]
    private InputActionAsset systemControls;
    [SerializeField] private TMP_Text listText;
    [SerializeField] private Button backButton;

    /// <summary>Gets keyboard/gamepad focus when the screen opens.</summary>
    public GameObject FirstSelectable => backButton != null ? backButton.gameObject : null;

    /// <summary>The text shown (used by tests).</summary>
    public string Text => listText != null ? listText.text : "";

    // Which actions to show, in order, with a friendly name. (map, action, label)
    private static readonly (string map, string action, string label)[] Rows =
    {
        ("Player", "Move", "Move / aim"),
        ("Player", "Jump", "Jump (hold = higher)"),
        ("Player", "Dash", "Dash"),
        ("Player", "Swing", "Mallet swing"),
        ("Player", "Throw", "Throw Star Coin"),
        ("Player", "BlueSight", "Blue Sight"),
        ("Player", "TearBurst", "Tear Burst (gauge full)"),
        ("Systems", "Interact", "Talk / use"),
        ("Systems", "Journal", "Journal"),
        ("Systems", "Pause", "Pause"),
    };

    private const string Tips =
        "<color=#FFD94D>TIPS</color>\n" +
        "Hold Up + Swing: hit above you.\n" +
        "In the air, hold Down + Swing: pogo bounce.\n" +
        "Hold Up + Throw: throw a coin upward.\n" +
        "The mallet is earned from Old Tumbles (Reflex Check).";

    private void OnEnable() => Refresh();

    /// <summary>Rebuilds the list from the input assets.</summary>
    public void Refresh()
    {
        if (listText == null)
            return;

        var sb = new StringBuilder();
        sb.AppendLine("<color=#FFD94D>ACTION<pos=42%>KEYBOARD / MOUSE<pos=74%>GAMEPAD</color>");
        foreach ((string map, string action, string label) in Rows)
        {
            InputAction a = Find(map, action);
            if (a == null)
                continue;
            sb.AppendLine($"{label}<pos=42%>{Describe(a, gamepad: false)}<pos=74%>{Describe(a, gamepad: true)}");
        }
        sb.AppendLine();
        sb.Append(Tips);
        listText.text = sb.ToString();
    }

    private InputAction Find(string map, string action)
    {
        InputActionAsset asset = map == "Player" ? playerControls : systemControls;
        return asset != null ? asset.FindAction($"{map}/{action}", throwIfNotFound: false) : null;
    }

    /// <summary>All of an action's buttons for one kind of device, e.g. "K / Right click".</summary>
    public static string Describe(InputAction action, bool gamepad)
    {
        var names = new List<string>();
        var bindings = action.bindings;
        for (int i = 0; i < bindings.Count; i++)
        {
            InputBinding binding = bindings[i];
            if (binding.isPartOfComposite)
                continue;

            if (binding.isComposite)
            {
                // e.g. WASD: collect the parts that follow the composite.
                var parts = new List<string>();
                for (int j = i + 1; j < bindings.Count && bindings[j].isPartOfComposite; j++)
                    if (IsGamepad(bindings[j].effectivePath) == gamepad)
                        parts.Add(Name(bindings[j].effectivePath));
                if (parts.Count > 0)
                    names.Add(CompositeName(parts));
                continue;
            }

            if (IsGamepad(binding.effectivePath) == gamepad)
                names.Add(Name(binding.effectivePath));
        }
        return names.Count > 0 ? string.Join(" / ", names) : "-";
    }

    /// <summary>A short, readable name for a binding path, e.g. "&lt;Gamepad&gt;/buttonWest" -> "X".</summary>
    public static string Name(string path)
    {
        switch (path)
        {
            case "<Gamepad>/buttonSouth": return "A";
            case "<Gamepad>/buttonEast": return "B";
            case "<Gamepad>/buttonWest": return "X";
            case "<Gamepad>/buttonNorth": return "Y";
            case "<Gamepad>/leftShoulder": return "LB";
            case "<Gamepad>/rightShoulder": return "RB";
            case "<Gamepad>/leftTrigger": return "LT";
            case "<Gamepad>/rightTrigger": return "RT";
            case "<Gamepad>/leftStick": return "Left stick";
            case "<Gamepad>/start": return "Start";
            case "<Gamepad>/select": return "Select";
            case "<Mouse>/leftButton": return "Left click";
            case "<Mouse>/rightButton": return "Right click";
            case "<Keyboard>/upArrow": return "Up";
            case "<Keyboard>/downArrow": return "Down";
            case "<Keyboard>/leftArrow": return "Left";
            case "<Keyboard>/rightArrow": return "Right";
            case "<Keyboard>/leftShift": return "Shift";
        }
        if (path.StartsWith("<Gamepad>/dpad/"))
            return "D-pad " + path.Substring("<Gamepad>/dpad/".Length);
        if (path.StartsWith("<Gamepad>/leftStick/"))
            return "Stick " + path.Substring("<Gamepad>/leftStick/".Length);
        return InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    private static bool IsGamepad(string path) => path != null && path.StartsWith("<Gamepad>");

    /// <summary>W,A,S,D -> "WASD"; Up,Left,Down,Right -> "Arrows"; anything else joined with "/".</summary>
    private static string CompositeName(List<string> parts)
    {
        if (parts.TrueForAll(p => p.Length == 1))
            return string.Concat(parts);
        if (parts.Contains("Up") && parts.Contains("Down") && parts.Contains("Left") && parts.Contains("Right"))
            return "Arrows";
        return string.Join("/", parts);
    }
}
