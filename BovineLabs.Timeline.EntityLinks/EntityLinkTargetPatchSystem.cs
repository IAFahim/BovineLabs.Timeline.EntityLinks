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
    public partial struct EntityLinkTargetPatchSystem : ISystem
    {
        private ComponentLookup<Targets> targetsLookup;
        private ComponentLookup<TargetsCustom> targetsCustoms;
        private UnsafeComponentLookup<EntityLinkSource> sources;
        private UnsafeBufferLookup<EntityLinkEntry> links;
        private EntityLock entityLock;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntityLinkTargetPatch>();
            this.targetsLookup = state.GetComponentLookup<Targets>(false);
            this.targetsCustoms = state.GetComponentLookup<TargetsCustom>(false);
            this.sources = state.GetUnsafeComponentLookup<EntityLinkSource>(true);
            this.links = state.GetUnsafeBufferLookup<EntityLinkEntry>(true);
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
            this.links.Update(ref state);

            state.Dependency = new PatchJob
            {
                TargetsLookup = this.targetsLookup,
                TargetsCustoms = this.targetsCustoms,
                Sources = this.sources,
                Links = this.links,
                EntityLock = this.entityLock,
                ECB = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct PatchJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<Targets> TargetsLookup;
            
            [NativeDisableParallelForRestriction]
            public ComponentLookup<TargetsCustom> TargetsCustoms;

            [ReadOnly] public UnsafeComponentLookup<EntityLinkSource> Sources;
            [ReadOnly] public UnsafeBufferLookup<EntityLinkEntry> Links;

            public EntityLock EntityLock;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([EntityIndexInQuery] int sortKey, in TrackBinding binding, in EntityLinkTargetPatch patch)
            {
                var bindingEntity = binding.Value;
                if (bindingEntity == Entity.Null || !this.TargetsLookup.TryGetComponent(bindingEntity, out var targets)) return;

                var resolved = EntityLinkResolver.ResolveOrFallback(
                    bindingEntity,
                    targets,
                    patch,
                    this.TargetsCustoms,
                    this.Sources,
                    this.Links);

                if (resolved == Entity.Null) return;

                using (this.EntityLock.Acquire(bindingEntity))
                {
                    targets = this.TargetsLookup[bindingEntity];
                    
                    switch (patch.WriteTo)
                    {
                        case Target.Owner:
                            targets.Owner = resolved;
                            this.TargetsLookup[bindingEntity] = targets;
                            break;

                        case Target.Source:
                            targets.Source = resolved;
                            this.TargetsLookup[bindingEntity] = targets;
                            break;

                        case Target.Target:
                            targets.Target = resolved;
                            this.TargetsLookup[bindingEntity] = targets;
                            break;

                        case Target.Custom0:
                            this.WriteCustom0(sortKey, bindingEntity, resolved);
                            break;

                        case Target.Custom1:
                            this.WriteCustom1(sortKey, bindingEntity, resolved);
                            break;
                    }
                }
            }

            private void WriteCustom0(int sortKey, Entity entity, Entity target)
            {
                if (this.TargetsCustoms.TryGetComponent(entity, out var custom))
                {
                    custom.Target0 = target;
                    this.TargetsCustoms[entity] = custom;
                    return;
                }

                this.ECB.AddComponent(sortKey, entity, new TargetsCustom { Target0 = target });
            }

            private void WriteCustom1(int sortKey, Entity entity, Entity target)
            {
                if (this.TargetsCustoms.TryGetComponent(entity, out var custom))
                {
                    custom.Target1 = target;
                    this.TargetsCustoms[entity] = custom;
                    return;
                }

                this.ECB.AddComponent(sortKey, entity, new TargetsCustom { Target1 = target });
            }
        }
    }
}