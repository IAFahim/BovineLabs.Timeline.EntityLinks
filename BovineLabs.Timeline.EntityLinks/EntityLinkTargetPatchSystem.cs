using BovineLabs.Core.Extensions;
using BovineLabs.Core.Iterators;
using BovineLabs.Core.Utility;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.Data;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks
{
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation)]
    public partial struct EntityLinkTargetPatchSystem : ISystem
    {
        private UnsafeComponentLookup<Targets> _targetsLookup;
        private UnsafeComponentLookup<EntityLinkSource> _sources;
        private UnsafeBufferLookup<EntityLinkEntry> _links;
        private EntityLock _entityLock;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntityLinkTargetPatch>();
            _targetsLookup = state.GetUnsafeComponentLookup<Targets>();
            _sources = state.GetUnsafeComponentLookup<EntityLinkSource>(true);
            _links = state.GetUnsafeBufferLookup<EntityLinkEntry>(true);
            _entityLock = new EntityLock(Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            _entityLock.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _targetsLookup.Update(ref state);
            _sources.Update(ref state);
            _links.Update(ref state);

            state.Dependency = new PatchJob
            {
                TargetsLookup = _targetsLookup,
                Sources = _sources,
                Links = _links,
                EntityLock = _entityLock
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct PatchJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public UnsafeComponentLookup<Targets> TargetsLookup;

            [ReadOnly] public UnsafeComponentLookup<EntityLinkSource> Sources;
            [ReadOnly] public UnsafeBufferLookup<EntityLinkEntry> Links;

            public EntityLock EntityLock;

            private void Execute([EntityIndexInQuery] int sortKey, in TrackBinding binding,
                in EntityLinkTargetPatch patch)
            {
                var bindingEntity = binding.Value;
                if (bindingEntity == Entity.Null ||
                    !TargetsLookup.TryGetComponent(bindingEntity, out var targets)) return;

                var resolved = EntityLinkResolver.ResolveOrFallback(
                    bindingEntity,
                    targets,
                    patch,
                    Sources,
                    Links);

                if (resolved == Entity.Null) return;

                using (EntityLock.Acquire(bindingEntity))
                {
                    targets = TargetsLookup[bindingEntity];

                    switch (patch.WriteTo)
                    {
                        case Target.Owner:
                            targets.Owner = resolved;
                            TargetsLookup[bindingEntity] = targets;
                            break;

                        case Target.Source:
                            targets.Source = resolved;
                            TargetsLookup[bindingEntity] = targets;
                            break;

                        case Target.Target:
                            targets.Target = resolved;
                            TargetsLookup[bindingEntity] = targets;
                            break;

                        case Target.Custom:
                            targets.Custom = resolved;
                            TargetsLookup[bindingEntity] = targets;
                            break;
                    }
                }
            }
        }
    }
}