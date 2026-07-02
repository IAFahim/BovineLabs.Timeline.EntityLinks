using BovineLabs.Core.Extensions;
using BovineLabs.Core.Iterators;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.Data;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Timeline.EntityLinks
{
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    [UpdateAfter(typeof(EntityLinkMutateSystem))]
    [UpdateAfter(typeof(EntityLinkTargetPatchSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation)]
    public partial struct EntityLinkCopyTransformSystem : ISystem
    {
        private ComponentLookup<LocalToWorld> _ltwLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private ComponentLookup<Parent> _parentLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntityLinkCopyTransform>();
            _ltwLookup = state.GetComponentLookup<LocalToWorld>(true);
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(false); // written directly (see CopyTransformJob)
            _parentLookup = state.GetComponentLookup<Parent>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _ltwLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            _parentLookup.Update(ref state);

            state.Dependency = new CopyTransformJob
            {
                TargetsLookup = state.GetUnsafeComponentLookup<Targets>(true),
                Sources = state.GetUnsafeComponentLookup<EntityLinkSource>(true),
                Links = state.GetUnsafeBufferLookup<EntityLinkEntry>(true),
                LtwLookup = _ltwLookup,
                LocalTransformLookup = _localTransformLookup,
                ParentLookup = _parentLookup
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        private partial struct CopyTransformJob : IJobEntity
        {
            [ReadOnly] public UnsafeComponentLookup<Targets> TargetsLookup;
            [ReadOnly] public UnsafeComponentLookup<EntityLinkSource> Sources;
            [ReadOnly] public UnsafeBufferLookup<EntityLinkEntry> Links;
            [ReadOnly] public ComponentLookup<LocalToWorld> LtwLookup;
            [ReadOnly] public ComponentLookup<Parent> ParentLookup;

            // Read (current pose) and written (composed result) in the same job. NativeDisableParallelForRestriction
            // because writes are keyed by resolved target entity, not the iterated clip entity.
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> LocalTransformLookup;

            private void Execute(in TrackBinding binding, in EntityLinkCopyTransform config)
            {
                var bindingEntity = binding.Value;
                if (bindingEntity == Entity.Null ||
                    !TargetsLookup.TryGetComponent(bindingEntity, out var targets)) return;

                var entityToMove = targets.Get(config.EntityToMove, bindingEntity);
                if (entityToMove == Entity.Null ||
                    !LocalTransformLookup.TryGetComponent(entityToMove, out var targetTransform)) return;

                var resolvedSource = EntityLinkResolver.ResolveOrFallback(
                    bindingEntity,
                    targets,
                    new EntityLinkTargetPatch
                        { ReadRootFrom = config.ReadRootFrom, LinkKey = config.LinkKey, Fallback = Target.None },
                    Sources,
                    Links);

                if (resolvedSource == Entity.Null ||
                    !LtwLookup.TryGetComponent(resolvedSource, out var sourceLtw)) return;

                var parentLtw = float4x4.identity;
                var hasParent = false;
                if (ParentLookup.TryGetComponent(entityToMove, out var parent) &&
                    LtwLookup.TryGetComponent(parent.Value, out var parentLtwValue))
                {
                    hasParent = true;
                    parentLtw = parentLtwValue.Value;
                }

                EntityLinkTransform.TryComposeChildLocal(
                    targetTransform,
                    sourceLtw.Value,
                    config.CopyPosition,
                    config.CopyRotation,
                    config.PositionOffset,
                    config.RotationOffset,
                    hasParent,
                    parentLtw,
                    out var result);

                // Direct value write (the line-77 guard proves the component exists) — consumed by
                // TransformSystemGroup this frame, avoiding the begin-frame ECB's one-frame follow lag.
                LocalTransformLookup[entityToMove] = result;
            }
        }
    }
}