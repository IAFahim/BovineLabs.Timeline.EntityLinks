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
    public sealed class EntityLinkCopyTransformClip : DOTSClip, ITimelineClipAsset
    {
        [Tooltip("Which Targets slot is moved each frame to match the linked source.")]
        public Target entityToMove = Target.Target;

        [Header("Source Link")] [Tooltip("Which Targets slot to read the link-map root from.")]
        public Target readRootFrom = Target.Owner;

        [Tooltip("The link key whose entity is followed as the transform source.")]
        public EntityLinkSchema link;

        [Header("Copy Mask")] [Tooltip("Copy the source position each frame.")]
        public bool copyPosition = true;

        [Tooltip("Copy the source rotation each frame.")]
        public bool copyRotation = true;

        [Header("Offsets (Applied in Source Space)")] [Tooltip("Position offset added in the source's local space.")]
        public Vector3 positionOffset;

        [Tooltip("Rotation offset (Euler degrees) applied in the source's local space.")]
        public Vector3 rotationOffset;

        public override double duration => 1;
        public ClipCaps clipCaps => ClipCaps.None;

        public override void Bake(Entity clipEntity, BakingContext context)
        {
            if (!EntityLinkAuthoringUtility.TryGetKey(link, out var key))
            {
                Debug.LogError($"{nameof(EntityLinkCopyTransformClip)} '{name}' missing link schema.");
                return;
            }

            var builder = new EntityLinkCopyTransformBuilder
            {
                EntityToMove = entityToMove,
                ReadRootFrom = readRootFrom,
                LinkKey = key,
                CopyPosition = copyPosition,
                CopyRotation = copyRotation,
                PositionOffset = positionOffset,
                RotationOffset = quaternion.Euler(math.radians(rotationOffset))
            };
            var commands = new BakerCommands(context.Baker, clipEntity);
            builder.ApplyTo(ref commands);

            base.Bake(clipEntity, context);
        }
    }
}