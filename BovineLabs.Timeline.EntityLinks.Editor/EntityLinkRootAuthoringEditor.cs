using System.Collections.Generic;
using BovineLabs.Core.Editor.Inspectors;
using BovineLabs.Timeline.Core.Editor;
using BovineLabs.Timeline.EntityLinks.Authoring;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BovineLabs.Timeline.EntityLinks.Editor
{
    [CustomEditor(typeof(EntityLinkRootAuthoring))]
    public sealed class EntityLinkRootAuthoringEditor : ElementEditor
    {
        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            var foldout = CreateFoldout("Resolved Links", true);
            root.Add(foldout);

            Rebuild(foldout);
            foldout.TrackSerializedObjectValue(serializedObject, _ => Rebuild(foldout));
        }

        private void Rebuild(VisualElement foldout)
        {
            foldout.Clear();

            if (MultiEditing)
            {
                foldout.Add(new Label("(multi-editing)"));
                return;
            }

            var authoring = (EntityLinkRootAuthoring)target;
            var rows = Resolve(authoring);

            if (rows.Count == 0)
            {
                var empty = new Label("No links — add EntityLinkSourceAuthoring under this root and assign schemas.");
                empty.style.opacity = 0.6f;
                empty.style.whiteSpace = WhiteSpace.Normal;
                foldout.Add(empty);
                return;
            }

            foreach (var row in rows) foldout.Add(row.Warning != null ? Warning(row.Warning) : LinkRow(row));
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

            var target = row.Target;
            var open = new Button(() => EditorInspect.Open(target))
            {
                text = target != null ? $"◎  {target.name}" : "◎  (missing)",
                tooltip = target != null ? $"Open '{target.name}' properties." : "Linked GameObject missing."
            };
            open.style.flexGrow = 1;
            open.style.unityTextAlign = TextAnchor.MiddleLeft;
            container.Add(open);

            return container;
        }

        private static HelpBox Warning(string text)
        {
            return new HelpBox(text, HelpBoxMessageType.Warning);
        }

        private static List<Row> Resolve(EntityLinkRootAuthoring authoring)
        {
            var rows = new List<Row>();
            var seen = new HashSet<ushort>();

            foreach (var source in authoring.Links)
            {
                if (source == null) continue;

                if (!source.TryGetRoot(out var sourceRoot) || sourceRoot != authoring)
                {
                    rows.Add(Row.Warn($"'{source.name}' is not under this root — skipped."));
                    continue;
                }

                foreach (var schema in source.Schemas)
                {
                    if (schema == null) continue;

                    if (schema.Id == 0)
                    {
                        rows.Add(Row.Warn(
                            $"Schema '{schema.name}' on '{source.name}' has id 0 (un-imported) — re-import to assign a key."));
                        continue;
                    }

                    if (!seen.Add(schema.Id))
                    {
                        rows.Add(Row.Warn(
                            $"Duplicate link key {schema.Id} ('{schema.name}' on '{source.name}') — ignored by the baker."));
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
                SchemaName = schemaName;
                Id = id;
                Target = target;
                Warning = warning;
            }

            public static Row Link(string schemaName, ushort id, GameObject target)
            {
                return new Row(schemaName, id, target, null);
            }

            public static Row Warn(string warning)
            {
                return new Row(null, 0, null, warning);
            }
        }
    }
}