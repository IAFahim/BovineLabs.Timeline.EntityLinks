using BovineLabs.Reaction.Data.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Timeline.EntityLinks.Data
{
    public struct EntityLinkParentData : IComponentData
    {
        public Target EntityToParent;
        public Target ReadRootFrom;
        public ushort ParentLinkKey;

        public float3 LocalPosition;
        public quaternion LocalRotation;

        public bool RestoreOnEnd;
    }

    public struct EntityLinkParentState : IComponentData
    {
        public Entity Target;
        public Entity PreviousParent;
        public bool HadParent;
        public bool ParentApplied;
    }
}