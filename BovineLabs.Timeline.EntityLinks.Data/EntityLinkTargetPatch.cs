using BovineLabs.Reaction.Data.Core;
using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks.Data
{
    public struct EntityLinkTargetPatch : IComponentData
    {
        public Target ReadRootFrom;
        public ushort LinkKey;
        public Target WriteTo;
        public Target Fallback;
    }
}