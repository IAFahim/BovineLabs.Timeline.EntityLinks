using BovineLabs.Core.Collections;
using BovineLabs.Core.Extensions;
using BovineLabs.Core.Iterators;
using BovineLabs.Testing;
using BovineLabs.Timeline.EntityLinks.Data;
using NUnit.Framework;
using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks.Tests
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation)]
    public partial struct EntriesLookupSystem : ISystem
    {
        public static UnsafeBufferLookup<EntityLinkEntry> Entries;

        public void OnCreate(ref SystemState state)
        {
            Entries = state.GetUnsafeBufferLookup<EntityLinkEntry>();
        }

        public void OnUpdate(ref SystemState state)
        {
            Entries.Update(ref state);
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }

    public class EntityLinkEntriesTests : ECSTestsFixture
    {
        private UnsafeDynamicBuffer<EntityLinkEntry> Buffer(Entity entity)
        {
            World.CreateSystem<EntriesLookupSystem>();
            World.Update();
            EntriesLookupSystem.Entries.TryGetBuffer(entity, out var buffer);
            return buffer;
        }

        [Test]
        public void Assign_NoMatch_Adds()
        {
            var entity = Manager.CreateEntity();
            Manager.AddBuffer<EntityLinkEntry>(entity);
            var target = Manager.CreateEntity();
            var buffer = Buffer(entity);

            EntityLinkEntries.Assign(buffer, 1, target);

            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(1, buffer[0].Key);
            Assert.AreEqual(target, buffer[0].Target);
        }

        [Test]
        public void Assign_FirstMatchOnly()
        {
            var entity = Manager.CreateEntity();
            var first = Manager.CreateEntity();
            var second = Manager.CreateEntity();
            var buffer = Manager.AddBuffer<EntityLinkEntry>(entity);
            buffer.Add(new EntityLinkEntry { Key = 7, Target = first });
            buffer.Add(new EntityLinkEntry { Key = 7, Target = second });

            var unsafeBuffer = Buffer(entity);
            var newTarget = Manager.CreateEntity();
            EntityLinkEntries.Assign(unsafeBuffer, 7, newTarget);

            Assert.AreEqual(2, unsafeBuffer.Length);
            Assert.AreEqual(newTarget, unsafeBuffer[0].Target);
            Assert.AreEqual(second, unsafeBuffer[1].Target);
        }

        [Test]
        public void Swap_ExchangesTargets()
        {
            var entity = Manager.CreateEntity();
            var targetA = Manager.CreateEntity();
            var targetB = Manager.CreateEntity();
            var buffer = Manager.AddBuffer<EntityLinkEntry>(entity);
            buffer.Add(new EntityLinkEntry { Key = 1, Target = targetA });
            buffer.Add(new EntityLinkEntry { Key = 2, Target = targetB });

            var unsafeBuffer = Buffer(entity);
            EntityLinkEntries.Swap(unsafeBuffer, 1, 2);

            Assert.AreEqual(2, unsafeBuffer.Length);
            Assert.AreEqual(targetB, FindTarget(unsafeBuffer, 1));
            Assert.AreEqual(targetA, FindTarget(unsafeBuffer, 2));
        }

        [Test]
        public void Swap_OneSideMissing()
        {
            var entity = Manager.CreateEntity();
            var targetA = Manager.CreateEntity();
            var buffer = Manager.AddBuffer<EntityLinkEntry>(entity);
            buffer.Add(new EntityLinkEntry { Key = 1, Target = targetA });

            var unsafeBuffer = Buffer(entity);
            EntityLinkEntries.Swap(unsafeBuffer, 1, 2);

            Assert.AreEqual(1, unsafeBuffer.Length);
            Assert.AreEqual(2, unsafeBuffer[0].Key);
            Assert.AreEqual(targetA, unsafeBuffer[0].Target);
        }

        [Test]
        public void Swap_SameKey_NoOp()
        {
            var entity = Manager.CreateEntity();
            var targetA = Manager.CreateEntity();
            var buffer = Manager.AddBuffer<EntityLinkEntry>(entity);
            buffer.Add(new EntityLinkEntry { Key = 1, Target = targetA });

            var unsafeBuffer = Buffer(entity);
            EntityLinkEntries.Swap(unsafeBuffer, 1, 1);

            Assert.AreEqual(1, unsafeBuffer.Length);
            Assert.AreEqual(1, unsafeBuffer[0].Key);
            Assert.AreEqual(targetA, unsafeBuffer[0].Target);
        }

        [Test]
        public void Remove_DeletesAllMatchesDescending()
        {
            var entity = Manager.CreateEntity();
            var survivor = Manager.CreateEntity();
            var buffer = Manager.AddBuffer<EntityLinkEntry>(entity);
            buffer.Add(new EntityLinkEntry { Key = 1, Target = Manager.CreateEntity() });
            buffer.Add(new EntityLinkEntry { Key = 2, Target = survivor });
            buffer.Add(new EntityLinkEntry { Key = 1, Target = Manager.CreateEntity() });

            var unsafeBuffer = Buffer(entity);
            EntityLinkEntries.Remove(unsafeBuffer, 1);

            Assert.AreEqual(1, unsafeBuffer.Length);
            Assert.AreEqual(2, unsafeBuffer[0].Key);
            Assert.AreEqual(survivor, unsafeBuffer[0].Target);
        }

        [Test]
        public void SetRemoveOrAdd_NullTargetRemoves()
        {
            var entity = Manager.CreateEntity();
            var targetB = Manager.CreateEntity();
            var buffer = Manager.AddBuffer<EntityLinkEntry>(entity);
            buffer.Add(new EntityLinkEntry { Key = 2, Target = targetB });

            var unsafeBuffer = Buffer(entity);
            EntityLinkEntries.Swap(unsafeBuffer, 1, 2);

            Assert.AreEqual(1, unsafeBuffer.Length);
            Assert.AreEqual(1, unsafeBuffer[0].Key);
            Assert.AreEqual(targetB, unsafeBuffer[0].Target);
        }

        private static Entity FindTarget(UnsafeDynamicBuffer<EntityLinkEntry> buffer, ushort key)
        {
            for (var i = 0; i < buffer.Length; i++)
                if (buffer[i].Key == key)
                    return buffer[i].Target;

            return Entity.Null;
        }
    }
}
