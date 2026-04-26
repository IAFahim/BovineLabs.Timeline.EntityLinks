using System.Runtime.CompilerServices;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks
{
    public static class EntityLinkResolver
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryResolveRoot(Entity entity, in ComponentLookup<EntityLinkSource> sources, out Entity root)
        {
            if (entity == Entity.Null)
            {
                root = Entity.Null;
                return false;
            }

            root = sources.TryGetComponent(entity, out var source) && source.Root != Entity.Null
                ? source.Root
                : entity;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryResolve(Entity root, ushort key, in BufferLookup<EntityLink> links, out Entity result)
        {
            result = Entity.Null;

            if (root == Entity.Null || key == 0 || !links.TryGetBuffer(root, out var buffer))
            {
                return false;
            }

            for (var i = 0; i < buffer.Length; i++)
            {
                var link = buffer[i];
                if (link.Key == key)
                {
                    result = link.Target;
                    return result != Entity.Null;
                }

                if (link.Key > key)
                {
                    break;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryResolve(
            Entity entity,
            ushort key,
            in ComponentLookup<EntityLinkSource> sources,
            in BufferLookup<EntityLink> links,
            out Entity result)
        {
            if (!TryResolveRoot(entity, sources, out var root))
            {
                result = Entity.Null;
                return false;
            }

            return TryResolve(root, key, links, out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryResolve(
            Entity self,
            in Targets targets,
            Target readRootFrom,
            ushort key,
            in ComponentLookup<TargetsCustom> targetsCustoms,
            in ComponentLookup<EntityLinkSource> sources,
            in BufferLookup<EntityLink> links,
            out Entity result)
        {
            var rootCandidate = targets.Get(readRootFrom, self, targetsCustoms);
            if (rootCandidate == Entity.Null)
            {
                result = Entity.Null;
                return false;
            }

            return TryResolve(rootCandidate, key, sources, links, out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity ResolveOrFallback(
            Entity self,
            in Targets targets,
            in EntityLinkTargetPatch patch,
            in ComponentLookup<TargetsCustom> targetsCustoms,
            in ComponentLookup<EntityLinkSource> sources,
            in BufferLookup<EntityLink> links)
        {
            if (TryResolve(self, targets, patch.ReadRootFrom, patch.LinkKey, targetsCustoms, sources, links, out var linked))
            {
                return linked;
            }

            return targets.Get(patch.Fallback, self, targetsCustoms);
        }
    }
}
