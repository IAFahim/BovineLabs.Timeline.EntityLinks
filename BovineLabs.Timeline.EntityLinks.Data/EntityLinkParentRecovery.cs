using Unity.Mathematics;

namespace BovineLabs.Timeline.EntityLinks.Data
{
    public static class EntityLinkParentRecovery
    {
        public const float MinDeterminant = 1e-12f;

        public static bool TryRecoverRigid(in float4x4 selfLtw, bool hasPtm, in float4x4 ptm, out float4x4 rigid)
        {
            rigid = selfLtw;

            if (!hasPtm || math.abs(math.determinant(ptm)) <= MinDeterminant)
                return false;

            var candidate = math.mul(selfLtw, math.inverse(ptm));
            if (!IsFinite(candidate))
                return false;

            rigid = candidate;
            return true;
        }

        private static bool IsFinite(in float4x4 m)
        {
            return math.all(math.isfinite(m.c0)) && math.all(math.isfinite(m.c1)) &&
                   math.all(math.isfinite(m.c2)) && math.all(math.isfinite(m.c3));
        }
    }
}
