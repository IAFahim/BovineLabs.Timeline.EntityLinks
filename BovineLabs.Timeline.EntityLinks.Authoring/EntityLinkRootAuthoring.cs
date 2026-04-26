using System;
using System.Collections.Generic;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    [DisallowMultipleComponent]
    public sealed class EntityLinkRootAuthoring : MonoBehaviour
    {
        public bool AutoCollectAnchors = true;
        public Link[] Links = Array.Empty<Link>();

        [Serializable]
        public sealed class Link
        {
            public EntityLinkSchema Schema;
            public EntityLinkSourceAuthoring Target;
            public bool TryGetTarget(out EntityLinkSourceAuthoring target) { target = Target; return target!= null; }
        }

        private enum LinkOrigin : byte { Auto, Manual }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!AutoCollectAnchors) return;
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                AutoPopulateLinks();
            };
        }

        private void AutoPopulateLinks()
        {
            var sources = GetComponentsInChildren<EntityLinkSourceAuthoring>(true);
            var newLinks = new List<Link>();
            var seen = new HashSet<EntityLinkSchema>();

            foreach (var src in sources)
            {
                if (!src.TryGetRoot(out var root) || root!= this)
                {
                    src.Root = this;
                    EditorUtility.SetDirty(src);
                }

                if (src.Schema!= null && seen.Add(src.Schema))
                    newLinks.Add(new Link { Schema = src.Schema, Target = src });

                if (src.Aliases!= null)
                {
                    foreach (var alias in src.Aliases)
                    {
                        if (alias!= null && seen.Add(alias))
                            newLinks.Add(new Link { Schema = alias, Target = src });
                    }
                }
            }

            if (newLinks.Count!= Links.Length)
            {
                Undo.RecordObject(this, "Auto Populate Entity Links");
                Links = newLinks.ToArray();
                EditorUtility.SetDirty(this);
            }
        }
#endif
        private sealed class Baker : Baker<EntityLinkRootAuthoring>
        {
            public override void Bake(EntityLinkRootAuthoring authoring)
            {
                var rootEntity = this.GetEntity(TransformUsageFlags.None);
                var links = new Dictionary<ushort, EntityLinkAuthoringUtility.Entry>();

                if (authoring.AutoCollectAnchors)
                {
                    this.CollectSources(authoring, links);
                }

                this.CollectManualLinks(authoring, links);

                var entries = new List<EntityLinkAuthoringUtility.Entry>(links.Values);
                entries.Sort((a, b) => a.Key.CompareTo(b.Key));

                if (entries.Count > ushort.MaxValue)
                {
                    Debug.LogError($"EntityLinkRoot '{authoring.name}' has too many links.");
                    return;
                }

                var blob = EntityLinkAuthoringUtility.CreateBlob(entries);
                this.AddBlobAsset(ref blob, out _);

                this.AddComponent<EntityLinkRoot>(rootEntity);
                this.AddComponent(rootEntity, new EntityLinkMap { Blob = blob });

                var values = this.AddBuffer<EntityLinkValue>(rootEntity);
                foreach (var entry in entries)
                {
                    values.Add(new EntityLinkValue
                    {
                        Value = this.GetEntity(entry.Target, TransformUsageFlags.None)
                    });
                }
            }

            private void CollectSources(EntityLinkRootAuthoring root,
                Dictionary<ushort, EntityLinkAuthoringUtility.Entry> links)
            {
                var sources = root.GetComponentsInChildren<EntityLinkSourceAuthoring>(true);
                var schemas = new List<EntityLinkSchema>(4);

                foreach (var source in sources)
                {
                    this.DependsOn(source);

                    if (!source.TryGetRoot(out var sourceRoot) || sourceRoot != root)
                    {
                        continue;
                    }

                    schemas.Clear();
                    source.AddSchemas(schemas);

                    foreach (var schema in schemas)
                    {
                        if (!EntityLinkAuthoringUtility.TryGetKey(schema, out var key))
                        {
                            continue;
                        }

                        this.DependsOn(schema);
                        this.AddLink(root, links, key, source, schema.name, LinkOrigin.Auto);
                    }
                }
            }

            private void CollectManualLinks(EntityLinkRootAuthoring root,
                Dictionary<ushort, EntityLinkAuthoringUtility.Entry> links)
            {
                foreach (var link in root.Links)
                {
                    if (link == null)
                    {
                        continue;
                    }

                    if (!EntityLinkAuthoringUtility.TryGetKey(link.Schema, out var key))
                    {
                        continue;
                    }

                    if (!link.TryGetTarget(out var target))
                    {
                        Debug.LogError($"EntityLink '{link.Schema.name}' on '{root.name}' has null target.");
                        continue;
                    }

                    this.DependsOn(link.Schema);
                    this.DependsOn(target);
                    this.AddLink(root, links, key, target, link.Schema.name, LinkOrigin.Manual);
                }
            }

            private void AddLink(
                EntityLinkRootAuthoring root,
                Dictionary<ushort, EntityLinkAuthoringUtility.Entry> links,
                ushort key,
                EntityLinkSourceAuthoring target,
                string name,
                LinkOrigin origin)
            {
                if (target == null)
                {
                    Debug.LogError($"EntityLink '{name}' on '{root.name}' has null target.");
                    return;
                }

                if (!target.TryGetRoot(out var targetRoot))
                {
                    Debug.LogError($"EntityLink '{name}' target '{target.name}' has no root.");
                    return;
                }

                if (targetRoot != root)
                {
                    Debug.LogError(
                        $"EntityLink '{name}' on '{root.name}' targets '{target.name}' under different root '{targetRoot.name}'.");
                    return;
                }

                var entry = new EntityLinkAuthoringUtility.Entry(key, target, origin == LinkOrigin.Manual);

                if (!links.TryGetValue(key, out var existing))
                {
                    links.Add(key, entry);
                    return;
                }

                if (!existing.IsManual && entry.IsManual)
                {
                    links[key] = entry;
                    return;
                }

                Debug.LogError($"Duplicate EntityLink '{name}' on '{root.name}'.");
            }
        }
    }
}