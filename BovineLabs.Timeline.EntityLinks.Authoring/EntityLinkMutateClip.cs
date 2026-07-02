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
        [Header("Operation")]
        [Tooltip(
            "Assign / Swap / Remove. These are PERMANENT runtime mutations to the link map: the change is applied once when the clip starts and is NOT auto-restored when the clip ends (unlike the Parent clip's restoreOnEnd). Undo it with a compensating clip.")]
        public EntityLinkMutateMode mode = EntityLinkMutateMode.Assign;

        [Header("Link")] [Tooltip("The link key whose entry in the link map is mutated.")]
        public EntityLinkSchema link;

        [Tooltip("Which Targets slot to read the link-map root from.")]
        public Target readRootFrom = Target.Source;

        [Header("Assign Target")]
        [Tooltip("Entity written into the link on Assign. Ignored on Swap and Remove.")]
        public Target newTarget = Target.Target;

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

            if (!EntityLinkAuthoringUtility.TryGetKey(swapLink, out var swapKey) && mode == EntityLinkMutateMode.Swap)
            {
                Debug.LogError($"{nameof(EntityLinkMutateClip)} '{name}' Swap mode requires a swap link.");
                return;
            }

            if (mode == EntityLinkMutateMode.Swap && key == swapKey)
            {
                Debug.LogError($"{nameof(EntityLinkMutateClip)} '{name}' Swap requires two different link schemas (link and swapLink are the same).");
                return;
            }

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