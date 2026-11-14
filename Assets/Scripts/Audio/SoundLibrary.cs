using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The list of every sound in the game: an id (see SoundIds) and the clip(s) to play for it.
/// One asset: Assets/Resources/Audio/SoundLibrary.
///
///   Clips             one is picked at random each time (give a hit 3 versions for variety)
///   Volume            0..1, multiplied by the player's Music/Sound setting
///   Pitch Variation   e.g. 0.1 = each play is randomly 10% higher or lower (less repetitive)
///
/// TO USE REAL AUDIO: replace the placeholder files in Assets/Audio/Placeholders keeping their
/// names, or drag new clips into an entry here. No code changes either way.
/// </summary>
[CreateAssetMenu(menuName = "Clown Who Cried/Audio/Sound Library", fileName = "SoundLibrary")]
public class SoundLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string id;
        public List<AudioClip> clips = new List<AudioClip>();
        [Range(0f, 1f)] public float volume = 0.8f;
        [Range(0f, 0.5f)] public float pitchVariation;
    }

    public List<Entry> entries = new List<Entry>();

    private Dictionary<string, Entry> _byId;

    /// <summary>The entry with this id, or null.</summary>
    public Entry Find(string id)
    {
        if (_byId == null || _byId.Count != entries.Count)
        {
            _byId = new Dictionary<string, Entry>();
            foreach (Entry e in entries)
                if (e != null && !string.IsNullOrEmpty(e.id))
                    _byId[e.id] = e;
        }
        return id != null && _byId.TryGetValue(id, out Entry entry) ? entry : null;
    }

    /// <summary>A random clip of the entry, or null if it has none.</summary>
    public static AudioClip PickClip(Entry entry)
    {
        if (entry == null || entry.clips.Count == 0)
            return null;
        return entry.clips[UnityEngine.Random.Range(0, entry.clips.Count)];
    }

    // Rebuild the lookup when the asset is edited in the Inspector.
    private void OnValidate() => _byId = null;
}
