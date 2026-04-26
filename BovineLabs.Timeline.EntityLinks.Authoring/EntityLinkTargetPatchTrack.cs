using System;
using System.ComponentModel;
using BovineLabs.Reaction.Authoring.Core;
using BovineLabs.Timeline.Authoring;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    [Serializable]
    [TrackClipType(typeof(EntityLinkTargetPatchClip))]
    [TrackColor(0.2f, 0.8f, 0.8f)]
    [TrackBindingType(typeof(TargetsAuthoring))]
    [DisplayName("BovineLabs/Timeline/Entity Links/Target Patch")]
    public sealed class EntityLinkTargetPatchTrack : DOTSTrack
    {
    }
}