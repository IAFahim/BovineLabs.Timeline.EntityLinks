// <copyright file="EntityLinkSchemaDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.EntityLinks.Editor
{
    using BovineLabs.Timeline.EntityLinks.Authoring;
    using UnityEditor;
    using UnityEditor.Timeline;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    /// <summary>
    /// Draws every <see cref="EntityLinkSchema" /> reference with a <b>ping</b> button that selects/pings the
    /// GameObject this link resolves to. On a Timeline clip it resolves the Source under the bound root
    /// (mirroring <see cref="EntityLinkAuthoringUtility" />); the button greys out when nothing resolves.
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
            var pingRect = new Rect(position.xMax - ButtonWidth, position.y, ButtonWidth, position.height);

            EditorGUI.BeginChangeCheck();
            var newSchema = (EntityLinkSchema)EditorGUI.ObjectField(
                fieldRect, label, property.objectReferenceValue, typeof(EntityLinkSchema), false);
            if (EditorGUI.EndChangeCheck())
            {
                property.objectReferenceValue = newSchema;
            }

            var schema = property.objectReferenceValue as EntityLinkSchema;
            var target = ResolveLinkedGameObject(property, schema);

            using (new EditorGUI.DisabledScope(target == null))
            {
                var tooltip = target != null
                    ? $"Ping '{target.name}' — the GameObject this link resolves to."
                    : "No resolvable GameObject (select the clip in a Timeline bound to a root).";

                if (GUI.Button(pingRect, new GUIContent("◎", tooltip)) && target != null)
                {
                    EntityLinkEditorPing.Ping(target);
                }
            }

            EditorGUI.EndProperty();
        }

        // The Source GameObject this schema lands on, resolved via the inspected timeline's bound root.
        private static GameObject ResolveLinkedGameObject(SerializedProperty property, EntityLinkSchema schema)
        {
            if (schema == null || !IsClip(property) || !TryResolveBoundRoot(property, out var root))
            {
                return null;
            }

            return EntityLinkAuthoringUtility.TryFindLinkedComponent(root, schema, out var linked)
                ? linked.gameObject
                : null;
        }

        private static bool IsClip(SerializedProperty property)
        {
            var targets = property.serializedObject.targetObjects;
            return targets.Length == 1 && targets[0] is PlayableAsset;
        }

        private static bool TryResolveBoundRoot(SerializedProperty property, out EntityLinkRootAuthoring root)
        {
            root = null;
            try
            {
                var director = TimelineEditor.inspectedDirector;
                var asset = TimelineEditor.inspectedAsset;
                if (director == null || asset == null)
                {
                    return false;
                }

                var clipAsset = property.serializedObject.targetObject;
                TrackAsset track = null;
                foreach (var t in asset.GetOutputTracks())
                {
                    foreach (var c in t.GetClips())
                    {
                        if (ReferenceEquals(c.asset, clipAsset))
                        {
                            track = t;
                            break;
                        }
                    }

                    if (track != null)
                    {
                        break;
                    }
                }

                if (track == null)
                {
                    return false;
                }

                var binding = director.GetGenericBinding(track);
                var component = binding as Component ?? (binding as GameObject)?.transform;
                if (component == null)
                {
                    return false;
                }

                root = component.GetComponentInParent<EntityLinkRootAuthoring>(true)
                       ?? component.GetComponentInChildren<EntityLinkRootAuthoring>(true);

                return root != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
