using BovineLabs.Core.Editor.Inspectors;
using BovineLabs.Core.Editor.ObjectManagement;
using BovineLabs.Timeline.EntityLinks.Authoring;
using UnityEditor;
using UnityEngine.UIElements;

namespace BovineLabs.Timeline.EntityLinks.Editor
{
    [CustomEditor(typeof(EntityLinkSettings))]
    public class EntityLinkSettingsEditor : ElementEditor
    {
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            return property.name switch
            {
                "entityLinkTagSchemas" => new AssetCreator<EntityLinkTagSchema>(this.serializedObject, property).Element,
                _ => base.CreateElement(property),
            };
        }
    }
}
