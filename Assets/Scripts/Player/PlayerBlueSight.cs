using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Pip's Blue Sight ability and its meter. Put it on Pip next to PlayerController.
///
/// Meter: Charge goes from 1 (full) to 0 (empty).
///   - While Blue Sight is on it drains: a full meter lasts maxDuration seconds (5).
///   - While it's off it refills: empty to full takes refillTime seconds (4).
///   - When it runs empty, Blue Sight switches off. In hold mode the button must be released
///     and pressed again (so holding it doesn't flicker on and off as it refills).
///
/// Controls: hold mode (default) = on while held. Toggle mode = press to turn on, press again to
/// turn off. ToggleMode is public so an options menu can change it.
///
/// This script only manages the meter and input. It flips the shared BlueSight switch,
/// and the world and screen effects react to that.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerBlueSight : MonoBehaviour
{
    [Header("Meter")]
    [SerializeField, Min(0.1f), Tooltip("Seconds Blue Sight lasts from a full meter.")]
    private float maxDuration = 5f;
    [SerializeField, Min(0.1f), Tooltip("Seconds to refill from empty to full.")]
    private float refillTime = 4f;
    [SerializeField, Range(0f, 1f), Tooltip("Meter fraction needed to turn Blue Sight on (stops tiny flickers on an almost-empty meter).")]
    private float minChargeToActivate = 0.05f;

    [Header("Controls")]
    [SerializeField, Tooltip("Off = hold the button. On = press to toggle.")]
    private bool toggleMode;

    // ------------------------------------------------------------------ Public API

    /// <summary>Meter fill from 0 (empty) to 1 (full). For the UI bar.</summary>
    public float Charge { get; private set; } = 1f;

    /// <summary>Seconds of Blue Sight left in the meter.</summary>
    public float SecondsLeft => Charge * maxDuration;

    public bool IsActive { get; private set; }

    public bool ToggleMode
    {
        get => toggleMode;
        set => toggleMode = value;
    }

    /// <summary>Fired with the meter fill (0..1) whenever it changes. For the UI.</summary>
    public event Action<float> MeterChanged;

    /// <summary>Fired with true/false when Blue Sight turns on/off. For sound and Pip's eye animation.</summary>
    public event Action<bool> ActiveChanged;

    // ------------------------------------------------------------------ Private state

    private PlayerController _controller;
    private InputAction _action;
    private bool _toggledOn;      // Toggle mode: the player's on/off choice.
    private bool _needsRelease;   // Hold mode: meter ran out while held; wait for release.

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        _action = _controller.InputActions.FindActionMap("Player", true).FindAction("BlueSight", true);
    }

    private void Start()
    {
        MeterChanged?.Invoke(Charge); // Give the UI its starting value.
    }

    private void OnDisable()
    {
        SetActive(false);
    }

    private void Update()
    {
        SetActive(WantsBlueSight());
        UpdateMeter();
    }

    // ------------------------------------------------------------------ Input

    /// <summary>Works out whether Blue Sight should be on this frame, from the input and the meter.</summary>
    private bool WantsBlueSight()
    {
        // Action disabled (e.g. Pip died): always off.
        if (!_action.enabled)
        {
            _toggledOn = false;
            return false;
        }

        bool wants;
        if (toggleMode)
        {
            if (_action.WasPressedThisFrame())
                _toggledOn = !IsActive;
            wants = _toggledOn;
        }
        else
        {
            bool held = _action.IsPressed();
            if (_needsRelease && !held)
                _needsRelease = false;
            wants = held && !_needsRelease;
        }

        // Turning on needs a little charge; staying on just needs some left.
        if (wants && !IsActive && Charge < minChargeToActivate)
        {
            _toggledOn = false;
            return false;
        }

        return wants;
    }

    // ------------------------------------------------------------------ Meter

    private void UpdateMeter()
    {
        float before = Charge;

        if (IsActive)
        {
            // Drain: a full meter (1) lasts maxDuration seconds.
            Charge -= Time.deltaTime / maxDuration;
            if (Charge <= 0f)
            {
                Charge = 0f;
                _toggledOn = false;
                _needsRelease = true;
                SetActive(false);
            }
        }
        else
        {
            // Refill: empty to full takes refillTime seconds.
            Charge = Mathf.Min(1f, Charge + Time.deltaTime / refillTime);
        }

        if (!Mathf.Approximately(before, Charge))
            MeterChanged?.Invoke(Charge);
    }

    private void SetActive(bool active)
    {
        if (IsActive == active)
            return;

        IsActive = active;
        BlueSight.SetActive(active); // Tell the world and the screen effect.
        ActiveChanged?.Invoke(active);
    }
}
