using System;
using System.IO;
using System.Linq;
using EmpireAtWar.Models.Factions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloRegistrationWriter
    {
        private static readonly string[] _registryFields =
        {
            "factionGuid", "shipsDataGuid", "assetMappingGuid", "shipUiGuid", "tooltipGuid", "reinforcementGuid"
        };

        public static JObject Preflight(JObject recipe) => Process(recipe, true, false, null);
        public static JObject Apply(JObject recipe, string jobDirectory)
        {
            AloInvocationPolicy.RequireAssetWrite(jobDirectory, Array.Empty<string>());
            return Process(recipe, true, true, jobDirectory);
        }
        public static JObject Verify(JObject recipe) => Process(recipe, false, false, null);

        private static JObject Process(JObject recipe, bool write, bool commit, string jobDirectory)
        {
            AloRecipeSchemaValidator.Validate(recipe);
            if ((string)recipe["kind"] != "ship" || (string)recipe["operation"] != "update")
                throw new InvalidOperationException("UNSUPPORTED_CAPABILITY: registration adapter supports existing ships.");
            JObject definition = (JObject)recipe["registrations"];
            if (definition == null) throw new InvalidOperationException("Missing explicit registration contract.");
            JToken identity = recipe["identity"]["unit"];
            int id = (int)identity["value"];
            if ((string)identity["enum"] != nameof(ShipType) || Enum.GetName(typeof(ShipType), id) != (string)identity["name"])
                throw new InvalidOperationException("INPUT_CHANGED: compiled ship identity differs from recipe.");
            var registries = _registryFields.ToDictionary(field => field, field => Load((string)definition[field]));
            var faction = (FactionDefinition)registries["factionGuid"].targetObject;
            if (faction.FactionType.ToString() != (string)recipe["identity"]["faction"])
                throw new InvalidOperationException("OUTPUT_CONFLICT: faction registry differs from recipe routing.");
            string PathFor(string roleField) => (string)recipe["integration"]["outputs"].Single(output =>
                (string)output["role"] == (string)definition[roleField])["path"];
            string view = PathFor("viewRole"), data = PathFor("dataRole"), placement = PathFor("placementRole");
            Sprite icon = AssetDatabase.LoadAllAssetsAtPath(PathFor("iconRole")).OfType<Sprite>().Single(sprite =>
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string _, out long fileId) && fileId == (long)definition["iconFileId"]);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long iconId);
            string iconKey = iconGuid + ":" + iconId;
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) throw new InvalidOperationException("Addressables settings are missing.");
            if (AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(settings)) != (string)definition["addressableSettingsGuid"])
                throw new InvalidOperationException("INPUT_CHANGED: active Addressables settings differ from recipe.");
            var viewGroup = Group((string)definition["viewGroupGuid"], settings);
            var dataGroup = Group((string)definition["dataGroupGuid"], settings);
            var addressables = new[] { (Path: view, Group: viewGroup), (Path: data, Group: dataGroup), (Path: placement, Group: viewGroup) };
            foreach (var item in addressables)
            {
                if (AssetDatabase.LoadMainAssetAtPath(item.Path) == null) throw new InvalidOperationException("Missing output " + item.Path);
                string address = Path.GetFileNameWithoutExtension(item.Path);
                string guid = AssetDatabase.AssetPathToGUID(item.Path);
                if (settings.groups.SelectMany(group => group.entries).Any(entry => entry.address == address && entry.guid != guid))
                    throw new InvalidOperationException("OUTPUT_CONFLICT: Addressables address already belongs to another asset: " + address);
            }
            // Reload all registries before selecting owned rows; never restore a stale whole-file snapshot.
            SerializedProperty roster = Row(registries["factionGuid"].FindProperty("ships.keyValue"), id, false);
            void Reference(SerializedProperty property, Object expected)
            {
                if (property.objectReferenceValue == expected) return;
                if (!write) throw new InvalidOperationException("Registration reference differs: " + property.propertyPath);
                property.objectReferenceValue = expected;
            }
            void Text(SerializedProperty property, string expected)
            {
                if (property.stringValue == expected) return;
                if (!write) throw new InvalidOperationException("Registration value differs: " + property.propertyPath);
                property.stringValue = expected;
            }
            void AssetReference(SerializedProperty property, string expected)
            {
                Text(property.FindPropertyRelative("m_AssetGUID"), expected);
                foreach (string field in new[] { "m_SubObjectName", "m_SubObjectType", "m_SubObjectGUID" })
                    Text(property.FindPropertyRelative(field), "");
            }
            Reference(roster.FindPropertyRelative("value.<Icon>k__BackingField"), icon);
            Text(roster.FindPropertyRelative("value.iconKey"), iconKey);
            AssetReference(Row(registries["shipsDataGuid"].FindProperty("shipsData.keyValue"), id, write).FindPropertyRelative("value"), AssetDatabase.AssetPathToGUID(data));
            Reference(Row(registries["shipUiGuid"].FindProperty("shipIconWrapper.keyValue"), id, write).FindPropertyRelative("value"), icon);
            Reference(Row(registries["tooltipGuid"].FindProperty("icons"), iconKey, write).FindPropertyRelative("sprite"), icon);
            AssetReference(Row(registries["reinforcementGuid"].FindProperty("spawnShipWrapper.keyValue"), id, write).FindPropertyRelative("value"), AssetDatabase.AssetPathToGUID(placement));
            foreach (var item in addressables.Where(item => item.Path != placement))
                AssetReference(Row(registries["assetMappingGuid"].FindProperty("assetMappings.keyValue"), Path.GetFileNameWithoutExtension(item.Path), write)
                    .FindPropertyRelative("value"), AssetDatabase.AssetPathToGUID(item.Path));
            string[] publicationPaths = Array.Empty<string>();
            if (commit)
            {
                publicationPaths = registries.Values.Select(registry => AssetDatabase.GetAssetPath(registry.targetObject))
                    .Concat(addressables.Select(item => AssetDatabase.GetAssetPath(item.Group)))
                    .Concat(addressables.Select(item => settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(item.Path)))
                        .Where(entry => entry != null).Select(entry => AssetDatabase.GetAssetPath(entry.parentGroup)))
                    .Append(AssetDatabase.GetAssetPath(settings)).Distinct().ToArray();
                JObject plan = JObject.Parse(File.ReadAllText(Path.Combine(jobDirectory, "BuildPlan.json")));
                AloPublicationJournal.Prepare(jobDirectory, publicationPaths, new JObject { ["registryKeys"] = plan["registryKeys"].DeepClone() });
                foreach (SerializedObject registry in registries.Values)
                    if (registry.ApplyModifiedPropertiesWithoutUndo()) EditorUtility.SetDirty(registry.targetObject);
            }
            foreach (var item in addressables)
            {
                string guid = AssetDatabase.AssetPathToGUID(item.Path), address = Path.GetFileNameWithoutExtension(item.Path);
                var entry = settings.FindAssetEntry(guid);
                if (!write && (entry == null || entry.parentGroup != item.Group || entry.address != address))
                    throw new InvalidOperationException("Addressable registration differs: " + item.Path);
                if (commit && (entry == null || entry.parentGroup != item.Group || entry.address != address))
                {
                    if (entry != null && entry.parentGroup != item.Group) EditorUtility.SetDirty(entry.parentGroup);
                    settings.CreateOrMoveEntry(guid, item.Group).SetAddress(address);
                    EditorUtility.SetDirty(item.Group);
                    EditorUtility.SetDirty(settings);
                }
            }
            if (commit)
            {
                AssetDatabase.SaveAssets();
                AloPublicationJournal.Checkpoint(jobDirectory, "registrations-saved", publicationPaths);
                return Process(recipe, false, false, null);
            }
            return new JObject { ["shipRegistrations"] = write ? "preflight-passed" : "passed", ["unitId"] = id, ["iconKey"] = iconKey };
        }

        private static SerializedObject Load(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null || EditorUtility.IsDirty(asset)) throw new InvalidOperationException("INPUT_CHANGED: missing or unsaved registry " + guid);
            return new SerializedObject(AssetDatabase.LoadMainAssetAtPath(path));
        }

        private static AddressableAssetGroup Group(string guid, AddressableAssetSettings settings)
        {
            var group = AssetDatabase.LoadAssetAtPath<AddressableAssetGroup>(AssetDatabase.GUIDToAssetPath(guid));
            if (group == null || !settings.groups.Contains(group)) throw new InvalidOperationException("Existing Addressables group is missing: " + guid);
            return group;
        }

        private static SerializedProperty Row(SerializedProperty array, object key, bool create)
        {
            bool Matches(SerializedProperty value) => key is int number ? value.intValue == number : value.stringValue == (string)key;
            var matches = Enumerable.Range(0, array.arraySize).Where(index => Matches(array.GetArrayElementAtIndex(index).FindPropertyRelative("key"))).ToArray();
            if (matches.Length > 1) throw new InvalidOperationException("Duplicate registry key: " + key);
            if (matches.Length == 1) return array.GetArrayElementAtIndex(matches[0]);
            if (!create) throw new InvalidOperationException("Missing existing registry key: " + key);
            int appended = array.arraySize;
            array.InsertArrayElementAtIndex(appended);
            SerializedProperty row = array.GetArrayElementAtIndex(appended), property = row.FindPropertyRelative("key");
            if (key is int id) property.intValue = id; else property.stringValue = (string)key;
            return row;
        }
    }
}
