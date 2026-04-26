using System;
using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks.Data
{
    [InternalBufferCapacity(0)]
    public struct EntityLink : IBufferElementData, IComparable<EntityLink>
    {
        public ushort Key;
        public Entity Target;

        public int CompareTo(EntityLink other)
        {
            return this.Key.CompareTo(other.Key);
        }
    }
}
