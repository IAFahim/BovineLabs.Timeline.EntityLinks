using BovineLabs.EntityLinks;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.Core;
using BovineLabs.Timeline.Data;
using BovineLabs.Timeline.Instantiate;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Timeline.EntityLinks
{
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public partial struct EntityLinkInstantiateSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            state.Dependency = new InstantiateAtLinkJob
            {
                ECB = ecb,
                ParentLookup = SystemAPI.GetComponentLookup<Parent>(true),
                TargetsLookup = SystemAPI.GetComponentLookup<Targets>(true),
                StoreLookup = SystemAPI.GetBufferLookup<EntityLookupStoreBuffer>(true),
                LtwLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true)
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive), typeof(OnClipActiveEntityLinkInstantiateTag))]
        [WithNone(typeof(ClipActivePrevious))]
        private partial struct InstantiateAtLinkJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<Parent> ParentLookup;
            [ReadOnly] public ComponentLookup<Targets> TargetsLookup;
            [ReadOnly] public BufferLookup<EntityLookupStoreBuffer> StoreLookup;
            [ReadOnly] public ComponentLookup<LocalToWorld> LtwLookup;

            private void Execute([ChunkIndexInQuery] int chunkIndex, in EntityLinkInstantiateConfig config, in TrackBinding binding)
            {
                var contextEntity = binding.Value;

                // 1. Resolve Link
                if (!EntityLinkResolverUtility.TryResolve(contextEntity, config.LinkKey, config.ResolveRule, ref ParentLookup, ref TargetsLookup, ref StoreLookup, out var resolvedEntity, out var linkOffset))
                {
                    // Failed to resolve. Do not spawn to prevent garbage at origin.
                    return;
                }

                // 2. Instantiate
                var instance = ECB.Instantiate(chunkIndex, config.Prefab);

                // 3. Apply Transform Configuration
                var setParent = config.TransformConfig.HasAny(ParentTransformConfig.SetParent);
                var setTransform = config.TransformConfig.HasAny(ParentTransformConfig.SetTransform);

                if (setParent)
                {
                    ECB.AddComponent(chunkIndex, instance, new Parent { Value = resolvedEntity });
                    if (setTransform)
                    {
                        // If parented, local transform perfectly matches the baked offset
                        ECB.SetComponent(chunkIndex, instance, linkOffset);
                    }
                }
                else if (setTransform)
                {
                    // If NOT parented but SetTransform is true, we must calculate the absolute World Position
                    if (LtwLookup.TryGetComponent(resolvedEntity, out var resolvedLtw))
                    {
                        var worldMatrix = math.mul(resolvedLtw.Value, linkOffset.ToMatrix());
                        worldMatrix.ExtractLocalTransform(out var worldTransform);
                        ECB.SetComponent(chunkIndex, instance, worldTransform);
                    }
                }
            }
        }
    }
}