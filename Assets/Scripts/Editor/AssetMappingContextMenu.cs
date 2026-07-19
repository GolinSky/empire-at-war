using EmpireAtWar.Repository.Data;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace EmpireAtWar.Editor
{
    public static class AssetMappingContextMenu
    {
        private const string MappingDataPath = "Assets/Settings/AssetMappingData.asset";

        [MenuItem("Assets/Add to Asset Mapping", false, 20)]
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

                string key = GetMappingKey(selectedAsset);
                if (string.IsNullOrEmpty(key))
                    continue;

                string guid = AssetDatabase.AssetPathToGUID(path);
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

        [MenuItem("Tools/Asset Mapping/Rebuild From Addressables")]
        public static void RebuildFromAddressables()
        {
            AssetMappingData mappingData = GetOrCreateMappingData();
            SerializedObject serializedMapping = new(mappingData);
            SerializedProperty mappings = serializedMapping
                .FindProperty("assetMappings")
                .FindPropertyRelative("keyValue");
            mappings.ClearArray();

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[AssetMapping] AddressableAssetSettings was not found.");
                return;
            }

            string mappingGuid = AssetDatabase.AssetPathToGUID(MappingDataPath);
            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                    continue;

                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry == null || entry.guid == mappingGuid || string.IsNullOrEmpty(entry.address))
                        continue;

                    SetMapping(mappings, entry.address, entry.guid);
                }
            }

            AddToAddressables(mappingGuid, nameof(AssetMappingData));
            serializedMapping.ApplyModifiedProperties();
            EditorUtility.SetDirty(mappingData);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AssetMapping] Rebuilt {mappings.arraySize} mappings from Addressables.", mappingData);
        }

        [MenuItem("Assets/Add to Asset Mapping", true)]
        private static bool ValidateAddToAssetMapping()
        {
            return Selection.objects is { Length: > 0 };
        }

        private static string GetMappingKey(Object asset)
        {
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
            AssetMappingData mappingData = AssetDatabase.LoadAssetAtPath<AssetMappingData>(MappingDataPath);
            if (mappingData != null)
                return mappingData;

            string folder = System.IO.Path.GetDirectoryName(MappingDataPath)?.Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "Settings");

            mappingData = ScriptableObject.CreateInstance<AssetMappingData>();
            AssetDatabase.CreateAsset(mappingData, MappingDataPath);
            return mappingData;
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
