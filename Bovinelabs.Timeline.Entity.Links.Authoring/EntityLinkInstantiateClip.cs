using BovineLabs.Timeline.Authoring;
using Bovinelabs.Timeline.Entity.Links.Data;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Timeline;

namespace Bovinelabs.Timeline.Entity.Links.Authoring
{
    public sealed class EntityLinkInstantiateClip : DOTSClip, ITimelineClipAsset
    {
        public GameObject Prefab;
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

            context.Baker.AddComponent(clipEntity, new EntityLinkInstantiateConfig
            {
                Prefab = context.Baker.GetEntity(this.Prefab, TransformUsageFlags.Dynamic),
                LinkKey = this.LinkSchema != null ? this.LinkSchema.Id : (byte)0,
                ResolveRule = this.ResolveRule,
                TransformFlags = this.TransformFlags
            });

            base.Bake(clipEntity, context);
        }
    }
}