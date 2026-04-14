using System;
using System.Collections.Generic;
using BovineLabs.Core.Keys;
using BovineLabs.Core.Settings;
using Bovinelabs.Timeline.EntityLinks.Data;
using UnityEngine;

namespace Bovinelabs.Timeline.EntityLinks.Authoring
{
    [SettingsGroup("EntityLinks")]
    public class EntityLinkSettings : KSettingsBase<EntityLinkSettings, byte>
    {
        [SerializeField] private EntityLinkTagSchema[] entityLinkTagSchemas = Array.Empty<EntityLinkTagSchema>();
        public IReadOnlyList<EntityLinkTagSchema> EntityLinkTagSchemas => entityLinkTagSchemas;

        public override IEnumerable<NameValue<byte>> Keys
        {
            get
            {
                for (byte index = 0; index < entityLinkTagSchemas.Length; index++)
                {
                    var entityLinkTagSchema = entityLinkTagSchemas[index];
                    var actionName = entityLinkTagSchema != null
                        ? entityLinkTagSchema.name
                        : $"[Unassigned Action ID: {index}]";
                    entityLinkTagSchema.id = index;

                    yield return new NameValue<byte>(actionName, index);
                }
            }
        }
    }
}