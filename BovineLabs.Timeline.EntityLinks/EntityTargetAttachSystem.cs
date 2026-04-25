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
        private UnsafeComponentLookup<Targets> _entityTargetLookup;

        public void OnCreate(ref SystemState state)
        {
            _entityTargetLookup = state.GetUnsafeComponentLookup<Targets>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _entityTargetLookup.Update(ref state);

            state.Dependency = new ApplyEntityEssenceJob
            {
                TargetLookup = _entityTargetLookup,
                EssenceRefLookup = SystemAPI.GetComponentLookup<EntityEssenceRef>(true),
            }.Schedule(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(ClipActive), typeof(EntityEssenceClipTag))]
    [WithDisabled(typeof(ClipActivePrevious))]
    internal partial struct ApplyEntityEssenceJob : IJobEntity
    {
        public UnsafeComponentLookup<Targets> TargetLookup;
        [ReadOnly] public ComponentLookup<EntityEssenceRef> EssenceRefLookup;

        private void Execute(in TrackBinding binding)
        {
            var bindingEntity = binding.Value;
            if (!EssenceRefLookup.TryGetComponent(bindingEntity, out var essenceRef)) return;
            if (essenceRef.Value == Entity.Null) return;

            var targets = TargetLookup[bindingEntity];
            targets.Target = essenceRef.Value;
            TargetLookup[bindingEntity] = targets;
        }
    }
}