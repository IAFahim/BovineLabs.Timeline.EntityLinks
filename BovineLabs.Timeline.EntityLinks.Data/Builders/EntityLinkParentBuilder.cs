using BovineLabs.Core.EntityCommands;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Mathematics;

namespace BovineLabs.Timeline.EntityLinks.Data.Builders
{
    public struct EntityLinkParentBuilder
    {
        public Target EntityToParent;
        public Target ReadRootFrom;
        public ushort ParentLinkKey;
        public float3 LocalPosition;
        public quaternion LocalRotation;
        public bool RestoreOnEnd;

        public void ApplyTo<T>(ref T builder)
            where T : struct, IEntityCommands
        {
            builder.AddComponent(new EntityLinkParentData
            {
                EntityToParent = EntityToParent,
                ReadRootFrom = ReadRootFrom,
                ParentLinkKey = ParentLinkKey,
                LocalPosition = LocalPosition,
                LocalRotation = LocalRotation,
                RestoreOnEnd = RestoreOnEnd
            });

            builder.AddComponent<EntityLinkParentState>();
        }
    }
}