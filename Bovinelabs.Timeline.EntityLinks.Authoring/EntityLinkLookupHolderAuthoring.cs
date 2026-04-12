using System;
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
            links = GetComponentsInChildren<EntityTagAuthoring>(true);
        }

        public class EntityLinkLookupResolverBaker : Baker<EntityLinkLookupHolderAuthoring>
        {
            public override void Bake(EntityLinkLookupHolderAuthoring holderAuthoring)
            {
                var buffer = AddBuffer<EntityLookupStoreData>(GetEntity(TransformUsageFlags.None));

                foreach (var entityTagsMonoBehavior in holderAuthoring.links)
                    buffer.Add(new EntityLookupStoreData
                        {
                            Tag = entityTagsMonoBehavior.tag.Id,
                            Value = GetEntity(entityTagsMonoBehavior, TransformUsageFlags.None)
                        }
                    );
            }
        }
    }
}