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
    [UpdateAfter(typeof(EntityLinkTargetPatchSystem))]
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
                // Retry resolution every active frame until the first success, instead of only on the enter
                // edge — a one-frame link/parent resolve miss no longer silently kills the whole clip.
                // ExitJob clears ParentApplied so re-entry reparents again.
                if (state.ParentApplied)
                    return;

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

                var childTransform = LocalTransform.FromPositionRotation(config.LocalPosition, config.LocalRotation);

                if (resolvedParent != Entity.Null && LtwLookup.HasComponent(resolvedParent))
                {
                    // Capture the original parent/transform only on the first successful apply so a later
                    // retry frame can't overwrite the snapshot with mid-clip state.
                    state.Target = entityToParent;
                    state.HadParent = ParentLookup.TryGetComponent(entityToParent, out var oldParent);
                    state.PreviousParent = state.HadParent ? oldParent.Value : Entity.Null;
                    state.HadLocalTransform = LocalTransformLookup.TryGetComponent(entityToParent, out var originalLocal);
                    state.OriginalLocalTransform = state.HadLocalTransform ? originalLocal : LocalTransform.Identity;

                    // Reparent through Unity's ParentSystem: write only Parent and let its
                    // PreviousParent-diffing maintain BOTH the old and new parent's Child buffers.
                    // SetupParent must NOT be used for an entity that may already have a parent — it
                    // sets PreviousParent = new parent, suppressing the diff so the child is never
                    // removed from its original parent's Child buffer (hierarchy corruption).
                    if (ParentLookup.HasComponent(entityToParent))
                        ECB.SetComponent(sortKey, entityToParent, new Parent { Value = resolvedParent });
                    else
                        ECB.AddComponent(sortKey, entityToParent, new Parent { Value = resolvedParent });

                    if (state.HadLocalTransform)
                        ECB.SetComponent(sortKey, entityToParent, childTransform);
                    else
                        ECB.AddComponent(sortKey, entityToParent, childTransform);

                    state.ParentApplied = true;
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
                if (state.Target == Entity.Null || !state.ParentApplied)
                    return;

                var sortKey = indexInQuery + ExitSortKeyOffset;

                if (config.RestoreOnEnd)
                {
                    if (state.HadParent && state.PreviousParent != Entity.Null &&
                        LtwLookup.HasComponent(state.PreviousParent))
                    {
                        // Restore the original parent pointer only; ParentSystem moves the child out of
                        // the timeline parent's Child buffer and back into PreviousParent's via diffing.
                        // (Same reason as EnterJob: never hand-roll PreviousParent / the Child buffer.)
                        ECB.SetComponent(sortKey, state.Target, new Parent { Value = state.PreviousParent });

                        if (state.HadLocalTransform)
                            ECB.SetComponent(sortKey, state.Target, state.OriginalLocalTransform);
                    }
                    else
                    {
                        ECB.RemoveComponent<Parent>(sortKey, state.Target);

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

                // Clear on the exit edge (regardless of RestoreOnEnd) so a re-activated clip reparents again.
                state.ParentApplied = false;
            }
        }
    }
}