using UnityEngine.Timeline;

/// <summary>
/// A Timeline track for conversations. In the Timeline window: Add (+) > Dialogue Track, then
/// right-click the track > Add Dialogue Clip, and drag a DialogueData into the clip's Inspector.
/// See DialogueClip for how it plays.
/// </summary>
[TrackColor(0.95f, 0.75f, 0.3f)]
[TrackClipType(typeof(DialogueClip))]
public class DialogueTrack : TrackAsset
{
}
