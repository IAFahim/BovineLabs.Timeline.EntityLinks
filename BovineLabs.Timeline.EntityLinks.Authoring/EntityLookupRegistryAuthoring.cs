using System;
using System.Collections.Generic;
using BovineLabs.Core.Authoring.EntityCommands;
using BovineLabs.Timeline.EntityLinks.Data;
using BovineLabs.Timeline.EntityLinks.Data.Builders;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    public class EntityLookupRegistryAuthoring : MonoBehaviour
    {
        public EntityTagAuthoring[] entityTagAuthorings = Array.Empty<EntityTagAuthoring>();
        public bool allowInactive = true;

        private void OnValidate()
        {
            entityTagAuthorings = GetComponentsInChildren<EntityTagAuthoring>(allowInactive);
        }

        public class EntityLinkLookupHolderBaker : Baker<EntityLookupRegistryAuthoring>
        {
            public override void Bake(EntityLookupRegistryAuthoring authoring)
            {
                var validLinks = new List<(byte tag, EntityTagAuthoring src)>();
                foreach (var entityTagAuthoring in authoring.entityTagAuthorings)
                {
                    var entityLinkTagSchema = entityTagAuthoring.entityLinkTagSchema;
                    if (entityLinkTagSchema == null)
                    {
                        Debug.LogError(
                            $"Missing schema on {entityTagAuthoring.name} (holder: {authoring.gameObject.name})",
                            entityTagAuthoring.gameObject
                        );
                        continue;
                    }

                    validLinks.Add((entityLinkTagSchema.id, entityTagAuthoring));
                    DependsOn(entityTagAuthoring);
                }

                var builder = new BlobBuilder(Allocator.Temp);
                ref var entityLookupRegistry = ref builder.ConstructRoot<EntityLookupRegistry>();
                var array = builder.Allocate(ref entityLookupRegistry.Entries, validLinks.Count);

                for (var i = 0; i < validLinks.Count; i++)
                    array[i] = new EntityLookup
                    {
                        Tag = validLinks[i].tag,
                        Value = GetEntity(validLinks[i].src, TransformUsageFlags.None)
                    };

                var blobRef = builder.CreateBlobAssetReference<EntityLookupRegistry>(
                    Allocator.Persistent);
                builder.Dispose();

                var entity = GetEntity(TransformUsageFlags.None);
                var commands = new BakerCommands(this, entity);
                var registryBuilder = new EntityLookupRegistryBuilder()
                    .WithBlob(blobRef);
                registryBuilder.ApplyTo(ref commands);
            }
        }
    }
}