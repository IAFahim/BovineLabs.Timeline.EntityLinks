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
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntityLinkTargetPatch>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new PatchJob
            {
                TargetsLookup = SystemAPI.GetComponentLookup<Targets>(true),
                TargetsCustoms = SystemAPI.GetComponentLookup<TargetsCustom>(true),
                Sources = SystemAPI.GetComponentLookup<EntityLinkSource>(true),
                Links = SystemAPI.GetBufferLookup<EntityLinkEntry>(true),
                ECB = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct PatchJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<Targets> TargetsLookup;
            [ReadOnly] public ComponentLookup<TargetsCustom> TargetsCustoms;

            [ReadOnly] public ComponentLookup<EntityLinkSource> Sources;
            [ReadOnly] public BufferLookup<EntityLinkEntry> Links;

            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute(Entity clipEntity, [EntityIndexInQuery] int sortKey, in TrackBinding binding,
                in EntityLinkTargetPatch patch)
            {
                var bindingEntity = binding.Value;
                if (bindingEntity == Entity.Null) return;

                if (!TargetsLookup.TryGetComponent(bindingEntity, out var targets)) return;

                var resolved = EntityLinkResolver.ResolveOrFallback(
                    bindingEntity,
                    targets,
                    patch,
                    TargetsCustoms,
                    Sources,
                    Links);

                if (resolved == Entity.Null) return;

                switch (patch.WriteTo)
                {
                    case Target.Owner:
                        targets.Owner = resolved;
                        ECB.SetComponent(sortKey, bindingEntity, targets);
                        break;

                    case Target.Source:
                        targets.Source = resolved;
                        ECB.SetComponent(sortKey, bindingEntity, targets);
                        break;

                    case Target.Target:
                        targets.Target = resolved;
                        ECB.SetComponent(sortKey, bindingEntity, targets);
                        break;

                    case Target.Custom0:
                        WriteCustom0(sortKey, bindingEntity, resolved);
                        break;

                    case Target.Custom1:
                        WriteCustom1(sortKey, bindingEntity, resolved);
                        break;
                }
            }

            private void WriteCustom0(int sortKey, Entity entity, Entity target)
            {
                if (TargetsCustoms.TryGetComponent(entity, out var custom))
                {
                    custom.Target0 = target;
                    ECB.SetComponent(sortKey, entity, custom);
                    return;
                }

                ECB.AddComponent(sortKey, entity, new TargetsCustom { Target0 = target });
            }

            private void WriteCustom1(int sortKey, Entity entity, Entity target)
            {
                if (TargetsCustoms.TryGetComponent(entity, out var custom))
                {
                    custom.Target1 = target;
                    ECB.SetComponent(sortKey, entity, custom);
                    return;
                }

                ECB.AddComponent(sortKey, entity, new TargetsCustom { Target1 = target });
            }
        }
    }
}