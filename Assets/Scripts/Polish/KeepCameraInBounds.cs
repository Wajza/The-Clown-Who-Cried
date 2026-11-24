using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// In this level the camera follows Pip but never shows past the background art: it stops at the
/// level's Camera Bounds (LevelInfo), which should be the area the background covers. Used in Tumbleton.
///
/// Everywhere else PipCameraFollow switches the camera limits off; this turns them back on here,
/// with no slow-down near the edges.
///
/// Setup: on any object in the level (e.g. CameraBounds).
/// </summary>
public class KeepCameraInBounds : MonoBehaviour
{
    private void Start()
    {
        if (CameraRig.Current == null || CameraRig.Current.PipCamera == null)
            return;
        var confiner = CameraRig.Current.PipCamera.GetComponent<CinemachineConfiner2D>();
        if (confiner == null)
            return;
        confiner.Damping = 0f;
        confiner.SlowingDistance = 0f;
        confiner.InvalidateBoundingShapeCache();
        confiner.enabled = true; // Start runs after PipCameraFollow, which ran on scene load.
    }
}
