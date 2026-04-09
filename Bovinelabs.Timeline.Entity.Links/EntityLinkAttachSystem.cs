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
    public partial struct EntityLinkAttachSystem : ISystem
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

            state.Dependency = new ConnectTransition
            {
                Commands = commands,
                Graph = graph,
                Parents = SystemAPI.GetComponentLookup<Parent>(true),
                LocalTransforms = SystemAPI.GetComponentLookup<LocalTransform>(true),
                WorldTransforms = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                PostTransformMatrices = SystemAPI.GetComponentLookup<PostTransformMatrix>(true)
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new DisconnectTransition
            {
                Commands = commands,
                Parents = SystemAPI.GetComponentLookup<Parent>(true),
                PostTransformMatrices = SystemAPI.GetComponentLookup<PostTransformMatrix>(true)
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithNone(typeof(ClipActivePrevious))]
        private partial struct ConnectTransition : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Commands;
            public Graph Graph;
            [ReadOnly] public ComponentLookup<Parent> Parents;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransforms;
            [ReadOnly] public ComponentLookup<LocalToWorld> WorldTransforms;
            [ReadOnly] public ComponentLookup<PostTransformMatrix> PostTransformMatrices;

            private void Execute([ChunkIndexInQuery] int chunk, ref EntityLinkAttachState state, in EntityLinkAttachConfig config, in TrackBinding binding)
            {
                var origin = binding.Value;
                var destination = this.Graph.Evaluate(origin, config.LinkKey, config.ResolveRule);

                if (destination == Unity.Entities.Entity.Null)
                {
                    state = new EntityLinkAttachState();
                    return;
                }

                var originParent = this.Parents.TryGetComponent(origin, out var p) ? p.Value : Unity.Entities.Entity.Null;
                var local = this.LocalTransforms.TryGetComponent(origin, out var l) ? l : LocalTransform.Identity;
                var world = this.WorldTransforms.TryGetComponent(origin, out var w) ? w : new LocalToWorld { Value = local.ToMatrix() };
                var destinationWorld = this.WorldTransforms.TryGetComponent(destination, out var dw) ? dw : new LocalToWorld { Value = float4x4.identity };
                
                var hadPTM = this.PostTransformMatrices.HasComponent(origin);
                var ptm = hadPTM ? this.PostTransformMatrices[origin].Value : float4x4.identity;

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

            private void ApplyTopology(int chunk, Unity.Entities.Entity origin, Unity.Entities.Entity destination, LocalTransform local, float4x4 ptm, bool hadPtm, LocalToWorld world, LocalToWorld destinationWorld, AttachmentTransformFlags flags)
            {
                if (flags.HasAny(AttachmentTransformFlags.SetParent))
                {
                    if (this.Parents.HasComponent(origin))
                    {
                        this.Commands.SetComponent(chunk, origin, new Parent { Value = destination });
                    }
                    else
                    {
                        this.Commands.AddComponent(chunk, origin, new Parent { Value = destination });
                    }
                }

                var resolvedTopology = Topology.Evaluate(local, ptm, hadPtm, world, destinationWorld, flags);
                
                this.Commands.SetComponent(chunk, origin, resolvedTopology.Local);
                
                if (resolvedTopology.HasPostTransform)
                {
                    if (hadPtm) this.Commands.SetComponent(chunk, origin, new PostTransformMatrix { Value = resolvedTopology.PostTransform });
                    else this.Commands.AddComponent(chunk, origin, new PostTransformMatrix { Value = resolvedTopology.PostTransform });
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
            [ReadOnly] public ComponentLookup<Parent> Parents;
            [ReadOnly] public ComponentLookup<PostTransformMatrix> PostTransformMatrices;

            private void Execute([ChunkIndexInQuery] int chunk, ref EntityLinkAttachState state, in TrackBinding binding)
            {
                if (!state.IsAttached) return;

                var origin = binding.Value;
                this.Commands.SetComponent(chunk, origin, state.CapturedOriginalTransform);

                var currentlyHasPtm = this.PostTransformMatrices.HasComponent(origin);

                if (state.HadPostTransformMatrix)
                {
                    if (currentlyHasPtm) this.Commands.SetComponent(chunk, origin, new PostTransformMatrix { Value = state.CapturedOriginalPTM });
                    else this.Commands.AddComponent(chunk, origin, new PostTransformMatrix { Value = state.CapturedOriginalPTM });
                }
                else if (currentlyHasPtm)
                {
                    this.Commands.RemoveComponent<PostTransformMatrix>(chunk, origin);
                }

                this.RevertTopology(chunk, origin, state.CapturedPreviousParent);
                state = default;
            }

            private void RevertTopology(int chunk, Unity.Entities.Entity origin, Unity.Entities.Entity previousParent)
            {
                if (previousParent == Unity.Entities.Entity.Null)
                {
                    this.Commands.RemoveComponent<Parent>(chunk, origin);
                    this.Commands.RemoveComponent<PreviousParent>(chunk, origin);
                }
                else
                {
                    if (this.Parents.HasComponent(origin))
                    {
                        this.Commands.SetComponent(chunk, origin, new Parent { Value = previousParent });
                    }
                    else
                    {
                        this.Commands.AddComponent(chunk, origin, new Parent { Value = previousParent });
                    }
                }
            }
        }
    }
}