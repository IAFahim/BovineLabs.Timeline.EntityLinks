using BovineLabs.EntityLinks;
using BovineLabs.Timeline.Instantiate;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Timeline.EntityLinks
{
    public struct EntityLinkAttachState : IComponentData
    {
        public byte LinkKey;
        public ResolveRule ResolveRule;
        public ParentTransformConfig TransformConfig;
        
        public Entity CapturedPreviousParent;
        public LocalTransform CapturedOriginalTransform;
        public bool HadPostTransformMatrix;
        public float4x4 CapturedPostTransformMatrix;

        public bool WasSuccessfullyAttached;
        public Entity ResolvedTarget;
    }

    public struct EntityLinkInstantiateConfig : IComponentData
    {
        public Entity Prefab;
        public byte LinkKey;
        public ResolveRule ResolveRule;
        public ParentTransformConfig TransformConfig;
    }

    public struct OnClipActiveEntityLinkInstantiateTag : IComponentData { }
}