using System;
using System.Collections.Generic;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TransformAuthoring))]
    public sealed class EntityLinkSourceAuthoring : MonoBehaviour
    {
        public EntityLinkRootAuthoring Root;
        public EntityLinkSchema[] Schemas = Array.Empty<EntityLinkSchema>();

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (Root == null)
            {
                TryGetRoot(out var found);
                Root = found;
            }
#endif
        }

        public bool TryGetRoot(out EntityLinkRootAuthoring root)
        {
            root = Root != null ? Root : GetComponentInParent<EntityLinkRootAuthoring>(true);
            return root != null;
        }

        public bool HasSchema(EntityLinkSchema schema)
        {
            if (schema == null) return false;

            foreach (var s in Schemas)
                if (s == schema) return true;

            return false;
        }

        internal void AddSchemas(List<EntityLinkSchema> schemas)
        {
            foreach (var s in Schemas)
            {
                if (s != null) schemas.Add(s);
            }
        }

        private sealed class Baker : Baker<EntityLinkSourceAuthoring>
        {
            public override void Bake(EntityLinkSourceAuthoring authoring)
            {
                if (!authoring.TryGetRoot(out var root))
                {
                    Debug.LogError(
                        $"{nameof(EntityLinkSourceAuthoring)} on '{authoring.name}' could not find {nameof(EntityLinkRootAuthoring)}.");
                    return;
                }

                DependsOn(root);

                AddComponent(GetEntity(TransformUsageFlags.None), new EntityLinkSource
                {
                    Root = GetEntity(root, TransformUsageFlags.None)
                });
            }
        }
    }
}