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
    public partial struct EntityLinkMutateSystem : ISystem
    {
        private ComponentLookup<Targets> targetsLookup;
        private ComponentLookup<TargetsCustom> targetsCustoms;
        private UnsafeComponentLookup<EntityLinkSource> sources;
        private UnsafeBufferLookup<EntityLinkEntry> entries;
        private EntityLock entityLock;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntityLinkMutate>();
            this.targetsLookup = state.GetComponentLookup<Targets>(true);
            this.targetsCustoms = state.GetComponentLookup<TargetsCustom>(true);
            this.sources = state.GetUnsafeComponentLookup<EntityLinkSource>(true);
            this.entries = state.GetUnsafeBufferLookup<EntityLinkEntry>(false);
            this.entityLock = new EntityLock(Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            this.entityLock.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.targetsLookup.Update(ref state);
            this.targetsCustoms.Update(ref state);
            this.sources.Update(ref state);
            this.entries.Update(ref state);

            state.Dependency = new MutateJob
            {
                TargetsLookup = this.targetsLookup,
                TargetsCustoms = this.targetsCustoms,
                Sources = this.sources,
                Entries = this.entries,
                EntityLock = this.entityLock
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

            [NativeDisableParallelForRestriction]
            public UnsafeBufferLookup<EntityLinkEntry> Entries;
            
            public EntityLock EntityLock;

            private void Execute(in TrackBinding binding, in EntityLinkMutate mutate)
            {
                var bindingEntity = binding.Value;
                if (bindingEntity == Entity.Null || !this.TargetsLookup.TryGetComponent(bindingEntity, out var targets)) return;

                var rootCandidate = targets.Get(mutate.ReadRootFrom, bindingEntity, this.TargetsCustoms);
                if (rootCandidate == Entity.Null || !EntityLinkResolver.TryResolveRoot(rootCandidate, this.Sources, out var root)) return;

                using (this.EntityLock.Acquire(root))
                {
                    if (!this.Entries.TryGetBuffer(root, out var buffer)) return;

                    switch (mutate.Mode)
                    {
                        case EntityLinkMutateMode.Assign:
                        {
                            var newTarget = targets.Get(mutate.NewTarget, bindingEntity, this.TargetsCustoms);
                            var found = false;
                            for (var i = 0; i < buffer.Length; i++)
                            {
                                if (buffer[i].Key == mutate.LinkKey)
                                {
                                    buffer[i] = new EntityLinkEntry { Key = mutate.LinkKey, Target = newTarget };
                                    found = true;
                                    break;
                                }
                            }
                            if (!found) buffer.Add(new EntityLinkEntry { Key = mutate.LinkKey, Target = newTarget });
                            break;
                        }

                        case EntityLinkMutateMode.Swap:
                        {
                            int idxA = -1, idxB = -1;
                            for (var i = 0; i < buffer.Length; i++)
                            {
                                if (buffer[i].Key == mutate.LinkKey) idxA = i;
                                else if (buffer[i].Key == mutate.SwapKey) idxB = i;
                            }
                            
                            var targetA = idxA != -1 ? buffer[idxA].Target : Entity.Null;
                            var targetB = idxB != -1 ? buffer[idxB].Target : Entity.Null;

                            if (idxA != -1) buffer[idxA] = new EntityLinkEntry { Key = mutate.LinkKey, Target = targetB };
                            else buffer.Add(new EntityLinkEntry { Key = mutate.LinkKey, Target = targetB });

                            if (idxB != -1) buffer[idxB] = new EntityLinkEntry { Key = mutate.SwapKey, Target = targetA };
                            else buffer.Add(new EntityLinkEntry { Key = mutate.SwapKey, Target = targetA });
                            break;
                        }

                        case EntityLinkMutateMode.Remove:
                        {
                            for (var i = buffer.Length - 1; i >= 0; i--)
                            {
                                if (buffer[i].Key == mutate.LinkKey)
                                    buffer.RemoveAt(i);
                            }
                            break;
                        }
                    }
                }
            }
        }
    }
}