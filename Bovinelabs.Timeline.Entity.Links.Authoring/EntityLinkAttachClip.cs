using BovineLabs.Core.Keys;
using BovineLabs.EntityLinks;
using BovineLabs.Timeline.Authoring;
using BovineLabs.Timeline.Instantiate;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    public class EntityLinkAttachClip : DOTSClip, ITimelineClipAsset
    {
        [K(nameof(EntityLinkKeys))] public byte linkKey;

        public ResolveRule resolveRule = ResolveRule.Parent | ResolveRule.Owner;

        public ParentTransformConfig transformConfig =
            ParentTransformConfig.SetParent | ParentTransformConfig.SetTransform;

        public override double duration => 1;
        public ClipCaps clipCaps => ClipCaps.None;

        public override void Bake(Entity clipEntity, BakingContext context)
        {
            context.Baker.AddComponent(clipEntity, new EntityLinkAttachState
            {
                LinkKey = linkKey,
                ResolveRule = resolveRule,
                TransformConfig = transformConfig,
                CapturedPreviousParent = Entity.Null,
                CapturedOriginalTransform = LocalTransform.Identity,
                WasSuccessfullyAttached = false,
                ResolvedTarget = Entity.Null
            });

            base.Bake(clipEntity, context);
        }
    }
}