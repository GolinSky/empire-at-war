using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class VerifyTibannaRegistration
{
    private const string DATA_ROOT = "Assets/Settings/Data/Models/MiningFacilities/";
    private const string VIEW = "Assets/Prefabs/Models/MiningFacilities/TibannaGasStationView.prefab";
    private const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/TibannaGasStationReinforcementView.prefab";

    public static string Main()
    {
        var donor = JObject.Parse(EditorJsonUtility.ToJson(AssetDatabase.LoadMainAssetAtPath(DATA_ROOT + "MiningFacilityData.asset")));
        var actual = JObject.Parse(EditorJsonUtility.ToJson(AssetDatabase.LoadMainAssetAtPath(DATA_ROOT + "TibannaGasStationData.asset")));
        foreach (string key in new[] { "<ComponentData>k__BackingField", "<Income>k__BackingField" })
            Require(JToken.DeepEquals(donor[key], actual[key]), "Changed gameplay stats: " + key);
        var catalog = Load("Assets/Settings/Data/Factions/Shared/MiningFacilityCatalog.asset");
        var rows = catalog.FindProperty("entries.keyValue");
        var oldUnit = Row(rows, 0).FindPropertyRelative("value");
        var newUnit = Row(rows, 3).FindPropertyRelative("value");
        foreach (string field in new[] { "MaxCount", "AvailableLevel", "Price", "BuildTime", "UnitCapacity" })
        {
            string path = "<" + field + ">k__BackingField";
            var oldValue = oldUnit.FindPropertyRelative(path);
            var newValue = newUnit.FindPropertyRelative(path);
            Require(oldValue.propertyType == SerializedPropertyType.Float
                    ? oldValue.doubleValue == newValue.doubleValue
                    : oldValue.longValue == newValue.longValue,
                "Changed catalog balance: " + field);
        }
        Require(newUnit.FindPropertyRelative("<Name>k__BackingField").stringValue == "Tibanna Gas Station", "Wrong display name.");
        foreach (string faction in new[] { "Empire", "Rebellion", "Republic", "Separatist" })
        {
            var definition = Load("Assets/Settings/Data/Factions/" + faction + "/" + faction + "Faction.asset");
            var types = definition.FindProperty("miningFacilities");
            int expected = faction == "Empire" || faction == "Rebellion" ? 3 : 0;
            Require(types.arraySize == 1 && types.GetArrayElementAtIndex(0).intValue == expected, "Wrong mining roster: " + faction);
        }
        var mapping = Load("Assets/Settings/AssetMappingData.asset").FindProperty("assetMappings.keyValue");
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (string path in new[] { DATA_ROOT + "TibannaGasStationData.asset", VIEW, PREVIEW })
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            string key = Path.GetFileNameWithoutExtension(path);
            var entry = settings.FindAssetEntry(guid);
            Require(entry != null && entry.address == key, "Missing Addressables entry: " + key);
            if (path == PREVIEW) continue;
            int index = Enumerable.Range(0, mapping.arraySize).Single(i => mapping.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == key);
            Require(mapping.GetArrayElementAtIndex(index).FindPropertyRelative("value.m_AssetGUID").stringValue == guid, "Wrong asset mapping: " + key);
        }
        var previewRows = Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset").FindProperty("spawnFacilityWrapper.keyValue");
        Require(Row(previewRows, 3).FindPropertyRelative("value.m_AssetGUID").stringValue == AssetDatabase.AssetPathToGUID(PREVIEW), "Wrong placement preview.");
        var icon = newUnit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue;
        Require(icon != null && AssetDatabase.GetAssetPath(icon).EndsWith("TibannaGasStationIcon.png"), "Wrong catalog icon.");
        string iconKey = newUnit.FindPropertyRelative("iconKey").stringValue;
        var icons = Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset").FindProperty("icons");
        Require(Enumerable.Range(0, icons.arraySize).Any(i => icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == iconKey &&
            icons.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue == icon), "Missing tooltip icon.");
        var data = Load(DATA_ROOT + "TibannaGasStationData.asset");
        var wreck = data.FindProperty("<Wreck>k__BackingField").objectReferenceValue;
        Require(wreck != null && wreck.name == "TibannaGasStationWreckData", "Wrong wreck data.");
        var wreckReference = new SerializedObject(wreck).FindProperty("prefab.m_AssetGUID").stringValue;
        string wreckPath = AssetDatabase.GUIDToAssetPath(wreckReference);
        Require(wreckPath.EndsWith("TibannaGasStationWreckView.prefab"), "Wrong wreck prefab.");
        Require(settings.FindAssetEntry(wreckReference) != null, "Missing wreck Addressables entry.");
        foreach (string path in new[] { VIEW, PREVIEW, wreckPath })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Require(prefab.transform.localScale == Vector3.one, "Nonidentity prefab scale: " + path);
            foreach (var component in prefab.GetComponentsInChildren<Component>(true))
            {
                Require(component != null, "Missing script: " + path);
                var iterator = new SerializedObject(component).GetIterator();
                while (iterator.Next(true))
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                        Require(iterator.objectReferenceValue != null || iterator.objectReferenceInstanceIDValue == 0,
                            "Broken reference: " + path + " / " + component.name + " / " + iterator.propertyPath);
            }
        }
        string report = new JObject
        {
            ["balanceMatchesOriginal"] = true,
            ["empireAndRebellionType"] = 3,
            ["republicAndSeparatistType"] = 0,
            ["assetMappingsVerified"] = true,
            ["placementIconWreckVerified"] = true,
            ["missingScriptsOrReferences"] = 0
        }.ToString();
        File.WriteAllText("output/TibannaGasStationImport/RegistrationVerification.json", report);
        return report;
    }
    private static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadMainAssetAtPath(path));
    private static SerializedProperty Row(SerializedProperty list, int key) => list.GetArrayElementAtIndex(
        Enumerable.Range(0, list.arraySize).Single(i => list.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue == key));
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
