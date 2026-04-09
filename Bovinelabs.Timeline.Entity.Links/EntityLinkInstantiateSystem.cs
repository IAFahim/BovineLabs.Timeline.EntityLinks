using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline;
using BovineLabs.Timeline.Data;
using Bovinelabs.Timeline.Entity.Links.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Bovinelabs.Timeline.Entity.Links
{
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public partial struct EntityLinkInstantiateSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commands = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var graph = new Graph
            {
                Parents = SystemAPI.GetComponentLookup<Parent>(true),
                Targets = SystemAPI.GetComponentLookup<Targets>(true),
                Stores = SystemAPI.GetBufferLookup<EntityLookupStoreData>(true)
            };

            state.Dependency = new ConstructTransition
            {
                Commands = commands,
                Graph = graph,
                LocalTransforms = SystemAPI.GetComponentLookup<LocalTransform>(true),
                WorldTransforms = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                PostTransformMatrices = SystemAPI.GetComponentLookup<PostTransformMatrix>(true)
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithNone(typeof(ClipActivePrevious))]
        private partial struct ConstructTransition : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Commands;
            public Graph Graph;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransforms;
            [ReadOnly] public ComponentLookup<LocalToWorld> WorldTransforms;
            [ReadOnly] public ComponentLookup<PostTransformMatrix> PostTransformMatrices;

            private void Execute([ChunkIndexInQuery] int chunk, in EntityLinkInstantiateConfig config, in TrackBinding binding)
            {
                var destination = this.Graph.Evaluate(binding.Value, config.LinkKey, config.ResolveRule);

                if (destination == Unity.Entities.Entity.Null)
                {
                    return;
                }

                var instance = this.Commands.Instantiate(chunk, config.Prefab);
                var local = this.LocalTransforms.TryGetComponent(config.Prefab, out var l) ? l : LocalTransform.Identity;
                
                var hadPtm = this.PostTransformMatrices.HasComponent(config.Prefab);
                var ptm = hadPtm ? this.PostTransformMatrices[config.Prefab].Value : float4x4.identity;

                var world = new LocalToWorld { Value = math.mul(local.ToMatrix(), ptm) };
                var destinationWorld = this.WorldTransforms.TryGetComponent(destination, out var dw) ? dw : new LocalToWorld { Value = float4x4.identity };

                this.ApplyTopology(chunk, instance, destination, local, ptm, hadPtm, world, destinationWorld, config.TransformFlags);
            }

            private void ApplyTopology(int chunk, Unity.Entities.Entity instance, Unity.Entities.Entity destination, LocalTransform local, float4x4 ptm, bool hadPtm, LocalToWorld world, LocalToWorld destinationWorld, AttachmentTransformFlags flags)
            {
                if (flags.HasAny(AttachmentTransformFlags.SetParent))
                {
                    this.Commands.AddComponent(chunk, instance, new Parent { Value = destination });
                }

                var resolvedTopology = Topology.Evaluate(local, ptm, hadPtm, world, destinationWorld, flags);
                
                this.Commands.SetComponent(chunk, instance, resolvedTopology.Local);
                
                if (resolvedTopology.HasPostTransform)
                {
                    this.Commands.AddComponent(chunk, instance, new PostTransformMatrix { Value = resolvedTopology.PostTransform });
                }
            }
        }
    }
}