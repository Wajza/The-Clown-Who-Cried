using UnityEngine;

/// <summary>
/// Marker: ground that can disappear (trapdoors, crumbling or Blue Sight platforms).
/// PlayerHazards never records this as a "safe" respawn spot, so a hazard can't put
/// Pip back on a floor that is about to vanish under her again.
/// </summary>
public class UnsafeGround : MonoBehaviour
{
}
