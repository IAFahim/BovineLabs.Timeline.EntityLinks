using BovineLabs.Core.EntityCommands;
using BovineLabs.Core.Extensions;
using BovineLabs.Core.Iterators;
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
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation)]
    public partial struct EntityLinkParentSystem : ISystem
    {
        private ComponentLookup<LocalToWorld> _ltwLookup;
        private BufferLookup<Child> _childLookup;
        private ComponentLookup<PostTransformMatrix> _ptmLookup;
        private UnsafeComponentLookup<Targets> _targetsLookup;
        private UnsafeComponentLookup<EntityLinkSource> _sources;
        private UnsafeBufferLookup<EntityLinkEntry> _links;
        private UnsafeComponentLookup<Parent> _parentLookup;
        private UnsafeComponentLookup<LocalTransform> _localTransformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntityLinkParentData>();
            _ltwLookup = state.GetComponentLookup<LocalToWorld>(true);
            _childLookup = state.GetBufferLookup<Child>(true);
            _ptmLookup = state.GetComponentLookup<PostTransformMatrix>(true);
            _targetsLookup = state.GetUnsafeComponentLookup<Targets>(true);
            _sources = state.GetUnsafeComponentLookup<EntityLinkSource>(true);
            _links = state.GetUnsafeBufferLookup<EntityLinkEntry>(true);
            _parentLookup = state.GetUnsafeComponentLookup<Parent>(true);
            _localTransformLookup = state.GetUnsafeComponentLookup<LocalTransform>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _ltwLookup.Update(ref state);
            _childLookup.Update(ref state);
            _ptmLookup.Update(ref state);
            _targetsLookup.Update(ref state);
            _sources.Update(ref state);
            _links.Update(ref state);
            _parentLookup.Update(ref state);
            _localTransformLookup.Update(ref state);

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            state.Dependency = new EnterJob
            {
                TargetsLookup = _targetsLookup,
                Sources = _sources,
                Links = _links,
                LtwLookup = _ltwLookup,
                ChildLookup = _childLookup,
                ParentLookup = _parentLookup,
                LocalTransformLookup = _localTransformLookup,
                ECB = ecb
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new ExitJob
            {
                LtwLookup = _ltwLookup,
                ChildLookup = _childLookup,
                PtmLookup = _ptmLookup,
                ECB = ecb
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct EnterJob : IJobEntity
        {
            [ReadOnly] public UnsafeComponentLookup<Targets> TargetsLookup;
            [ReadOnly] public UnsafeComponentLookup<EntityLinkSource> Sources;
            [ReadOnly] public UnsafeBufferLookup<EntityLinkEntry> Links;
            [ReadOnly] public ComponentLookup<LocalToWorld> LtwLookup;
            [ReadOnly] public BufferLookup<Child> ChildLookup;
            [ReadOnly] public UnsafeComponentLookup<Parent> ParentLookup;
            [ReadOnly] public UnsafeComponentLookup<LocalTransform> LocalTransformLookup;

            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute(
                [EntityIndexInQuery] int sortKey,
                in TrackBinding binding,
                in EntityLinkParentData config,
                ref EntityLinkParentState state)
            {
                state.ParentApplied = false;

                var bindingEntity = binding.Value;
                if (bindingEntity == Entity.Null)
                    return;

                if (!TargetsLookup.TryGetComponent(bindingEntity, out var targets))
                    return;

                var entityToParent = targets.Get(config.EntityToParent, bindingEntity);
                if (entityToParent == Entity.Null)
                    return;

                var rootCandidate = targets.Get(config.ReadRootFrom, bindingEntity);
                if (rootCandidate == Entity.Null)
                    return;

                if (!EntityLinkResolver.TryResolveRoot(rootCandidate, Sources, out var root))
                    return;

                if (!EntityLinkResolver.TryResolveFromRoot(root, config.ParentLinkKey, Links, out var resolvedParent))
                    return;

                state.Target = entityToParent;
                state.HadParent = ParentLookup.TryGetComponent(entityToParent, out var oldParent);
                state.PreviousParent = state.HadParent ? oldParent.Value : Entity.Null;
                state.HadLocalTransform = LocalTransformLookup.TryGetComponent(entityToParent, out var originalLocal);
                state.OriginalLocalTransform = state.HadLocalTransform ? originalLocal : LocalTransform.Identity;

                var childTransform = LocalTransform.FromPositionRotation(config.LocalPosition, config.LocalRotation);
                var commands = new CommandBufferParallelCommands(ECB, sortKey, entityToParent);

                if (resolvedParent != Entity.Null && LtwLookup.TryGetComponent(resolvedParent, out var parentLtw))
                {
                    var childs = ChildLookup.HasBuffer(resolvedParent) ? ChildLookup[resolvedParent] : default;
                    TransformUtility.SetupParent(ref commands, resolvedParent, entityToParent, parentLtw,
                        childTransform, childs);
                    state.ParentApplied = true;

                    if (state.HadLocalTransform)
                        ECB.SetComponent(sortKey, entityToParent, childTransform);
                    else
                        ECB.AddComponent(sortKey, entityToParent, childTransform);
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClipActivePrevious))]
        [WithDisabled(typeof(ClipActive))]
        private partial struct ExitJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<LocalToWorld> LtwLookup;
            [ReadOnly] public BufferLookup<Child> ChildLookup;
            [ReadOnly] public ComponentLookup<PostTransformMatrix> PtmLookup;

            public EntityCommandBuffer.ParallelWriter ECB;

            public const int ExitSortKeyOffset = 1 << 24;

            private void Execute(
                [EntityIndexInQuery] int indexInQuery,
                in EntityLinkParentData config,
                ref EntityLinkParentState state)
            {
                if (!config.RestoreOnEnd || state.Target == Entity.Null || !state.ParentApplied)
                    return;

                var sortKey = indexInQuery + ExitSortKeyOffset;

                if (state.HadParent && state.PreviousParent != Entity.Null &&
                    LtwLookup.TryGetComponent(state.PreviousParent, out var parentLtw))
                {
                    var commands = new CommandBufferParallelCommands(ECB, sortKey, state.Target);
                    var childs = ChildLookup.HasBuffer(state.PreviousParent)
                        ? ChildLookup[state.PreviousParent]
                        : default;

                    TransformUtility.SetupParent(ref commands, state.PreviousParent, state.Target, parentLtw,
                        state.OriginalLocalTransform, childs);

                    if (state.HadLocalTransform)
                        ECB.SetComponent(sortKey, state.Target, state.OriginalLocalTransform);
                }
                else
                {
                    ECB.RemoveComponent<Parent>(sortKey, state.Target);
                    ECB.RemoveComponent<PreviousParent>(sortKey, state.Target);

                    if (!state.HadLocalTransform)
                    {
                        ECB.RemoveComponent<LocalTransform>(sortKey, state.Target);
                    }
                    else if (!state.HadParent)
                    {
                        ECB.SetComponent(sortKey, state.Target, state.OriginalLocalTransform);
                    }
                    else if (LtwLookup.TryGetComponent(state.Target, out var selfLtw))
                    {
                        var hasPtm = PtmLookup.TryGetComponent(state.Target, out var ptm);
                        EntityLinkParentRecovery.TryRecoverRigid(selfLtw.Value, hasPtm, ptm.Value, out var rigid);
                        ECB.SetComponent(sortKey, state.Target, LocalTransform.FromMatrix(rigid));
                    }
                }
            }
        }
    }
}