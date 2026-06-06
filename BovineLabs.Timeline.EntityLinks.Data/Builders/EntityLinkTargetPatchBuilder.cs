using BovineLabs.Core.EntityCommands;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.EntityLinks.Data;

namespace BovineLabs.Timeline.EntityLinks.Data.Builders
{
    public struct EntityLinkTargetPatchBuilder
    {
        public Target ReadRootFrom;
        public ushort LinkKey;
        public Target WriteTo;
        public Target Fallback;

        public void ApplyTo<T>(ref T builder)
            where T : struct, IEntityCommands
        {
            builder.AddComponent(new EntityLinkTargetPatch
            {
                ReadRootFrom = ReadRootFrom,
                LinkKey = LinkKey,
                WriteTo = WriteTo,
                Fallback = Fallback
            });
        }
    }
}