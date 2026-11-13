using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

/// <summary>
/// Pip's Star Coin throw. Put it on Pip next to PlayerController.
///
/// Coin count: Pip carries maxCoins coins. Each throw takes one, and that coin comes back
/// returnTime seconds after it was thrown (whether it hit something or not). The count is
/// "max minus coins still waiting to return", so it can never get out of sync.
///
/// Juggle Shot (upgrade): when unlocked, one Throw sends every available coin (up to 3) in a fan.
///
/// Pooling: coins are created once (in Awake) and reused. A thrown coin is taken from the pool
/// and given back when it finishes, so throwing never calls Instantiate or Destroy.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerCoinThrower : MonoBehaviour
{
    // ------------------------------------------------------------------ Inspector fields

    [Header("Coins")]
    [SerializeField, Tooltip("Star Coin prefab (must have the StarCoin component).")]
    private StarCoin coinPrefab;
    [SerializeField, Min(1), Tooltip("How many coins Pip carries.")]
    private int maxCoins = 3;
    [SerializeField, Min(0f), Tooltip("Seconds after a throw until that coin is back in Pip's count.")]
    private float returnTime = 1.5f;
    [SerializeField, Min(0f), Tooltip("Minimum seconds between throws.")]
    private float throwCooldown = 0.12f;

    [Header("Flight")]
    [SerializeField, Min(0.1f), Tooltip("Coin speed (tiles per second).")]
    private float coinSpeed = 20f;
    [SerializeField, Min(0.1f), Tooltip("How far a coin flies before vanishing (tiles).")]
    private float coinRange = 8f;
    [SerializeField, Tooltip("Spawn point for a forward throw, relative to Pip's feet (facing right).")]
    private Vector2 forwardThrowOffset = new Vector2(0.5f, 1f);
    [SerializeField, Tooltip("Spawn point for an upward throw, relative to Pip's feet.")]
    private Vector2 upThrowOffset = new Vector2(0f, 1.6f);
    [SerializeField, Range(0.1f, 0.9f), Tooltip("How far up the stick must be held to throw upward.")]
    private float aimThreshold = 0.5f;

    [Header("Damage")]
    [SerializeField, Min(0), Tooltip("Damage per coin.")]
    private int damage = 1;
    [SerializeField, Min(0f), Tooltip("Knockback speed given to targets (tiles per second).")]
    private float knockbackForce = 2f;
    [SerializeField, Tooltip("Layers that stop a coin: Ground plus everything it can hit (Enemy, Breakable). Not Pip's layer.")]
    private LayerMask hitLayers;

    [Header("Juggle Shot (upgrade)")]
    [SerializeField, Tooltip("When on, Throw sends all available coins in a fan.")]
    private bool juggleShotUnlocked;
    [SerializeField, Range(0f, 45f), Tooltip("Angle between neighbouring coins in the fan (degrees).")]
    private float fanAngle = 15f;

    // ------------------------------------------------------------------ Public API

    /// <summary>Coins Pip can throw right now.</summary>
    public int AvailableCoins => maxCoins - _returnTimes.Count;

    /// <summary>Maximum coins Pip carries.</summary>
    public int MaxCoins => maxCoins;

    /// <summary>The Juggle Shot upgrade. Set to true from a save file, pickup or shop.</summary>
    public bool JuggleShotUnlocked
    {
        get => juggleShotUnlocked;
        set => juggleShotUnlocked = value;
    }

    /// <summary>Fired with (available, max) whenever the coin count changes. For the UI.</summary>
    public event Action<int, int> CoinCountChanged;

    /// <summary>Fired with the number of coins thrown (1 normally, up to 3 with Juggle Shot). For animation/sound.</summary>
    public event Action<int> CoinsThrown;

    /// <summary>True while a damage boost (Confetti Popper) is active.</summary>
    public bool DamageBoostActive => Time.time < _boostEndsAt;

    /// <summary>Seconds left on the damage boost (0 when none). For a UI timer.</summary>
    public float DamageBoostTimeLeft => Mathf.Max(0f, _boostEndsAt - Time.time);

    /// <summary>Fired with true when a damage boost starts (or is refreshed) and false when it ends.</summary>
    public event Action<bool> DamageBoostChanged;

    // ------------------------------------------------------------------ Private state

    private PlayerController _controller;
    private InputAction _moveAction;
    private InputAction _throwAction;
    private ObjectPool<StarCoin> _pool;
    private float _nextThrowTime;

    // Damage boost (Confetti Popper).
    private float _boostMultiplier = 1f;
    private float _boostEndsAt = float.NegativeInfinity;
    private bool _boostWasActive;
    private float _damageCarry; // Leftover fraction of boosted damage, added to the next coin.

    // When each thrown coin comes back (Time.time values). Its length = coins currently out.
    private readonly List<float> _returnTimes = new List<float>();

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();

        if (coinPrefab == null)
        {
            Debug.LogError("PlayerCoinThrower: assign the Star Coin prefab.", this);
            enabled = false;
            return;
        }

        _pool = new ObjectPool<StarCoin>(
            createFunc: () => Instantiate(coinPrefab),                    // Called only when the pool is empty.
            actionOnGet: coin => coin.gameObject.SetActive(true),
            actionOnRelease: coin => coin.gameObject.SetActive(false),
            actionOnDestroy: coin => { if (coin != null) Destroy(coin.gameObject); },
            collectionCheck: true,                                         // Catches double-release bugs in the Editor.
            defaultCapacity: maxCoins,
            maxSize: 16);

        // Pre-warm: create all coins now so the first throw doesn't cause a hitch.
        var warm = new List<StarCoin>();
        for (int i = 0; i < maxCoins; i++)
            warm.Add(_pool.Get());
        foreach (StarCoin coin in warm)
            _pool.Release(coin);
    }

    private void Start()
    {
        CoinCountChanged?.Invoke(AvailableCoins, maxCoins); // Give the UI its starting value.
    }

    private void OnEnable()
    {
        // Same Input Actions asset as PlayerController (which enables the map).
        InputActionMap map = _controller.InputActions.FindActionMap("Player", true);
        _moveAction = map.FindAction("Move", true);
        _throwAction = map.FindAction("Throw", true);
        _throwAction.performed += OnThrowPressed;
    }

    private void OnDisable()
    {
        if (_throwAction != null)
            _throwAction.performed -= OnThrowPressed;
    }

    private void OnDestroy()
    {
        _pool?.Clear(); // Destroys the pooled (inactive) coins.
    }

    private void Update()
    {
        // Give back every coin whose return time has passed.
        bool changed = false;
        for (int i = _returnTimes.Count - 1; i >= 0; i--)
        {
            if (Time.time >= _returnTimes[i])
            {
                _returnTimes.RemoveAt(i);
                changed = true;
            }
        }

        if (changed)
            CoinCountChanged?.Invoke(AvailableCoins, maxCoins);

        // End the damage boost when its time runs out.
        if (_boostWasActive && !DamageBoostActive)
        {
            _boostWasActive = false;
            _boostMultiplier = 1f;
            _damageCarry = 0f;
            DamageBoostChanged?.Invoke(false);
        }
    }

    // ------------------------------------------------------------------ Damage boost

    /// <summary>
    /// Multiplies coin damage for 'duration' seconds (Confetti Popper: 1.5x for 20 s).
    /// Picking up another boost restarts the timer; boosts don't stack.
    /// </summary>
    public void ApplyDamageBoost(float multiplier, float duration)
    {
        _boostMultiplier = multiplier;
        _boostEndsAt = Time.time + duration;
        _boostWasActive = true;
        DamageBoostChanged?.Invoke(true);
    }

    /// <summary>
    /// Damage for the next coin. Damage is a whole number, so a fractional result (1 x 1.5 = 1.5)
    /// keeps the leftover for the next coin: boosted coins deal 1, 2, 1, 2... = 1.5 on average.
    /// </summary>
    private int NextCoinDamage()
    {
        float multiplier = DamageBoostActive ? _boostMultiplier : 1f;
        float exact = damage * multiplier + _damageCarry;
        int whole = Mathf.FloorToInt(exact + 0.0001f); // Tiny epsilon so 1.9999 counts as 2.
        _damageCarry = exact - whole;
        return whole;
    }

    // ------------------------------------------------------------------ Throwing

    private void OnThrowPressed(InputAction.CallbackContext ctx)
    {
        TryThrow();
    }

    private void TryThrow()
    {
        if (_controller.IsDashing || Time.time < _nextThrowTime || AvailableCoins <= 0)
            return; // No coins left (or busy): nothing happens.

        // Aim: up if Up is held, otherwise the way Pip faces.
        bool aimUp = _moveAction.ReadValue<Vector2>().y > aimThreshold;
        int facing = _controller.FacingDirection;
        Vector2 baseDirection = aimUp ? Vector2.up : new Vector2(facing, 0f);
        Vector2 offset = aimUp ? upThrowOffset : new Vector2(forwardThrowOffset.x * facing, forwardThrowOffset.y);
        Vector2 spawnPosition = (Vector2)transform.position + offset;

        // Normal throw = 1 coin. Juggle Shot = every available coin, at most 3.
        int count = juggleShotUnlocked ? Mathf.Min(AvailableCoins, 3) : 1;

        for (int i = 0; i < count; i++)
        {
            // Center the fan on the aim direction, fanAngle apart:
            // 1 coin = 0°, 2 coins = -7.5°/+7.5°, 3 coins = -15°/0°/+15° (with fanAngle = 15).
            float angle = (i - (count - 1) / 2f) * fanAngle;
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * baseDirection;
            LaunchCoin(spawnPosition, direction);
        }

        _nextThrowTime = Time.time + throwCooldown;
        CoinsThrown?.Invoke(count);
        CoinCountChanged?.Invoke(AvailableCoins, maxCoins);
    }

    private void LaunchCoin(Vector2 position, Vector2 direction)
    {
        _returnTimes.Add(Time.time + returnTime); // This coin is now "out".

        StarCoin coin = _pool.Get();
        coin.Launch(new StarCoin.LaunchData
        {
            Position = position,
            Direction = direction.normalized,
            Speed = coinSpeed,
            Range = coinRange,
            Damage = NextCoinDamage(),
            Knockback = knockbackForce,
            HitLayers = hitLayers,
            Owner = gameObject,
        }, ReturnToPool);
    }

    /// <summary>Called by a coin when its flight ends. Only the visual goes back; the count uses returnTime.</summary>
    private void ReturnToPool(StarCoin coin)
    {
        if (_pool != null)
            _pool.Release(coin);
    }

    // ------------------------------------------------------------------ Editor helpers

    private void OnDrawGizmosSelected()
    {
        // Shows the spawn points and the coin range.
        int facing = Application.isPlaying && _controller != null ? _controller.FacingDirection : 1;
        Vector2 feet = transform.position;
        Vector2 forwardStart = feet + new Vector2(forwardThrowOffset.x * facing, forwardThrowOffset.y);
        Vector2 upStart = feet + upThrowOffset;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(forwardStart, forwardStart + new Vector2(facing * coinRange, 0f));
        Gizmos.DrawLine(upStart, upStart + Vector2.up * coinRange);
    }
}
