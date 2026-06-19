// <copyright file="EntityLinkSchemaDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.EntityLinks.Editor
{
    using BovineLabs.Timeline.Core.Editor;
    using BovineLabs.Timeline.EntityLinks.Authoring;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Draws every <see cref="EntityLinkSchema" /> reference with a ◎ button that opens (Alt+P style) the GameObject
    /// this link resolves to. On a Timeline clip it resolves the Source under the bound root (via
    /// <see cref="TimelineBinding" /> + <see cref="EntityLinkAuthoringUtility" />); the button greys out otherwise.
    /// </summary>
    [CustomPropertyDrawer(typeof(EntityLinkSchema))]
    public sealed class EntityLinkSchemaDrawer : PropertyDrawer
    {
        private const float ButtonWidth = 24f;

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var fieldRect = new Rect(position.x, position.y, position.width - ButtonWidth - 2f, position.height);
            var buttonRect = new Rect(position.xMax - ButtonWidth, position.y, ButtonWidth, position.height);

            EditorGUI.BeginChangeCheck();
            var newSchema = (EntityLinkSchema)EditorGUI.ObjectField(
                fieldRect, label, property.objectReferenceValue, typeof(EntityLinkSchema), false);
            if (EditorGUI.EndChangeCheck())
            {
                property.objectReferenceValue = newSchema;
            }

            var target = ResolveLinkedGameObject(property, property.objectReferenceValue as EntityLinkSchema);
            EditorInspect.OpenButton(buttonRect, target, "No resolvable GameObject (select the clip in a Timeline bound to a root).");

            EditorGUI.EndProperty();
        }

        // The Source GameObject this schema lands on, resolved via the inspected timeline's bound root.
        private static GameObject ResolveLinkedGameObject(SerializedProperty property, EntityLinkSchema schema)
        {
            if (schema == null || !TimelineBinding.TryGetBoundComponent(property, out var bound))
            {
                return null;
            }

            var root = bound.GetComponentInParent<EntityLinkRootAuthoring>(true)
                       ?? bound.GetComponentInChildren<EntityLinkRootAuthoring>(true);

            return root != null && EntityLinkAuthoringUtility.TryFindLinkedComponent(root, schema, out var linked)
                ? linked.gameObject
                : null;
        }
    }
}
