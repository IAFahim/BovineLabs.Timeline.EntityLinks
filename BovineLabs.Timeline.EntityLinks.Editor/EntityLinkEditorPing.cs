// <copyright file="EntityLinkEditorPing.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.EntityLinks.Editor
{
    using UnityEditor;
    using UnityEngine;

    /// <summary> Safe ping for the EntityLinks editor affordances. </summary>
    internal static class EntityLinkEditorPing
    {
        /// <summary>
        /// Ping an object, tolerating Unity's Hierarchy throwing when the target can't be framed (objects in
        /// unloaded SubScenes / Timeline-preview contexts throw <c>HierarchyNode not found</c>). The throw from
        /// <see cref="EditorGUIUtility.PingObject" /> is synchronous so we can swallow it; we deliberately do NOT
        /// fall back to <c>Selection.activeObject</c> — that defers the same framing to the Hierarchy's next
        /// update, where the throw escapes our try/catch and spams the console.
        /// </summary>
        public static void Ping(Object obj)
        {
            if (obj == null)
            {
                return;
            }

            try
            {
                EditorGUIUtility.PingObject(obj);
            }
            catch
            {
                // Target isn't framable from the current Hierarchy (SubScene/preview). Nothing safe to do —
                // never let a debug affordance throw into the inspector.
            }
        }
    }
}
