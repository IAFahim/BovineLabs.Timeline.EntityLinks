// <copyright file="EntityLookupRegistryBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

using BovineLabs.Core.EntityCommands;
using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks.Data.Builders
{
    public struct EntityLookupRegistryBuilder
    {
        private BlobAssetReference<EntityLookupRegistry> blob;

        public EntityLookupRegistryBuilder WithBlob(BlobAssetReference<EntityLookupRegistry> blobRef)
        {
            blob = blobRef;
            return this;
        }

        public void ApplyTo<T>(ref T builder)
            where T : struct, IEntityCommands
        {
            if (!blob.IsCreated) return;

            var blobRef = blob;
            builder.AddBlobAsset(ref blobRef, out _);
            builder.AddComponent(new EntityLookupBlobComponent { Blob = blobRef });
        }
    }
}