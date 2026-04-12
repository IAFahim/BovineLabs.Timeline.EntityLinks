using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Timeline.EntityLinks.Data
{
    public struct EntityLookupStoreData : IBufferElementData
    {
        public byte Tag;
        public Entity Value;
    }

    [Flags]
    public enum AttachmentTransformFlags : byte
    {
        None = 0,
        SetParent = 1 << 0,
        SetTransform = 1 << 1
    }

    [Flags]
    public enum ResolveRule : byte
    {
        None = 0,
        Parent = 1 << 0,
        ParentsTarget = 1 << 1,
        SelfTarget = 1 << 2,
        Owner = 1 << 3,
        Source = 1 << 4,
        Target = 1 << 5
    }

    public struct EntityLinkAttachConfig : IComponentData
    {
        public byte LinkKey;
        public ResolveRule ResolveRule;
        public AttachmentTransformFlags TransformFlags;
    }

    public struct EntityLinkAttachState : IComponentData
    {
        public Entity ResolvedTarget;
        public Entity CapturedPreviousParent;
        public LocalTransform CapturedOriginalTransform;
        public float4x4 CapturedOriginalPTM;
        public bool IsAttached;
        public bool HadPostTransformMatrix;
    }

    public struct EntityLinkInstantiateConfig : IComponentData
    {
        public Entity Prefab;
        public byte LinkKey;
        public ResolveRule ResolveRule;
        public AttachmentTransformFlags TransformFlags;
    }

    public static class ResolveRuleExtensions
    {
        public static bool HasAny(this ResolveRule rule, in ResolveRule flags)
        {
            return (rule & flags) != 0;
        }
    }

    public static class AttachmentTransformFlagsExtensions
    {
        public static bool HasAny(this AttachmentTransformFlags configuration, in AttachmentTransformFlags flags)
        {
            return (configuration & flags) != 0;
        }
    }
}