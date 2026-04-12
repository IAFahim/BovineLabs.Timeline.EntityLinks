using BovineLabs.Core.Extensions;
using BovineLabs.Core.Iterators;
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
    public partial struct EntityLinkAttachSystem : ISystem
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

            state.Dependency = new ConnectTransition
            {
                Commands = commands,
                Graph = graph,
                AccentLimit = 1,
                Parents = this.parents,
                LocalTransforms = this.localTransforms,
                WorldTransforms = this.worldTransforms,
                PostTransformMatrices = this.postTransformMatrices
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new DisconnectTransition
            {
                Commands = commands,
                Parents = this.parents,
                PostTransformMatrices = this.postTransformMatrices
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithNone(typeof(ClipActivePrevious))]
        private partial struct ConnectTransition : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Commands;
            public Graph Graph;
            [ReadOnly] public byte AccentLimit;
            [ReadOnly] public UnsafeComponentLookup<Parent> Parents;
            [ReadOnly] public UnsafeComponentLookup<LocalTransform> LocalTransforms;
            [ReadOnly] public UnsafeComponentLookup<LocalToWorld> WorldTransforms;
            [ReadOnly] public UnsafeComponentLookup<PostTransformMatrix> PostTransformMatrices;

            private void Execute([ChunkIndexInQuery] int chunk, ref EntityLinkAttachState state, in EntityLinkAttachConfig config, in TrackBinding binding)
            {
                var origin = binding.Value;
                this.Graph.Evaluate(origin, config.LinkKey, config.ResolveRule, this.AccentLimit, out var destination);

                if (destination == Entity.Null)
                {
                    state = default;
                    return;
                }

                var originParent = this.Parents.TryGetComponent(origin, out var p) ? p.Value : Entity.Null;
                var local = this.LocalTransforms.TryGetComponent(origin, out var l) ? l : LocalTransform.Identity;
                var world = this.WorldTransforms.TryGetComponent(origin, out var w) ? w : new LocalToWorld { Value = local.ToMatrix() };
                var destinationWorld = this.WorldTransforms.TryGetComponent(destination, out var dw) ? dw : new LocalToWorld { Value = float4x4.identity };

                var hadPTM = this.PostTransformMatrices.TryGetComponent(origin, out var ptmData);
                var ptm = hadPTM ? ptmData.Value : float4x4.identity;

                state = new EntityLinkAttachState
                {
                    ResolvedTarget = destination,
                    CapturedPreviousParent = originParent,
                    CapturedOriginalTransform = local,
                    CapturedOriginalPTM = ptm,
                    HadPostTransformMatrix = hadPTM,
                    IsAttached = true
                };

                this.ApplyTopology(chunk, origin, destination, local, ptm, hadPTM, world, destinationWorld, config.TransformFlags);
            }

            private void ApplyTopology(int chunk, Entity origin, Entity destination, LocalTransform local, float4x4 ptm,
                bool hadPtm, LocalToWorld world, LocalToWorld destinationWorld, AttachmentTransformFlags flags)
            {
                if (flags.HasAny(AttachmentTransformFlags.SetParent))
                {
                    if (this.Parents.TryGetComponent(origin, out _))
                        this.Commands.SetComponent(chunk, origin, new Parent { Value = destination });
                    else
                        this.Commands.AddComponent(chunk, origin, new Parent { Value = destination });
                }

                var resolvedTopology = Topology.Evaluate(local, ptm, hadPtm, world, destinationWorld, flags);

                this.Commands.SetComponent(chunk, origin, resolvedTopology.Local);

                if (resolvedTopology.HasPostTransform)
                {
                    if (hadPtm)
                        this.Commands.SetComponent(chunk, origin, new PostTransformMatrix { Value = resolvedTopology.PostTransform });
                    else
                        this.Commands.AddComponent(chunk, origin, new PostTransformMatrix { Value = resolvedTopology.PostTransform });
                }
                else if (hadPtm)
                {
                    this.Commands.RemoveComponent<PostTransformMatrix>(chunk, origin);
                }
            }
        }

        [BurstCompile]
        [WithNone(typeof(ClipActive))]
        [WithAll(typeof(ClipActivePrevious))]
        private partial struct DisconnectTransition : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Commands;
            [ReadOnly] public UnsafeComponentLookup<Parent> Parents;
            [ReadOnly] public UnsafeComponentLookup<PostTransformMatrix> PostTransformMatrices;

            private void Execute([ChunkIndexInQuery] int chunk, ref EntityLinkAttachState state, in TrackBinding binding)
            {
                if (!state.IsAttached) return;

                var origin = binding.Value;
                this.Commands.SetComponent(chunk, origin, state.CapturedOriginalTransform);

                var currentlyHasPtm = this.PostTransformMatrices.TryGetComponent(origin, out _);

                if (state.HadPostTransformMatrix)
                {
                    if (currentlyHasPtm)
                        this.Commands.SetComponent(chunk, origin, new PostTransformMatrix { Value = state.CapturedOriginalPTM });
                    else
                        this.Commands.AddComponent(chunk, origin, new PostTransformMatrix { Value = state.CapturedOriginalPTM });
                }
                else if (currentlyHasPtm)
                {
                    this.Commands.RemoveComponent<PostTransformMatrix>(chunk, origin);
                }

                this.RevertTopology(chunk, origin, state.CapturedPreviousParent);
                state = default;
            }

            private void RevertTopology(int chunk, Entity origin, Entity previousParent)
            {
                if (previousParent == Entity.Null)
                {
                    this.Commands.RemoveComponent<Parent>(chunk, origin);
                    this.Commands.RemoveComponent<PreviousParent>(chunk, origin);
                }
                else
                {
                    if (this.Parents.TryGetComponent(origin, out _))
                        this.Commands.SetComponent(chunk, origin, new Parent { Value = previousParent });
                    else
                        this.Commands.AddComponent(chunk, origin, new Parent { Value = previousParent });
                }
            }
        }
    }
}