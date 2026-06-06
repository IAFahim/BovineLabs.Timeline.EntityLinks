using BovineLabs.Core.EntityCommands;
using BovineLabs.Reaction.Data.Core;
using Unity.Mathematics;

namespace BovineLabs.Timeline.EntityLinks.Data.Builders
{
    public struct EntityLinkCopyTransformBuilder
    {
        public Target EntityToMove;
        public Target ReadRootFrom;
        public ushort LinkKey;
        public bool CopyPosition;
        public bool CopyRotation;
        public float3 PositionOffset;
        public quaternion RotationOffset;

        public void ApplyTo<T>(ref T builder)
            where T : struct, IEntityCommands
        {
            builder.AddComponent(new EntityLinkCopyTransform
            {
                EntityToMove = EntityToMove,
                ReadRootFrom = ReadRootFrom,
                LinkKey = LinkKey,
                CopyPosition = CopyPosition,
                CopyRotation = CopyRotation,
                PositionOffset = PositionOffset,
                RotationOffset = RotationOffset
            });
        }
    }
}