using UnityEngine;
#if CINEMACHINE_3
using Unity.Cinemachine;
#endif

/// <summary>
/// Screen shake through Cinemachine Impulse. Put ONE in the scene (e.g. on the camera rig);
/// anything can then call ScreenShake.Shake(strength).
///
/// Strength is roughly "how far the camera jolts, in tiles": 0.1 = small tap, 0.5 = big hit.
/// It respects GameFeelSettings (on/off and the strength slider) and this component's
/// enabled checkbox, so shake can be turned off globally or per scene.
///
/// Needs Cinemachine 3. Without it, the code still compiles and Shake() just does nothing.
/// The CinemachineCamera also needs a "Cinemachine Impulse Listener" extension.
/// </summary>
#if CINEMACHINE_3
[RequireComponent(typeof(CinemachineImpulseSource))]
#endif
public class ScreenShake : MonoBehaviour
{
    [SerializeField, Range(0f, 3f), Tooltip("Multiplies every shake in this scene. 0 = no shake.")]
    private float globalMultiplier = 1f;

    private static ScreenShake s_instance;

#if CINEMACHINE_3
    private CinemachineImpulseSource _source;
#endif

    private void Awake()
    {
#if CINEMACHINE_3
        _source = GetComponent<CinemachineImpulseSource>();
#else
        Debug.LogWarning("ScreenShake: install Cinemachine 3 to enable screen shake.", this);
#endif
    }

    private void OnEnable() => s_instance = this;

    private void OnDisable()
    {
        if (s_instance == this)
            s_instance = null;
    }

    /// <summary>
    /// Shakes the camera. direction is optional: give it (e.g. the knockback direction) for a
    /// directional jolt, or leave it out for a random one.
    /// </summary>
    public static void Shake(float strength, Vector2 direction = default)
    {
        if (s_instance == null || !GameFeelSettings.ScreenShakeEnabled)
            return;

        float force = strength * s_instance.globalMultiplier * GameFeelSettings.ScreenShakeStrength;
        if (force <= 0f)
            return;

#if CINEMACHINE_3
        Vector2 dir = direction == Vector2.zero ? Random.insideUnitCircle.normalized : direction.normalized;
        if (dir == Vector2.zero)
            dir = Vector2.down;
        s_instance._source.GenerateImpulse(new Vector3(dir.x, dir.y, 0f) * force);
#endif
    }
}
