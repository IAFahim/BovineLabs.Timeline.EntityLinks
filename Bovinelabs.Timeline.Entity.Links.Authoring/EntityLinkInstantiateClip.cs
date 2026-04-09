using BovineLabs.Core.Keys;
using BovineLabs.EntityLinks;
using BovineLabs.Timeline.Authoring;
using BovineLabs.Timeline.Instantiate;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    public class EntityLinkInstantiateClip : DOTSClip, ITimelineClipAsset
    {
        public GameObject prefab;

        [K(nameof(EntityLinkKeys))] 
        public byte linkKey;
        
        public ResolveRule resolveRule = ResolveRule.Parent | ResolveRule.Owner;

        public ParentTransformConfig parentTransformConfig = 
            ParentTransformConfig.SetParent | ParentTransformConfig.SetTransform;

        public override double duration => 1;
        public ClipCaps clipCaps => ClipCaps.None;

        public override void Bake(Entity clipEntity, BakingContext context)
        {
            var prefabEntity = context.Baker.GetEntity(prefab, TransformUsageFlags.None);

            context.Baker.AddComponent(clipEntity, new EntityLinkInstantiateConfig
            {
                Prefab = prefabEntity,
                LinkKey = linkKey,
                ResolveRule = resolveRule,
                TransformConfig = parentTransformConfig
            });

            // Add the trigger tag
            context.Baker.AddComponent<OnClipActiveEntityLinkInstantiateTag>(clipEntity);

            base.Bake(clipEntity, context);
        }
    }
}