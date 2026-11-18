using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Prints what Pip is doing to the Console, to make testing and debugging easy:
///   [Pip input]  every button pressed (and on which device), and presses that are IGNORED, with
///                the reason (e.g. Swing pressed before the mallet is earned).
///   [Pip]        what she does: jump, dash, mallet swing, coin throw, Blue Sight, Tear Burst...
///   [Pip hit]    what got hit: mallet hits, coin hits, and Pip losing stars.
///
/// Turn it off: untick the component, untick a category, or set PlayerDebugLog.Enabled = false.
/// It only runs in the Editor and development builds, never in a release build.
///
/// Setup: on Pip (the Combat setup tool adds it to the Pip prefab).
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerDebugLog : MonoBehaviour
{
    /// <summary>Global switch for all Pip logging.</summary>
    public static bool Enabled = true;

    [SerializeField, Tooltip("Buttons pressed, and pressed-but-ignored buttons with the reason.")]
    private bool logButtons = true;
    [SerializeField, Tooltip("Jump, dash, swing, throw, Blue Sight, Tear Burst, pickups...")]
    private bool logActions = true;
    [SerializeField, Tooltip("What the mallet and coins hit, and Pip getting hurt.")]
    private bool logHits = true;

    private PlayerController _controller;
    private PlayerMallet _mallet;
    private PlayerCoinThrower _coins;
    private PlayerHealth _health;
    private PlayerHazards _hazards;
    private TearGauge _tearGauge;
    private PlayerBlueSight _blueSight;
    private InputActionMap _map;
    private Vector2Int _lastMove;

    // Actions that can be switched off on their own (PlayerAbilities), and so can be "ignored".
    private static readonly string[] AbilityActions = { "Swing", "Throw", "Dash", "BlueSight" };

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        if (!Debug.isDebugBuild) // True in the Editor and development builds only.
        {
            enabled = false;
            return;
        }

        _controller = GetComponent<PlayerController>();
        _mallet = GetComponent<PlayerMallet>();
        _coins = GetComponent<PlayerCoinThrower>();
        _health = GetComponent<PlayerHealth>();
        _hazards = GetComponent<PlayerHazards>();
        _tearGauge = GetComponent<TearGauge>();
        _blueSight = GetComponent<PlayerBlueSight>();
    }

    private void OnEnable()
    {
        if (_controller == null || _controller.InputActions == null)
            return;

        _map = _controller.InputActions.FindActionMap("Player", throwIfNotFound: false);
        if (_map != null)
            _map.actionTriggered += OnActionTriggered;

        _controller.Jumped += OnJumped;
        _controller.DashingChanged += OnDashingChanged;
        if (_mallet != null)
        {
            _mallet.SwingStarted += OnSwingStarted;
            _mallet.HitLanded += OnMalletHit;
            _mallet.Pogoed += OnPogoed;
        }
        if (_coins != null) _coins.CoinsThrown += OnCoinsThrown;
        if (_health != null)
        {
            _health.StarLost += OnStarLost;
            _health.ShieldChanged += OnShieldChanged;
            _health.OnDeath += OnDeath;
        }
        if (_hazards != null) _hazards.HazardHit += OnHazardHit;
        if (_tearGauge != null) _tearGauge.TearBurstReleased += OnTearBurst;
        if (_blueSight != null) _blueSight.ActiveChanged += OnBlueSight;
        StarCoin.AnyHit += OnCoinHit;
        Pickup.AnyCollected += OnPickup;
    }

    private void OnDisable()
    {
        if (_map != null)
            _map.actionTriggered -= OnActionTriggered;
        if (_controller == null)
            return;

        _controller.Jumped -= OnJumped;
        _controller.DashingChanged -= OnDashingChanged;
        if (_mallet != null)
        {
            _mallet.SwingStarted -= OnSwingStarted;
            _mallet.HitLanded -= OnMalletHit;
            _mallet.Pogoed -= OnPogoed;
        }
        if (_coins != null) _coins.CoinsThrown -= OnCoinsThrown;
        if (_health != null)
        {
            _health.StarLost -= OnStarLost;
            _health.ShieldChanged -= OnShieldChanged;
            _health.OnDeath -= OnDeath;
        }
        if (_hazards != null) _hazards.HazardHit -= OnHazardHit;
        if (_tearGauge != null) _tearGauge.TearBurstReleased -= OnTearBurst;
        if (_blueSight != null) _blueSight.ActiveChanged -= OnBlueSight;
        StarCoin.AnyHit -= OnCoinHit;
        Pickup.AnyCollected -= OnPickup;
    }

    private void Update()
    {
        if (logButtons)
            LogIgnoredPresses();
    }

    // ------------------------------------------------------------------ Buttons

    private void OnActionTriggered(InputAction.CallbackContext ctx)
    {
        if (!logButtons)
            return;

        string action = ctx.action.name;
        if (action == "Move")
        {
            // Only when the direction changes, not every frame the stick moves.
            Vector2 v = ctx.phase == InputActionPhase.Canceled ? Vector2.zero : ctx.ReadValue<Vector2>();
            var dir = new Vector2Int(Mathf.Abs(v.x) > 0.5f ? (int)Mathf.Sign(v.x) : 0, Mathf.Abs(v.y) > 0.5f ? (int)Mathf.Sign(v.y) : 0);
            if (dir != _lastMove)
            {
                _lastMove = dir;
                Log("Pip input", dir == Vector2Int.zero ? "Move: released" : $"Move: {DirectionName(dir)} ({ControlName(ctx.control)})");
            }
            return;
        }

        if (ctx.phase == InputActionPhase.Performed)
        {
            string note = action == "TearBurst" && _tearGauge != null && !_tearGauge.IsFull
                ? $"  (nothing happens: Tear Gauge is {_tearGauge.Value}/{_tearGauge.MaxValue})"
                : "";
            Log("Pip input", $"Pressed {action}: {ControlName(ctx.control)}{note}");
        }
        else if (ctx.phase == InputActionPhase.Canceled && (action == "Jump" || action == "BlueSight"))
        {
            Log("Pip input", $"Released {action}");
        }
    }

    /// <summary>A disabled action never reports presses, so look at its buttons directly and say why.</summary>
    private void LogIgnoredPresses()
    {
        if (_map == null || !_map.enabled)
            return; // Whole map off = dialogue, cutscene, menu or death: not interesting here.

        foreach (string name in AbilityActions)
        {
            InputAction action = _map.FindAction(name);
            if (action == null || action.enabled)
                continue;

            foreach (InputControl control in action.controls)
            {
                if (control is ButtonControl button && button.wasPressedThisFrame)
                {
                    Log("Pip input", $"Pressed {name} ({ControlName(control)}) but it's IGNORED: {WhyOff(name)}");
                    break;
                }
            }
        }
    }

    private static string WhyOff(string action)
    {
        Ability ability = action switch
        {
            "Swing" => Ability.Mallet,
            "Throw" => Ability.Coins,
            "Dash" => Ability.Dash,
            _ => Ability.BlueSight,
        };

        if (!PlayerAbilities.IsUnlocked(ability))
            return ability switch
            {
                Ability.Mallet => "Pip doesn't have the mallet yet. Old Tumbles gives it in the Reflex Check (P4).",
                Ability.BlueSight => "Blue Sight isn't earned yet (W2 Crooked Mirror Maze).",
                _ => $"{ability} isn't earned yet.",
            };
        if (PlayerAbilities.IsBlocked(ability))
            return $"{ability} is blocked right now (e.g. while carrying a box).";
        return "the action is switched off.";
    }

    // ------------------------------------------------------------------ What Pip does

    private void OnJumped() { if (logActions) Log("Pip", "Jump"); }

    private void OnDashingChanged(bool dashing) { if (logActions && dashing) Log("Pip", "Dash"); }

    private void OnSwingStarted(SwingDirection direction) { if (logActions) Log("Pip", $"Mallet swing ({direction})"); }

    private void OnPogoed() { if (logActions) Log("Pip", "Pogo bounce!"); }

    private void OnCoinsThrown(int count)
    {
        if (logActions)
            Log("Pip", $"Threw {count} Star Coin{(count > 1 ? "s" : "")} ({_coins.AvailableCoins}/{_coins.MaxCoins} left)");
    }

    private void OnShieldChanged(bool on) { if (logActions) Log("Pip", on ? "Got a Balloon Shield" : "Balloon Shield popped"); }

    private void OnTearBurst(Vector2 center, float radius) { if (logActions) Log("Pip", "TEAR BURST!"); }

    private void OnBlueSight(bool on) { if (logActions) Log("Pip", on ? "Blue Sight ON" : "Blue Sight off"); }

    private void OnPickup(Pickup pickup) { if (logActions) Log("Pip", $"Picked up {pickup.name.Replace("(Clone)", "")}"); }

    // ------------------------------------------------------------------ Hits

    private void OnMalletHit(IDamageable target, DamageInfo info)
    {
        if (logHits)
            Log("Pip hit", $"Mallet hit {TargetName(target)} ({info.Amount} damage)");
    }

    private void OnCoinHit(StarCoin coin, Collider2D what)
    {
        if (!logHits)
            return;
        bool damageable = what.GetComponentInParent<IDamageable>() != null;
        Log("Pip hit", $"Star Coin hit {what.name}{(damageable ? "" : " (wall/ground, no damage)")}");
    }

    private void OnStarLost()
    {
        if (logHits)
            Log("Pip hit", $"Pip lost a Smile Star ({_health.CurrentStars}/{_health.MaxStars} left)");
    }

    private void OnHazardHit() { if (logHits) Log("Pip hit", "Hazard! Pip is put back on safe ground"); }

    private void OnDeath() { if (logHits) Log("Pip hit", "Pip ran out of Smile Stars: respawning at the last checkpoint"); }

    // ------------------------------------------------------------------ Helpers

    private void Log(string category, string message)
    {
        if (Enabled && enabled)
            Debug.Log($"<color=#FF9EC4>[{category}]</color> {message}", this);
    }

    private static string ControlName(InputControl control) =>
        control == null ? "?" : $"{control.displayName} on {control.device.displayName}";

    private static string TargetName(IDamageable target) =>
        target is Component c && c != null ? c.name.Replace("(Clone)", "") : target?.ToString() ?? "?";

    private static string DirectionName(Vector2Int d)
    {
        string v = d.y > 0 ? "up" : d.y < 0 ? "down" : "";
        string h = d.x > 0 ? "right" : d.x < 0 ? "left" : "";
        return v.Length > 0 && h.Length > 0 ? $"{v}-{h}" : v + h;
    }
}
