using BovineLabs.Core.Collections;
using BovineLabs.Core.Extensions;
using BovineLabs.Core.Iterators;
using BovineLabs.Core.Utility;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.Data;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks
{
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    [UpdateBefore(typeof(EntityLinkTargetPatchSystem))]
    [UpdateBefore(typeof(EntityLinkParentSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation)]
    public partial struct EntityLinkMutateSystem : ISystem
    {
        private UnsafeComponentLookup<Targets> _targetsLookup;
        private UnsafeComponentLookup<EntityLinkSource> _sources;
        private UnsafeBufferLookup<EntityLinkEntry> _entries;
        private EntityLock _entityLock;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntityLinkMutate>();
            _targetsLookup = state.GetUnsafeComponentLookup<Targets>(true);
            _sources = state.GetUnsafeComponentLookup<EntityLinkSource>(true);
            _entries = state.GetUnsafeBufferLookup<EntityLinkEntry>();
            _entityLock = new EntityLock(Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            _entityLock.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _targetsLookup.Update(ref state);
            _sources.Update(ref state);
            _entries.Update(ref state);

            state.Dependency = new MutateJob
            {
                TargetsLookup = _targetsLookup,
                Sources = _sources,
                Entries = _entries,
                EntityLock = _entityLock
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct MutateJob : IJobEntity
        {
            [ReadOnly] public UnsafeComponentLookup<Targets> TargetsLookup;
            [ReadOnly] public UnsafeComponentLookup<EntityLinkSource> Sources;

            [NativeDisableParallelForRestriction] public UnsafeBufferLookup<EntityLinkEntry> Entries;

            public EntityLock EntityLock;

            private void Execute(in TrackBinding binding, in EntityLinkMutate mutate)
            {
                var bindingEntity = binding.Value;
                if (bindingEntity == Entity.Null ||
                    !TargetsLookup.TryGetComponent(bindingEntity, out var targets)) return;

                var rootCandidate = targets.Get(mutate.ReadRootFrom, bindingEntity);
                if (rootCandidate == Entity.Null ||
                    !EntityLinkResolver.TryResolveRoot(rootCandidate, Sources, out var root)) return;

                var newTarget = targets.Get(mutate.NewTarget, bindingEntity);

                using (EntityLock.Acquire(root))
                {
                    if (Entries.TryGetBuffer(root, out var buffer))
                        ApplyMutation(buffer, mutate, newTarget);
                }
            }

            private static void ApplyMutation(UnsafeDynamicBuffer<EntityLinkEntry> buffer, in EntityLinkMutate mutate,
                Entity newTarget)
            {
                switch (mutate.Mode)
                {
                    case EntityLinkMutateMode.Assign:
                        if (newTarget != Entity.Null)
                            EntityLinkEntries.Assign(buffer, mutate.LinkKey, newTarget);
                        break;

                    case EntityLinkMutateMode.Swap:
                        EntityLinkEntries.Swap(buffer, mutate.LinkKey, mutate.SwapKey);
                        break;

                    case EntityLinkMutateMode.Remove:
                        EntityLinkEntries.Remove(buffer, mutate.LinkKey);
                        break;
                }
            }
        }
    }
}