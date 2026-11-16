using UnityEngine;

/// <summary>
/// Who is talking: a name, a portrait and a name colour. One asset per character
/// (Pip, the Mayor, ...), reused by every line they say.
///
/// Create one: right-click in the Project window > Create > Clown Who Cried > Dialogue > Speaker.
///
/// Like DialogueData, this is a "data asset": its fields are public because they are only
/// information, and nothing needs to check them when they change.
/// </summary>
[CreateAssetMenu(menuName = "Clown Who Cried/Dialogue/Speaker", fileName = "Speaker_")]
public class DialogueSpeaker : ScriptableObject
{
    [Tooltip("Name shown above the text box.")]
    public string displayName = "Someone";

    [Tooltip("Optional face shown next to the text. Leave empty for no portrait.")]
    public Sprite portrait;

    [Tooltip("Colour of the name.")]
    public Color nameColor = Color.white;
}
