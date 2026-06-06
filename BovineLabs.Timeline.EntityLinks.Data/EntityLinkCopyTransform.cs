using BovineLabs.Reaction.Data.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Timeline.EntityLinks.Data
{
    public struct EntityLinkCopyTransform : IComponentData
    {
        public Target EntityToMove;
        public Target ReadRootFrom;
        public ushort LinkKey;

        public bool CopyPosition;
        public bool CopyRotation;

        public float3 PositionOffset;
        public quaternion RotationOffset;
    }
}