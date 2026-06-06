using BovineLabs.Core.Authoring.EntityCommands;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.Authoring;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    public sealed class EntityLinkTargetPatchClip : DOTSClip, ITimelineClipAsset
    {
        public EntityLinkSchema Link;
        public Target ReadRootFrom = Target.Source;
        public Target WriteTo = Target.Target;
        public Target Fallback = Target.Target;

        public override double duration => 1;
        public ClipCaps clipCaps => ClipCaps.None;

        public override void Bake(Entity clipEntity, BakingContext context)
        {
            var commands = new BakerCommands(context.Baker, clipEntity);

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

            commands.AddComponent(new EntityLinkTargetPatch
            {
                ReadRootFrom = ReadRootFrom,
                LinkKey = key,
                WriteTo = WriteTo,
                Fallback = Fallback
            });

            base.Bake(clipEntity, context);
        }
    }
}