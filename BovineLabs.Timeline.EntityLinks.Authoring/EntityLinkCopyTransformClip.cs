using BovineLabs.Core.Authoring.EntityCommands;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.Authoring;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    public sealed class EntityLinkCopyTransformClip : DOTSClip, ITimelineClipAsset
    {
        public Target entityToMove = Target.Target;

        [Header("Source Link")]
        public Target readRootFrom = Target.Owner;
        public EntityLinkSchema link;

        [Header("Copy Mask")]
        public bool copyPosition = true;
        public bool copyRotation = true;

        [Header("Offsets (Applied in Source Space)")]
        public Vector3 positionOffset;
        public Vector3 rotationOffset;

        public override double duration => 1;
        public ClipCaps clipCaps => ClipCaps.None;

        public override void Bake(Entity clipEntity, BakingContext context)
        {
            var commands = new BakerCommands(context.Baker, clipEntity);

            if (!EntityLinkAuthoringUtility.TryGetKey(this.link, out var key))
            {
                Debug.LogError($"{nameof(EntityLinkCopyTransformClip)} '{this.name}' missing link schema.");
                return;
            }

            commands.AddComponent(new EntityLinkCopyTransform
            {
                EntityToMove = this.entityToMove,
                ReadRootFrom = this.readRootFrom,
                LinkKey = key,
                CopyPosition = this.copyPosition,
                CopyRotation = this.copyRotation,
                PositionOffset = this.positionOffset,
                RotationOffset = quaternion.Euler(math.radians(this.rotationOffset))
            });

            base.Bake(clipEntity, context);
        }
    }
}
