using BovineLabs.Timeline.Authoring;
using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    [Serializable]
    [TrackClipType(typeof(EntityLinkAttachClip))]
    [TrackColor(0.2f, 0.8f, 0.4f)]
    [TrackBindingType(typeof(GameObject))]
    [DisplayName("BovineLabs/Timeline/Entity Links/Attach to Link")]
    public class EntityLinkAttachTrack : DOTSTrack
    {
    }
}