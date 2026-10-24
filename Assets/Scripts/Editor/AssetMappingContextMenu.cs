using System.Collections.Generic;
using EmpireAtWar.Repository.Data;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace EmpireAtWar.Editor
{
    [EmpireAtWar.Editor.EditorToolInfo("Add selected assets to the asset mapping, or rebuild all mappings from Addressables.")]
    public static class AssetMappingContextMenu
    {
        private const string MAPPING_DATA_PATH = "Assets/Settings/AssetMappingData.asset";

        [MenuItem("Assets/Add to Asset Mapping", false, 20)]
        [MenuItem("Tools/Empire At War/Workflow/Asset Mapping/Add Selected Assets", false, 20)]
        private static void AddToAssetMapping()
        {
            AssetMappingData mappingData = GetOrCreateMappingData();

            SerializedObject serializedMapping = new(mappingData);
            SerializedProperty mappings = serializedMapping
                .FindProperty("assetMappings")
                .FindPropertyRelative("keyValue");
            bool updated = false;

            foreach (Object selectedAsset in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(selectedAsset);
                if (string.IsNullOrEmpty(path) || selectedAsset == mappingData)
                    continue;

                string guid = AssetDatabase.AssetPathToGUID(path);
                AddressableAssetEntry existingEntry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid);
                string key = GetMappingKey(selectedAsset, existingEntry == null ? null : existingEntry.address);
                if (string.IsNullOrEmpty(key))
                    continue;

                string ownerGuid = FindMappedGuid(mappings, key);
                if (ownerGuid != null && ownerGuid != guid)
                {
                    Debug.LogError($"[AssetMapping] Key {key} already maps to {AssetDatabase.GUIDToAssetPath(ownerGuid)}; " +
                        $"{path} was not mapped. Give it a distinct Addressables address first.", selectedAsset);
                    continue;
                }

                AddToAddressables(guid, selectedAsset.name);
                SetMapping(mappings, key, guid);
                updated = true;
                Debug.Log($"[AssetMapping] Mapped {key} -> {guid}.", selectedAsset);
            }

            if (!updated)
                return;

            serializedMapping.ApplyModifiedProperties();
            EditorUtility.SetDirty(mappingData);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Empire At War/Workflow/Asset Mapping/Rebuild From Addressables")]
        public static void RebuildFromAddressables()
        {
            AssetMappingData mappingData = GetOrCreateMappingData();
            SerializedObject serializedMapping = new(mappingData);
            SerializedProperty mappings = serializedMapping
                .FindProperty("assetMappings")
                .FindPropertyRelative("keyValue");

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[AssetMapping] AddressableAssetSettings was not found.");
                return;
            }

            string mappingGuid = AssetDatabase.AssetPathToGUID(MAPPING_DATA_PATH);
            List<(string Address, string Guid)> entries = new();
            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                    continue;

                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry == null || entry.guid == mappingGuid || string.IsNullOrEmpty(entry.address))
                        continue;

                    entries.Add((entry.address, entry.guid));
                }
            }

            List<(string Key, string Guid)> rebuilt = BuildMappings(ReadMappings(mappings), entries);
            mappings.ClearArray();
            foreach ((string key, string guid) in rebuilt)
                SetMapping(mappings, key, guid);

            AddToAddressables(mappingGuid, nameof(AssetMappingData));
            serializedMapping.ApplyModifiedProperties();
            EditorUtility.SetDirty(mappingData);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AssetMapping] Rebuilt {mappings.arraySize} mappings from Addressables.", mappingData);
        }

        [MenuItem("Assets/Add to Asset Mapping", true)]
        [MenuItem("Tools/Empire At War/Workflow/Asset Mapping/Add Selected Assets", true)]
        private static bool ValidateAddToAssetMapping()
        {
            return Selection.objects is { Length: > 0 };
        }

        /// <summary>
        /// Keys every Addressables entry by its address, then keeps existing aliases: mappings whose key differs
        /// from the address of a GUID that is still an entry (e.g. VenatorIIShipView → ResoluteShipView).
        /// </summary>
        internal static List<(string Key, string Guid)> BuildMappings(
            IReadOnlyList<(string Key, string Guid)> existing, IReadOnlyList<(string Address, string Guid)> entries)
        {
            Dictionary<string, string> addressByGuid = new();
            List<(string Key, string Guid)> mappings = new();
            foreach ((string address, string guid) in entries)
            {
                addressByGuid[guid] = address;
                mappings.Add((address, guid));
            }

            foreach ((string key, string guid) in existing)
            {
                if (!addressByGuid.TryGetValue(guid, out string address) || address == key)
                    continue;

                mappings.RemoveAll(mapping => mapping.Key == key);
                mappings.Add((key, guid));
            }

            return mappings;
        }

        /// <summary>An existing Addressables address is the canonical key; otherwise the key derives from the asset.</summary>
        internal static string GetMappingKey(Object asset, string entryAddress)
        {
            if (!string.IsNullOrEmpty(entryAddress))
                return entryAddress;

            if (asset is ScriptableObject)
                return asset.GetType().Name;

            if (asset is GameObject prefab)
            {
                MonoBehaviour[] components = prefab.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour component in components)
                {
                    if (component != null && component.GetType().Namespace?.StartsWith("EmpireAtWar") == true)
                        return component.GetType().Name;
                }

                return prefab.name;
            }

            return asset.name;
        }

        private static AssetMappingData GetOrCreateMappingData()
        {
            AssetMappingData mappingData = AssetDatabase.LoadAssetAtPath<AssetMappingData>(MAPPING_DATA_PATH);
            if (mappingData != null)
                return mappingData;

            string folder = System.IO.Path.GetDirectoryName(MAPPING_DATA_PATH)?.Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "Settings");

            mappingData = ScriptableObject.CreateInstance<AssetMappingData>();
            AssetDatabase.CreateAsset(mappingData, MAPPING_DATA_PATH);
            return mappingData;
        }

        private static List<(string Key, string Guid)> ReadMappings(SerializedProperty mappings)
        {
            List<(string Key, string Guid)> result = new();
            for (int i = 0; i < mappings.arraySize; i++)
            {
                SerializedProperty entry = mappings.GetArrayElementAtIndex(i);
                result.Add((entry.FindPropertyRelative("key").stringValue,
                    entry.FindPropertyRelative("value").FindPropertyRelative("m_AssetGUID").stringValue));
            }

            return result;
        }

        internal static string FindMappedGuid(SerializedProperty mappings, string key)
        {
            foreach ((string mappedKey, string guid) in ReadMappings(mappings))
            {
                if (mappedKey == key)
                    return guid;
            }

            return null;
        }

        private static void SetMapping(SerializedProperty mappings, string key, string guid)
        {
            for (int i = 0; i < mappings.arraySize; i++)
            {
                SerializedProperty entry = mappings.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("key").stringValue != key)
                    continue;

                entry.FindPropertyRelative("value").FindPropertyRelative("m_AssetGUID").stringValue = guid;
                return;
            }

            mappings.arraySize++;
            SerializedProperty newEntry = mappings.GetArrayElementAtIndex(mappings.arraySize - 1);
            newEntry.FindPropertyRelative("key").stringValue = key;
            newEntry.FindPropertyRelative("value").FindPropertyRelative("m_AssetGUID").stringValue = guid;
        }

        private static void AddToAddressables(string guid, string defaultAddress)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return;

            AddressableAssetEntry entry = settings.FindAssetEntry(guid);
            if (entry == null)
            {
                entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                entry.SetAddress(defaultAddress);
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            }
        }
    }
}
