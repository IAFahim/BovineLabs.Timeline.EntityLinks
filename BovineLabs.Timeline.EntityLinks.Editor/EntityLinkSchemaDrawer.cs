using BovineLabs.Timeline.Core.Editor;
using BovineLabs.Timeline.EntityLinks.Authoring;
using UnityEditor;
using UnityEngine;

namespace BovineLabs.Timeline.EntityLinks.Editor
{
    [CustomPropertyDrawer(typeof(EntityLinkSchema))]
    public sealed class EntityLinkSchemaDrawer : PropertyDrawer
    {
        private const float ButtonWidth = 24f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var fieldRect = new Rect(position.x, position.y, position.width - ButtonWidth - 2f, position.height);
            var buttonRect = new Rect(position.xMax - ButtonWidth, position.y, ButtonWidth, position.height);

            EditorGUI.BeginChangeCheck();
            var newSchema = (EntityLinkSchema)EditorGUI.ObjectField(
                fieldRect, label, property.objectReferenceValue, typeof(EntityLinkSchema), false);
            if (EditorGUI.EndChangeCheck()) property.objectReferenceValue = newSchema;

            var target = ResolveLinkedGameObject(property, property.objectReferenceValue as EntityLinkSchema);
            EditorInspect.OpenButton(buttonRect, target,
                "No resolvable GameObject (select the clip in a Timeline bound to a root).");

            EditorGUI.EndProperty();
        }

        private static GameObject ResolveLinkedGameObject(SerializedProperty property, EntityLinkSchema schema)
        {
            if (schema == null || !TimelineBinding.TryGetBoundComponent(property, out var bound)) return null;

            var root = bound.GetComponentInParent<EntityLinkRootAuthoring>(true)
                       ?? bound.GetComponentInChildren<EntityLinkRootAuthoring>(true);

            return root != null && EntityLinkAuthoringUtility.TryFindLinkedComponent(root, schema, out var linked)
                ? linked.gameObject
                : null;
        }
    }
}