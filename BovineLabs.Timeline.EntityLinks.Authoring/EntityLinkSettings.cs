using System;
using System.Collections.Generic;
using BovineLabs.Core.Keys;
using BovineLabs.Core.Settings;
using UnityEngine;

namespace BovineLabs.Timeline.EntityLinks.Authoring
{
    [SettingsGroup("Entity Links")]
    public sealed class EntityLinkSettings : KSettingsBase<EntityLinkSettings, ushort>
    {
        [SerializeField]
        private EntityLinkSchema[] schemas = Array.Empty<EntityLinkSchema>();

        public IReadOnlyList<EntityLinkSchema> Schemas => this.schemas;

        public override IEnumerable<NameValue<ushort>> Keys
        {
            get
            {
                foreach (var schema in this.schemas)
                {
                    if (schema == null)
                    {
                        continue;
                    }

                    yield return new NameValue<ushort>(schema.name, schema.Id);
                }
            }
        }
    }
}