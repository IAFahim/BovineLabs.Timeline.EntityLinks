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
                Links = SystemAPI.GetBufferLookup<EntityLink>(true),
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
            [ReadOnly] public BufferLookup<EntityLink> Links;

            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute(Entity clipEntity, [EntityIndexInQuery] int sortKey, in TrackBinding binding, in EntityLinkTargetPatch patch)
            {
                var bindingEntity = binding.Value;
                if (bindingEntity == Entity.Null)
                {
                    return;
                }

                if (!this.TargetsLookup.TryGetComponent(bindingEntity, out var targets))
                {
                    return;
                }

                var resolved = EntityLinkResolver.ResolveOrFallback(
                    bindingEntity,
                    targets,
                    patch,
                    this.TargetsCustoms,
                    this.Sources,
                    this.Links);

                if (resolved == Entity.Null)
                {
                    return;
                }

                switch (patch.WriteTo)
                {
                    case Target.Owner:
                        targets.Owner = resolved;
                        this.ECB.SetComponent(sortKey, bindingEntity, targets);
                        break;

                    case Target.Source:
                        targets.Source = resolved;
                        this.ECB.SetComponent(sortKey, bindingEntity, targets);
                        break;

                    case Target.Target:
                        targets.Target = resolved;
                        this.ECB.SetComponent(sortKey, bindingEntity, targets);
                        break;

                    case Target.Custom0:
                        this.WriteCustom0(sortKey, bindingEntity, resolved);
                        break;

                    case Target.Custom1:
                        this.WriteCustom1(sortKey, bindingEntity, resolved);
                        break;
                }
            }

            private void WriteCustom0(int sortKey, Entity entity, Entity target)
            {
                if (this.TargetsCustoms.TryGetComponent(entity, out var custom))
                {
                    custom.Target0 = target;
                    this.ECB.SetComponent(sortKey, entity, custom);
                    return;
                }

                this.ECB.AddComponent(sortKey, entity, new TargetsCustom { Target0 = target });
            }

            private void WriteCustom1(int sortKey, Entity entity, Entity target)
            {
                if (this.TargetsCustoms.TryGetComponent(entity, out var custom))
                {
                    custom.Target1 = target;
                    this.ECB.SetComponent(sortKey, entity, custom);
                    return;
                }

                this.ECB.AddComponent(sortKey, entity, new TargetsCustom { Target1 = target });
            }
        }
    }
}
