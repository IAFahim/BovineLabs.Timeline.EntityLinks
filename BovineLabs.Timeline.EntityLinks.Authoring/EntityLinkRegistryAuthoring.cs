using System;
using System.Collections.Generic;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Entities;
using UnityEngine;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    public class EntityLinkRegistryAuthoring : MonoBehaviour
    {
        public EntityTagAuthoring[] entityTagAuthorings = Array.Empty<EntityTagAuthoring>();
        public bool allowInactive = true;

        private void OnValidate()
        {
            this.entityTagAuthorings = GetComponentsInChildren<EntityTagAuthoring>(this.allowInactive);
        }

        public class Baker : Baker<EntityLinkRegistryAuthoring>
        {
            public override void Bake(EntityLinkRegistryAuthoring authoring)
            {
                var validLinks = new List<(byte tag, EntityTagAuthoring src)>();
                var tagsSet = new HashSet<byte>();

                foreach (var entityTagAuthoring in authoring.entityTagAuthorings)
                {
                    if (entityTagAuthoring == null)
                    {
                        Debug.LogError(
                            $"Null linked GameObject on EntityLinkRegistryAuthoring holder: {authoring.gameObject.name}",
                            authoring.gameObject
                        );
                        continue;
                    }

                    var entityLinkTagSchema = entityTagAuthoring.entityLinkTagSchema;
                    if (entityLinkTagSchema == null)
                    {
                        Debug.LogError(
                            $"Missing schema on {entityTagAuthoring.name} (holder: {authoring.gameObject.name})",
                            entityTagAuthoring.gameObject
                        );
                        continue;
                    }

                    if (!tagsSet.Add(entityLinkTagSchema.Id))
                    {
                        Debug.LogError(
                            $"Duplicate schema tag '{entityLinkTagSchema.name}' ({entityLinkTagSchema.Id}) on {entityTagAuthoring.name} (holder: {authoring.gameObject.name})",
                            entityTagAuthoring.gameObject
                        );
                        continue;
                    }

                    validLinks.Add((entityLinkTagSchema.Id, entityTagAuthoring));
                    DependsOn(entityTagAuthoring);
                }

                if (validLinks.Count == 0)
                    return;

                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<EntityLinkElement>(entity);
                buffer.ResizeUninitialized(validLinks.Count);

                for (var i = 0; i < validLinks.Count; i++)
                {
                    buffer[i] = new EntityLinkElement
                    {
                        Tag = validLinks[i].tag,
                        Value = GetEntity(validLinks[i].src, TransformUsageFlags.None)
                    };
                }
            }
        }
    }
}
