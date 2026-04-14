using System;
using BovineLabs.Core.ObjectManagement;
using Unity.Entities;

namespace BovineLabs.Timeline.EntityLinks.Data
{
    public struct EntityLookup
    {
        public byte Tag;
        public Entity Value;
    }

    public struct EntityLookUpBlob : IComponentData
    {
        public BlobAssetReference<EntityLookupBlobData> Blob;
    }

    public struct EntityLookupBlobData
    {
        public BlobArray<EntityLookup> Entries;
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
    }

    public struct EntityLinkAttachState : IComponentData
    {
        public Entity ResolvedTarget;
        public Entity CapturedPreviousParent;
        public bool IsAttached;
    }

    public struct EntityLinkInstantiateConfig : IComponentData
    {
        public ObjectId Prefab;
        public byte LinkKey;
        public ResolveRule ResolveRule;
    }

    public static class ResolveRuleExtensions
    {
        public static bool HasAny(this ResolveRule rule, in ResolveRule flags)
        {
            return (rule & flags) != 0;
        }
    }
}