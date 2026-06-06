using BovineLabs.Core.EntityCommands;
using BovineLabs.Reaction.Data.Core;

namespace BovineLabs.Timeline.EntityLinks.Data.Builders
{
    public struct EntityLinkMutateBuilder
    {
        public EntityLinkMutateMode Mode;
        public Target ReadRootFrom;
        public ushort LinkKey;
        public Target NewTarget;
        public ushort SwapKey;

        public void ApplyTo<T>(ref T builder)
            where T : struct, IEntityCommands
        {
            builder.AddComponent(new EntityLinkMutate
            {
                Mode = Mode,
                ReadRootFrom = ReadRootFrom,
                LinkKey = LinkKey,
                NewTarget = NewTarget,
                SwapKey = SwapKey
            });
        }
    }
}