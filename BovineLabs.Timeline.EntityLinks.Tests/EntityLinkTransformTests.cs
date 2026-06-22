using BovineLabs.Timeline.EntityLinks.Data;
using NUnit.Framework;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Timeline.EntityLinks.Tests
{
    public class EntityLinkTransformTests
    {
        private const float Tolerance = 1e-4f;

        private static LocalTransform Identity => LocalTransform.Identity;

        private static float4x4 SourceLtw(float3 pos, quaternion rot)
        {
            return float4x4.TRS(pos, rot, new float3(1f, 1f, 1f));
        }

        private static void AssertApproximately(float3 expected, float3 actual)
        {
            Assert.That(math.distance(expected, actual), Is.LessThan(Tolerance));
        }

        private static void AssertApproximately(quaternion expected, quaternion actual)
        {
            Assert.That(math.abs(math.dot(expected.value, actual.value)), Is.GreaterThan(1f - Tolerance));
        }

        [Test]
        public void NoParent_CopyBoth_WritesWorldPose()
        {
            var pos = new float3(3f, -2f, 5f);
            var rot = quaternion.Euler(0.3f, 0.7f, -0.4f);

            var ok = EntityLinkTransform.TryComposeChildLocal(
                Identity, SourceLtw(pos, rot), true, true, float3.zero, quaternion.identity,
                false, float4x4.identity, out var result);

            Assert.IsTrue(ok);
            AssertApproximately(pos, result.Position);
            AssertApproximately(rot, result.Rotation);
        }

        [Test]
        public void PositionOffset_RotatedIntoWorld()
        {
            var pos = new float3(1f, 0f, 0f);
            var rot = quaternion.RotateY(math.PI / 2f);
            var offset = new float3(0f, 0f, 2f);

            EntityLinkTransform.TryComposeChildLocal(
                Identity, SourceLtw(pos, rot), true, false, offset, quaternion.identity,
                false, float4x4.identity, out var result);

            var expected = pos + math.rotate(rot, offset);
            AssertApproximately(expected, result.Position);
        }

        [Test]
        public void RotationOffset_IgnoredWhenIdentityOrTiny()
        {
            var pos = new float3(0f, 1f, 0f);
            var rot = quaternion.RotateX(0.5f);

            EntityLinkTransform.TryComposeChildLocal(
                Identity, SourceLtw(pos, rot), false, true, float3.zero, quaternion.identity,
                false, float4x4.identity, out var result);

            AssertApproximately(rot, result.Rotation);
        }

        [Test]
        public void Parent_DeconvertsToLocal()
        {
            var pos = new float3(4f, 1f, -3f);
            var rot = quaternion.Euler(0.2f, -0.6f, 0.1f);
            var parentLtw = float4x4.TRS(new float3(2f, 3f, 1f), quaternion.RotateZ(0.8f), new float3(1f, 1f, 1f));

            EntityLinkTransform.TryComposeChildLocal(
                Identity, SourceLtw(pos, rot), true, true, float3.zero, quaternion.identity,
                true, parentLtw, out var result);

            var expectedPos = math.transform(math.inverse(parentLtw), pos);
            var expectedRot = math.mul(math.inverse(LocalTransform.FromMatrix(parentLtw).Rotation), rot);

            AssertApproximately(expectedPos, result.Position);
            AssertApproximately(expectedRot, result.Rotation);
        }

        [Test]
        public void Parent_ScaleShearOnly_RotationUsesFromMatrix()
        {
            var rot = quaternion.RotateY(0.9f);
            var parentLtw = float4x4.TRS(float3.zero, quaternion.RotateY(0.4f), new float3(2f, 0.5f, 3f));

            EntityLinkTransform.TryComposeChildLocal(
                Identity, SourceLtw(float3.zero, rot), false, true, float3.zero, quaternion.identity,
                true, parentLtw, out var result);

            var expectedRot = math.mul(math.inverse(LocalTransform.FromMatrix(parentLtw).Rotation), rot);
            AssertApproximately(expectedRot, result.Rotation);
        }

        [Test]
        public void SingularParent_FallsThroughToWorld()
        {
            var pos = new float3(1f, 2f, 3f);
            var rot = quaternion.RotateX(0.3f);
            var parentLtw = float4x4.TRS(float3.zero, quaternion.identity, new float3(1f, 0f, 1f));

            var ok = EntityLinkTransform.TryComposeChildLocal(
                Identity, SourceLtw(pos, rot), true, true, float3.zero, quaternion.identity,
                true, parentLtw, out var result);

            Assert.IsTrue(ok);
            AssertApproximately(pos, result.Position);
            AssertApproximately(rot, result.Rotation);
            Assert.IsFalse(math.any(math.isnan(result.Position)));
            Assert.IsFalse(math.any(math.isnan(result.Rotation.value)));
        }

        [Test]
        public void CopyPositionOnly_LeavesRotation()
        {
            var pos = new float3(5f, 5f, 5f);
            var rot = quaternion.RotateZ(1.2f);
            var current = LocalTransform.FromRotation(quaternion.RotateX(0.7f));

            EntityLinkTransform.TryComposeChildLocal(
                current, SourceLtw(pos, rot), true, false, float3.zero, quaternion.identity,
                false, float4x4.identity, out var result);

            AssertApproximately(pos, result.Position);
            AssertApproximately(current.Rotation, result.Rotation);
        }

        [Test]
        public void CopyRotationOnly_LeavesPosition()
        {
            var pos = new float3(5f, 5f, 5f);
            var rot = quaternion.RotateZ(1.2f);
            var current = LocalTransform.FromPosition(new float3(-1f, -2f, -3f));

            EntityLinkTransform.TryComposeChildLocal(
                current, SourceLtw(pos, rot), false, true, float3.zero, quaternion.identity,
                false, float4x4.identity, out var result);

            AssertApproximately(current.Position, result.Position);
            AssertApproximately(rot, result.Rotation);
        }
    }
}
