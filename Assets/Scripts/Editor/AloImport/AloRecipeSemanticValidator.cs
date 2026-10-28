using System;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloRecipeSemanticValidator
    {
        public static void Validate(JObject recipe)
        {
            SafePath((string)recipe["modelId"], false);
            SafePath((string)recipe["source"]["alo"], false);
            if (recipe["source"]["xml"] != null) SafePath((string)recipe["source"]["xml"], false);
            if (recipe["source"]["textureDirectory"] != null) SafePath((string)recipe["source"]["textureDirectory"], false);
            if (recipe["source"]["textureOverrides"] is JArray overrides)
            {
                Unique(overrides, "source");
                foreach (JToken texture in overrides)
                {
                    SafePath((string)texture["source"], false);
                    SafePath((string)texture["path"], false);
                }
            }
            if (recipe["source"]["archives"] is JArray archives)
                foreach (string archive in archives.Values<string>()) SafePath(archive, false);
            if (recipe["source"]["xmlFiles"] is JArray xmlFiles)
            {
                string[] paths = xmlFiles.Values<string>().ToArray();
                if (paths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != paths.Length) Fail("Duplicate XML path");
                foreach (string path in paths) SafePath(path, false);
            }
            if (recipe["source"]["definitionOverrides"] is JArray definitions)
            {
                Unique(definitions, "name");
                foreach (JToken definition in definitions) SafePath((string)definition["file"], false);
            }
            JArray pieces = (JArray)recipe["pieces"];
            Unique(pieces, "id");
            string[] ids = pieces.Select(piece => (string)piece["id"]).ToArray();
            PieceReferences(recipe, ids);
            foreach (JToken piece in pieces) SafePath((string)piece["alo"], false);
            Unique((JArray)recipe["attachments"], "id");
            foreach (JToken attachment in recipe["attachments"])
            {
                if (attachment["parentInstance"] != null && !recipe["attachments"].Any(parent => (string)parent["id"] == (string)attachment["parentInstance"])) Fail("Unknown parent instance");
                if (!ids.Contains((string)attachment["piece"]) || !ids.Contains((string)attachment["parentPiece"])) Fail("Unknown attachment piece");
                if ((string)attachment["piece"] == (string)attachment["parentPiece"]) Fail("Attachment cannot parent itself");
            }
            JArray outputs = (JArray)recipe["integration"]["outputs"];
            Unique(outputs, "role");
            Unique(outputs, "path");
            foreach (JToken output in outputs)
            {
                SafePath((string)output["path"], true);
                if ((string)recipe["operation"] == "update" && output["guid"].Type == JTokenType.Null) Fail("Update requires output GUID baseline");
            }
            string[] guids = outputs.Where(output => output["guid"].Type != JTokenType.Null).Select(output => (string)output["guid"]).ToArray();
            if (guids.Distinct().Count() != guids.Length) Fail("Duplicate output GUID");
            string[] roles = outputs.Select(output => (string)output["role"]).ToArray();
            if (recipe["registrations"] is JObject registrations)
                foreach (JProperty registration in registrations.Properties())
                    if (registration.Name.EndsWith("Role", StringComparison.Ordinal) && !roles.Contains((string)registration.Value)) Fail("Unknown registration output role");
            foreach (JToken binding in recipe["integration"]["bindings"])
                if (!roles.Contains((string)binding["component"]["role"]) || (binding["target"] != null && !roles.Contains((string)binding["target"]["role"]))) Fail("Unknown binding output role");
            foreach (JToken material in recipe["materials"])
            {
                Unique((JArray)material["selector"]["parameters"], "name");
                Unique((JArray)material["textures"], "semantic");
                Unique((JArray)material["livery"]["parameters"], "name");
                foreach (JToken texture in material["textures"]) SafePath((string)texture["source"], false);
            }
            JToken[] selectors = recipe["materials"].Select(material => material["selector"]).ToArray();
            for (int i = 0; i < selectors.Length; i++)
                if (selectors.Take(i).Any(selector => JToken.DeepEquals(selector, selectors[i]))) Fail("Duplicate material selector");
            Unique((JArray)recipe["gameplay"]["values"], "name");
            if (recipe["gameplay"]["baselineGuid"].Type == JTokenType.Null && !recipe["gameplay"]["values"].Any()) Fail("Gameplay baseline or explicit values required");
            if (recipe["derivatives"]["wreckPieces"].Values<string>().Any(piece => !ids.Contains(piece))) Fail("Unknown wreck piece");
            Unique((JArray)recipe["hardpoints"], "id");
            Unique((JArray)recipe["hangar"], "id");
            foreach (JToken hardpoint in recipe["hardpoints"])
            {
                if (hardpoint["component"] != null && !roles.Contains((string)hardpoint["component"]["role"])) Fail("Unknown hardpoint component role");
                if ((double)hardpoint["yawRangeDegrees"][0] > (double)hardpoint["yawRangeDegrees"][1]) Fail("Reversed yaw range");
                if ((string)hardpoint["kind"] == "weapon" && (hardpoint["profileGuid"].Type == JTokenType.Null || hardpoint["profileKey"] == null)) Fail("Weapon profile GUID and key required");
                JToken anchor = hardpoint["anchor"];
                if (anchor["piece"] != null && !ids.Contains((string)anchor["piece"])) Fail("Unknown hardpoint piece");
                if ((string)hardpoint["kind"] == "weapon" && !(bool)hardpoint["targetable"] &&
                    !recipe["exceptions"].Any(exception => (string)exception["ruleId"] == "targetable-weapon" &&
                        exception["sourceIds"].Values<string>().Contains("hardpoint:" + hardpoint["id"]) &&
                        exception["approval"] != null && !string.IsNullOrEmpty((string)exception["approval"]))) Fail("Untargetable weapon requires approved exception");
            }
            Unique((JArray)recipe["decisions"], "key");
            if (recipe["decisions"].Any(decision => (string)decision["status"] == "unresolved")) Fail("Unresolved recipe decisions");
            if ((string)recipe["kind"] == "ship")
            {
                if (pieces.Count(piece => (string)piece["purpose"] == "hull") != 1) Fail("Ship requires one hull");
                if (recipe["dimensions"]["targetDiameter"] != null) Fail("Ship forbids targetDiameter");
                return;
            }
            if (recipe["dimensions"]["targetLength"] != null) Fail("Station forbids targetLength");
            JArray levels = (JArray)recipe["station"]["levels"];
            if (!levels.Select(level => (int)level["level"]).SequenceEqual(Enumerable.Range(1, levels.Count))) Fail("Station levels must be ordered and contiguous");
            Unique(levels, "piece");
            foreach (JToken level in levels)
                if (!pieces.Any(piece => (string)piece["id"] == (string)level["piece"] && (string)piece["purpose"] == "level")) Fail("Invalid station hull piece");
            Unique((JArray)recipe["station"]["mounts"], "id");
            foreach (JToken mount in recipe["station"]["mounts"])
            {
                if (!recipe["hardpoints"].Any(point => JToken.DeepEquals(point["id"], mount["hardpoint"])) ||
                    !levels.Any(level => JToken.DeepEquals(level["level"], mount["unlockLevel"]))) Fail("Invalid station mount");
                foreach (JToken anchor in mount["anchors"])
                    if (!levels.Any(level => JToken.DeepEquals(level["level"], anchor["level"])) || !ids.Contains((string)anchor["piece"])) Fail("Invalid station anchor");
            }
        }

        private static void PieceReferences(JToken value, string[] ids)
        {
            if (value is JObject obj)
                foreach (JProperty property in obj.Properties())
                {
                    if ((property.Name == "piece" || property.Name == "parentPiece" || property.Name == "artPiece") &&
                        property.Value.Type != JTokenType.Null && !ids.Contains((string)property.Value)) Fail("Unknown piece " + property.Value);
                    PieceReferences(property.Value, ids);
                }
            else if (value is JArray array)
                foreach (JToken item in array) PieceReferences(item, ids);
        }

        public static void SafePath(string path, bool asset)
        {
            string[] parts = path.Split('/');
            if (path.IndexOfAny(new[] { '\\', ':', '<', '>', '"', '|', '?', '*' }) >= 0 || path.Any(char.IsControl) ||
                parts.Any(part => part.Length == 0 || part == "." || part == ".." || part.EndsWith(".") || part.EndsWith(" ") ||
                    Regex.IsMatch(part, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(\..*)?$", RegexOptions.IgnoreCase))) Fail("Unsafe relative path " + path);
            if (asset && (parts.Length < 2 || parts[0] != "Assets" || parts[1].Equals("AddressableAssetsData", StringComparison.OrdinalIgnoreCase))) Fail("Output must be inside Assets outside AddressableAssetsData");
        }

        private static void Unique(JArray records, string key)
        {
            string[] values = records.Select(record => record[key].ToString()).ToArray();
            if (values.Distinct(StringComparer.OrdinalIgnoreCase).Count() != values.Length) Fail("Duplicate " + key);
        }

        private static void Fail(string reason) => throw new JsonException(reason);
    }
}
