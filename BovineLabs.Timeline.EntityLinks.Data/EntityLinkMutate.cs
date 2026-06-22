using BovineLabs.Reaction.Data.Core;
using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks.Data
{
    public enum EntityLinkMutateMode : byte
    {
        Assign = 0,
        Swap = 1,
        Remove = 2
    }

    public struct EntityLinkMutate : IComponentData
    {
        public EntityLinkMutateMode Mode;
        public Target ReadRootFrom;
        public ushort LinkKey;

        public Target NewTarget;

        public ushort SwapKey;
    }
}