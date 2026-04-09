using BovineLabs.Core.Keys;
using BovineLabs.EntityLinks;
using BovineLabs.Timeline.Authoring;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    public class EntityLinkAttachClip : DOTSClip, ITimelineClipAsset
    {
        [K(nameof(EntityLinkKeys))] 
        public byte linkKey;
        
        public ResolveRule resolveRule = ResolveRule.Parent | ResolveRule.Owner;

        public override double duration => 1;
        public ClipCaps clipCaps => ClipCaps.None; // Discrete state, no blending

        public override void Bake(Entity clipEntity, BakingContext context)
        {
            context.Baker.AddComponent(clipEntity, new EntityLinkAttachState
            {
                LinkKey = linkKey,
                ResolveRule = resolveRule,
                // Initialize state captures as empty; the System populates these on frame 1
                CapturedPreviousParent = Entity.Null,
                CapturedOriginalTransform = LocalTransform.Identity,
                WasSuccessfullyAttached = false
            });

            base.Bake(clipEntity, context);
        }
    }
}