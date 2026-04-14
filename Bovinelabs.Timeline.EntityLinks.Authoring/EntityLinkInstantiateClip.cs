using BovineLabs.Core.Authoring.ObjectManagement;
using BovineLabs.Timeline.Authoring;
using Bovinelabs.Timeline.EntityLinks.Data;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Entities;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    public sealed class EntityLinkInstantiateClip : DOTSClip, ITimelineClipAsset
    {
        public ObjectDefinition objectDefinition;
        public EntityLinkTagSchema entityLinkTagSchema;
        public ResolveRule resolveRule = ResolveRule.Parent;
        public override double duration => 1;

        public ClipCaps clipCaps => ClipCaps.None;

        public override void Bake(Entity clipEntity, BakingContext context)
        {
            if (context.Binding != null && context.Binding.Target != Entity.Null)
                context.Baker.AddTransformUsageFlags(context.Binding.Target, TransformUsageFlags.None);

            context.Baker.AddComponent(clipEntity, new EntityLinkInstantiateConfig
            {
                Prefab = objectDefinition,
                LinkKey = entityLinkTagSchema.id,
                ResolveRule = resolveRule
            });

            base.Bake(clipEntity, context);
        }
    }
}