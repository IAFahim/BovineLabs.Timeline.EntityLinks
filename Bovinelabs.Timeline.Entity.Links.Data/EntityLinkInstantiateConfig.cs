using BovineLabs.EntityLinks;
using BovineLabs.Timeline.Instantiate;
using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks
{
    /// <summary>
    /// Configuration for spawning an entity and attaching it to a resolved link.
    /// </summary>
    public struct EntityLinkInstantiateConfig : IComponentData
    {
        public Entity Prefab;
        public byte LinkKey;
        public ResolveRule ResolveRule;
        public ParentTransformConfig TransformConfig;
    }

    /// <summary>
    /// Trigger tag: Only exists on the exact frame the clip activates.
    /// </summary>
    public struct OnClipActiveEntityLinkInstantiateTag : IComponentData { }
}