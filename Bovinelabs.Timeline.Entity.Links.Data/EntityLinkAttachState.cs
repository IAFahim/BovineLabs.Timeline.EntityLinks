using BovineLabs.EntityLinks;
using Unity.Entities;
using Unity.Transforms;

namespace BovineLabs.Timeline.EntityLinks
{
    public struct EntityLinkAttachState : IComponentData
    {
        public byte LinkKey;
        public ResolveRule ResolveRule;
        
        // Runtime State Capture
        public Entity CapturedPreviousParent;
        public LocalTransform CapturedOriginalTransform;
        public bool WasSuccessfullyAttached;
        public Entity ResolvedTarget; // Added for Debug Drawing
    }
}