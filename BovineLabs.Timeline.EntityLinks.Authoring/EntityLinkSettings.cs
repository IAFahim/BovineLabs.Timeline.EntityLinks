using System;
using System.Collections.Generic;
using BovineLabs.Core.Keys;
using BovineLabs.Core.Settings;
using UnityEngine;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    [SettingsGroup("EntityLinks")]
    public class EntityLinkSettings : KSettingsBase<EntityLinkSettings, byte>
    {
        [SerializeField] 
        private EntityLinkTagSchema[] entityLinkTagSchemas = Array.Empty<EntityLinkTagSchema>();
        
        public IReadOnlyList<EntityLinkTagSchema> EntityLinkTagSchemas => this.entityLinkTagSchemas;

        public override IEnumerable<NameValue<byte>> Keys
        {
            get
            {
                foreach (var schema in this.entityLinkTagSchemas)
                {
                    if (schema == null) continue;
                    yield return new NameValue<byte>(schema.name, schema.Id);
                }
            }
        }
    }
}
