using UnityEngine;

/// <summary>
/// One torn journal page. Pip finds them in the levels (JournalPagePickup) or gets them from
/// the story (e.g. page 2 after beating Chortle). Found pages can be read in the Journal.
///
/// WHERE TO PUT PAGE ASSETS
/// In Assets/Resources/JournalPages (subfolders are fine). The Journal lists every page there,
/// sorted by Order, and shows the ones not found yet as torn out.
///
/// Create one: Create > Clown Who Cried > Journal > Page.
/// </summary>
[CreateAssetMenu(menuName = "Clown Who Cried/Journal/Page", fileName = "JournalPage_")]
public class JournalPageData : ScriptableObject
{
    [Tooltip("Unique id used in the save file, e.g. 'page_2'. Don't change it once saves exist.")]
    public string id;

    [Tooltip("Position in the Journal (page number).")]
    public int order;

    [Tooltip("Shown in the page list once found, e.g. 'A Sad Song'.")]
    public string title;

    [TextArea(4, 10), Tooltip("What's written on the page. Plain letters and punctuation only (the pixel font has no fancy quotes).")]
    public string text;

    [Tooltip("Optional drawing on the page (artist).")]
    public Sprite picture;

    [Tooltip("Only for automatic tests: hidden from the Journal unless a test asks for it.")]
    public bool testOnly;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
            id = name;
    }
}
