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
using Unity.Mathematics;
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

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntityLinkParentData>();
            _ltwLookup = state.GetComponentLookup<LocalToWorld>(true);
            _childLookup = state.GetBufferLookup<Child>(true);
            _ptmLookup = state.GetComponentLookup<PostTransformMatrix>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _ltwLookup.Update(ref state);
            _childLookup.Update(ref state);
            _ptmLookup.Update(ref state);

            var ecb = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            state.Dependency = new EnterJob
            {
                TargetsLookup = state.GetUnsafeComponentLookup<Targets>(true),
                Sources = state.GetUnsafeComponentLookup<EntityLinkSource>(true),
                Links = state.GetUnsafeBufferLookup<EntityLinkEntry>(true),
                LtwLookup = _ltwLookup,
                ChildLookup = _childLookup,
                ParentLookup = state.GetUnsafeComponentLookup<Parent>(true),
                LocalTransformLookup = state.GetUnsafeComponentLookup<LocalTransform>(true),
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

                // Only reparent AND move the entity when the parent link actually resolved. If it did not,
                // leave the entity untouched rather than teleporting it to the clip's local pose in world space.
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

            private void Execute(
                [EntityIndexInQuery] int sortKey,
                in EntityLinkParentData config,
                ref EntityLinkParentState state)
            {
                if (!config.RestoreOnEnd || state.Target == Entity.Null || !state.ParentApplied)
                    return;

                if (state.HadParent && state.PreviousParent != Entity.Null &&
                    LtwLookup.TryGetComponent(state.PreviousParent, out var parentLtw))
                {
                    var commands = new CommandBufferParallelCommands(ECB, sortKey, state.Target);
                    var childs = ChildLookup.HasBuffer(state.PreviousParent)
                        ? ChildLookup[state.PreviousParent]
                        : default;

                    TransformUtility.SetupParent(ref commands, state.PreviousParent, state.Target, parentLtw,
                        state.OriginalLocalTransform, childs);

                    // SetupParent writes Parent/PreviousParent/LocalToWorld but NOT LocalTransform. Without
                    // this, the stale clip-offset LocalTransform survives and the next transform pass snaps
                    // the entity to that offset relative to the restored parent. Restore the captured pose.
                    if (state.HadLocalTransform)
                        ECB.SetComponent(sortKey, state.Target, state.OriginalLocalTransform);
                }
                else
                {
                    ECB.RemoveComponent<Parent>(sortKey, state.Target);
                    ECB.RemoveComponent<PreviousParent>(sortKey, state.Target);

                    if (!state.HadLocalTransform)
                    {
                        // We ADDED a LocalTransform on enter (the entity had none); remove it so the
                        // transform-less case is fully idempotent rather than leaving a stray component.
                        ECB.RemoveComponent<LocalTransform>(sortKey, state.Target);
                    }
                    else if (!state.HadParent)
                    {
                        // Genuinely had no parent originally: OriginalLocalTransform is a valid rootless pose.
                        ECB.SetComponent(sortKey, state.Target, state.OriginalLocalTransform);
                    }
                    else if (LtwLookup.TryGetComponent(state.Target, out var selfLtw))
                    {
                        // It HAD a parent that no longer exists, so OriginalLocalTransform is parent-RELATIVE
                        // and writing it as a rootless pose would teleport the entity. Snap the current WORLD
                        // pose as the rootless local pose so it stays where it is. Strip an invertible
                        // PostTransformMatrix first (it carries the non-uniform scale FromMatrix would
                        // collapse), mirroring TemporaryDetachSystem; the matrix component is left in place.
                        var rigid = selfLtw.Value;
                        if (PtmLookup.TryGetComponent(state.Target, out var ptm) &&
                            math.abs(math.determinant(ptm.Value)) > 1e-12f)
                        {
                            var candidate = math.mul(selfLtw.Value, math.inverse(ptm.Value));
                            if (math.all(math.isfinite(candidate.c0)) && math.all(math.isfinite(candidate.c1)) &&
                                math.all(math.isfinite(candidate.c2)) && math.all(math.isfinite(candidate.c3)))
                                rigid = candidate;
                        }

                        ECB.SetComponent(sortKey, state.Target, LocalTransform.FromMatrix(rigid));
                    }
                }
            }
        }
    }
}