using BovineLabs.Core.Authoring.EntityCommands;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.Authoring;
using BovineLabs.Timeline.EntityLinks.Data.Builders;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    public sealed class EntityLinkTargetPatchClip : DOTSClip, ITimelineClipAsset
    {
        [Tooltip(
            "The link key whose entity is written into the Targets slot. PERMANENT: re-points the slot once at clip start and is NOT auto-restored when the clip ends.")]
        public EntityLinkSchema Link;

        [Tooltip("Which Targets slot to read the link-map root from.")]
        public Target ReadRootFrom = Target.Source;

        [Tooltip("Which Targets slot is overwritten with the linked entity. Cannot be None or Self.")]
        public Target WriteTo = Target.Target;

        [Tooltip("Entity written to the slot when the link resolves to Null.")]
        public Target Fallback = Target.Target;

        public override double duration => 1;
        public ClipCaps clipCaps => ClipCaps.None;

        public override void Bake(Entity clipEntity, BakingContext context)
        {
            if (!EntityLinkAuthoringUtility.TryGetKey(Link, out var key))
            {
                Debug.LogError($"{nameof(EntityLinkTargetPatchClip)} '{name}' missing link schema.");
                return;
            }

            if (WriteTo is Target.None or Target.Self)
            {
                Debug.LogError($"{nameof(EntityLinkTargetPatchClip)} '{name}' cannot write to '{WriteTo}'.");
                return;
            }

            var builder = new EntityLinkTargetPatchBuilder
            {
                ReadRootFrom = ReadRootFrom,
                LinkKey = key,
                WriteTo = WriteTo,
                Fallback = Fallback
            };
            var commands = new BakerCommands(context.Baker, clipEntity);
            builder.ApplyTo(ref commands);

            base.Bake(clipEntity, context);
        }
    }
}