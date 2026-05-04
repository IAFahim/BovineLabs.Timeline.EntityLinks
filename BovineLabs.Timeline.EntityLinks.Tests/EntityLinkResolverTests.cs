using BovineLabs.Core.Extensions;
using BovineLabs.Core.Iterators;
using BovineLabs.Testing;
using BovineLabs.Timeline.EntityLinks.Data;
using NUnit.Framework;
using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks.Tests
{
    public partial struct LookupHelperSystem : ISystem
    {
        public static UnsafeComponentLookup<EntityLinkSource> Sources;
        public static UnsafeBufferLookup<EntityLinkEntry> Entries;

        public void OnCreate(ref SystemState state)
        {
            Sources = state.GetUnsafeComponentLookup<EntityLinkSource>(true);
            Entries = state.GetUnsafeBufferLookup<EntityLinkEntry>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            Sources.Update(ref state);
            Entries.Update(ref state);
        }

        public void OnDestroy(ref SystemState state) { }
    }

    public class EntityLinkResolverTests : ECSTestsFixture
    {
        private void InitLookups()
        {
            World.CreateSystem<LookupHelperSystem>();
            World.Update();
        }

        [Test]
        public void TryResolveRoot_NullEntity_ReturnsFalse()
        {
            InitLookups();
            Assert.IsFalse(EntityLinkResolver.TryResolveRoot(Entity.Null, LookupHelperSystem.Sources, out var root));
            Assert.AreEqual(Entity.Null, root);
        }

        [Test]
        public void TryResolveRoot_NoSourceComponent_ReturnsEntityAsRoot()
        {
            var entity = this.Manager.CreateEntity();
            InitLookups();

            Assert.IsTrue(EntityLinkResolver.TryResolveRoot(entity, LookupHelperSystem.Sources, out var root));
            Assert.AreEqual(entity, root);
        }

        [Test]
        public void TryResolveRoot_SourceWithNullRoot_ReturnsEntityAsRoot()
        {
            var entity = this.Manager.CreateEntity();
            this.Manager.AddComponentData(entity, new EntityLinkSource { Root = Entity.Null });
            InitLookups();

            Assert.IsTrue(EntityLinkResolver.TryResolveRoot(entity, LookupHelperSystem.Sources, out var root));
            Assert.AreEqual(entity, root);
        }

        [Test]
        public void TryResolveRoot_SourceWithValidRoot_ReturnsRoot()
        {
            var rootEntity = this.Manager.CreateEntity();
            var entity = this.Manager.CreateEntity();
            this.Manager.AddComponentData(entity, new EntityLinkSource { Root = rootEntity });
            InitLookups();

            Assert.IsTrue(EntityLinkResolver.TryResolveRoot(entity, LookupHelperSystem.Sources, out var resolved));
            Assert.AreEqual(rootEntity, resolved);
        }

        [Test]
        public void TryResolveFromRoot_NullRoot_ReturnsFalse()
        {
            InitLookups();
            Assert.IsFalse(EntityLinkResolver.TryResolveFromRoot(Entity.Null, 1, LookupHelperSystem.Entries, out var result));
            Assert.AreEqual(Entity.Null, result);
        }

        [Test]
        public void TryResolveFromRoot_ZeroKey_ReturnsFalse()
        {
            var entity = this.Manager.CreateEntity();
            InitLookups();
            Assert.IsFalse(EntityLinkResolver.TryResolveFromRoot(entity, 0, LookupHelperSystem.Entries, out var result));
            Assert.AreEqual(Entity.Null, result);
        }

        [Test]
        public void TryResolveFromRoot_NoBuffer_ReturnsFalse()
        {
            var entity = this.Manager.CreateEntity();
            InitLookups();
            Assert.IsFalse(EntityLinkResolver.TryResolveFromRoot(entity, 1, LookupHelperSystem.Entries, out var result));
            Assert.AreEqual(Entity.Null, result);
        }

        [Test]
        public void TryResolveFromRoot_BufferWithMatchingKey_ReturnsTarget()
        {
            var target = this.Manager.CreateEntity();
            var entity = this.Manager.CreateEntity();
            var buffer = this.Manager.AddBuffer<EntityLinkEntry>(entity);
            buffer.Add(new EntityLinkEntry { Key = 1, Target = target });
            InitLookups();

            Assert.IsTrue(EntityLinkResolver.TryResolveFromRoot(entity, 1, LookupHelperSystem.Entries, out var result));
            Assert.AreEqual(target, result);
        }

        [Test]
        public void TryResolveFromRoot_BufferWithoutMatchingKey_ReturnsFalse()
        {
            var entity = this.Manager.CreateEntity();
            var buffer = this.Manager.AddBuffer<EntityLinkEntry>(entity);
            buffer.Add(new EntityLinkEntry { Key = 1, Target = this.Manager.CreateEntity() });
            InitLookups();

            Assert.IsFalse(EntityLinkResolver.TryResolveFromRoot(entity, 2, LookupHelperSystem.Entries, out var result));
            Assert.AreEqual(Entity.Null, result);
        }

        [Test]
        public void TryResolveFromRoot_MultipleEntries_FindsCorrectTarget()
        {
            var t1 = this.Manager.CreateEntity();
            var t2 = this.Manager.CreateEntity();
            var t3 = this.Manager.CreateEntity();
            var entity = this.Manager.CreateEntity();
            var buffer = this.Manager.AddBuffer<EntityLinkEntry>(entity);
            buffer.Add(new EntityLinkEntry { Key = 10, Target = t1 });
            buffer.Add(new EntityLinkEntry { Key = 20, Target = t2 });
            buffer.Add(new EntityLinkEntry { Key = 30, Target = t3 });
            InitLookups();

            Assert.IsTrue(EntityLinkResolver.TryResolveFromRoot(entity, 20, LookupHelperSystem.Entries, out var result));
            Assert.AreEqual(t2, result);
        }

        [Test]
        public void TryResolve_CombinesRootAndKeyLookup()
        {
            var target = this.Manager.CreateEntity();
            var rootEntity = this.Manager.CreateEntity();
            var buffer = this.Manager.AddBuffer<EntityLinkEntry>(rootEntity);
            buffer.Add(new EntityLinkEntry { Key = 5, Target = target });

            var entity = this.Manager.CreateEntity();
            this.Manager.AddComponentData(entity, new EntityLinkSource { Root = rootEntity });
            InitLookups();

            Assert.IsTrue(EntityLinkResolver.TryResolve(entity, 5, LookupHelperSystem.Sources, LookupHelperSystem.Entries, out var result));
            Assert.AreEqual(target, result);
        }

        [Test]
        public void TryResolve_NullEntity_ReturnsFalse()
        {
            InitLookups();
            Assert.IsFalse(EntityLinkResolver.TryResolve(Entity.Null, 1, LookupHelperSystem.Sources, LookupHelperSystem.Entries, out var result));
            Assert.AreEqual(Entity.Null, result);
        }

        [Test]
        public void TryResolve_EntityWithoutSource_UsesEntityAsRoot()
        {
            var target = this.Manager.CreateEntity();
            var entity = this.Manager.CreateEntity();
            var buffer = this.Manager.AddBuffer<EntityLinkEntry>(entity);
            buffer.Add(new EntityLinkEntry { Key = 3, Target = target });
            InitLookups();

            Assert.IsTrue(EntityLinkResolver.TryResolve(entity, 3, LookupHelperSystem.Sources, LookupHelperSystem.Entries, out var result));
            Assert.AreEqual(target, result);
        }

        [Test]
        public void TryResolve_KeyNotFound_ReturnsFalse()
        {
            var entity = this.Manager.CreateEntity();
            var buffer = this.Manager.AddBuffer<EntityLinkEntry>(entity);
            buffer.Add(new EntityLinkEntry { Key = 1, Target = this.Manager.CreateEntity() });
            InitLookups();

            Assert.IsFalse(EntityLinkResolver.TryResolve(entity, 99, LookupHelperSystem.Sources, LookupHelperSystem.Entries, out var result));
            Assert.AreEqual(Entity.Null, result);
        }
    }
}
