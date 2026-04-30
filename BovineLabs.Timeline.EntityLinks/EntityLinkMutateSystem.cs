using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.Data;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks
{
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public partial struct EntityLinkMutateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntityLinkMutate>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new MutateJob
            {
                TargetsLookup = SystemAPI.GetComponentLookup<Targets>(true),
                TargetsCustoms = SystemAPI.GetComponentLookup<TargetsCustom>(true),
                Sources = SystemAPI.GetComponentLookup<EntityLinkSource>(true),
                Entries = SystemAPI.GetBufferLookup<EntityLinkEntry>(false)
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct MutateJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<Targets> TargetsLookup;
            [ReadOnly] public ComponentLookup<TargetsCustom> TargetsCustoms;
            [ReadOnly] public ComponentLookup<EntityLinkSource> Sources;

            public BufferLookup<EntityLinkEntry> Entries;

            private void Execute(Entity clipEntity, in TrackBinding binding, in EntityLinkMutate mutate)
            {
                var bindingEntity = binding.Value;
                if (bindingEntity == Entity.Null)
                    return;

                if (!TargetsLookup.TryGetComponent(bindingEntity, out var targets))
                    return;

                var rootCandidate = targets.Get(mutate.ReadRootFrom, bindingEntity, TargetsCustoms);
                if (rootCandidate == Entity.Null)
                    return;

                if (!EntityLinkResolver.TryResolveRoot(rootCandidate, Sources, out var root))
                    return;

                if (!Entries.TryGetBuffer(root, out var buffer))
                    return;

                switch (mutate.Mode)
                {
                    case EntityLinkMutateMode.Assign:
                    {
                        var newTarget = targets.Get(mutate.NewTarget, bindingEntity, TargetsCustoms);
                        var found = false;
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            if (buffer[i].Key == mutate.LinkKey)
                            {
                                buffer[i] = new EntityLinkEntry { Key = mutate.LinkKey, Target = newTarget };
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                            buffer.Add(new EntityLinkEntry { Key = mutate.LinkKey, Target = newTarget });
                        break;
                    }

                    case EntityLinkMutateMode.Swap:
                    {
                        var valA = Entity.Null;
                        var valB = Entity.Null;
                        var hasA = false;
                        var hasB = false;
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            if (buffer[i].Key == mutate.LinkKey) { valA = buffer[i].Target; hasA = true; }
                            if (buffer[i].Key == mutate.SwapKey) { valB = buffer[i].Target; hasB = true; }
                        }
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            if (buffer[i].Key == mutate.LinkKey)
                                buffer[i] = new EntityLinkEntry { Key = mutate.LinkKey, Target = valB };
                            if (buffer[i].Key == mutate.SwapKey)
                                buffer[i] = new EntityLinkEntry { Key = mutate.SwapKey, Target = valA };
                        }
                        break;
                    }

                    case EntityLinkMutateMode.Remove:
                    {
                        for (int i = buffer.Length - 1; i >= 0; i--)
                        {
                            if (buffer[i].Key == mutate.LinkKey)
                                buffer.RemoveAtSwapBack(i);
                        }
                        break;
                    }
                }
            }
        }
    }
}
