using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks.Data
{
    public struct EntityLinkBlob
    {
        public BlobArray<EntityLinkBlobElement> Elements;
    }

    public struct EntityLinkBlobElement
    {
        public ulong Key;
        public ushort Index;
    }
}
