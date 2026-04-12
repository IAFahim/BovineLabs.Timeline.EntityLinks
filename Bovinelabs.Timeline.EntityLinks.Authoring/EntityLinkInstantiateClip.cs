using BovineLabs.Core.Authoring;
using BovineLabs.Timeline.Authoring;
using Bovinelabs.Timeline.EntityLinks.Authoring;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    public sealed class EntityLinkInstantiateClip : DOTSClip, ITimelineClipAsset
    {
        public GameObject Prefab;
        public EntityLinkTagSchema LinkSchema;
        public ResolveRule ResolveRule = ResolveRule.Parent;

        public AttachmentTransformFlags TransformFlags = AttachmentTransformFlags.SetParent | AttachmentTransformFlags.SetTransform;

        public override double duration => 1;

        public ClipCaps clipCaps => ClipCaps.None;

        public override void Bake(Entity clipEntity, BakingContext context)
        {
            if (context.Binding != null && context.Binding.Target != Entity.Null)
            {
                context.Baker.AddTransformUsageFlags(context.Binding.Target, TransformUsageFlags.Dynamic);
            }

            context.Baker.AddComponent(clipEntity, new EntityLinkInstantiateConfig
            {
                Prefab = context.Baker.GetEntity(Prefab, TransformUsageFlags.Dynamic),
                LinkKey = LinkSchema != null ? LinkSchema.Id : (byte)0,
                ResolveRule = ResolveRule,
                TransformFlags = TransformFlags
            });

            base.Bake(clipEntity, context);
        }
    }
}