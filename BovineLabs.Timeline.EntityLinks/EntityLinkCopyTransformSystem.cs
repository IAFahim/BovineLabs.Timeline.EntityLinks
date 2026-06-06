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
    [Unity.Entities.WorldSystemFilter(Unity.Entities.WorldSystemFilterFlags.LocalSimulation | Unity.Entities.WorldSystemFilterFlags.ClientSimulation | Unity.Entities.WorldSystemFilterFlags.ServerSimulation)]
    public partial struct EntityLinkCopyTransformSystem : ISystem
    {
        private ComponentLookup<LocalToWorld> ltwLookup;
        private ComponentLookup<LocalTransform> localTransformLookup;
        private ComponentLookup<Parent> parentLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntityLinkCopyTransform>();
            this.ltwLookup = state.GetComponentLookup<LocalToWorld>(true);
            this.localTransformLookup = state.GetComponentLookup<LocalTransform>(false);
            this.parentLookup = state.GetComponentLookup<Parent>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.ltwLookup.Update(ref state);
            this.localTransformLookup.Update(ref state);
            this.parentLookup.Update(ref state);

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            state.Dependency = new CopyTransformJob
            {
                TargetsLookup = state.GetUnsafeComponentLookup<Targets>(true),
                Sources = state.GetUnsafeComponentLookup<EntityLinkSource>(true),
                Links = state.GetUnsafeBufferLookup<EntityLinkEntry>(true),
                LtwLookup = this.ltwLookup,
                LocalTransformLookup = this.localTransformLookup,
                ParentLookup = this.parentLookup
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        private partial struct CopyTransformJob : IJobEntity
        {
            [ReadOnly] public UnsafeComponentLookup<Targets> TargetsLookup;
            [ReadOnly] public UnsafeComponentLookup<EntityLinkSource> Sources;
            [ReadOnly] public UnsafeBufferLookup<EntityLinkEntry> Links;
            [ReadOnly] public ComponentLookup<LocalToWorld> LtwLookup;
            [ReadOnly] public ComponentLookup<Parent> ParentLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([EntityIndexInQuery] int sortKey, in TrackBinding binding, in EntityLinkCopyTransform config)
            {
                var bindingEntity = binding.Value;
                if (bindingEntity == Entity.Null || !this.TargetsLookup.TryGetComponent(bindingEntity, out var targets))
                {
                    return;
                }

                var entityToMove = targets.Get(config.EntityToMove, bindingEntity);
                if (entityToMove == Entity.Null || !this.LocalTransformLookup.HasComponent(entityToMove))
                {
                    return;
                }

                var resolvedSource = EntityLinkResolver.ResolveOrFallback(
                    bindingEntity,
                    targets,
                    new EntityLinkTargetPatch { ReadRootFrom = config.ReadRootFrom, LinkKey = config.LinkKey, Fallback = Target.None },
                    this.Sources,
                    this.Links);

                if (resolvedSource == Entity.Null || !this.LtwLookup.TryGetComponent(resolvedSource, out var sourceLtw))
                {
                    return;
                }

                var targetTransform = this.LocalTransformLookup[entityToMove];

                var sourceWorldTransform = LocalTransform.FromMatrix(sourceLtw.Value);
                var desiredWorldPos = sourceWorldTransform.Position;
                var desiredWorldRot = sourceWorldTransform.Rotation;

                if (config.CopyPosition && math.lengthsq(config.PositionOffset) > 0)
                {
                    desiredWorldPos += math.rotate(desiredWorldRot, config.PositionOffset);
                }

                if (config.CopyRotation && !config.RotationOffset.Equals(quaternion.identity))
                {
                    desiredWorldRot = math.mul(desiredWorldRot, config.RotationOffset);
                }

                if (this.ParentLookup.TryGetComponent(entityToMove, out var parent) &&
                    this.LtwLookup.TryGetComponent(parent.Value, out var parentLtw))
                {
                    var parentInverse = math.inverse(parentLtw.Value);

                    if (config.CopyPosition)
                    {
                        targetTransform.Position = math.transform(parentInverse, desiredWorldPos);
                    }

                    if (config.CopyRotation)
                    {
                        var parentWorldTransform = LocalTransform.FromMatrix(parentLtw.Value);
                        targetTransform.Rotation = math.mul(math.inverse(parentWorldTransform.Rotation), desiredWorldRot);
                    }
                }
                else
                {
                    if (config.CopyPosition)
                    {
                        targetTransform.Position = desiredWorldPos;
                    }

                    if (config.CopyRotation)
                    {
                        targetTransform.Rotation = desiredWorldRot;
                    }
                }

                this.ECB.SetComponent(sortKey, entityToMove, targetTransform);
            }
        }
    }
}
