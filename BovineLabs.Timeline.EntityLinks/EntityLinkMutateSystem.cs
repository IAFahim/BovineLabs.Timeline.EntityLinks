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
    public partial struct EntityLinkMutateSystem : ISystem
    {
        private ComponentLookup<Targets> _targetsLookup;
        private ComponentLookup<TargetsCustom> _targetsCustoms;
        private UnsafeComponentLookup<EntityLinkSource> _sources;
        private UnsafeBufferLookup<EntityLinkEntry> _entries;
        private EntityLock _entityLock;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntityLinkMutate>();
            _targetsLookup = state.GetComponentLookup<Targets>(true);
            _targetsCustoms = state.GetComponentLookup<TargetsCustom>(true);
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
            _targetsCustoms.Update(ref state);
            _sources.Update(ref state);
            _entries.Update(ref state);

            state.Dependency = new MutateJob
            {
                TargetsLookup = _targetsLookup,
                TargetsCustoms = _targetsCustoms,
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
            [ReadOnly] public ComponentLookup<Targets> TargetsLookup;
            [ReadOnly] public ComponentLookup<TargetsCustom> TargetsCustoms;
            [ReadOnly] public UnsafeComponentLookup<EntityLinkSource> Sources;

            [NativeDisableParallelForRestriction] public UnsafeBufferLookup<EntityLinkEntry> Entries;

            public EntityLock EntityLock;

            private void Execute(in TrackBinding binding, in EntityLinkMutate mutate)
            {
                var bindingEntity = binding.Value;
                if (bindingEntity == Entity.Null ||
                    !TargetsLookup.TryGetComponent(bindingEntity, out var targets)) return;

                var rootCandidate = targets.Get(mutate.ReadRootFrom, bindingEntity, TargetsCustoms);
                if (rootCandidate == Entity.Null ||
                    !EntityLinkResolver.TryResolveRoot(rootCandidate, Sources, out var root)) return;

                using (EntityLock.Acquire(root))
                {
                    if (!Entries.TryGetBuffer(root, out var buffer)) return;

                    switch (mutate.Mode)
                    {
                        case EntityLinkMutateMode.Assign:
                        {
                            var newTarget = targets.Get(mutate.NewTarget, bindingEntity, TargetsCustoms);
                            var found = false;
                            for (var i = 0; i < buffer.Length; i++)
                                if (buffer[i].Key == mutate.LinkKey)
                                {
                                    buffer[i] = new EntityLinkEntry { Key = mutate.LinkKey, Target = newTarget };
                                    found = true;
                                    break;
                                }

                            if (!found) buffer.Add(new EntityLinkEntry { Key = mutate.LinkKey, Target = newTarget });
                            break;
                        }

                        case EntityLinkMutateMode.Swap:
                        {
                            int idxA = -1, idxB = -1;
                            for (var i = 0; i < buffer.Length; i++)
                                if (buffer[i].Key == mutate.LinkKey) idxA = i;
                                else if (buffer[i].Key == mutate.SwapKey) idxB = i;

                            var targetA = idxA != -1 ? buffer[idxA].Target : Entity.Null;
                            var targetB = idxB != -1 ? buffer[idxB].Target : Entity.Null;

                            if (idxA != -1)
                                buffer[idxA] = new EntityLinkEntry { Key = mutate.LinkKey, Target = targetB };
                            else buffer.Add(new EntityLinkEntry { Key = mutate.LinkKey, Target = targetB });

                            if (idxB != -1)
                                buffer[idxB] = new EntityLinkEntry { Key = mutate.SwapKey, Target = targetA };
                            else buffer.Add(new EntityLinkEntry { Key = mutate.SwapKey, Target = targetA });
                            break;
                        }

                        case EntityLinkMutateMode.Remove:
                        {
                            for (var i = buffer.Length - 1; i >= 0; i--)
                                if (buffer[i].Key == mutate.LinkKey)
                                    buffer.RemoveAt(i);

                            break;
                        }
                    }
                }
            }
        }
    }
}