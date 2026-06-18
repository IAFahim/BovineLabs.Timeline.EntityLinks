// <copyright file="EntityLinkSourceAuthoringEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.EntityLinks.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Timeline.EntityLinks.Authoring;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    /// Inspector for <see cref="EntityLinkSourceAuthoring" /> that, like the TargetsAuthoring treatment, reveals
    /// the auto-resolved <c>Root</c>: when the field is empty the Source still binds to its parent
    /// <see cref="EntityLinkRootAuthoring" /> (<c>TryGetRoot</c>), so a slight hint shows which root that is —
    /// or warns when none exists. Schema ids on the <c>Schemas</c> array are surfaced by
    /// <see cref="EntityLinkSchemaDrawer" />.
    /// </summary>
    [CustomEditor(typeof(EntityLinkSourceAuthoring))]
    public sealed class EntityLinkSourceAuthoringEditor : ElementEditor
    {
        /// <inheritdoc />
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            if (property.name != "Root" || this.MultiEditing)
            {
                return base.CreateElement(property);
            }

            var container = new VisualElement();
            container.Add(CreatePropertyField(property, this.serializedObject));

            var authoring = (EntityLinkSourceAuthoring)this.target;
            GameObject resolved = null;

            // When Root is empty it still bakes to the parent root — a ping button jumps to that GameObject.
            var ping = new Button(() => EntityLinkEditorPing.Ping(resolved));
            ping.style.unityTextAlign = TextAnchor.MiddleLeft;
            container.Add(ping);

            var warn = new Label("⚠ no EntityLinkRootAuthoring in parents — this Source won't bake.")
            {
                pickingMode = PickingMode.Ignore,
            };
            warn.style.opacity = 0.7f;
            warn.style.whiteSpace = WhiteSpace.Normal;
            container.Add(warn);

            void Refresh()
            {
                var prop = this.serializedObject.FindProperty("Root");
                if (prop.objectReferenceValue != null)
                {
                    ping.style.display = DisplayStyle.None;
                    warn.style.display = DisplayStyle.None;
                    return;
                }

                if (authoring != null && authoring.TryGetRoot(out var root) && root != null)
                {
                    resolved = root.gameObject;
                    ping.text = $"◎  auto root: {root.name}";
                    ping.tooltip = $"Empty → binds to the parent root '{root.name}'. Click to ping it.";
                    ping.style.display = DisplayStyle.Flex;
                    warn.style.display = DisplayStyle.None;
                }
                else
                {
                    resolved = null;
                    ping.style.display = DisplayStyle.None;
                    warn.style.display = DisplayStyle.Flex;
                }
            }

            Refresh();
            container.TrackPropertyValue(property, _ => Refresh());
            return container;
        }
    }
}
