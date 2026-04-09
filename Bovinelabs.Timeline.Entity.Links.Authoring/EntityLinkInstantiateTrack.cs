using System;
using System.ComponentModel;
using BovineLabs.Timeline.Authoring;
using UnityEngine;
using UnityEngine.Timeline;

namespace Bovinelabs.Timeline.Entity.Links.Authoring
{
    [Serializable]
    [TrackClipType(typeof(EntityLinkInstantiateClip))]
    [TrackColor(0.9f, 0.6f, 0.2f)]
    [TrackBindingType(typeof(GameObject))]
    [DisplayName("BovineLabs/Timeline/Entity Links/Instantiate Track")]
    public sealed class EntityLinkInstantiateTrack : DOTSTrack
    {
    }
}