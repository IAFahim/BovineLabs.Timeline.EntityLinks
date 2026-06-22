using BovineLabs.Timeline.EntityLinks.Data;
using NUnit.Framework;
using Unity.Mathematics;

namespace BovineLabs.Timeline.EntityLinks.Tests
{
    public class EntityLinkParentRecoveryTests
    {
        [Test]
        public void NoPtm_ReturnsSelf()
        {
            var selfLtw = float4x4.TRS(new float3(3, -1, 2), quaternion.identity, new float3(1, 1, 1));

            var recovered = EntityLinkParentRecovery.TryRecoverRigid(selfLtw, false, float4x4.identity, out var rigid);

            Assert.IsFalse(recovered);
            AssertEqual(selfLtw, rigid);
        }

        [Test]
        public void IdentityPtm_StripsToSelf()
        {
            var selfLtw = float4x4.TRS(new float3(5, 6, 7), quaternion.RotateY(0.5f), new float3(1, 1, 1));

            var recovered = EntityLinkParentRecovery.TryRecoverRigid(selfLtw, true, float4x4.identity, out var rigid);

            Assert.IsTrue(recovered);
            AssertEqual(selfLtw, rigid);
        }

        [Test]
        public void ValidPtm_Strips()
        {
            var selfLtw = float4x4.TRS(new float3(2, 0, -4), quaternion.RotateZ(0.25f), new float3(2, 3, 4));
            var ptm = float4x4.Scale(2, 3, 4);

            var recovered = EntityLinkParentRecovery.TryRecoverRigid(selfLtw, true, ptm, out var rigid);

            Assert.IsTrue(recovered);
            AssertEqual(math.mul(selfLtw, math.inverse(ptm)), rigid);
        }

        [Test]
        public void DegeneratePtm_FallsThrough()
        {
            var selfLtw = float4x4.TRS(new float3(1, 2, 3), quaternion.identity, new float3(1, 1, 1));
            var ptm = float4x4.Scale(0, 1, 1);

            var recovered = EntityLinkParentRecovery.TryRecoverRigid(selfLtw, true, ptm, out var rigid);

            Assert.IsFalse(recovered);
            AssertEqual(selfLtw, rigid);
        }

        [Test]
        public void NonFinitePtm_FallsThrough()
        {
            var selfLtw = float4x4.TRS(new float3(1, 2, 3), quaternion.identity, new float3(1, 1, 1));
            var ptm = new float4x4(
                new float4(float.NaN, 0, 0, 0),
                new float4(0, 1, 0, 0),
                new float4(0, 0, 1, 0),
                new float4(0, 0, 0, 1));

            var recovered = EntityLinkParentRecovery.TryRecoverRigid(selfLtw, true, ptm, out var rigid);

            Assert.IsFalse(recovered);
            AssertEqual(selfLtw, rigid);
            Assert.IsTrue(math.all(math.isfinite(rigid.c0)) && math.all(math.isfinite(rigid.c1)) &&
                          math.all(math.isfinite(rigid.c2)) && math.all(math.isfinite(rigid.c3)));
        }

        private static void AssertEqual(in float4x4 expected, in float4x4 actual)
        {
            Assert.AreEqual(expected.c0.x, actual.c0.x, 1e-5f);
            Assert.AreEqual(expected.c0.y, actual.c0.y, 1e-5f);
            Assert.AreEqual(expected.c0.z, actual.c0.z, 1e-5f);
            Assert.AreEqual(expected.c0.w, actual.c0.w, 1e-5f);
            Assert.AreEqual(expected.c1.x, actual.c1.x, 1e-5f);
            Assert.AreEqual(expected.c1.y, actual.c1.y, 1e-5f);
            Assert.AreEqual(expected.c1.z, actual.c1.z, 1e-5f);
            Assert.AreEqual(expected.c1.w, actual.c1.w, 1e-5f);
            Assert.AreEqual(expected.c2.x, actual.c2.x, 1e-5f);
            Assert.AreEqual(expected.c2.y, actual.c2.y, 1e-5f);
            Assert.AreEqual(expected.c2.z, actual.c2.z, 1e-5f);
            Assert.AreEqual(expected.c2.w, actual.c2.w, 1e-5f);
            Assert.AreEqual(expected.c3.x, actual.c3.x, 1e-5f);
            Assert.AreEqual(expected.c3.y, actual.c3.y, 1e-5f);
            Assert.AreEqual(expected.c3.z, actual.c3.z, 1e-5f);
            Assert.AreEqual(expected.c3.w, actual.c3.w, 1e-5f);
        }
    }
}
