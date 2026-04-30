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
        public static bool TryResolveFromRoot(Entity root, ushort key, in BufferLookup<EntityLinkEntry> entries, out Entity result)
        {
            if (root != Entity.Null && key != 0 && entries.TryGetBuffer(root, out var buffer))
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i].Key == key)
                    {
                        result = buffer[i].Target;
                        return true;
                    }
                }
            }

            result = Entity.Null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryResolve(
            Entity entity,
            ushort key,
            in ComponentLookup<EntityLinkSource> sources,
            in BufferLookup<EntityLinkEntry> entries,
            out Entity result)
        {
            if (!TryResolveRoot(entity, sources, out var root))
            {
                result = Entity.Null;
                return false;
            }

            return TryResolveFromRoot(root, key, entries, out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryResolve(
            Entity self,
            in Targets targets,
            Target readRootFrom,
            ushort key,
            in ComponentLookup<TargetsCustom> targetsCustoms,
            in ComponentLookup<EntityLinkSource> sources,
            in BufferLookup<EntityLinkEntry> entries,
            out Entity result)
        {
            var rootCandidate = targets.Get(readRootFrom, self, targetsCustoms);
            if (rootCandidate == Entity.Null)
            {
                result = Entity.Null;
                return false;
            }

            return TryResolve(rootCandidate, key, sources, entries, out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity ResolveOrFallback(
            Entity self,
            in Targets targets,
            in EntityLinkTargetPatch patch,
            in ComponentLookup<TargetsCustom> targetsCustoms,
            in ComponentLookup<EntityLinkSource> sources,
            in BufferLookup<EntityLinkEntry> entries)
        {
            if (TryResolve(self, targets, patch.ReadRootFrom, patch.LinkKey, targetsCustoms, sources, entries,
                    out var linked)) return linked;

            return targets.Get(patch.Fallback, self, targetsCustoms);
        }
    }
}
