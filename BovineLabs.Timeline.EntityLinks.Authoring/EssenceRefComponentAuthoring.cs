using BovineLabs.Core.Authoring.EntityCommands;
using BovineLabs.Essence.Authoring;
using BovineLabs.Timeline.EntityLinks.Data.Builders;
using Unity.Entities;
using UnityEngine;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    public class EssenceRefComponentAuthoring : MonoBehaviour
    {
        public StatAuthoring statAuthoring;

        public class EntityEssenceRefComponentBaker : Baker<EssenceRefComponentAuthoring>
        {
            public override void Bake(EssenceRefComponentAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var commands = new BakerCommands(this, entity);
                var builder = new EntityEssenceRefBuilder()
                    .WithEssenceEntity(GetEntity(authoring.statAuthoring.gameObject, TransformUsageFlags.None));
                builder.ApplyTo(ref commands);
            }
        }
    }
}