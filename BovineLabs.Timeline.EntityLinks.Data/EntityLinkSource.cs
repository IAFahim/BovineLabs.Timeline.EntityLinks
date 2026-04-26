using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks.Data
{
    public struct EntityLinkSource : IComponentData
    {
        public Entity Root;
    }
}