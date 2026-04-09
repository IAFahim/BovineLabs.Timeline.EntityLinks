using BovineLabs.Core.Authoring;
using UnityEngine;

namespace Bovinelabs.Timeline.Entity.Links.Authoring
{
    public class EntityTagAuthoring : MonoBehaviour
    {
        public EntityLinkTagSchema tag;

        private void OnValidate()
        {
            if (!transform.gameObject.TryGetComponent(out TransformAuthoring transformAuthoring))
            {
                transform.gameObject.AddComponent<TransformAuthoring>();
            }
        }
    }
}