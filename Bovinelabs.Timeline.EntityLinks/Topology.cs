using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Timeline.EntityLinks
{
    public struct TopologyResult
    {
        public LocalTransform Local;
        public float4x4 PostTransform;
        public bool HasPostTransform;
    }

    public static class Topology
    {
        /// <summary>
        /// Highly optimized, branchless (where possible) topology matrix resolution.
        /// </summary>
        public static TopologyResult Evaluate(
            LocalTransform local,
            float4x4 currentPtm,
            bool hadPtm,
            LocalToWorld originWorld,
            LocalToWorld destinationWorld,
            AttachmentTransformFlags flags)
        {
            var result = new TopologyResult
            {
                Local = local,
                PostTransform = currentPtm,
                HasPostTransform = hadPtm
            };

            if (flags.HasAny(AttachmentTransformFlags.SetParent))
            {
                if (flags.HasAny(AttachmentTransformFlags.SetTransform))
                {
                    // Snaps position/rotation to Destination, but preserves Origin's World Scale
                    result.Local.Position = float3.zero;
                    result.Local.Rotation = quaternion.identity;
                    result.Local.Scale = 1f;

                    var originScale = ExtractScale(originWorld.Value);
                    var destScale = ExtractScale(destinationWorld.Value);

                    var compensatedScale = originScale / math.max(destScale, 1e-6f);

                    result.PostTransform = float4x4.Scale(compensatedScale);
                    result.HasPostTransform = true;
                }
                else
                {
                    // Maintains exact world position/rotation/scale despite new parent
                    var localMatrix = math.mul(math.inverse(destinationWorld.Value), originWorld.Value);

                    result.Local.Position = localMatrix.c3.xyz;

                    var c0 = localMatrix.c0.xyz;
                    var c1 = localMatrix.c1.xyz;
                    var c2 = localMatrix.c2.xyz;
                    var scale = new float3(math.length(c0), math.length(c1), math.length(c2));

                    // Normalize columns to prevent rotational shearing
                    var rotMatrix = new float4x4(
                        new float4(c0 / math.max(scale.x, 1e-6f), 0),
                        new float4(c1 / math.max(scale.y, 1e-6f), 0),
                        new float4(c2 / math.max(scale.z, 1e-6f), 0),
                        new float4(0, 0, 0, 1)
                    );

                    result.Local.Rotation = new quaternion(rotMatrix);
                    result.Local.Scale = 1f;
                    result.PostTransform = float4x4.Scale(scale);
                    result.HasPostTransform = true;
                }
            }
            else if (flags.HasAny(AttachmentTransformFlags.SetTransform))
            {
                result.Local.Position = destinationWorld.Position;

                var c0 = destinationWorld.Value.c0.xyz;
                var c1 = destinationWorld.Value.c1.xyz;
                var c2 = destinationWorld.Value.c2.xyz;
                var scale = new float3(math.length(c0), math.length(c1), math.length(c2));

                var rotMatrix = new float4x4(
                    new float4(c0 / math.max(scale.x, 1e-6f), 0),
                    new float4(c1 / math.max(scale.y, 1e-6f), 0),
                    new float4(c2 / math.max(scale.z, 1e-6f), 0),
                    new float4(0, 0, 0, 1)
                );

                result.Local.Rotation = new quaternion(rotMatrix);
                result.Local.Scale = 1f;
                result.PostTransform = float4x4.Scale(scale);
                result.HasPostTransform = true;
            }

            return result;
        }

        private static float3 ExtractScale(float4x4 m)
        {
            return new float3(math.length(m.c0.xyz), math.length(m.c1.xyz), math.length(m.c2.xyz));
        }
    }
}