using System;
using System.ComponentModel;
using BovineLabs.Timeline.Authoring;
using BovineLabs.Timeline.EntityLinks.Authoring;
using UnityEngine;
using UnityEngine.Timeline;

namespace Bovinelabs.Timeline.EntityLinks.Authoring
{
    [Serializable]
    [TrackClipType(typeof(EntityLinkAttachClip))]
    [TrackColor(0.2f, 0.8f, 0.4f)]
    [TrackBindingType(typeof(GameObject))]
    [DisplayName("BovineLabs/Timeline/Entity Links/Attach Track")]
    public sealed class EntityLinkAttachTrack : DOTSTrack
    {
    }
}