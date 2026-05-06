using System;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.EntityLinks.Data;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Timeline.EntityLinks.Tests
{
    [TestFixture]
    public class EntityLinkDataTests
    {
        [Test]
        public void EntityLinkEntry_Defaults()
        {
            var entry = new EntityLinkEntry();
            Assert.AreEqual(0, entry.Key);
            Assert.AreEqual(Entity.Null, entry.Target);
        }

        [Test]
        public void EntityLinkEntry_AssignedValues()
        {
            var entity = new Entity { Index = 7, Version = 3 };
            var entry = new EntityLinkEntry { Key = 42, Target = entity };
            Assert.AreEqual(42, entry.Key);
            Assert.AreEqual(entity, entry.Target);
        }

        [Test]
        public void EntityLinkSource_DefaultRootIsNull()
        {
            var source = new EntityLinkSource();
            Assert.AreEqual(Entity.Null, source.Root);
        }

        [Test]
        public void EntityLinkSource_AssignedRoot()
        {
            var entity = new Entity { Index = 1, Version = 1 };
            var source = new EntityLinkSource { Root = entity };
            Assert.AreEqual(entity, source.Root);
        }

        [Test]
        public void EntityLinkTargetPatch_Defaults()
        {
            var patch = new EntityLinkTargetPatch();
            Assert.AreEqual(Target.None, patch.ReadRootFrom);
            Assert.AreEqual(0, patch.LinkKey);
            Assert.AreEqual(Target.None, patch.WriteTo);
            Assert.AreEqual(Target.None, patch.Fallback);
        }

        [Test]
        public void EntityLinkTargetPatch_LinkKeyAssignment()
        {
            var patch = new EntityLinkTargetPatch { LinkKey = 10 };
            Assert.AreEqual(10, patch.LinkKey);
        }

        [Test]
        public void EntityLinkTargetPatch_AllFieldsSet()
        {
            var patch = new EntityLinkTargetPatch
            {
                ReadRootFrom = default,
                LinkKey = 5,
                WriteTo = default,
                Fallback = default
            };
            Assert.AreEqual(5, patch.LinkKey);
        }

        [Test]
        public void EntityLinkMutate_Defaults()
        {
            var mutate = new EntityLinkMutate();
            Assert.AreEqual(EntityLinkMutateMode.Assign, mutate.Mode);
            Assert.AreEqual(Target.None, mutate.ReadRootFrom);
            Assert.AreEqual(0, mutate.LinkKey);
            Assert.AreEqual(Target.None, mutate.NewTarget);
            Assert.AreEqual(0, mutate.SwapKey);
        }

        [Test]
        public void EntityLinkMutate_AssignedValues()
        {
            var mutate = new EntityLinkMutate
            {
                Mode = EntityLinkMutateMode.Swap,
                LinkKey = 5,
                SwapKey = 6
            };
            Assert.AreEqual(EntityLinkMutateMode.Swap, mutate.Mode);
            Assert.AreEqual(5, mutate.LinkKey);
            Assert.AreEqual(6, mutate.SwapKey);
        }

        [Test]
        public void EntityLinkMutateMode_Values()
        {
            Assert.AreEqual(0, (int)EntityLinkMutateMode.Assign);
            Assert.AreEqual(1, (int)EntityLinkMutateMode.Swap);
            Assert.AreEqual(2, (int)EntityLinkMutateMode.Remove);
        }

        [Test]
        public void EntityLinkMutateMode_AllModesDistinct()
        {
            var values = Enum.GetValues(typeof(EntityLinkMutateMode));
            Assert.AreEqual(3, values.Length);
        }

        [Test]
        public void EntityLinkParentData_Defaults()
        {
            var data = new EntityLinkParentData();
            Assert.AreEqual(Target.None, data.EntityToParent);
            Assert.AreEqual(Target.None, data.ReadRootFrom);
            Assert.AreEqual(0, data.ParentLinkKey);
            Assert.AreEqual(float3.zero, data.LocalPosition);
            Assert.AreEqual(default(quaternion).value, data.LocalRotation.value);
            Assert.IsFalse(data.RestoreOnEnd);
        }

        [Test]
        public void EntityLinkParentData_PositionRotationAssignment()
        {
            var rot = quaternion.EulerXYZ(1f, 2f, 3f);
            var data = new EntityLinkParentData
            {
                ParentLinkKey = 7,
                LocalPosition = new float3(1, 2, 3),
                LocalRotation = rot,
                RestoreOnEnd = true
            };
            Assert.AreEqual(7, data.ParentLinkKey);
            Assert.AreEqual(new float3(1, 2, 3), data.LocalPosition);
            Assert.AreEqual(rot.value, data.LocalRotation.value);
            Assert.IsTrue(data.RestoreOnEnd);
        }

        [Test]
        public void EntityLinkParentState_Defaults()
        {
            var state = new EntityLinkParentState();
            Assert.AreEqual(Entity.Null, state.Target);
            Assert.AreEqual(Entity.Null, state.PreviousParent);
            Assert.IsFalse(state.HadParent);
            Assert.IsFalse(state.ParentApplied);
        }

        [Test]
        public void EntityLinkParentState_AssignedValues()
        {
            var target = new Entity { Index = 5, Version = 1 };
            var prev = new Entity { Index = 3, Version = 2 };
            var state = new EntityLinkParentState
            {
                Target = target,
                PreviousParent = prev,
                HadParent = true,
                ParentApplied = true
            };
            Assert.AreEqual(target, state.Target);
            Assert.AreEqual(prev, state.PreviousParent);
            Assert.IsTrue(state.HadParent);
            Assert.IsTrue(state.ParentApplied);
        }

        [Test]
        public void EntityLinkEntry_IsValueType()
        {
            Assert.IsTrue(typeof(EntityLinkEntry).IsValueType);
        }

        [Test]
        public void EntityLinkSource_IsValueType()
        {
            Assert.IsTrue(typeof(EntityLinkSource).IsValueType);
        }

        [Test]
        public void EntityLinkMutate_IsValueType()
        {
            Assert.IsTrue(typeof(EntityLinkMutate).IsValueType);
        }

        [Test]
        public void EntityLinkTargetPatch_IsValueType()
        {
            Assert.IsTrue(typeof(EntityLinkTargetPatch).IsValueType);
        }

        [Test]
        public void EntityLinkParentData_IsValueType()
        {
            Assert.IsTrue(typeof(EntityLinkParentData).IsValueType);
        }

        [Test]
        public void EntityLinkParentState_IsValueType()
        {
            Assert.IsTrue(typeof(EntityLinkParentState).IsValueType);
        }
    }
}