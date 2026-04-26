using System;
using System.Collections.Generic;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Entities;
using UnityEngine;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TransformAuthoring))]
    public sealed class EntityLinkSourceAuthoring : MonoBehaviour
    {
        public EntityLinkRootAuthoring Root;
        public EntityLinkSchema Schema;
        public EntityLinkSchema[] Aliases = Array.Empty<EntityLinkSchema>();

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
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
            root = this.Root != null ? this.Root : this.GetComponentInParent<EntityLinkRootAuthoring>(true);
            return root != null;
        }

        public bool HasSchema(EntityLinkSchema schema)
        {
            if (schema == null)
            {
                return false;
            }

            if (this.Schema == schema)
            {
                return true;
            }

            if (this.Aliases == null)
            {
                return false;
            }

            foreach (var alias in this.Aliases)
            {
                if (alias == schema)
                {
                    return true;
                }
            }

            return false;
        }

        internal void AddSchemas(List<EntityLinkSchema> schemas)
        {
            if (this.Schema != null)
            {
                schemas.Add(this.Schema);
            }

            if (this.Aliases == null)
            {
                return;
            }

            foreach (var alias in this.Aliases)
            {
                if (alias == null)
                {
                    continue;
                }

                schemas.Add(alias);
            }
        }


        private sealed class Baker : Baker<EntityLinkSourceAuthoring>
        {
            public override void Bake(EntityLinkSourceAuthoring authoring)
            {
                if (!authoring.TryGetRoot(out var root))
                {
                    Debug.LogError($"{nameof(EntityLinkSourceAuthoring)} on '{authoring.name}' could not find {nameof(EntityLinkRootAuthoring)}.");
                    return;
                }

                this.DependsOn(root);

                this.AddComponent(this.GetEntity(TransformUsageFlags.None), new EntityLinkSource
                {
                    Root = this.GetEntity(root, TransformUsageFlags.None)
                });
            }
        }
    }
}