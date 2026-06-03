using System;
using System.ComponentModel;
using BovineLabs.Reaction.Authoring.Core;
using BovineLabs.Timeline.Authoring;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    [Serializable]
    [TrackClipType(typeof(EntityLinkCopyTransformClip))]
    [TrackColor(0.85f, 0.2f, 0.4f)]
    [TrackBindingType(typeof(TargetsAuthoring))]
    [DisplayName("BovineLabs/Entity Links/Copy Transform")]
    public sealed class EntityLinkCopyTransformTrack : DOTSTrack
    {
    }
}
