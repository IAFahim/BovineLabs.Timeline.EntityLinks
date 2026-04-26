using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks.Data
{
    [InternalBufferCapacity(8)]
    public struct EntityLinkValue : IBufferElementData
    {
        public Entity Value;
    }
}
