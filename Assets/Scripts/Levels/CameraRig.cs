using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// The camera set-up in every level (the "CameraRig" object the level builder makes):
///
///   CameraRig                  this script + my teammate's ScreenShake (+ CinemachineImpulseSource)
///   ├─ Main Camera             Camera, CinemachineBrain, Pixel Perfect Camera (480 x 270, 16 PPU)
///   └─ PipCamera               CinemachineCamera following Pip, with:
///                                - CinemachinePositionComposer (smooth follow, small dead zone)
///                                - CinemachineConfiner2D (stays inside the level/room bounds)
///                                - CinemachinePixelPerfect (no wobbly pixels)
///                                - CinemachineImpulseListener (receives ScreenShake)
///
/// WHAT THIS SCRIPT DOES
///   - Points the CinemachineCamera at Pip. Each level spawns a new Pip, so it re-checks every frame
///     and jumps (no slow pan) to her when she changes or is teleported to a spawn point.
///   - Confines the camera to LevelInfo's Camera Bounds, or to a CameraZone while Pip is in one.
///
/// Setup: comes with every level the builder makes. In a hand-made level, copy the CameraRig
/// from one of them and assign the level's bounds in LevelInfo.
/// </summary>
public class CameraRig : MonoBehaviour
{
    [SerializeField] private CinemachineCamera pipCamera;
    [SerializeField] private CinemachineConfiner2D confiner;

    // ------------------------------------------------------------------ Public API

    public static CameraRig Current { get; private set; }

    public CinemachineCamera PipCamera => pipCamera;

    /// <summary>The shape the camera is kept inside right now.</summary>
    public Collider2D CurrentBounds => confiner != null ? confiner.BoundingShape2D : null;

    /// <summary>Called by CameraZone when Pip walks in.</summary>
    public void EnterZone(CameraZone zone)
    {
        _zones.Remove(zone);
        _zones.Add(zone); // The newest zone wins.
        UseBounds(zone.Bounds);
    }

    /// <summary>Called by CameraZone when Pip walks out.</summary>
    public void ExitZone(CameraZone zone)
    {
        _zones.Remove(zone);
        UseBounds(_zones.Count > 0 ? _zones[_zones.Count - 1].Bounds : LevelBounds);
    }

    // ------------------------------------------------------------------ Private state

    private readonly List<CameraZone> _zones = new List<CameraZone>();
    private PlayerController _following;

    private static Collider2D LevelBounds => LevelInfo.Current != null ? LevelInfo.Current.CameraBounds : null;

    // ------------------------------------------------------------------ Unity lifecycle

    private void Awake() => Current = this;

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }

    private void OnEnable()
    {
        GameEvents.SceneLoaded += OnSceneLoaded;
        GameEvents.PlayerRespawned += SnapToPip;
    }

    private void OnDisable()
    {
        GameEvents.SceneLoaded -= OnSceneLoaded;
        GameEvents.PlayerRespawned -= SnapToPip;
    }

    private void Start() => UseBounds(LevelBounds);

    private void LateUpdate()
    {
        if (PlayerController.Current != _following)
            SnapToPip();
    }

    // Pip has just been placed at her spawn point: cut straight to her.
    private void OnSceneLoaded(string scene, string spawnPointId) => SnapToPip();

    // ------------------------------------------------------------------ Helpers

    private void SnapToPip()
    {
        _following = PlayerController.Current;
        if (pipCamera == null)
            return;

        pipCamera.Follow = _following != null ? _following.transform : null;
        pipCamera.PreviousStateIsValid = false; // Jump there this frame instead of gliding.
    }

    private void UseBounds(Collider2D bounds)
    {
        if (confiner == null || confiner.BoundingShape2D == bounds)
            return;
        confiner.BoundingShape2D = bounds;
        confiner.InvalidateBoundingShapeCache();
        if (pipCamera != null)
            pipCamera.PreviousStateIsValid = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Current = null;
}
