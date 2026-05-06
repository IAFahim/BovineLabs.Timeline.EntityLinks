using BovineLabs.Testing;
using BovineLabs.Timeline.EntityLinks.Data;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks.Tests
{
    public class EntityLinkMutateTests : ECSTestsFixture
    {
        [Test]
        public void EntityLinkMutate_CanBeAddedToEntity()
        {
            var entity = Manager.CreateEntity();
            Manager.AddComponentData(entity, new EntityLinkMutate());
            Assert.IsTrue(Manager.HasComponent<EntityLinkMutate>(entity));
        }

        [Test]
        public void EntityLinkMutate_DefaultModeIsAssign()
        {
            var entity = Manager.CreateEntity();
            Manager.AddComponentData(entity, new EntityLinkMutate());
            var data = Manager.GetComponentData<EntityLinkMutate>(entity);
            Assert.AreEqual(EntityLinkMutateMode.Assign, data.Mode);
        }

        [Test]
        public void EntityLinkMutate_RoundtripValues()
        {
            var entity = Manager.CreateEntity();
            Manager.AddComponentData(entity, new EntityLinkMutate
            {
                Mode = EntityLinkMutateMode.Remove,
                LinkKey = 99,
                SwapKey = 10
            });

            var data = Manager.GetComponentData<EntityLinkMutate>(entity);
            Assert.AreEqual(EntityLinkMutateMode.Remove, data.Mode);
            Assert.AreEqual(99, data.LinkKey);
            Assert.AreEqual(10, data.SwapKey);
        }

        [Test]
        public void EntityLinkMutate_ModeSwap_Roundtrip()
        {
            var entity = Manager.CreateEntity();
            Manager.AddComponentData(entity, new EntityLinkMutate
            {
                Mode = EntityLinkMutateMode.Swap,
                LinkKey = 1,
                SwapKey = 2
            });

            var data = Manager.GetComponentData<EntityLinkMutate>(entity);
            Assert.AreEqual(EntityLinkMutateMode.Swap, data.Mode);
            Assert.AreEqual(1, data.LinkKey);
            Assert.AreEqual(2, data.SwapKey);
        }

        [Test]
        public void EntityLinkEntry_BufferCanBeAdded()
        {
            var entity = Manager.CreateEntity();
            var buffer = Manager.AddBuffer<EntityLinkEntry>(entity);
            Assert.IsTrue(Manager.HasBuffer<EntityLinkEntry>(entity));
            Assert.AreEqual(0, buffer.Length);
        }

        [Test]
        public void EntityLinkEntry_BufferAddEntries()
        {
            var entity = Manager.CreateEntity();
            var buffer = Manager.AddBuffer<EntityLinkEntry>(entity);

            var t1 = Manager.CreateEntity();
            var t2 = Manager.CreateEntity();
            buffer.Add(new EntityLinkEntry { Key = 1, Target = t1 });
            buffer.Add(new EntityLinkEntry { Key = 2, Target = t2 });

            Assert.AreEqual(2, buffer.Length);
            Assert.AreEqual(1, buffer[0].Key);
            Assert.AreEqual(t1, buffer[0].Target);
            Assert.AreEqual(2, buffer[1].Key);
            Assert.AreEqual(t2, buffer[1].Target);
        }

        [Test]
        public void EntityLinkEntry_BufferRemoveAt()
        {
            var entity = Manager.CreateEntity();
            var buffer = Manager.AddBuffer<EntityLinkEntry>(entity);
            buffer.Add(new EntityLinkEntry { Key = 1 });
            buffer.Add(new EntityLinkEntry { Key = 2 });
            buffer.Add(new EntityLinkEntry { Key = 3 });

            buffer.RemoveAt(1);
            Assert.AreEqual(2, buffer.Length);
            Assert.AreEqual(1, buffer[0].Key);
            Assert.AreEqual(3, buffer[1].Key);
        }

        [Test]
        public void EntityLinkEntry_BufferClear()
        {
            var entity = Manager.CreateEntity();
            var buffer = Manager.AddBuffer<EntityLinkEntry>(entity);
            buffer.Add(new EntityLinkEntry { Key = 1 });
            buffer.Add(new EntityLinkEntry { Key = 2 });

            buffer.Clear();
            Assert.AreEqual(0, buffer.Length);
        }

        [Test]
        public void EntityLinkEntry_BufferModifyInPlace()
        {
            var entity = Manager.CreateEntity();
            var buffer = Manager.AddBuffer<EntityLinkEntry>(entity);
            var target = Manager.CreateEntity();
            buffer.Add(new EntityLinkEntry { Key = 5, Target = Entity.Null });

            buffer[0] = new EntityLinkEntry { Key = 5, Target = target };
            Assert.AreEqual(target, buffer[0].Target);
        }

        [Test]
        public void EntityLinkEntry_DuplicateKeysAllowed()
        {
            var entity = Manager.CreateEntity();
            var buffer = Manager.AddBuffer<EntityLinkEntry>(entity);
            buffer.Add(new EntityLinkEntry { Key = 1, Target = Manager.CreateEntity() });
            buffer.Add(new EntityLinkEntry { Key = 1, Target = Manager.CreateEntity() });

            Assert.AreEqual(2, buffer.Length);
            Assert.AreEqual(1, buffer[0].Key);
            Assert.AreEqual(1, buffer[1].Key);
        }

        [Test]
        public void EntityLinkEntry_MaxKey()
        {
            var entity = Manager.CreateEntity();
            var buffer = Manager.AddBuffer<EntityLinkEntry>(entity);
            buffer.Add(new EntityLinkEntry { Key = ushort.MaxValue });

            Assert.AreEqual(ushort.MaxValue, buffer[0].Key);
        }

        [Test]
        public void EntityLinkSource_CanBeAddedToEntity()
        {
            var entity = Manager.CreateEntity();
            Manager.AddComponentData(entity, new EntityLinkSource());
            Assert.IsTrue(Manager.HasComponent<EntityLinkSource>(entity));
        }

        [Test]
        public void EntityLinkSource_Roundtrip()
        {
            var root = Manager.CreateEntity();
            var entity = Manager.CreateEntity();
            Manager.AddComponentData(entity, new EntityLinkSource { Root = root });

            var data = Manager.GetComponentData<EntityLinkSource>(entity);
            Assert.AreEqual(root, data.Root);
        }

        [Test]
        public void EntityLinkTargetPatch_CanBeAddedToEntity()
        {
            var entity = Manager.CreateEntity();
            Manager.AddComponentData(entity, new EntityLinkTargetPatch());
            Assert.IsTrue(Manager.HasComponent<EntityLinkTargetPatch>(entity));
        }

        [Test]
        public void EntityLinkTargetPatch_Roundtrip()
        {
            var entity = Manager.CreateEntity();
            Manager.AddComponentData(entity, new EntityLinkTargetPatch
            {
                LinkKey = 42
            });

            var data = Manager.GetComponentData<EntityLinkTargetPatch>(entity);
            Assert.AreEqual(42, data.LinkKey);
        }

        [Test]
        public void EntityLinkParentData_CanBeAddedToEntity()
        {
            var entity = Manager.CreateEntity();
            Manager.AddComponentData(entity, new EntityLinkParentData());
            Assert.IsTrue(Manager.HasComponent<EntityLinkParentData>(entity));
        }

        [Test]
        public void EntityLinkParentState_CanBeAddedToEntity()
        {
            var entity = Manager.CreateEntity();
            Manager.AddComponentData(entity, new EntityLinkParentState());
            Assert.IsTrue(Manager.HasComponent<EntityLinkParentState>(entity));
        }

        [Test]
        public void EntityLinkParentState_Roundtrip()
        {
            var entity = Manager.CreateEntity();
            var target = Manager.CreateEntity();
            var prev = Manager.CreateEntity();

            Manager.AddComponentData(entity, new EntityLinkParentState
            {
                Target = target,
                PreviousParent = prev,
                HadParent = true,
                ParentApplied = true
            });

            var data = Manager.GetComponentData<EntityLinkParentState>(entity);
            Assert.AreEqual(target, data.Target);
            Assert.AreEqual(prev, data.PreviousParent);
            Assert.IsTrue(data.HadParent);
            Assert.IsTrue(data.ParentApplied);
        }

        [Test]
        public void MultipleEntities_WithEntityLinkEntryBuffer()
        {
            var archetype = Manager.CreateArchetype(typeof(EntityLinkEntry));

            using var entities = Manager.CreateEntity(archetype, 3, Allocator.Temp);
            for (var i = 0; i < entities.Length; i++)
            {
                var buffer = Manager.GetBuffer<EntityLinkEntry>(entities[i]);
                buffer.Add(new EntityLinkEntry { Key = (ushort)(i + 1) });
            }

            for (var i = 0; i < entities.Length; i++)
            {
                var buffer = Manager.GetBuffer<EntityLinkEntry>(entities[i]);
                Assert.AreEqual(1, buffer.Length);
                Assert.AreEqual((ushort)(i + 1), buffer[0].Key);
            }
        }
    }
}