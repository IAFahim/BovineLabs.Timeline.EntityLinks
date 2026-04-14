using System;
using System.Linq;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Entities;
using UnityEngine;

namespace Bovinelabs.Timeline.EntityLinks.Authoring
{
    public class EntityLinkLookupBlobAuthoring : MonoBehaviour
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
                        if (parent.TryGetComponent(out EntityLinkLookupBlobAuthoring holder))
                            return holder == this;
                        parent = parent.parent;
                    }
                    return false;
                })
                .ToArray();
        }

        public class EntityLinkLookupHolderBaker : Baker<EntityLinkLookupBlobAuthoring>
        {
            public override void Bake(EntityLinkLookupBlobAuthoring authoring)
            {
                var validLinks = new System.Collections.Generic.List<(byte tag, EntityTagAuthoring src)>();
                foreach (var entityTagAuthoring in authoring.links)
                {
                    if (entityTagAuthoring.gameObject == authoring.gameObject) continue;

                    var schema = entityTagAuthoring.entityLinkTagSchema;
                    if (schema == null)
                    {
                        Debug.LogError(
                            $"Missing schema on {entityTagAuthoring.name} (holder: {authoring.gameObject.name})",
                            entityTagAuthoring.gameObject);
                        continue;
                    }

                    validLinks.Add((schema.id, entityTagAuthoring));
                }

                var builder = new BlobBuilder(Unity.Collections.Allocator.Temp);
                ref var root = ref builder.ConstructRoot<EntityLookupBlobData>();
                var array = builder.Allocate(ref root.Entries, validLinks.Count);

                for (int i = 0; i < validLinks.Count; i++)
                {
                    array[i] = new EntityLookup
                    {
                        Tag   = validLinks[i].tag,
                        Value = GetEntity(validLinks[i].src, TransformUsageFlags.None)
                    };
                }

                var blobRef = builder.CreateBlobAssetReference<EntityLookupBlobData>(
                    Unity.Collections.Allocator.Persistent);
                builder.Dispose();

                AddBlobAsset(ref blobRef, out _);

                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EntityLookUpBlob { Blob = blobRef });
            }
        }
    }
}