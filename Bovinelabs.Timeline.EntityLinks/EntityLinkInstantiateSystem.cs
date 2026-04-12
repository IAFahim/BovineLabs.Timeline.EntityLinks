using BovineLabs.Core.Extensions;
using BovineLabs.Core.Iterators;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline;
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
    public partial struct EntityLinkInstantiateSystem : ISystem
    {
        private UnsafeComponentLookup<Parent> parents;
        private UnsafeComponentLookup<Targets> targets;
        private UnsafeBufferLookup<EntityLookupStoreData> stores;
        private UnsafeComponentLookup<LocalTransform> localTransforms;
        private UnsafeComponentLookup<LocalToWorld> worldTransforms;
        private UnsafeComponentLookup<PostTransformMatrix> postTransformMatrices;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            this.parents = state.GetUnsafeComponentLookup<Parent>(true);
            this.targets = state.GetUnsafeComponentLookup<Targets>(true);
            this.stores = state.GetUnsafeBufferLookup<EntityLookupStoreData>(true);
            this.localTransforms = state.GetUnsafeComponentLookup<LocalTransform>(true);
            this.worldTransforms = state.GetUnsafeComponentLookup<LocalToWorld>(true);
            this.postTransformMatrices = state.GetUnsafeComponentLookup<PostTransformMatrix>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.parents.Update(ref state);
            this.targets.Update(ref state);
            this.stores.Update(ref state);
            this.localTransforms.Update(ref state);
            this.worldTransforms.Update(ref state);
            this.postTransformMatrices.Update(ref state);

            var commands = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var graph = new Graph
            {
                Parents = this.parents,
                Targets = this.targets,
                Stores = this.stores
            };

            state.Dependency = new ConstructTransition
            {
                Commands = commands,
                Graph = graph,
                LocalTransforms = this.localTransforms,
                WorldTransforms = this.worldTransforms,
                PostTransformMatrices = this.postTransformMatrices
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithNone(typeof(ClipActivePrevious))]
        private partial struct ConstructTransition : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Commands;
            public Graph Graph;
            [ReadOnly] public UnsafeComponentLookup<LocalTransform> LocalTransforms;
            [ReadOnly] public UnsafeComponentLookup<LocalToWorld> WorldTransforms;
            [ReadOnly] public UnsafeComponentLookup<PostTransformMatrix> PostTransformMatrices;

            private void Execute([ChunkIndexInQuery] int chunk, in EntityLinkInstantiateConfig config, in TrackBinding binding)
            {
                this.Graph.Evaluate(binding.Value, config.LinkKey, config.ResolveRule, 1, out var destination);

                if (destination == Entity.Null) return;

                var instance = this.Commands.Instantiate(chunk, config.Prefab);
                var local = this.LocalTransforms.TryGetComponent(config.Prefab, out var l) ? l : LocalTransform.Identity;

                var hadPtm = this.PostTransformMatrices.TryGetComponent(config.Prefab, out var ptmData);
                var ptm = hadPtm ? ptmData.Value : float4x4.identity;

                var world = new LocalToWorld { Value = math.mul(local.ToMatrix(), ptm) };
                var destinationWorld = this.WorldTransforms.TryGetComponent(destination, out var dw)
                    ? dw
                    : new LocalToWorld { Value = float4x4.identity };

                this.ApplyTopology(chunk, instance, destination, local, ptm, hadPtm, world, destinationWorld, config.TransformFlags);
            }

            private void ApplyTopology(int chunk, Entity instance, Entity destination,
                LocalTransform local, float4x4 ptm, bool hadPtm, LocalToWorld world, LocalToWorld destinationWorld,
                AttachmentTransformFlags flags)
            {
                if (flags.HasAny(AttachmentTransformFlags.SetParent))
                    this.Commands.AddComponent(chunk, instance, new Parent { Value = destination });

                var resolvedTopology = Topology.Evaluate(local, ptm, hadPtm, world, destinationWorld, flags);

                this.Commands.SetComponent(chunk, instance, resolvedTopology.Local);

                if (resolvedTopology.HasPostTransform)
                    this.Commands.AddComponent(chunk, instance, new PostTransformMatrix { Value = resolvedTopology.PostTransform });
            }
        }
    }
}