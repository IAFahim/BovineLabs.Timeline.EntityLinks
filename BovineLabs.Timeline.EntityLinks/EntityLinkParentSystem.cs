using BovineLabs.Core.EntityCommands;
using BovineLabs.Core.Utility;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.Data;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace BovineLabs.Timeline.EntityLinks
{
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public partial struct EntityLinkParentSystem : ISystem
    {
        private ComponentLookup<LocalToWorld> _ltwLookup;
        private BufferLookup<Child> _childLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntityLinkParentData>();
            _ltwLookup = state.GetComponentLookup<LocalToWorld>(true);
            _childLookup = state.GetBufferLookup<Child>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _ltwLookup.Update(ref state);
            _childLookup.Update(ref state);

            var ecb = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            state.Dependency = new EnterJob
            {
                TargetsLookup = SystemAPI.GetComponentLookup<Targets>(true),
                TargetsCustoms = SystemAPI.GetComponentLookup<TargetsCustom>(true),
                Sources = SystemAPI.GetComponentLookup<EntityLinkSource>(true),
                Links = SystemAPI.GetBufferLookup<EntityLinkEntry>(true),
                LtwLookup = _ltwLookup,
                ChildLookup = _childLookup,
                ParentLookup = SystemAPI.GetComponentLookup<Parent>(true),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                ECB = ecb
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new ExitJob
            {
                LtwLookup = _ltwLookup,
                ChildLookup = _childLookup,
                ECB = ecb
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct EnterJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<Targets> TargetsLookup;
            [ReadOnly] public ComponentLookup<TargetsCustom> TargetsCustoms;
            [ReadOnly] public ComponentLookup<EntityLinkSource> Sources;
            [ReadOnly] public BufferLookup<EntityLinkEntry> Links;
            [ReadOnly] public ComponentLookup<LocalToWorld> LtwLookup;
            [ReadOnly] public BufferLookup<Child> ChildLookup;
            [ReadOnly] public ComponentLookup<Parent> ParentLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;

            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute(
                Entity clipEntity,
                [EntityIndexInQuery] int sortKey,
                in TrackBinding binding,
                in EntityLinkParentData config,
                ref EntityLinkParentState state)
            {
                var bindingEntity = binding.Value;
                if (bindingEntity == Entity.Null)
                    return;

                if (!TargetsLookup.TryGetComponent(bindingEntity, out var targets))
                    return;

                var entityToParent = targets.Get(config.EntityToParent, bindingEntity, TargetsCustoms);
                if (entityToParent == Entity.Null)
                    return;

                var rootCandidate = targets.Get(config.ReadRootFrom, bindingEntity, TargetsCustoms);
                if (rootCandidate == Entity.Null)
                    return;

                if (!EntityLinkResolver.TryResolveRoot(rootCandidate, Sources, out var root))
                    return;

                if (!EntityLinkResolver.TryResolveFromRoot(root, config.ParentLinkKey, Links, out var resolvedParent))
                    return;

                state.Target = entityToParent;
                state.HadParent = ParentLookup.TryGetComponent(entityToParent, out var oldParent);
                state.PreviousParent = state.HadParent ? oldParent.Value : Entity.Null;

                var childTransform = LocalTransform.FromPositionRotation(config.LocalPosition, config.LocalRotation);
                var commands = new CommandBufferParallelCommands(ECB, sortKey, entityToParent);

                if (resolvedParent != Entity.Null && LtwLookup.TryGetComponent(resolvedParent, out var parentLtw))
                {
                    var childs = ChildLookup.HasBuffer(resolvedParent) ? ChildLookup[resolvedParent] : default;
                    TransformUtility.SetupParent(ref commands, resolvedParent, entityToParent, parentLtw, childTransform, childs);
                    state.ParentApplied = true;
                }

                if (LocalTransformLookup.HasComponent(entityToParent))
                    ECB.SetComponent(sortKey, entityToParent, childTransform);
                else
                    ECB.AddComponent(sortKey, entityToParent, childTransform);
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClipActivePrevious))]
        [WithDisabled(typeof(ClipActive))]
        private partial struct ExitJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<LocalToWorld> LtwLookup;
            [ReadOnly] public BufferLookup<Child> ChildLookup;

            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute(
                [EntityIndexInQuery] int sortKey,
                in EntityLinkParentData config,
                ref EntityLinkParentState state)
            {
                if (!config.RestoreOnEnd || state.Target == Entity.Null || !state.ParentApplied)
                    return;

                if (state.HadParent && state.PreviousParent != Entity.Null && LtwLookup.TryGetComponent(state.PreviousParent, out var parentLtw))
                {
                    var commands = new CommandBufferParallelCommands(ECB, sortKey, state.Target);
                    var childs = ChildLookup.HasBuffer(state.PreviousParent) ? ChildLookup[state.PreviousParent] : default;
                    
                    // Note: We don't have its current LocalTransform easily here to preserve world space perfectly, 
                    // but restoring the original Parent hierarchy prevents orphaned Transforms.
                    TransformUtility.SetupParent(ref commands, state.PreviousParent, state.Target, parentLtw, LocalTransform.Identity, childs);
                }
                else
                {
                    ECB.RemoveComponent<Parent>(sortKey, state.Target);
                    ECB.RemoveComponent<PreviousParent>(sortKey, state.Target);
                }
            }
        }
    }
}