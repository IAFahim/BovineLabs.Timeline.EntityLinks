using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks.Data
{
    public struct EntityLinkEntry : IBufferElementData
    {
        public ushort Key;
        public Entity Target;
    }
}