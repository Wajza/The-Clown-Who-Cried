using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// A room where the camera does NOT follow Pip: it shows exactly the Area (e.g. the whole boss
/// background) and stays still. Used in Chortle's arena.
///
/// How: zooms the level's camera so its 16:9 view fits the Area, and keeps the camera confiner on
/// with the Area as its limits. A view as big as its limits can't move, so the camera is fixed
/// wherever Pip goes. (Everywhere else PipCameraFollow switches the confiner off.)
///
/// Setup: on any object in the scene, with Area = a trigger BoxCollider2D the size of the picture.
/// </summary>
public class FixedCameraRoom : MonoBehaviour
{
    [SerializeField, Tooltip("What the camera shows (a trigger BoxCollider2D around the background art).")]
    private BoxCollider2D area;

    public BoxCollider2D Area => area;

    /// <summary>Camera zoom (orthographic size) that shows the whole area on a 16:9 screen.</summary>
    public static float ZoomFor(Vector2 size) => Mathf.Min(size.y * 0.5f, size.x / (2f * 16f / 9f));

    private void Start()
    {
        if (area == null || CameraRig.Current == null || CameraRig.Current.PipCamera == null)
            return;

        CinemachineCamera cam = CameraRig.Current.PipCamera;
        var lens = cam.Lens;
        lens.OrthographicSize = ZoomFor(area.size);
        cam.Lens = lens;

        var confiner = cam.GetComponent<CinemachineConfiner2D>();
        if (confiner != null)
        {
            confiner.BoundingShape2D = area;
            confiner.Damping = 0f;
            confiner.SlowingDistance = 0f;
            confiner.InvalidateBoundingShapeCache();
            confiner.enabled = true; // Runs after PipCameraFollow (Start comes after scene load).
        }
        cam.PreviousStateIsValid = false; // Cut straight to the fixed view.
    }
}
