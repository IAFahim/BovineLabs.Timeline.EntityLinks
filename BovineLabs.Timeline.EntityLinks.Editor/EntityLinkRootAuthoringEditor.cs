// <copyright file="EntityLinkRootAuthoringEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.EntityLinks.Editor
{
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Timeline.EntityLinks.Authoring;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    /// Inspector for <see cref="EntityLinkRootAuthoring" /> that shows the <b>resolved link table</b> the baker
    /// produces — each schema (name + id) mapped to the Source GameObject it points at — plus the baker's
    /// failure cases (id 0, duplicate key, Source under a different root) as inline warnings. Otherwise the
    /// inspector only shows a raw <c>Links</c> array and the actual "which Source is the 'Hand' link?" mapping
    /// stays invisible until runtime.
    /// </summary>
    [CustomEditor(typeof(EntityLinkRootAuthoring))]
    public sealed class EntityLinkRootAuthoringEditor : ElementEditor
    {
        /// <inheritdoc />
        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            var foldout = CreateFoldout("Resolved Links", true);
            root.Add(foldout);

            this.Rebuild(foldout);
            foldout.TrackSerializedObjectValue(this.serializedObject, _ => this.Rebuild(foldout));
        }

        private void Rebuild(VisualElement foldout)
        {
            foldout.Clear();

            if (this.MultiEditing)
            {
                foldout.Add(new Label("(multi-editing)"));
                return;
            }

            var authoring = (EntityLinkRootAuthoring)this.target;
            var rows = Resolve(authoring);

            if (rows.Count == 0)
            {
                var empty = new Label("No links — add EntityLinkSourceAuthoring under this root and assign schemas.");
                empty.style.opacity = 0.6f;
                empty.style.whiteSpace = WhiteSpace.Normal;
                foldout.Add(empty);
                return;
            }

            foreach (var row in rows)
            {
                foldout.Add(row.Warning != null ? Warning(row.Warning) : LinkRow(row));
            }
        }

        private static VisualElement LinkRow(Row row)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.marginBottom = 1;

            var label = new Label(row.SchemaName);
            label.style.flexGrow = 0;
            label.style.flexShrink = 0;
            label.style.minWidth = 120;
            container.Add(label);

            // The whole value is a ping button: click it to select/ping the linked GameObject in the Hierarchy.
            var target = row.Target;
            var ping = new Button(() => EntityLinkEditorPing.Ping(target))
            {
                text = target != null ? $"◎  {target.name}" : "◎  (missing)",
                tooltip = target != null ? $"Ping '{target.name}'." : "Linked GameObject missing.",
            };
            ping.style.flexGrow = 1;
            ping.style.unityTextAlign = TextAnchor.MiddleLeft;
            container.Add(ping);

            return container;
        }

        private static HelpBox Warning(string text)
        {
            return new HelpBox(text, HelpBoxMessageType.Warning);
        }

        // Mirror EntityLinkRootAuthoring.Baker: walk Links, key each non-zero schema, detect the same failures.
        private static List<Row> Resolve(EntityLinkRootAuthoring authoring)
        {
            var rows = new List<Row>();
            var seen = new HashSet<ushort>();

            foreach (var source in authoring.Links)
            {
                if (source == null)
                {
                    continue;
                }

                if (!source.TryGetRoot(out var sourceRoot) || sourceRoot != authoring)
                {
                    rows.Add(Row.Warn($"'{source.name}' is not under this root — skipped."));
                    continue;
                }

                foreach (var schema in source.Schemas)
                {
                    if (schema == null)
                    {
                        continue;
                    }

                    if (schema.Id == 0)
                    {
                        rows.Add(Row.Warn($"Schema '{schema.name}' on '{source.name}' has id 0 (un-imported) — re-import to assign a key."));
                        continue;
                    }

                    if (!seen.Add(schema.Id))
                    {
                        rows.Add(Row.Warn($"Duplicate link key {schema.Id} ('{schema.name}' on '{source.name}') — ignored by the baker."));
                        continue;
                    }

                    rows.Add(Row.Link(schema.name, schema.Id, source.gameObject));
                }
            }

            return rows;
        }

        private readonly struct Row
        {
            public readonly string SchemaName;
            public readonly ushort Id;
            public readonly GameObject Target;
            public readonly string Warning;

            private Row(string schemaName, ushort id, GameObject target, string warning)
            {
                this.SchemaName = schemaName;
                this.Id = id;
                this.Target = target;
                this.Warning = warning;
            }

            public static Row Link(string schemaName, ushort id, GameObject target) => new(schemaName, id, target, null);

            public static Row Warn(string warning) => new(null, 0, null, warning);
        }
    }
}
