using BovineLabs.Core.Editor.Inspectors;
using BovineLabs.Timeline.Core.Editor;
using BovineLabs.Timeline.EntityLinks.Authoring;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BovineLabs.Timeline.EntityLinks.Editor
{
    [CustomEditor(typeof(EntityLinkSourceAuthoring))]
    public sealed class EntityLinkSourceAuthoringEditor : ElementEditor
    {
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            if (property.name != "Root" || MultiEditing) return base.CreateElement(property);

            var container = new VisualElement();
            container.Add(CreatePropertyField(property, serializedObject));

            var authoring = (EntityLinkSourceAuthoring)target;
            GameObject resolved = null;

            var ping = new Button(() => EditorInspect.Open(resolved));
            ping.style.unityTextAlign = TextAnchor.MiddleLeft;
            container.Add(ping);

            var warn = new Label("⚠ no EntityLinkRootAuthoring in parents — this Source won't bake.")
            {
                pickingMode = PickingMode.Ignore
            };
            warn.style.opacity = 0.7f;
            warn.style.whiteSpace = WhiteSpace.Normal;
            container.Add(warn);

            void Refresh()
            {
                var prop = serializedObject.FindProperty("Root");
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