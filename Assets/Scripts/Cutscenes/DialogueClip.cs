using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// A conversation inside a cutscene Timeline. Drag a DialogueData onto a clip on a DialogueTrack.
///
/// When the playhead reaches the clip, the timeline PAUSES and the conversation plays in the
/// normal dialogue box. When the player finishes reading, the timeline carries on. So the clip's
/// length on the timeline doesn't matter; make it short (e.g. 0.5 s).
///
/// In the Editor's Timeline preview (not playing) nothing happens, so scrubbing is safe.
/// </summary>
[Serializable]
public class DialogueClip : PlayableAsset, ITimelineClipAsset
{
    [Tooltip("The conversation to play here.")]
    public DialogueData dialogue;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<DialogueClipBehaviour>.Create(graph);
        playable.GetBehaviour().dialogue = dialogue;
        return playable;
    }
}

/// <summary>What a DialogueClip does while the timeline plays it (see DialogueClip).</summary>
public class DialogueClipBehaviour : PlayableBehaviour
{
    public DialogueData dialogue;
    private bool _played;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (!Application.isPlaying || _played || dialogue == null || CutscenePlayer.IsSkipping)
            return;
        DialogueRunner runner = DialogueRunner.Instance;
        if (runner == null)
            return;

        _played = true;

        // Pause the whole timeline (speed 0) until the conversation is read, then carry on.
        Playable root = playable.GetGraph().GetRootPlayable(0);
        root.SetSpeed(0);
        bool started = runner.StartDialogue(dialogue, onFinished: () =>
        {
            if (root.IsValid())
                root.SetSpeed(1);
        });
        if (!started)
            root.SetSpeed(1); // Couldn't show it (another conversation was open): don't get stuck.
    }
}
