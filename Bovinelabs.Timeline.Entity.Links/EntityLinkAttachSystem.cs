using BovineLabs.EntityLinks;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace BovineLabs.Timeline.EntityLinks
{
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public partial struct EntityLinkAttachSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var parentLookup = SystemAPI.GetComponentLookup<Parent>(true);
            var targetsLookup = SystemAPI.GetComponentLookup<Targets>(true);
            var storeLookup = SystemAPI.GetBufferLookup<EntityLookupStoreBuffer>(true);
            var ltwLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

            state.Dependency = new AttachJob
            {
                ECB = ecb,
                ParentLookup = parentLookup,
                TargetsLookup = targetsLookup,
                StoreLookup = storeLookup,
                LocalTransformLookup = localTransformLookup
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new DetachJob
            {
                ECB = ecb
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithNone(typeof(ClipActivePrevious))]
        private partial struct AttachJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<Parent> ParentLookup;
            [ReadOnly] public ComponentLookup<Targets> TargetsLookup;
            [ReadOnly] public BufferLookup<EntityLookupStoreBuffer> StoreLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;

            private void Execute([ChunkIndexInQuery] int chunkIndex, ref EntityLinkAttachState state, in TrackBinding binding)
            {
                var target = binding.Value;

                // 1. Resolve the Link
                if (!EntityLinkResolverUtility.TryResolve(target, state.LinkKey, state.ResolveRule, ref ParentLookup, ref TargetsLookup, ref StoreLookup, out var resolvedEntity, out var linkOffset))
                {
                    state.WasSuccessfullyAttached = false;
                    state.ResolvedTarget = Entity.Null;
                    return;
                }

                // 2. Capture Original State
                state.CapturedPreviousParent = ParentLookup.TryGetComponent(target, out var p) ? p.Value : Entity.Null;
                state.CapturedOriginalTransform = LocalTransformLookup.TryGetComponent(target, out var lt) ? lt : LocalTransform.Identity;
                state.WasSuccessfullyAttached = true;
                state.ResolvedTarget = resolvedEntity;

                // 3. Attach and Apply Offset
                ECB.AddComponent(chunkIndex, target, new Parent { Value = resolvedEntity });
                ECB.SetComponent(chunkIndex, target, linkOffset);
            }
        }

        [BurstCompile]
        [WithNone(typeof(ClipActive))]
        [WithAll(typeof(ClipActivePrevious))]
        private partial struct DetachJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int chunkIndex, in EntityLinkAttachState state, in TrackBinding binding)
            {
                if (!state.WasSuccessfullyAttached) return;

                var target = binding.Value;

                // 1. Restore Original Transform
                ECB.SetComponent(chunkIndex, target, state.CapturedOriginalTransform);

                // 2. Restore Original Parent
                if (state.CapturedPreviousParent == Entity.Null)
                {
                    ECB.RemoveComponent<Parent>(chunkIndex, target);
                    ECB.RemoveComponent<PreviousParent>(chunkIndex, target);
                }
                else
                {
                    ECB.AddComponent(chunkIndex, target, new Parent { Value = state.CapturedPreviousParent });
                }
            }
        }
    }
}