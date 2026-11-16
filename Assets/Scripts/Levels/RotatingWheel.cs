using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A Ferris wheel (W3 "Runaway Coaster"): gondola platforms travel around a circle but stay
/// upright, so Pip can stand on them and ride up to high places.
///
/// HOW
/// The wheel doesn't rotate its children (that would tip the gondolas over). Instead, every
/// physics step it works out where each gondola should be on the circle and moves it there with
/// Rigidbody2D.MovePosition. Gondolas are spread evenly around the circle.
///
/// Each gondola needs: a KINEMATIC Rigidbody2D, a solid collider on the Ground layer (so Pip can
/// stand on it), and a PlatformCarrier (so Pip moves along with it).
///
/// Setup: an empty GameObject at the wheel's centre with this script; gondolas can be anywhere
/// (they're placed on the circle when the level starts). Optional "Spoke" art can be a child.
/// </summary>
public class RotatingWheel : MonoBehaviour
{
    [SerializeField] private List<Rigidbody2D> gondolas = new List<Rigidbody2D>();
    [SerializeField, Min(0.5f)] private float radius = 4.5f;
    [SerializeField, Tooltip("Degrees per second. Negative = clockwise.")]
    private float degreesPerSecond = 18f;

    public float Angle => _angle;
    public IReadOnlyList<Rigidbody2D> Gondolas => gondolas;

    private float _angle;

    private void Start()
    {
        foreach (Rigidbody2D gondola in gondolas)
            gondola.bodyType = RigidbodyType2D.Kinematic;
        Place(teleport: true);
    }

    private void FixedUpdate()
    {
        _angle = (_angle + degreesPerSecond * Time.fixedDeltaTime) % 360f;
        Place(teleport: false);
    }

    private void Place(bool teleport)
    {
        for (int i = 0; i < gondolas.Count; i++)
        {
            float a = (_angle + i * 360f / gondolas.Count) * Mathf.Deg2Rad;
            Vector2 point = (Vector2)transform.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            if (teleport)
                gondolas[i].position = point;
            else
                gondolas[i].MovePosition(point);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        const int segments = 32;
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments, b = (i + 1) * Mathf.PI * 2f / segments;
            Gizmos.DrawLine(transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                transform.position + new Vector3(Mathf.Cos(b), Mathf.Sin(b)) * radius);
        }
    }
}
