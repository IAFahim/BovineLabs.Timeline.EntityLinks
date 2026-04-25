using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks.Data
{
    public struct EntityLinkElement : IBufferElementData
    {
        public byte Tag;
        public Entity Value;
    }

    public static class EntityLinkBufferExtensions
    {
        public static bool TryGetLink(
            this DynamicBuffer<EntityLinkElement> links,
            byte tag,
            out Entity value)
        {
            for (var i = 0; i < links.Length; i++)
            {
                var link = links[i];
                if (link.Tag != tag)
                    continue;

                value = link.Value;
                return value != Entity.Null;
            }

            value = Entity.Null;
            return false;
        }
    }
}