using BovineLabs.Core.Authoring;
using Bovinelabs.Timeline.EntityLinks.Data;
using UnityEngine;

namespace Bovinelabs.Timeline.EntityLinks.Authoring
{
    public class EntityTagAuthoring : MonoBehaviour
    {
        public EntityLinkTagSchema entityLinkTagSchema;

        private void OnValidate()
        {
            if (!transform.gameObject.TryGetComponent(out TransformAuthoring transformAuthoring))
                transform.gameObject.AddComponent<TransformAuthoring>();
        }
    }
}