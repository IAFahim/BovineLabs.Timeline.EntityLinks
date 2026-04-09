using System;
using System.Collections.Generic;
using BovineLabs.Core.Settings;
using UnityEngine;

namespace Bovinelabs.Timeline.Entity.Links.Authoring
{
    [SettingsGroup("EntityLinks")]
    public class EntityLinkSettings : ScriptableObject, ISettings
    {
        [SerializeField] private EntityLinkTagSchema[] keys = Array.Empty<EntityLinkTagSchema>();
        public IReadOnlyCollection<EntityLinkTagSchema> Keys => this.keys;
    }
}