using System.Collections.Generic;
using System.Linq;
using BovineLabs.Timeline.Core.Editor.CliTools.Shared;
using BovineLabs.Timeline.EntityLinks.Authoring;
using Newtonsoft.Json.Linq;
using UnityCliConnector;
using UnityEditor;

namespace BovineLabs.Timeline.EntityLinks.Editor.CliTools
{
    [UnityCliTool(
        Name = "entitylink_list",
        Group = "vex",
        Description =
            "Read-only project sweep of every EntityLinkSchema asset: assetPath, guid, and the imported runtime id. Flags id == 0 as unusable (the id-0-unusable check). No subscene needed (§3.4 discovery).")]
    public static class EntityLinkListTool
    {
        public static object HandleCommand(JObject @params)
        {
            _ = new Params(@params);
            try
            {
                var found = new List<(string name, string assetPath, string guid, ushort id)>();

                foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(EntityLinkSchema)}"))
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var schema = AssetDatabase.LoadAssetAtPath<EntityLinkSchema>(assetPath);
                    if (schema == null) continue;
                    found.Add((schema.name, assetPath, guid, schema.Id));
                }

                var schemas = found
                    .OrderBy(s => s.id == 0 ? 0 : 1)
                    .ThenBy(s => s.id)
                    .Select(s => (object)new
                    {
                        s.name,
                        s.assetPath,
                        s.guid,
                        s.id,
                        idUsable = s.id != 0
                    })
                    .ToList();

                var unusable = found.Count(s => s.id == 0);
                var message = unusable == 0
                    ? $"{found.Count} link schema(s)."
                    : $"{found.Count} link schema(s); {unusable} with id 0 (UNUSABLE — re-import to assign a key).";

                return ToolEnvelope.Ok(message, new { schemas });
            }
            catch (ToolException e)
            {
                return ToolEnvelope.FromException(e);
            }
        }
    }
}