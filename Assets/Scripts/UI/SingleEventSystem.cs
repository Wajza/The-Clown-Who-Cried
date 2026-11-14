using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Unity allows only one EventSystem (the thing that lets keyboard, gamepad and mouse press UI
/// buttons). Mine lives in the GameSystems prefab and survives every scene. If a scene has its own
/// EventSystem too (e.g. one made automatically when someone added a Canvas), this switches the
/// scene's copy off while playing, so there are never two. Scene files are not changed.
///
/// Setup: on the EventSystem object in the GameSystems prefab.
/// </summary>
[RequireComponent(typeof(EventSystem))]
public class SingleEventSystem : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        DisableOthers();
    }

    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => DisableOthers();

    private void DisableOthers()
    {
        EventSystem mine = GetComponent<EventSystem>();
        foreach (EventSystem other in FindObjectsByType<EventSystem>())
        {
            if (other == mine)
                continue;
            other.gameObject.SetActive(false);
            Debug.Log($"SingleEventSystem: switched off the extra EventSystem '{other.name}' in this scene " +
                      "(the GameSystems prefab already has one). You can delete it from the scene.");
        }
        EventSystem.current = mine;
    }
}
