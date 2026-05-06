using BovineLabs.Timeline.Authoring;
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

        public static bool TryResolveLink(this BakingContext context, EntityLinkSchema schema, out Component linked)
        {
            linked = null;

            if (schema == null)
                return false;

            var binding = context.Director.GetGenericBinding(context.Track);
            var target = binding as Component ?? (binding as GameObject)?.transform;

            if (target == null)
                return false;

            var root = target.GetComponentInParent<EntityLinkRootAuthoring>();
            if (root == null)
                root = target.GetComponentInChildren<EntityLinkRootAuthoring>(true);

            return root != null && TryFindLinkedComponent(root, schema, out linked);
        }

        public static bool TryResolveLinkComponent<T>(this BakingContext context, EntityLinkSchema schema,
            out T component)
            where T : Component
        {
            component = null;

            if (!context.TryResolveLink(schema, out var linked))
                return false;

            component = linked.GetComponent<T>();
            return component != null;
        }

        public static bool TryFindLinkedComponent(EntityLinkRootAuthoring root, EntityLinkSchema schema,
            out Component linked)
        {
            linked = null;

            // Check manually registered links first (schema lives on the source now)
            foreach (var source in root.Links)
                if (source != null && source.HasSchema(schema))
                {
                    linked = source;
                    return true;
                }

            // Fall back to scanning all children
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