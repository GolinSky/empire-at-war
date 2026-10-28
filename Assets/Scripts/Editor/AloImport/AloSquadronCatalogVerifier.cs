using System;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Repository.Data;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloSquadronCatalogVerifier
    {
        public static string Resolve(JToken hangar)
        {
            int key = (int)hangar["squadronKey"];
            if (!Enum.IsDefined(typeof(SquadronType), key)) throw new InvalidOperationException("Unknown typed squadron key " + key);
            string catalogGuid = (string)hangar["squadronGuid"];
            var catalog = AssetDatabase.LoadAssetAtPath<AssetMappingData>(AssetDatabase.GUIDToAssetPath(catalogGuid));
            if (catalog == null) throw new InvalidOperationException("Hangar squadronGuid must identify the shared AssetMappingData catalog.");
            // SquadronFactory uses the typed enum name plus SquadronData as its runtime asset key.
            string address = ((SquadronType)key) + nameof(SquadronData);
            SerializedProperty entries = new SerializedObject(catalog).FindProperty("assetMappings.keyValue");
            if (entries == null) throw new InvalidOperationException("AssetMappingData has no serialized assetMappings catalog.");
            string dataGuid = null;
            int matches = 0;
            for (int index = 0; index < entries.arraySize; index++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("key").stringValue != address) continue;
                matches++;
                dataGuid = entry.FindPropertyRelative("value.m_AssetGUID").stringValue;
            }
            if (matches != 1 || string.IsNullOrEmpty(dataGuid)) throw new InvalidOperationException("Hangar runtime key must resolve exactly once: " + address);
            if (AssetDatabase.LoadAssetAtPath<SquadronData>(AssetDatabase.GUIDToAssetPath(dataGuid)) == null)
                throw new InvalidOperationException("Hangar runtime key does not resolve saved SquadronData: " + address);
            return dataGuid;
        }
    }
}
