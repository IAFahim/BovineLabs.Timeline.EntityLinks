// <copyright file="EntityEssenceRefBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

using BovineLabs.Core.EntityCommands;
using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks.Data.Builders
{
    public struct EntityEssenceRefBuilder
    {
        private Entity essenceEntity;

        public EntityEssenceRefBuilder WithEssenceEntity(Entity entity)
        {
            essenceEntity = entity;
            return this;
        }

        public void ApplyTo<T>(ref T builder)
            where T : struct, IEntityCommands
        {
            builder.AddComponent(new EntityEssenceRef { Value = essenceEntity });
        }
    }
}