using System.Runtime.CompilerServices;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Burst.CompilerServices;
using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks
{
    public static class EntityLinkResolver
    {
        private const int LinearSearchMax = 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryResolveRoot(
            Entity entity,
            in ComponentLookup<EntityLinkSource> sources,
            out Entity root)
        {
            if (entity == Entity.Null)
            {
                root = Entity.Null;
                return false;
            }

            if (sources.TryGetComponent(entity, out var source) && source.Root != Entity.Null)
            {
                root = source.Root;
                return true;
            }

            root = entity;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryResolve(
            Entity root,
            ushort key,
            in ComponentLookup<EntityLinkMap> maps,
            in BufferLookup<EntityLinkValue> values,
            out Entity result)
        {
            result = Entity.Null;

            if (root == Entity.Null || key == 0)
            {
                return false;
            }

            if (!maps.TryGetComponent(root, out var map) || !map.Blob.IsCreated)
            {
                return false;
            }

            if (!values.TryGetBuffer(root, out var valueBuffer))
            {
                return false;
            }

            if (!TryFindIndex(map.Blob, key, out var index))
            {
                return false;
            }

            if ((uint)index >= (uint)valueBuffer.Length)
            {
                return false;
            }

            result = valueBuffer[index].Value;
            return result != Entity.Null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryResolve(
            Entity entity,
            ushort key,
            in ComponentLookup<EntityLinkSource> sources,
            in ComponentLookup<EntityLinkMap> maps,
            in BufferLookup<EntityLinkValue> values,
            out Entity result)
        {
            if (!TryResolveRoot(entity, sources, out var root))
            {
                result = Entity.Null;
                return false;
            }

            return TryResolve(root, key, maps, values, out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryResolve(
            Entity self,
            in Targets targets,
            Target readRootFrom,
            ushort key,
            in ComponentLookup<TargetsCustom> targetsCustoms,
            in ComponentLookup<EntityLinkSource> sources,
            in ComponentLookup<EntityLinkMap> maps,
            in BufferLookup<EntityLinkValue> values,
            out Entity result)
        {
            var rootCandidate = targets.Get(readRootFrom, self, targetsCustoms);
            if (rootCandidate == Entity.Null)
            {
                result = Entity.Null;
                return false;
            }

            return TryResolve(rootCandidate, key, sources, maps, values, out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity ResolveOrFallback(
            Entity self,
            in Targets targets,
            in EntityLinkTargetPatch patch,
            in ComponentLookup<TargetsCustom> targetsCustoms,
            in ComponentLookup<EntityLinkSource> sources,
            in ComponentLookup<EntityLinkMap> maps,
            in BufferLookup<EntityLinkValue> values)
        {
            if (TryResolve(self, targets, patch.ReadRootFrom, patch.LinkKey, targetsCustoms, sources, maps, values, out var linked))
            {
                return linked;
            }

            return targets.Get(patch.Fallback, self, targetsCustoms);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFindIndex(
            BlobAssetReference<EntityLinkBlob> blob,
            ushort key,
            out int index)
        {
            ref var elements = ref blob.Value.Elements;

            if (elements.Length <= LinearSearchMax)
            {
                for (var i = 0; i < elements.Length; i++)
                {
                    if (elements[i].Key != key)
                    {
                        continue;
                    }

                    index = i;
                    return true;
                }

                index = -1;
                return false;
            }

            var min = 0;
            var max = elements.Length - 1;

            while (min <= max)
            {
                var mid = (min + max) >> 1;
                var candidate = elements[mid];

                if (candidate.Key == key)
                {
                    index = mid;
                    return true;
                }

                if (Hint.Likely(candidate.Key < key))
                {
                    min = mid + 1;
                }
                else
                {
                    max = mid - 1;
                }
            }

            index = -1;
            return false;
        }
    }
}