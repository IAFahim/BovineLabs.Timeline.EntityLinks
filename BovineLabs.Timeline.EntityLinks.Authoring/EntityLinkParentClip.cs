using BovineLabs.Core.Authoring.EntityCommands;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.Authoring;
using BovineLabs.Timeline.EntityLinks.Data.Builders;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    public sealed class EntityLinkParentClip : DOTSClip, ITimelineClipAsset
    {
        [Tooltip("Which Targets slot is reparented under the linked parent.")]
        public Target entityToParent = Target.Target;

        [Header("Parent Link")]
        [Tooltip("Which Targets slot to read the link-map root from.")]
        public Target readRootFrom = Target.Owner;

        [Tooltip("The link key whose entity becomes the new parent.")]
        public EntityLinkSchema parentLink;

        [Header("Offset")]
        [Tooltip("Local position under the new parent.")]
        public Vector3 localPosition;

        [Tooltip("Local rotation (Euler degrees) under the new parent.")]
        public Vector3 localRotation;

        [Header("Cleanup")] [Tooltip("If true, reverts to the previous parent when the clip ends.")]
        public bool restoreOnEnd = true;

        public override double duration => 1;
        public ClipCaps clipCaps => ClipCaps.None;

        public override void Bake(Entity clipEntity, BakingContext context)
        {
            if (!EntityLinkAuthoringUtility.TryGetKey(parentLink, out var key))
            {
                Debug.LogError($"{nameof(EntityLinkParentClip)} '{name}' missing parent link.");
                return;
            }

            var builder = new EntityLinkParentBuilder
            {
                EntityToParent = entityToParent,
                ReadRootFrom = readRootFrom,
                ParentLinkKey = key,
                LocalPosition = localPosition,
                LocalRotation = quaternion.Euler(math.radians(localRotation)),
                RestoreOnEnd = restoreOnEnd
            };
            var commands = new BakerCommands(context.Baker, clipEntity);
            builder.ApplyTo(ref commands);

            base.Bake(clipEntity, context);
        }
    }
}