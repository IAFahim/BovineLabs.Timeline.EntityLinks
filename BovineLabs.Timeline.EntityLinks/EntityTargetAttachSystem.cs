using System.Runtime.CompilerServices;
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
        private UnsafeComponentLookup<EntityEssenceRefComponent> _entityTargetRefComponentLookup;

        public void OnCreate(ref SystemState state)
        {
            _entityTargetLookup = state.GetUnsafeComponentLookup<Targets>();
            _entityTargetRefComponentLookup = state.GetUnsafeComponentLookup<EntityEssenceRefComponent>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _entityTargetLookup.Update(ref state);
            _entityTargetRefComponentLookup.Update(ref state);

            new ApplyEntityEssenceJob
            {
                TargetLookup = _entityTargetLookup,
                EntityTargetRefComponentLookup = _entityTargetRefComponentLookup
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    [WithAll(typeof(ClipActive), typeof(EntityEssenceClipTag))]
    [WithDisabled(typeof(ClipActivePrevious))]
    internal partial struct ApplyEntityEssenceJob : IJobEntity
    {
        [NativeDisableParallelForRestriction] public UnsafeComponentLookup<Targets> TargetLookup;

        [ReadOnly] public UnsafeComponentLookup<EntityEssenceRefComponent> EntityTargetRefComponentLookup;

        private void Execute(in TrackBinding binding)
        {
            UpdateTargetEntity(binding.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateTargetEntity(in Entity bindingEntity)
        {
            var targets = TargetLookup[bindingEntity];
            var resolvedTarget = ResolveTargetEntity(targets);
            targets.Target = resolvedTarget;
            TargetLookup[bindingEntity] = targets;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Entity ResolveTargetEntity(in Targets targets)
        {
            return EntityTargetRefComponentLookup[targets.Target].EssenceEntity;
        }
    }
}