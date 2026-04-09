using BovineLabs.EntityLinks;
using BovineLabs.Reaction.Data.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace BovineLabs.Timeline.EntityLinks
{
    /// <summary>
    /// S-Tier Utility: Extracts resolution logic so multiple Timeline jobs 
    /// can synchronously resolve links without code duplication.
    /// </summary>
    public static class EntityLinkResolverUtility
    {
        public static bool TryResolve(
            Entity entity, 
            byte key, 
            ResolveRule rule,
            ref ComponentLookup<Parent> parentLookup,
            ref ComponentLookup<Targets> targetsLookup,
            ref BufferLookup<EntityLookupStoreBuffer> storeLookup,
            out Entity resolvedEntity, 
            out LocalTransform linkOffset)
        {
            resolvedEntity = Entity.Null;
            linkOffset = LocalTransform.Identity;

            if (HasFlag(rule, ResolveRule.SelfTarget) && TryGetFromStore(entity, key, ref storeLookup, out resolvedEntity, out linkOffset))
                return true;

            if (HasFlag(rule, ResolveRule.Parent) && TryResolveFromHierarchy(entity, key, ref parentLookup, ref storeLookup, out resolvedEntity, out linkOffset))
                return true;

            if (targetsLookup.TryGetComponent(entity, out var selfTargets))
            {
                if (HasFlag(rule, ResolveRule.Owner) && TryGetFromStore(selfTargets.Owner, key, ref storeLookup, out resolvedEntity, out linkOffset))
                    return true;

                if (HasFlag(rule, ResolveRule.Source) && TryGetFromStore(selfTargets.Source, key, ref storeLookup, out resolvedEntity, out linkOffset))
                    return true;

                if (HasFlag(rule, ResolveRule.Target) && TryGetFromStore(selfTargets.Target, key, ref storeLookup, out resolvedEntity, out linkOffset))
                    return true;
            }

            if (HasFlag(rule, ResolveRule.ParentsTarget) && TryResolveFromParentsTarget(entity, key, ref parentLookup, ref targetsLookup, ref storeLookup, out resolvedEntity, out linkOffset))
                return true;

            return false;
        }

        private static bool TryResolveFromHierarchy(Entity entity, byte key, ref ComponentLookup<Parent> parentLookup, ref BufferLookup<EntityLookupStoreBuffer> storeLookup, out Entity resolvedEntity, out LocalTransform linkOffset)
        {
            var current = entity;
            var depth = 0;

            while (parentLookup.TryGetComponent(current, out var parent) && depth < 64)
            {
                current = parent.Value;
                if (TryGetFromStore(current, key, ref storeLookup, out resolvedEntity, out linkOffset))
                    return true;
                depth++;
            }

            resolvedEntity = Entity.Null;
            linkOffset = LocalTransform.Identity;
            return false;
        }

        private static bool TryResolveFromParentsTarget(Entity entity, byte key, ref ComponentLookup<Parent> parentLookup, ref ComponentLookup<Targets> targetsLookup, ref BufferLookup<EntityLookupStoreBuffer> storeLookup, out Entity resolvedEntity, out LocalTransform linkOffset)
        {
            if (parentLookup.TryGetComponent(entity, out var parent) && targetsLookup.TryGetComponent(parent.Value, out var parentTargets))
            {
                return TryGetFromStore(parentTargets.Target, key, ref storeLookup, out resolvedEntity, out linkOffset);
            }
            resolvedEntity = Entity.Null;
            linkOffset = LocalTransform.Identity;
            return false;
        }

        private static bool TryGetFromStore(Entity target, byte key, ref BufferLookup<EntityLookupStoreBuffer> storeLookup, out Entity resolvedEntity, out LocalTransform linkOffset)
        {
            if (target != Entity.Null && storeLookup.TryGetBuffer(target, out var store))
            {
                foreach (var element in store)
                {
                    if (element.Key == key)
                    {
                        resolvedEntity = element.Value;
                        linkOffset = element.LocalTransform;
                        return true;
                    }
                }
            }
            resolvedEntity = Entity.Null;
            linkOffset = LocalTransform.Identity;
            return false;
        }

        private static bool HasFlag(ResolveRule rule, ResolveRule flag) => (rule & flag) != 0;
    }
}