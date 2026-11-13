using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Reads and writes the save file. One save slot.
///
/// The file is JSON at Application.persistentDataPath/save.json. On Windows that is
///   C:\Users\[you]\AppData\LocalLow\[Company Name]\[Product Name]\save.json
/// (Company/Product Name come from Project Settings > Player). You can open it in a text
/// editor to check what was saved.
///
/// Writing is "safe": the data goes to save.json.tmp first, and only replaces the real file
/// once it is completely written. If the game crashes mid-save, the old save survives.
///
/// This class only does file work. GameManager decides WHEN to save and WHAT goes in it.
/// </summary>
public static class SaveSystem
{
    private const string FileName = "save.json";

    /// <summary>Full path of the save file.</summary>
    public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    /// <summary>True if a save file exists (the title screen uses this to show "Continue").</summary>
    public static bool HasSave => File.Exists(SavePath);

    /// <summary>Writes the data to disk. Returns false (and logs why) if it failed.</summary>
    public static bool Write(SaveData data)
    {
        if (data == null)
            return false;

        string tempPath = SavePath + ".tmp";
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true); // prettyPrint = readable file.
            File.WriteAllText(tempPath, json);

            // Swap the finished temp file in place of the old save.
            if (File.Exists(SavePath))
                File.Delete(SavePath);
            File.Move(tempPath, SavePath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem: could not write the save file.\n{e}");
            return false;
        }
    }

    /// <summary>Reads the save file. Returns null if there is no save or it can't be read.</summary>
    public static SaveData Read()
    {
        if (!HasSave)
            return null;

        try
        {
            string json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem: could not read the save file (it may be damaged).\n{e}");
            return null;
        }
    }

    /// <summary>Deletes the save file, if there is one.</summary>
    public static void Delete()
    {
        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem: could not delete the save file.\n{e}");
        }
    }
}
