using BovineLabs.Timeline.Authoring;
using Bovinelabs.Timeline.Entity.Links.Data;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine.Timeline;

namespace Bovinelabs.Timeline.Entity.Links.Authoring
{
    public sealed class EntityLinkAttachClip : DOTSClip, ITimelineClipAsset
    {
        public EntityLinkTagSchema LinkSchema;
        public ResolveRule ResolveRule = ResolveRule.Parent;
        public AttachmentTransformFlags TransformFlags = AttachmentTransformFlags.SetParent | AttachmentTransformFlags.SetTransform;

        public ClipCaps clipCaps => ClipCaps.None;
        public override double duration => 1;

        public override void Bake(Unity.Entities.Entity clipEntity, BakingContext context)
        {
            if (context.Binding != null && context.Binding.Target != Unity.Entities.Entity.Null)
            {
                context.Baker.AddTransformUsageFlags(context.Binding.Target, TransformUsageFlags.Dynamic);
            }

            context.Baker.AddComponent(clipEntity, new EntityLinkAttachConfig
            {
                LinkKey = this.LinkSchema != null ? this.LinkSchema.Id : (byte)0,
                ResolveRule = this.ResolveRule,
                TransformFlags = this.TransformFlags
            });

            context.Baker.AddComponent(clipEntity, new EntityLinkAttachState
            {
                ResolvedTarget = Unity.Entities.Entity.Null,
                CapturedPreviousParent = Unity.Entities.Entity.Null,
                CapturedOriginalTransform = LocalTransform.Identity,
                IsAttached = false
            });

            base.Bake(clipEntity, context);
        }
    }
}