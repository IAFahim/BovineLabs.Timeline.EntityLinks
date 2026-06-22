using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Timeline.EntityLinks.Data
{
    [BurstCompile]
    public static class EntityLinkTransform
    {
        public static bool TryComposeChildLocal(
            LocalTransform current,
            float4x4 sourceLtw,
            bool copyPosition,
            bool copyRotation,
            float3 positionOffset,
            quaternion rotationOffset,
            bool hasParent,
            float4x4 parentLtw,
            out LocalTransform result)
        {
            result = current;

            var sourceWorld = LocalTransform.FromMatrix(sourceLtw);
            var desiredWorldPos = sourceWorld.Position;
            var desiredWorldRot = sourceWorld.Rotation;

            if (copyPosition && math.lengthsq(positionOffset) > 0)
                desiredWorldPos += math.rotate(desiredWorldRot, positionOffset);

            if (copyRotation && math.lengthsq(rotationOffset.value) > 1e-6f &&
                !rotationOffset.Equals(quaternion.identity))
                desiredWorldRot = math.mul(desiredWorldRot, rotationOffset);

            if (hasParent && math.abs(math.determinant(parentLtw)) > 1e-12f)
            {
                var parentInverse = math.inverse(parentLtw);

                if (copyPosition) result.Position = math.transform(parentInverse, desiredWorldPos);

                if (copyRotation)
                {
                    var parentWorld = LocalTransform.FromMatrix(parentLtw);
                    result.Rotation = math.mul(math.inverse(parentWorld.Rotation), desiredWorldRot);
                }

                return true;
            }

            if (copyPosition) result.Position = desiredWorldPos;

            if (copyRotation) result.Rotation = desiredWorldRot;

            return true;
        }
    }
}
