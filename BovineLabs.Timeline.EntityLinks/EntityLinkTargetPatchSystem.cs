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
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new PatchJob
            {
                TargetsLookup = SystemAPI.GetComponentLookup<Targets>(),
                TargetsCustoms = SystemAPI.GetComponentLookup<TargetsCustom>(),
                Sources = SystemAPI.GetComponentLookup<EntityLinkSource>(true),
                Maps = SystemAPI.GetComponentLookup<EntityLinkMap>(true),
                Values = SystemAPI.GetBufferLookup<EntityLinkValue>(true),
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct PatchJob : IJobEntity
        {
            public ComponentLookup<Targets> TargetsLookup;
            public ComponentLookup<TargetsCustom> TargetsCustoms;

            [ReadOnly] public ComponentLookup<EntityLinkSource> Sources;
            [ReadOnly] public ComponentLookup<EntityLinkMap> Maps;
            [ReadOnly] public BufferLookup<EntityLinkValue> Values;

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
                    this.Maps,
                    this.Values);

                if (resolved == Entity.Null)
                {
                    return;
                }

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
