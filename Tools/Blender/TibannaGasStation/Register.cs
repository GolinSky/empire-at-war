using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class RegisterTibannaGasStation
{
    private const int UNIT_TYPE = 3;
    private const string DATA = "Assets/Settings/Data/Models/MiningFacilities/TibannaGasStationData.asset";
    private const string VIEW = "Assets/Prefabs/Models/MiningFacilities/TibannaGasStationView.prefab";
    private const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/TibannaGasStationReinforcementView.prefab";
    private const string DONOR_DATA = "Assets/Settings/Data/Models/MiningFacilities/MiningFacilityData.asset";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Registration requires Edit Mode.");
        if (!File.Exists(DATA) && !AssetDatabase.CopyAsset(DONOR_DATA, DATA))
            throw new InvalidOperationException("Cannot clone mining facility data.");
        var data = Load(DATA);
        data.targetObject.name = "TibannaGasStationData";
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue = AssetDatabase.LoadMainAssetAtPath(
            "Assets/Settings/Data/Models/Wrecks/Shared/TibannaGasStationWreckData.asset");
        if (data.FindProperty("<Wreck>k__BackingField").objectReferenceValue == null)
            throw new InvalidOperationException("Tibanna wreck data is missing.");
        Save(data);

        var mapping = Load("Assets/Settings/AssetMappingData.asset");
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (string path in new[] { DATA, VIEW, PREVIEW })
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("Missing asset: " + path);
            string donor = path == DATA ? DONOR_DATA : "Assets/Prefabs/Models/MiningFacilities/MiningFacilityView.prefab";
            var group = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(donor)).parentGroup;
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = Path.GetFileNameWithoutExtension(path);
            EditorUtility.SetDirty(group);
            if (path == PREVIEW) continue;
            var row = Entry(mapping.FindProperty("assetMappings.keyValue"), entry.address);
            row.FindPropertyRelative("value.m_AssetGUID").stringValue = guid;
        }
        Save(mapping);
        EditorUtility.SetDirty(settings);

        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/Ui/TibannaGasStationIcon.png");
        if (icon == null) throw new InvalidOperationException("Tibanna icon is missing.");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long iconId);
        string iconKey = iconGuid + ":" + iconId;
        var catalog = Load("Assets/Settings/Data/Factions/Shared/MiningFacilityCatalog.asset");
        var unit = CloneRow(catalog.FindProperty("entries.keyValue"), UNIT_TYPE).FindPropertyRelative("value");
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue = "Tibanna Gas Station";
        unit.FindPropertyRelative("role").stringValue = "Mining facility";
        unit.FindPropertyRelative("description").stringValue = "A spawnable Tibanna gas mining station that provides recurring credits.";
        unit.FindPropertyRelative("iconKey").stringValue = iconKey;
        unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = icon;
        Save(catalog);
        foreach (string faction in new[] { "Empire", "Rebellion" })
        {
            var definition = Load("Assets/Settings/Data/Factions/" + faction + "/" + faction + "Faction.asset");
            var list = definition.FindProperty("miningFacilities");
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).intValue == 0)
                    list.GetArrayElementAtIndex(i).intValue = UNIT_TYPE;
            Save(definition);
        }
        var reinforcement = Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        CloneRow(reinforcement.FindProperty("spawnFacilityWrapper.keyValue"), UNIT_TYPE)
            .FindPropertyRelative("value.m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(PREVIEW);
        Save(reinforcement);
        var tooltip = Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");
        Entry(tooltip.FindProperty("icons"), iconKey).FindPropertyRelative("sprite").objectReferenceValue = icon;
        Save(tooltip);
        AssetDatabase.SaveAssets();
        return "Registered Tibanna Gas Station for Empire/Rebellion; cloned original miner balance and preserved other faction rosters.";
    }

    private static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadMainAssetAtPath(path));
    private static void Save(SerializedObject data)
    {
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data.targetObject);
    }
    private static SerializedProperty CloneRow(SerializedProperty list, int target)
    {
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue == target)
                return list.GetArrayElementAtIndex(i);
        int donor = Enumerable.Range(0, list.arraySize).Single(i => list.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue == 0);
        list.InsertArrayElementAtIndex(donor);
        var row = list.GetArrayElementAtIndex(donor);
        row.FindPropertyRelative("key").intValue = target;
        return row;
    }
    private static SerializedProperty Entry(SerializedProperty list, string key)
    {
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == key)
                return list.GetArrayElementAtIndex(i);
        list.arraySize++;
        var row = list.GetArrayElementAtIndex(list.arraySize - 1);
        row.FindPropertyRelative("key").stringValue = key;
        return row;
    }
}
