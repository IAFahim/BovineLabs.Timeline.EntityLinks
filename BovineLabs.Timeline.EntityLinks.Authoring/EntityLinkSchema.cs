using BovineLabs.Core.ObjectManagement;
using BovineLabs.Core.PropertyDrawers;
using BovineLabs.Timeline.EntityLinks.Data;
using UnityEngine;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    [AutoRef(nameof(EntityLinkSettings), "schemas", nameof(EntityLinkSchema), "Schemas/EntityLinks/")]
    [CreateAssetMenu(menuName = "BovineLabs/Timeline/Entity Links/Schema")]
    public sealed class EntityLinkSchema : ScriptableObject, IUID
    {
        [SerializeField]
        [InspectorReadOnly]
        private ushort id;

        public ushort Id => this.id;

        int IUID.ID
        {
            get => this.id;
            set
            {
                if (value is < 0 or > ushort.MaxValue)
                {
                    Debug.LogError("Ran out of EntityLink schema keys.");
                    return;
                }

                this.id = (ushort)value;
            }
        }

        public static implicit operator ushort(EntityLinkSchema schema)
        {
            return schema == null ? (ushort)0 : schema.id;
        }

        public static implicit operator ulong(EntityLinkSchema schema)
        {
            return schema == null ? 0UL : schema.id;
        }

        public static implicit operator EntityLinkKey(EntityLinkSchema schema)
        {
            return new EntityLinkKey(schema == null ? (ushort)0 : schema.id);
        }
    }
}