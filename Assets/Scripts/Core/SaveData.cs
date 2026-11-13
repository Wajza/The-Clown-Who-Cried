using System;
using System.Collections.Generic;

/// <summary>
/// Everything that is written to the save file. A plain C# class (not a MonoBehaviour), so it
/// can be turned into JSON with Unity's JsonUtility.
///
/// JsonUtility rules to remember:
///   - Only public fields (or [SerializeField] private ones) are saved. Properties are NOT.
///   - Lists work; Dictionaries and HashSets do NOT. That's why everything here is a List.
///
/// If you add a field later, old save files still load: the new field just gets its default.
/// </summary>
[Serializable]
public class SaveData
{
    /// <summary>Bump this if the format changes in a way old saves can't handle.</summary>
    public int version = 1;

    /// <summary>The scene Pip was last in (e.g. "Tumbleton"). Empty = a brand-new game.</summary>
    public string sceneName = "";

    /// <summary>
    /// The SpawnPoint (or Checkpoint) Pip respawns at in that scene: the last checkpoint touched,
    /// or the door she came in through. Empty = wherever Pip is placed in the scene.
    /// </summary>
    public string spawnPointId = "";

    /// <summary>IDs of every Star Fragment collected (from my teammate's PlayerStarFragments).</summary>
    public List<string> starFragmentIds = new List<string>();

    /// <summary>
    /// Story flags: simple "this happened" markers, e.g. "prologue_intro_seen" or "met_mayor".
    /// Used by tasks, dialogue and cutscenes in later parts.
    /// </summary>
    public List<string> flags = new List<string>();

    /// <summary>IDs of tasks that are started but not finished (Part 3).</summary>
    public List<string> activeTaskIds = new List<string>();

    /// <summary>IDs of tasks that are finished (Part 3).</summary>
    public List<string> completedTaskIds = new List<string>();

    // ------------------------------------------------------------------ Flag helpers

    public bool HasFlag(string flag) => !string.IsNullOrEmpty(flag) && flags.Contains(flag);

    /// <summary>Adds a flag. Does nothing if it is empty or already set.</summary>
    public void SetFlag(string flag)
    {
        if (!string.IsNullOrEmpty(flag) && !flags.Contains(flag))
            flags.Add(flag);
    }

    public void ClearFlag(string flag) => flags.Remove(flag);
}
