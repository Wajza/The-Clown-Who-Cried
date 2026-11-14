using UnityEngine;

/// <summary>
/// Pip's sounds. It only LISTENS to my teammate's player events and asks the AudioManager to play
/// the matching sound; their scripts don't know it exists.
///
///   PlayerController   Jumped -> jump; DashingChanged(true) -> dash
///   PlayerHealth       StarLost -> star lost; OnDeath -> death; ShieldChanged(true) -> shield
///   PlayerMallet       SwingStarted -> swing; HitLanded -> hit; Pogoed -> pogo
///   PlayerCoinThrower  CoinsThrown -> coin throw
///   TearGauge          TearBurstReleased -> tear burst; GaugeChanged (becomes full) -> tear full
///   PlayerBlueSight    ActiveChanged -> Blue Sight on/off
///   PlayerHazards      HazardHit -> hazard
///   PlayerStarFragments FragmentCollected -> fragment
///
/// Each level has a new Pip, so (like the HUD) it re-links whenever PlayerController.Current
/// changes. Components Pip doesn't have are simply skipped.
///
/// Setup: on the GameSystems prefab (the builder adds it).
/// </summary>
public class PlayerSounds : MonoBehaviour
{
    private PlayerController _pip;
    private PlayerHealth _health;
    private PlayerMallet _mallet;
    private PlayerCoinThrower _coins;
    private TearGauge _tears;
    private PlayerBlueSight _blueSight;
    private PlayerHazards _hazards;
    private PlayerStarFragments _fragments;
    private bool _gaugeWasFull;

    private void Update()
    {
        if (PlayerController.Current != _pip)
        {
            Unlink();
            Link(PlayerController.Current);
        }
    }

    private void OnDisable() => Unlink();

    // ------------------------------------------------------------------ Linking

    private void Link(PlayerController pip)
    {
        _pip = pip;
        if (pip == null)
            return;

        pip.Jumped += OnJumped;
        pip.DashingChanged += OnDashingChanged;

        if ((_health = pip.GetComponent<PlayerHealth>()) != null)
        {
            _health.StarLost += OnStarLost;
            _health.OnDeath += OnDeath;
            _health.ShieldChanged += OnShieldChanged;
        }
        if ((_mallet = pip.GetComponent<PlayerMallet>()) != null)
        {
            _mallet.SwingStarted += OnSwing;
            _mallet.HitLanded += OnHitLanded;
            _mallet.Pogoed += OnPogo;
        }
        if ((_coins = pip.GetComponent<PlayerCoinThrower>()) != null)
            _coins.CoinsThrown += OnCoinsThrown;
        if ((_tears = pip.GetComponent<TearGauge>()) != null)
        {
            _tears.TearBurstReleased += OnTearBurst;
            _tears.GaugeChanged += OnGaugeChanged;
            _gaugeWasFull = _tears.IsFull;
        }
        if ((_blueSight = pip.GetComponent<PlayerBlueSight>()) != null)
            _blueSight.ActiveChanged += OnBlueSight;
        if ((_hazards = pip.GetComponent<PlayerHazards>()) != null)
            _hazards.HazardHit += OnHazard;
        if ((_fragments = pip.GetComponent<PlayerStarFragments>()) != null)
            _fragments.FragmentCollected += OnFragment;
    }

    private void Unlink()
    {
        if (_pip != null)
        {
            _pip.Jumped -= OnJumped;
            _pip.DashingChanged -= OnDashingChanged;
        }
        if (_health != null)
        {
            _health.StarLost -= OnStarLost;
            _health.OnDeath -= OnDeath;
            _health.ShieldChanged -= OnShieldChanged;
        }
        if (_mallet != null)
        {
            _mallet.SwingStarted -= OnSwing;
            _mallet.HitLanded -= OnHitLanded;
            _mallet.Pogoed -= OnPogo;
        }
        if (_coins != null) _coins.CoinsThrown -= OnCoinsThrown;
        if (_tears != null)
        {
            _tears.TearBurstReleased -= OnTearBurst;
            _tears.GaugeChanged -= OnGaugeChanged;
        }
        if (_blueSight != null) _blueSight.ActiveChanged -= OnBlueSight;
        if (_hazards != null) _hazards.HazardHit -= OnHazard;
        if (_fragments != null) _fragments.FragmentCollected -= OnFragment;

        _pip = null;
        _health = null;
        _mallet = null;
        _coins = null;
        _tears = null;
        _blueSight = null;
        _hazards = null;
        _fragments = null;
    }

    // ------------------------------------------------------------------ Event handlers

    private static void OnJumped() => AudioManager.Play(SoundIds.Jump);
    private static void OnDashingChanged(bool dashing) { if (dashing) AudioManager.Play(SoundIds.Dash); }
    private static void OnStarLost() => AudioManager.Play(SoundIds.StarLost);
    private static void OnDeath() => AudioManager.Play(SoundIds.Death);
    private static void OnShieldChanged(bool on) { if (on) AudioManager.Play(SoundIds.Shield); }
    private static void OnSwing(SwingDirection direction) => AudioManager.Play(SoundIds.Swing);
    private static void OnHitLanded(IDamageable target, DamageInfo info) => AudioManager.Play(SoundIds.Hit);
    private static void OnPogo() => AudioManager.Play(SoundIds.Pogo);
    private static void OnCoinsThrown(int count) => AudioManager.Play(SoundIds.CoinThrow);
    private static void OnTearBurst(Vector2 position, float radius) => AudioManager.Play(SoundIds.TearBurst);
    private static void OnBlueSight(bool on) => AudioManager.Play(on ? SoundIds.BlueSightOn : SoundIds.BlueSightOff);
    private static void OnHazard() => AudioManager.Play(SoundIds.Hazard);
    private static void OnFragment(string id, int count) => AudioManager.Play(SoundIds.Fragment);

    private void OnGaugeChanged(int value, int max)
    {
        bool full = max > 0 && value >= max;
        if (full && !_gaugeWasFull)
            AudioManager.Play(SoundIds.TearFull); // Only the moment it fills up.
        _gaugeWasFull = full;
    }
}
