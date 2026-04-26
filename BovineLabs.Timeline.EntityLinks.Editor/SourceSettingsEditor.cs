using BovineLabs.Core.Editor.Inspectors;
using BovineLabs.Core.Editor.ObjectManagement;
using BovineLabs.Timeline.EntityLinks.Authoring;
using UnityEditor;
using UnityEngine.UIElements;

namespace BovineLabs.Timeline.EntityLinks.Editor
{
    [CustomEditor(typeof(SourceSettings))]
    public class SourceSettingsEditor : ElementEditor
    {
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            return property.name switch
            {
                "sourceSchemas" => new AssetCreator<SourceSchema>(this.serializedObject, property).Element,
                _ => base.CreateElement(property),
            };
        }
    }
}
