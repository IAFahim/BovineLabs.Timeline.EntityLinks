using BovineLabs.Core.Authoring;
using BovineLabs.Timeline.Authoring;
using Bovinelabs.Timeline.EntityLinks.Authoring;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    public sealed class EntityLinkAttachClip : DOTSClip, ITimelineClipAsset
    {
        public EntityLinkTagSchema LinkSchema;
        public ResolveRule ResolveRule = ResolveRule.Parent;

        public AttachmentTransformFlags TransformFlags = AttachmentTransformFlags.SetParent | AttachmentTransformFlags.SetTransform;

        public override double duration => 1;

        public ClipCaps clipCaps => ClipCaps.None;

        public override void Bake(Entity clipEntity, BakingContext context)
        {
            if (context.Binding != null && context.Binding.Target != Entity.Null)
            {
                context.Baker.AddTransformUsageFlags(context.Binding.Target, TransformUsageFlags.None);
            }

            context.Baker.AddComponent(clipEntity, new EntityLinkAttachConfig
            {
                LinkKey = LinkSchema != null ? LinkSchema.Id : (byte)0,
                ResolveRule = ResolveRule,
                TransformFlags = TransformFlags
            });

            context.Baker.AddComponent(clipEntity, new EntityLinkAttachState
            {
                ResolvedTarget = Entity.Null,
                CapturedPreviousParent = Entity.Null,
                CapturedOriginalTransform = LocalTransform.Identity,
                IsAttached = false
            });

            base.Bake(clipEntity, context);
        }
    }
}