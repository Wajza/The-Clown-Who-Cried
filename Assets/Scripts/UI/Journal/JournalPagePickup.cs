using UnityEngine;

/// <summary>
/// A torn journal page lying in a level. Pip picks it up by touching it.
///
/// When touched: the page is marked as found (JournalPages.Add: saved + GameEvents.JournalPageFound)
/// and this object switches off. If the page was found before (e.g. after reloading the level),
/// the object switches itself off at the start, so each page can only be picked up once.
///
/// Setup: a GameObject with a trigger Collider2D (about 1 x 1 tile), this script with the Page
/// asset assigned, and a sprite child (Placeholder_UI_Page for now).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class JournalPagePickup : MonoBehaviour
{
    [SerializeField, Tooltip("Which page this is.")]
    private JournalPageData page;

    public JournalPageData Page => page;

    private void Awake() => GetComponent<Collider2D>().isTrigger = true;

    private void Start()
    {
        if (JournalPages.IsFound(page))
            gameObject.SetActive(false);
    }

    private void OnEnable() => GameEvents.SceneLoaded += OnSceneLoaded;
    private void OnDisable() => GameEvents.SceneLoaded -= OnSceneLoaded;

    // Checked again once the scene is fully loaded: when you press Play in the Editor, the save
    // only exists after this object's Start.
    private void OnSceneLoaded(string sceneName, string spawnPointId)
    {
        if (JournalPages.IsFound(page))
            gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        if (JournalPages.Add(page) || JournalPages.IsFound(page))
            gameObject.SetActive(false);
    }
}
