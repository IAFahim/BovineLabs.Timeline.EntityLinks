using System;
using System.Collections.Generic;
using BovineLabs.Timeline.EntityLinks.Data;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    [DisallowMultipleComponent]
    public sealed class EntityLinkRootAuthoring : MonoBehaviour
    {
        public EntityLinkSourceAuthoring[] Links = Array.Empty<EntityLinkSourceAuthoring>();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Links = GetComponentsInChildren<EntityLinkSourceAuthoring>(true);
        }
#endif

        private sealed class Baker : Baker<EntityLinkRootAuthoring>
        {
            public override void Bake(EntityLinkRootAuthoring authoring)
            {
                var rootEntity = GetEntity(TransformUsageFlags.None);
                var links = new Dictionary<ushort, EntityLinkAuthoringUtility.Entry>();
                var schemas = new List<EntityLinkSchema>(4);
                var seenSchemas = new HashSet<EntityLinkSchema>();

                foreach (var source in authoring.Links)
                {
                    if (source == null) continue;

                    DependsOn(source);

                    if (!source.TryGetRoot(out var sourceRoot) || sourceRoot != authoring)
                        continue;

                    schemas.Clear();
                    source.AddSchemas(schemas);

                    foreach (var schema in schemas)
                    {
                        if (!EntityLinkAuthoringUtility.TryGetKey(schema, out var key)) continue;

                        if (seenSchemas.Add(schema)) DependsOn(schema);
                        AddLink(authoring, links, key, source, schema.name);
                    }
                }

                var entries = new List<EntityLinkAuthoringUtility.Entry>(links.Values);
                entries.Sort((a, b) => a.Key.CompareTo(b.Key));

                var entryBuffer = AddBuffer<EntityLinkEntry>(rootEntity);

                foreach (var entry in entries)
                    entryBuffer.Add(new EntityLinkEntry
                    {
                        Key = entry.Key,
                        Target = GetEntity(entry.Target, TransformUsageFlags.None)
                    });
            }

            private void AddLink(
                EntityLinkRootAuthoring root,
                Dictionary<ushort, EntityLinkAuthoringUtility.Entry> links,
                ushort key,
                EntityLinkSourceAuthoring target,
                string schemaName)
            {
                if (!target.TryGetRoot(out var targetRoot))
                {
                    Debug.LogError($"EntityLink '{schemaName}' target '{target.name}' has no root.");
                    return;
                }

                if (targetRoot != root)
                {
                    Debug.LogError(
                        $"EntityLink '{schemaName}' on '{root.name}' targets '{target.name}' under different root '{targetRoot.name}'.");
                    return;
                }

                if (links.ContainsKey(key))
                {
                    Debug.LogError($"Duplicate EntityLink '{schemaName}' on '{root.name}'.");
                    return;
                }

                links.Add(key, new EntityLinkAuthoringUtility.Entry(key, target));
            }
        }
    }
}