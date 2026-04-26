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
            if (!EntityLinkAuthoringUtility.TryGetKey(this.Link, out ushort key))
            {
                Debug.LogError($"{nameof(EntityLinkTargetPatchClip)} '{this.name}' missing link schema.");
                return;
            }

            if (this.WriteTo is Target.None or Target.Self)
            {
                Debug.LogError($"{nameof(EntityLinkTargetPatchClip)} '{this.name}' cannot write to '{this.WriteTo}'.");
                return;
            }

            context.Baker.AddComponent(clipEntity, new EntityLinkTargetPatch
            {
                ReadRootFrom = this.ReadRootFrom,
                LinkKey = key,
                WriteTo = this.WriteTo,
                Fallback = this.Fallback
            });

            base.Bake(clipEntity, context);
        }
    }
}
