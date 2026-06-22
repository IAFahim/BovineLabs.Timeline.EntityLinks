using BovineLabs.Core.Collections;
using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks.Data
{
    internal static class EntityLinkEntries
    {
        internal static void Assign(UnsafeDynamicBuffer<EntityLinkEntry> buffer, ushort linkKey, Entity newTarget)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Key != linkKey)
                    continue;

                buffer[i] = new EntityLinkEntry { Key = linkKey, Target = newTarget };
                return;
            }

            buffer.Add(new EntityLinkEntry { Key = linkKey, Target = newTarget });
        }

        internal static void Swap(UnsafeDynamicBuffer<EntityLinkEntry> buffer, ushort linkKey, ushort swapKey)
        {
            if (linkKey == swapKey)
                return;

            int idxA = -1, idxB = -1;
            for (var i = 0; i < buffer.Length; i++)
                if (buffer[i].Key == linkKey) idxA = i;
                else if (buffer[i].Key == swapKey) idxB = i;

            if (idxA == -1 && idxB == -1)
                return;

            var targetA = idxA != -1 ? buffer[idxA].Target : Entity.Null;
            var targetB = idxB != -1 ? buffer[idxB].Target : Entity.Null;

            SetRemoveOrAdd(buffer, linkKey, targetB);
            SetRemoveOrAdd(buffer, swapKey, targetA);
        }

        internal static void Remove(UnsafeDynamicBuffer<EntityLinkEntry> buffer, ushort linkKey)
        {
            for (var i = buffer.Length - 1; i >= 0; i--)
                if (buffer[i].Key == linkKey)
                    buffer.RemoveAt(i);
        }

        private static void SetRemoveOrAdd(UnsafeDynamicBuffer<EntityLinkEntry> buffer, ushort key, Entity target)
        {
            if (target == Entity.Null)
            {
                Remove(buffer, key);
                return;
            }

            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Key != key)
                    continue;

                buffer[i] = new EntityLinkEntry { Key = key, Target = target };
                return;
            }

            buffer.Add(new EntityLinkEntry { Key = key, Target = target });
        }
    }
}
