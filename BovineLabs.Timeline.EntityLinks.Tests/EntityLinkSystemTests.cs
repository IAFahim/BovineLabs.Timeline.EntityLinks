using BovineLabs.Reaction.Data.Core;
using BovineLabs.Testing;
using BovineLabs.Timeline.Data;
using BovineLabs.Timeline.EntityLinks.Data;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Timeline.EntityLinks.Tests
{
    public class EntityLinkSystemTests : ECSTestsFixture
    {
        [Test]
        public void EntityLinkMutateSystem_Assign_AddsMissingEntry()
        {
            var system = World.CreateSystem<EntityLinkMutateSystem>();
            var root = CreateRoot();
            var target = Manager.CreateEntity();
            var binding = CreateBinding(new Targets { Source = root, Target = target });

            CreateActiveClip(binding, new EntityLinkMutate
            {
                Mode = EntityLinkMutateMode.Assign,
                ReadRootFrom = Target.Source,
                LinkKey = 10,
                NewTarget = Target.Target
            });

            system.Update(WorldUnmanaged);
            Manager.CompleteAllTrackedJobs();

            var buffer = Manager.GetBuffer<EntityLinkEntry>(root);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(10, buffer[0].Key);
            Assert.AreEqual(target, buffer[0].Target);
            World.DestroySystem(system);
        }

        [Test]
        public void EntityLinkMutateSystem_Swap_ExchangesEntryTargets()
        {
            var system = World.CreateSystem<EntityLinkMutateSystem>();
            var root = CreateRoot();
            var a = Manager.CreateEntity();
            var b = Manager.CreateEntity();
            var buffer = Manager.GetBuffer<EntityLinkEntry>(root);
            buffer.Add(new EntityLinkEntry { Key = 1, Target = a });
            buffer.Add(new EntityLinkEntry { Key = 2, Target = b });

            var binding = CreateBinding(new Targets { Source = root });
            CreateActiveClip(binding, new EntityLinkMutate
            {
                Mode = EntityLinkMutateMode.Swap,
                ReadRootFrom = Target.Source,
                LinkKey = 1,
                SwapKey = 2
            });

            system.Update(WorldUnmanaged);
            Manager.CompleteAllTrackedJobs();

            buffer = Manager.GetBuffer<EntityLinkEntry>(root);
            Assert.AreEqual(b, buffer[0].Target);
            Assert.AreEqual(a, buffer[1].Target);
            World.DestroySystem(system);
        }

        [Test]
        public void EntityLinkMutateSystem_Remove_RemovesAllMatchingEntries()
        {
            var system = World.CreateSystem<EntityLinkMutateSystem>();
            var root = CreateRoot();
            var buffer = Manager.GetBuffer<EntityLinkEntry>(root);
            buffer.Add(new EntityLinkEntry { Key = 3, Target = Manager.CreateEntity() });
            buffer.Add(new EntityLinkEntry { Key = 4, Target = Manager.CreateEntity() });
            buffer.Add(new EntityLinkEntry { Key = 3, Target = Manager.CreateEntity() });

            var binding = CreateBinding(new Targets { Source = root });
            CreateActiveClip(binding, new EntityLinkMutate
            {
                Mode = EntityLinkMutateMode.Remove,
                ReadRootFrom = Target.Source,
                LinkKey = 3
            });

            system.Update(WorldUnmanaged);
            Manager.CompleteAllTrackedJobs();

            buffer = Manager.GetBuffer<EntityLinkEntry>(root);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(4, buffer[0].Key);
            World.DestroySystem(system);
        }

        [Test]
        public void EntityLinkTargetPatchSystem_WriteTarget_UsesResolvedLink()
        {
            var ecbSystem = World.CreateSystem<BeginSimulationEntityCommandBufferSystem>();
            var system = World.CreateSystem<EntityLinkTargetPatchSystem>();
            var linked = Manager.CreateEntity();
            var root = CreateRoot(new EntityLinkEntry { Key = 5, Target = linked });
            var fallback = Manager.CreateEntity();
            var binding = CreateBinding(new Targets { Source = root, Target = fallback });

            CreateActiveClip(binding, new EntityLinkTargetPatch
            {
                ReadRootFrom = Target.Source,
                LinkKey = 5,
                WriteTo = Target.Target,
                Fallback = Target.Target
            });

            system.Update(WorldUnmanaged);
            ecbSystem.Update(WorldUnmanaged);
            Manager.CompleteAllTrackedJobs();

            Assert.AreEqual(linked, Manager.GetComponentData<Targets>(binding).Target);
            World.DestroySystem(system);
            World.DestroySystem(ecbSystem);
        }

        [Test]
        public void EntityLinkTargetPatchSystem_WriteCustom_AddsTargetsCustom()
        {
            var ecbSystem = World.CreateSystem<BeginSimulationEntityCommandBufferSystem>();
            var system = World.CreateSystem<EntityLinkTargetPatchSystem>();
            var linked = Manager.CreateEntity();
            var root = CreateRoot(new EntityLinkEntry { Key = 6, Target = linked });
            var binding = CreateBinding(new Targets { Source = root });

            CreateActiveClip(binding, new EntityLinkTargetPatch
            {
                ReadRootFrom = Target.Source,
                LinkKey = 6,
                WriteTo = Target.Custom0,
                Fallback = Target.None
            });

            system.Update(WorldUnmanaged);
            ecbSystem.Update(WorldUnmanaged);
            Manager.CompleteAllTrackedJobs();

            Assert.IsTrue(Manager.HasComponent<TargetsCustom>(binding));
            Assert.AreEqual(linked, Manager.GetComponentData<TargetsCustom>(binding).Target0);
            World.DestroySystem(system);
            World.DestroySystem(ecbSystem);
        }

        [Test]
        public void EntityLinkTargetPatchSystem_MissingLink_UsesFallback()
        {
            var ecbSystem = World.CreateSystem<BeginSimulationEntityCommandBufferSystem>();
            var system = World.CreateSystem<EntityLinkTargetPatchSystem>();
            var root = CreateRoot();
            var fallback = Manager.CreateEntity();
            var binding = CreateBinding(new Targets { Source = root, Target = fallback });

            CreateActiveClip(binding, new EntityLinkTargetPatch
            {
                ReadRootFrom = Target.Source,
                LinkKey = 99,
                WriteTo = Target.Owner,
                Fallback = Target.Target
            });

            system.Update(WorldUnmanaged);
            ecbSystem.Update(WorldUnmanaged);
            Manager.CompleteAllTrackedJobs();

            Assert.AreEqual(fallback, Manager.GetComponentData<Targets>(binding).Owner);
            World.DestroySystem(system);
            World.DestroySystem(ecbSystem);
        }

        [Test]
        public void EntityLinkParentSystem_Enter_ParentsTargetToResolvedLink()
        {
            var ecbSystem = World.CreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
            var system = World.CreateSystem<EntityLinkParentSystem>();
            var parent = CreateTransformEntity();
            var child = CreateTransformEntity();
            var root = CreateRoot(new EntityLinkEntry { Key = 8, Target = parent });
            var binding = CreateBinding(new Targets { Source = root, Target = child });
            var clip = CreateActiveClip(binding, new EntityLinkParentData
            {
                EntityToParent = Target.Target,
                ReadRootFrom = Target.Source,
                ParentLinkKey = 8,
                LocalPosition = new float3(1, 2, 3),
                LocalRotation = quaternion.identity,
                RestoreOnEnd = true
            });
            Manager.AddComponentData(clip, new EntityLinkParentState());

            system.Update(WorldUnmanaged);
            ecbSystem.Update(WorldUnmanaged);
            Manager.CompleteAllTrackedJobs();

            Assert.AreEqual(parent, Manager.GetComponentData<Parent>(child).Value);
            Assert.AreEqual(new float3(1, 2, 3), Manager.GetComponentData<LocalTransform>(child).Position);
            Assert.IsTrue(Manager.GetComponentData<EntityLinkParentState>(clip).ParentApplied);
            World.DestroySystem(system);
            World.DestroySystem(ecbSystem);
        }

        [Test]
        public void EntityLinkParentSystem_Exit_RestoresPreviousParent()
        {
            var ecbSystem = World.CreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
            var system = World.CreateSystem<EntityLinkParentSystem>();
            var previousParent = CreateTransformEntity();
            var currentParent = CreateTransformEntity();
            var child = CreateTransformEntity();
            Manager.AddComponentData(child, new Parent { Value = currentParent });

            var binding = CreateBinding(new Targets { Target = child });
            var clip = CreateClip(binding, new EntityLinkParentData { RestoreOnEnd = true }, false, true);
            Manager.AddComponentData(clip, new EntityLinkParentState
            {
                Target = child,
                PreviousParent = previousParent,
                HadParent = true,
                ParentApplied = true
            });

            system.Update(WorldUnmanaged);
            ecbSystem.Update(WorldUnmanaged);
            Manager.CompleteAllTrackedJobs();

            Assert.AreEqual(previousParent, Manager.GetComponentData<Parent>(child).Value);
            World.DestroySystem(system);
            World.DestroySystem(ecbSystem);
        }

        private Entity CreateRoot(params EntityLinkEntry[] entries)
        {
            var root = Manager.CreateEntity();
            var buffer = Manager.AddBuffer<EntityLinkEntry>(root);
            foreach (var entry in entries) buffer.Add(entry);

            return root;
        }

        private Entity CreateBinding(Targets targets)
        {
            var entity = Manager.CreateEntity();
            Manager.AddComponentData(entity, targets);
            return entity;
        }

        private Entity CreateActiveClip<T>(Entity binding, T data)
            where T : unmanaged, IComponentData
        {
            return CreateClip(binding, data, true, false);
        }

        private Entity CreateClip<T>(Entity binding, T data, bool active, bool activePrevious)
            where T : unmanaged, IComponentData
        {
            var clip = Manager.CreateEntity(
                typeof(TrackBinding),
                typeof(ClipActive),
                typeof(ClipActivePrevious),
                typeof(T));
            Manager.SetComponentData(clip, new TrackBinding { Value = binding });
            Manager.SetComponentData(clip, data);
            Manager.SetComponentEnabled<ClipActive>(clip, active);
            Manager.SetComponentEnabled<ClipActivePrevious>(clip, activePrevious);
            return clip;
        }

        private Entity CreateTransformEntity()
        {
            var entity = Manager.CreateEntity(typeof(LocalTransform), typeof(LocalToWorld));
            Manager.SetComponentData(entity, LocalTransform.Identity);
            Manager.SetComponentData(entity, new LocalToWorld { Value = float4x4.identity });
            return entity;
        }
    }
}