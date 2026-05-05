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
        [TestLeakDetection]
        public void EntityLinkMutateSystem_Assign_AddsMissingEntry()
        {
            var system = this.World.CreateSystem<EntityLinkMutateSystem>();
            var root = this.CreateRoot();
            var target = this.Manager.CreateEntity();
            var binding = this.CreateBinding(new Targets { Source = root, Target = target });

            this.CreateActiveClip(binding, new EntityLinkMutate
            {
                Mode = EntityLinkMutateMode.Assign,
                ReadRootFrom = Target.Source,
                LinkKey = 10,
                NewTarget = Target.Target
            });

            system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var buffer = this.Manager.GetBuffer<EntityLinkEntry>(root);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(10, buffer[0].Key);
            Assert.AreEqual(target, buffer[0].Target);
        }

        [Test]
        [TestLeakDetection]
        public void EntityLinkMutateSystem_Swap_ExchangesEntryTargets()
        {
            var system = this.World.CreateSystem<EntityLinkMutateSystem>();
            var root = this.CreateRoot();
            var a = this.Manager.CreateEntity();
            var b = this.Manager.CreateEntity();
            var buffer = this.Manager.GetBuffer<EntityLinkEntry>(root);
            buffer.Add(new EntityLinkEntry { Key = 1, Target = a });
            buffer.Add(new EntityLinkEntry { Key = 2, Target = b });

            var binding = this.CreateBinding(new Targets { Source = root });
            this.CreateActiveClip(binding, new EntityLinkMutate
            {
                Mode = EntityLinkMutateMode.Swap,
                ReadRootFrom = Target.Source,
                LinkKey = 1,
                SwapKey = 2
            });

            system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.AreEqual(b, buffer[0].Target);
            Assert.AreEqual(a, buffer[1].Target);
        }

        [Test]
        [TestLeakDetection]
        public void EntityLinkMutateSystem_Remove_RemovesAllMatchingEntries()
        {
            var system = this.World.CreateSystem<EntityLinkMutateSystem>();
            var root = this.CreateRoot();
            var buffer = this.Manager.GetBuffer<EntityLinkEntry>(root);
            buffer.Add(new EntityLinkEntry { Key = 3, Target = this.Manager.CreateEntity() });
            buffer.Add(new EntityLinkEntry { Key = 4, Target = this.Manager.CreateEntity() });
            buffer.Add(new EntityLinkEntry { Key = 3, Target = this.Manager.CreateEntity() });

            var binding = this.CreateBinding(new Targets { Source = root });
            this.CreateActiveClip(binding, new EntityLinkMutate
            {
                Mode = EntityLinkMutateMode.Remove,
                ReadRootFrom = Target.Source,
                LinkKey = 3
            });

            system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(4, buffer[0].Key);
        }

        [Test]
        [TestLeakDetection]
        public void EntityLinkTargetPatchSystem_WriteTarget_UsesResolvedLink()
        {
            var ecbSystem = this.World.CreateSystem<BeginSimulationEntityCommandBufferSystem>();
            var system = this.World.CreateSystem<EntityLinkTargetPatchSystem>();
            var linked = this.Manager.CreateEntity();
            var root = this.CreateRoot(new EntityLinkEntry { Key = 5, Target = linked });
            var fallback = this.Manager.CreateEntity();
            var binding = this.CreateBinding(new Targets { Source = root, Target = fallback });

            this.CreateActiveClip(binding, new EntityLinkTargetPatch
            {
                ReadRootFrom = Target.Source,
                LinkKey = 5,
                WriteTo = Target.Target,
                Fallback = Target.Target
            });

            system.Update(this.WorldUnmanaged);
            ecbSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.AreEqual(linked, this.Manager.GetComponentData<Targets>(binding).Target);
        }

        [Test]
        [TestLeakDetection]
        public void EntityLinkTargetPatchSystem_WriteCustom_AddsTargetsCustom()
        {
            var ecbSystem = this.World.CreateSystem<BeginSimulationEntityCommandBufferSystem>();
            var system = this.World.CreateSystem<EntityLinkTargetPatchSystem>();
            var linked = this.Manager.CreateEntity();
            var root = this.CreateRoot(new EntityLinkEntry { Key = 6, Target = linked });
            var binding = this.CreateBinding(new Targets { Source = root });

            this.CreateActiveClip(binding, new EntityLinkTargetPatch
            {
                ReadRootFrom = Target.Source,
                LinkKey = 6,
                WriteTo = Target.Custom0,
                Fallback = Target.None
            });

            system.Update(this.WorldUnmanaged);
            ecbSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.IsTrue(this.Manager.HasComponent<TargetsCustom>(binding));
            Assert.AreEqual(linked, this.Manager.GetComponentData<TargetsCustom>(binding).Target0);
        }

        [Test]
        [TestLeakDetection]
        public void EntityLinkTargetPatchSystem_MissingLink_UsesFallback()
        {
            var ecbSystem = this.World.CreateSystem<BeginSimulationEntityCommandBufferSystem>();
            var system = this.World.CreateSystem<EntityLinkTargetPatchSystem>();
            var root = this.CreateRoot();
            var fallback = this.Manager.CreateEntity();
            var binding = this.CreateBinding(new Targets { Source = root, Target = fallback });

            this.CreateActiveClip(binding, new EntityLinkTargetPatch
            {
                ReadRootFrom = Target.Source,
                LinkKey = 99,
                WriteTo = Target.Owner,
                Fallback = Target.Target
            });

            system.Update(this.WorldUnmanaged);
            ecbSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.AreEqual(fallback, this.Manager.GetComponentData<Targets>(binding).Owner);
        }

        [Test]
        [TestLeakDetection]
        public void EntityLinkParentSystem_Enter_ParentsTargetToResolvedLink()
        {
            var ecbSystem = this.World.CreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
            var system = this.World.CreateSystem<EntityLinkParentSystem>();
            var parent = this.CreateTransformEntity();
            var child = this.CreateTransformEntity();
            var root = this.CreateRoot(new EntityLinkEntry { Key = 8, Target = parent });
            var binding = this.CreateBinding(new Targets { Source = root, Target = child });
            var clip = this.CreateActiveClip(binding, new EntityLinkParentData
            {
                EntityToParent = Target.Target,
                ReadRootFrom = Target.Source,
                ParentLinkKey = 8,
                LocalPosition = new float3(1, 2, 3),
                LocalRotation = quaternion.identity,
                RestoreOnEnd = true
            });
            this.Manager.AddComponentData(clip, new EntityLinkParentState());

            system.Update(this.WorldUnmanaged);
            ecbSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.AreEqual(parent, this.Manager.GetComponentData<Parent>(child).Value);
            Assert.AreEqual(new float3(1, 2, 3), this.Manager.GetComponentData<LocalTransform>(child).Position);
            Assert.IsTrue(this.Manager.GetComponentData<EntityLinkParentState>(clip).ParentApplied);
        }

        [Test]
        [TestLeakDetection]
        public void EntityLinkParentSystem_Exit_RestoresPreviousParent()
        {
            var ecbSystem = this.World.CreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
            var system = this.World.CreateSystem<EntityLinkParentSystem>();
            var previousParent = this.CreateTransformEntity();
            var currentParent = this.CreateTransformEntity();
            var child = this.CreateTransformEntity();
            this.Manager.AddComponentData(child, new Parent { Value = currentParent });

            var binding = this.CreateBinding(new Targets { Target = child });
            var clip = this.CreateClip(binding, new EntityLinkParentData { RestoreOnEnd = true }, active: false, activePrevious: true);
            this.Manager.AddComponentData(clip, new EntityLinkParentState
            {
                Target = child,
                PreviousParent = previousParent,
                HadParent = true,
                ParentApplied = true
            });

            system.Update(this.WorldUnmanaged);
            ecbSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.AreEqual(previousParent, this.Manager.GetComponentData<Parent>(child).Value);
        }

        private Entity CreateRoot(params EntityLinkEntry[] entries)
        {
            var root = this.Manager.CreateEntity();
            var buffer = this.Manager.AddBuffer<EntityLinkEntry>(root);
            foreach (var entry in entries)
            {
                buffer.Add(entry);
            }

            return root;
        }

        private Entity CreateBinding(Targets targets)
        {
            var entity = this.Manager.CreateEntity();
            this.Manager.AddComponentData(entity, targets);
            return entity;
        }

        private Entity CreateActiveClip<T>(Entity binding, T data)
            where T : unmanaged, IComponentData
        {
            return this.CreateClip(binding, data, active: true, activePrevious: false);
        }

        private Entity CreateClip<T>(Entity binding, T data, bool active, bool activePrevious)
            where T : unmanaged, IComponentData
        {
            var clip = this.Manager.CreateEntity(
                typeof(TrackBinding),
                typeof(ClipActive),
                typeof(ClipActivePrevious),
                typeof(T));
            this.Manager.SetComponentData(clip, new TrackBinding { Value = binding });
            this.Manager.SetComponentData(clip, data);
            this.Manager.SetComponentEnabled<ClipActive>(clip, active);
            this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, activePrevious);
            return clip;
        }

        private Entity CreateTransformEntity()
        {
            var entity = this.Manager.CreateEntity(typeof(LocalTransform), typeof(LocalToWorld));
            this.Manager.SetComponentData(entity, LocalTransform.Identity);
            this.Manager.SetComponentData(entity, new LocalToWorld { Value = float4x4.identity });
            return entity;
        }
    }
}
