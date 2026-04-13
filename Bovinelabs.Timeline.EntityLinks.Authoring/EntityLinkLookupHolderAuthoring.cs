using System;
using System.Linq;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Entities;
using UnityEngine;

namespace Bovinelabs.Timeline.EntityLinks.Authoring
{
    public class EntityLinkLookupHolderAuthoring : MonoBehaviour
    {
        public EntityTagAuthoring[] links = Array.Empty<EntityTagAuthoring>();

        private void OnValidate()
        {
            links = GetComponentsInChildren<EntityTagAuthoring>(false)
                .Where(entityTagAuthoring =>
                {
                    if (entityTagAuthoring.gameObject == gameObject) return false;

                    var parent = entityTagAuthoring.transform.parent;
                    while (parent != null)
                    {
                        if (parent.TryGetComponent(out EntityLinkLookupHolderAuthoring holder))
                            return holder == this;
                        parent = parent.parent;
                    }
                    return false;
                })
                .ToArray();
        }

        public class EntityLinkLookupHolderBaker : Baker<EntityLinkLookupHolderAuthoring>
        {
            public override void Bake(EntityLinkLookupHolderAuthoring holderAuthoring)
            {
                var buffer = AddBuffer<EntityLookupStoreData>(GetEntity(TransformUsageFlags.None));

                foreach (var entityTagAuthoring in holderAuthoring.links)
                {
                    // Safety net: never bake our own tag
                    if (entityTagAuthoring.gameObject == holderAuthoring.gameObject) continue;

                    var entityLinkTagSchema = entityTagAuthoring.entityLinkTagSchema;
                    if (entityLinkTagSchema == null)
                    {
                        Debug.LogError(entityTagAuthoring.name, entityTagAuthoring);
                        continue;
                    }

                    buffer.Add(new EntityLookupStoreData
                    {
                        Tag = EntityLinkSettings.GetIndex(entityLinkTagSchema),
                        Value = GetEntity(entityTagAuthoring, TransformUsageFlags.None)
                    });
                }
            }
        }
    }
}