using BovineLabs.Core.Authoring.EntityCommands;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.Authoring;
using BovineLabs.Timeline.EntityLinks.Data;
using BovineLabs.Timeline.EntityLinks.Data.Builders;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    public sealed class EntityLinkMutateClip : DOTSClip, ITimelineClipAsset
    {
        [Header("Operation")] public EntityLinkMutateMode mode = EntityLinkMutateMode.Assign;

        [Header("Link")] public EntityLinkSchema link;

        public Target readRootFrom = Target.Source;

        [Header("Assign / Swap Target")] public Target newTarget = Target.Target;

        [Header("Swap")]
        [Tooltip("Second link key for swap operations. The entity at this key is swapped with the entity at Link.")]
        public EntityLinkSchema swapLink;

        public override double duration => 1;
        public ClipCaps clipCaps => ClipCaps.None;

        public override void Bake(Entity clipEntity, BakingContext context)
        {
            if (!EntityLinkAuthoringUtility.TryGetKey(link, out var key))
            {
                Debug.LogError($"{nameof(EntityLinkMutateClip)} '{name}' missing link schema.");
                return;
            }

            EntityLinkAuthoringUtility.TryGetKey(swapLink, out var swapKey);

            var builder = new EntityLinkMutateBuilder
            {
                Mode = mode,
                ReadRootFrom = readRootFrom,
                LinkKey = key,
                NewTarget = newTarget,
                SwapKey = swapKey
            };
            var commands = new BakerCommands(context.Baker, clipEntity);
            builder.ApplyTo(ref commands);

            base.Bake(clipEntity, context);
        }
    }
}