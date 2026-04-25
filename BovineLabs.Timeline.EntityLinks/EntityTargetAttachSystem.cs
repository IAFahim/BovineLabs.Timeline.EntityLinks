using BovineLabs.Core.Extensions;
using BovineLabs.Core.Iterators;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.Data;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks.Systems
{
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public partial struct EntityTargetAttachSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new ApplyEntityEssenceJob
            {
                TargetsLookup = state.GetUnsafeComponentLookup<Targets>(),
                EssenceRefLookup = SystemAPI.GetComponentLookup<EntityEssenceRef>(true)
            }.Schedule(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(ClipActive), typeof(EntityEssenceClipTag))]
    [WithDisabled(typeof(ClipActivePrevious))]
    internal partial struct ApplyEntityEssenceJob : IJobEntity
    {
        public UnsafeComponentLookup<Targets> TargetsLookup;
        [ReadOnly] public ComponentLookup<EntityEssenceRef> EssenceRefLookup;

        private void Execute(in TrackBinding binding)
        {
            var bindingEntity = binding.Value;
            if (!EssenceRefLookup.TryGetComponent(bindingEntity, out var essenceRef)) return;
            if (essenceRef.Value == Entity.Null) return;

            if (!TargetsLookup.HasComponent(bindingEntity)) return;
            var targets = TargetsLookup[bindingEntity];
            targets.Target = essenceRef.Value;
            TargetsLookup[bindingEntity] = targets;
        }
    }
}