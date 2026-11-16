using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Small helper for the torn journal pages: which pages exist, which are found, and finding one.
/// "Found" lives in the save file (SaveData.journalPageIds), like everything else that must last.
///
///   JournalPages.All          every page in Assets/Resources/JournalPages, sorted by Order
///   JournalPages.IsFound(p)   has Pip found it?
///   JournalPages.Add(p)       mark as found, raise GameEvents.JournalPageFound, save the game
///
/// Used by JournalPagePickup, JournalUI and (Part 8) the reward for beating Chortle.
/// </summary>
public static class JournalPages
{
    private const string ResourcesFolder = "JournalPages"; // = Assets/Resources/JournalPages

    private static List<JournalPageData> s_all;
    private static bool s_allIncludesTests;

    /// <summary>
    /// Pages and tasks marked "Test Only" are left out of the Journal unless this is true.
    /// Only the automatic tests switch it on. (Reset to false every time Play starts.)
    /// </summary>
    public static bool IncludeTestContent { get; set; }

    /// <summary>Every page, sorted by Order. Loaded once.</summary>
    public static IReadOnlyList<JournalPageData> All
    {
        get
        {
            if (s_all == null || s_allIncludesTests != IncludeTestContent)
            {
                s_all = new List<JournalPageData>();
                foreach (JournalPageData page in Resources.LoadAll<JournalPageData>(ResourcesFolder))
                    if (!page.testOnly || IncludeTestContent)
                        s_all.Add(page);
                s_all.Sort((a, b) => a.order.CompareTo(b.order));
                s_allIncludesTests = IncludeTestContent;
            }
            return s_all;
        }
    }

    public static bool IsFound(JournalPageData page)
    {
        SaveData save = GameManager.Instance != null ? GameManager.Instance.Save : null;
        return page != null && save != null && save.journalPageIds.Contains(page.id);
    }

    public static int FoundCount
    {
        get
        {
            int count = 0;
            foreach (JournalPageData page in All)
                if (IsFound(page))
                    count++;
            return count;
        }
    }

    /// <summary>Marks the page as found and saves. Returns false if it was already found.</summary>
    public static bool Add(JournalPageData page)
    {
        GameManager gm = GameManager.Instance;
        if (page == null || gm == null || IsFound(page))
            return false;

        gm.Save.journalPageIds.Add(page.id);
        GameEvents.RaiseJournalPageFound(page);
        gm.SaveGame();
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_all = null;
        IncludeTestContent = false;
    }
}
