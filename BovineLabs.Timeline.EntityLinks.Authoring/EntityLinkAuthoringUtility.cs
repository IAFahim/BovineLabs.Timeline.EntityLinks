using UnityEngine;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    public static class EntityLinkAuthoringUtility
    {
        public static bool TryGetKey(EntityLinkSchema schema, out ushort key)
        {
            key = schema == null ? (ushort)0 : schema.Id;
            return key != 0;
        }

        public static bool TryFindLinkedComponent(EntityLinkRootAuthoring root, EntityLinkSchema schema,
            out Component linked)
        {
            linked = null;

            foreach (var source in root.Links)
                if (source != null && source.HasSchema(schema) && source.TryGetRoot(out var sourceRoot) &&
                    sourceRoot == root)
                {
                    linked = source;
                    return true;
                }

            foreach (var source in root.GetComponentsInChildren<EntityLinkSourceAuthoring>(true))
                if (source.HasSchema(schema) && source.TryGetRoot(out var sourceRoot) && sourceRoot == root)
                {
                    linked = source;
                    return true;
                }

            return false;
        }

        public readonly struct Entry
        {
            public readonly ushort Key;
            public readonly Component Target;

            public Entry(ushort key, Component target)
            {
                Key = key;
                Target = target;
            }
        }
    }
}