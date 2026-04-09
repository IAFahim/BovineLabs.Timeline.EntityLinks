using BovineLabs.Timeline.Authoring;
using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    [Serializable]
    [TrackClipType(typeof(EntityLinkInstantiateClip))]
    [TrackColor(0.9f, 0.6f, 0.2f)]
    [TrackBindingType(typeof(GameObject))] // The context object to resolve the link from
    [DisplayName("BovineLabs/Timeline/Entity Links/Instantiate at Link")]
    public class EntityLinkInstantiateTrack : DOTSTrack
    {
    }
}