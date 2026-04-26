using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks.Data
{
    public struct EntityLinkMap : IComponentData
    {
        public BlobAssetReference<EntityLinkBlob> Blob;
    }
}
