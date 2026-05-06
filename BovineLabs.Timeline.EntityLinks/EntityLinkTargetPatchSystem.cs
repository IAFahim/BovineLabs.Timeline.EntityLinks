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
            targetsLookup = state.GetComponentLookup<Targets>();
            targetsCustoms = state.GetComponentLookup<TargetsCustom>();
            sources = state.GetUnsafeComponentLookup<EntityLinkSource>(true);
            links = state.GetUnsafeBufferLookup<EntityLinkEntry>(true);
            entityLock = new EntityLock(Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            entityLock.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            targetsLookup.Update(ref state);
            targetsCustoms.Update(ref state);
            sources.Update(ref state);
            links.Update(ref state);

            state.Dependency = new PatchJob
            {
                TargetsLookup = targetsLookup,
                TargetsCustoms = targetsCustoms,
                Sources = sources,
                Links = links,
                EntityLock = entityLock,
                ECB = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct PatchJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public ComponentLookup<Targets> TargetsLookup;

            [NativeDisableParallelForRestriction] public ComponentLookup<TargetsCustom> TargetsCustoms;

            [ReadOnly] public UnsafeComponentLookup<EntityLinkSource> Sources;
            [ReadOnly] public UnsafeBufferLookup<EntityLinkEntry> Links;

            public EntityLock EntityLock;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([EntityIndexInQuery] int sortKey, in TrackBinding binding,
                in EntityLinkTargetPatch patch)
            {
                var bindingEntity = binding.Value;
                if (bindingEntity == Entity.Null ||
                    !TargetsLookup.TryGetComponent(bindingEntity, out var targets)) return;

                var resolved = EntityLinkResolver.ResolveOrFallback(
                    bindingEntity,
                    targets,
                    patch,
                    TargetsCustoms,
                    Sources,
                    Links);

                if (resolved == Entity.Null) return;

                using (EntityLock.Acquire(bindingEntity))
                {
                    targets = TargetsLookup[bindingEntity];

                    switch (patch.WriteTo)
                    {
                        case Target.Owner:
                            targets.Owner = resolved;
                            TargetsLookup[bindingEntity] = targets;
                            break;

                        case Target.Source:
                            targets.Source = resolved;
                            TargetsLookup[bindingEntity] = targets;
                            break;

                        case Target.Target:
                            targets.Target = resolved;
                            TargetsLookup[bindingEntity] = targets;
                            break;

                        case Target.Custom0:
                            WriteCustom0(sortKey, bindingEntity, resolved);
                            break;

                        case Target.Custom1:
                            WriteCustom1(sortKey, bindingEntity, resolved);
                            break;
                    }
                }
            }

            private void WriteCustom0(int sortKey, Entity entity, Entity target)
            {
                if (TargetsCustoms.TryGetComponent(entity, out var custom))
                {
                    custom.Target0 = target;
                    TargetsCustoms[entity] = custom;
                    return;
                }

                ECB.AddComponent(sortKey, entity, new TargetsCustom { Target0 = target });
            }

            private void WriteCustom1(int sortKey, Entity entity, Entity target)
            {
                if (TargetsCustoms.TryGetComponent(entity, out var custom))
                {
                    custom.Target1 = target;
                    TargetsCustoms[entity] = custom;
                    return;
                }

                ECB.AddComponent(sortKey, entity, new TargetsCustom { Target1 = target });
            }
        }
    }
}