using BovineLabs.Core.ObjectManagement;
using BovineLabs.Core.PropertyDrawers;
using UnityEngine;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    [AutoRef(nameof(EntityLinkSettings), "entityLinkTagSchemas", nameof(EntityLinkTagSchema), "Schemas/EntityLinks")]
    [CreateAssetMenu(menuName = "BovineLabs/EntityLinks/Tag")]
    public class EntityLinkTagSchema : ScriptableObject, IUID
    {
        [SerializeField]
        [InspectorReadOnly]
        private byte id;

        public byte Id => this.id;

        int IUID.ID
        {
            get => this.id;
            set
            {
                if (value is < 0 or > byte.MaxValue)
                {
                    Debug.LogError("Ran out of keys");
                    return;
                }

                this.id = (byte)value;
            }
        }

        public static implicit operator byte(EntityLinkTagSchema schema)
        {
            return schema == null ? (byte)0 : schema.id;
        }
    }
}
