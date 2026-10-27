using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class RegisterWavecrest
{
    const int SHIP_ID = 111;
    const string VIEW = "Assets/Prefabs/Models/Ships/Separatist/WavecrestShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Separatist/WavecrestShipData.asset";
    const string MATCHUPS = "Assets/Settings/Data/Tooltip/Matchups/WavecrestMatchups.asset";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Wavecrest registration requires Edit Mode.");
        var audio = Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        CloneRow(audio.FindProperty("ships.keyValue"), "key", 100, SHIP_ID); Save(audio);
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/Separatist/WavecrestIcon.png");
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long localId)) throw new InvalidOperationException("Missing Wavecrest sprite.");
        string iconKey = iconGuid + ":" + localId;
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var mapping = Load("Assets/Settings/AssetMappingData.asset");
        foreach (var path in new[] {VIEW, DATA})
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            Entry(mapping.FindProperty("assetMappings.keyValue"), Path.GetFileNameWithoutExtension(path)).FindPropertyRelative("m_AssetGUID").stringValue = guid;
            var donor = path == VIEW ? "Assets/Prefabs/Models/Ships/Separatist/RecusantShipView.prefab" : "Assets/Settings/Data/Ship/Separatist/RecusantShipData.asset";
            var group = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(donor)).parentGroup;
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = Path.GetFileNameWithoutExtension(path); EditorUtility.SetDirty(group);
        }
        Save(mapping); EditorUtility.SetDirty(settings);
        var ships = Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
        Entry(ships.FindProperty("shipsData.keyValue"), SHIP_ID).FindPropertyRelative("m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(DATA); Save(ships);
        var ui = Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");
        Entry(ui.FindProperty("shipIconWrapper.keyValue"), SHIP_ID).objectReferenceValue = icon; Save(ui);
        var tooltip = Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset"); var icons = tooltip.FindProperty("icons");
        int index = Enumerable.Range(0, icons.arraySize).Where(i => icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == iconKey).DefaultIfEmpty(-1).First();
        if (index < 0) { index = icons.arraySize; icons.arraySize++; }
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue = iconKey;
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue = icon; Save(tooltip);
        var preview = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/WavecrestReinforcementView.prefab");
        string previewPath = AssetDatabase.GetAssetPath(preview);
        var previewEntry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(previewPath), settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(VIEW)).parentGroup);
        previewEntry.address = "WavecrestReinforcementView"; EditorUtility.SetDirty(previewEntry.parentGroup); EditorUtility.SetDirty(settings);
        var reinforcement = Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"), SHIP_ID).FindPropertyRelative("m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(previewPath); Save(reinforcement);
        if (!File.Exists(MATCHUPS) && !AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/RecusantMatchups.asset", MATCHUPS)) throw new InvalidOperationException(MATCHUPS);
        var matchups = Load(MATCHUPS); matchups.targetObject.name = "WavecrestMatchups";
        matchups.FindProperty("strongAgainst").arraySize = 0; matchups.FindProperty("weakAgainst").arraySize = 0; Save(matchups);
        var faction = Load("Assets/Settings/Data/Factions/Separatist/SeparatistFaction.asset");
        var unit = Entry(faction.FindProperty("ships.keyValue"), SHIP_ID);
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue = "Wavecrest-class Frigate";
        unit.FindPropertyRelative("role").stringValue = "Frigate / Escort";
        unit.FindPropertyRelative("description").stringValue = "3,100 hull and 1,500 shields. Eight turbolaser batteries, four laser cannon batteries, two ion cannon batteries and one proton torpedo launcher, all medium caliber. All 17 mounts are targetable, including engines and hangar. Carries two active Vulture Droid squadrons plus one reserve.";
        unit.FindPropertyRelative("iconKey").stringValue = iconKey; unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = icon;
        unit.FindPropertyRelative("matchups").objectReferenceValue = matchups.targetObject; unit.FindPropertyRelative("isHero").boolValue = false;
        foreach (var field in new[] {new {key = "MaxCount", value = 10}, new {key = "AvailableLevel", value = 4}, new {key = "Price", value = 3500}, new {key = "BuildTime", value = 45}, new {key = "UnitCapacity", value = 3}})
            unit.FindPropertyRelative("<" + field.key + ">k__BackingField").intValue = field.value;
        Save(faction); AssetDatabase.SaveAssets();
        return "Registered CIS Wavecrest (111): own data/view/icon/placement/audio; medium weapons; cost3500, build45, station4, population3.";
    }

    static SerializedProperty CloneRow(SerializedProperty array, string key, int source, int target)
    {
        for (int i = 0; i < array.arraySize; i++) if (array.GetArrayElementAtIndex(i).FindPropertyRelative(key).intValue == target) return array.GetArrayElementAtIndex(i);
        int donor = Enumerable.Range(0, array.arraySize).Single(i => array.GetArrayElementAtIndex(i).FindPropertyRelative(key).intValue == source);
        array.InsertArrayElementAtIndex(donor); var row = array.GetArrayElementAtIndex(donor); row.FindPropertyRelative(key).intValue = target; return row;
    }
    static SerializedProperty Entry(SerializedProperty array, object key)
    {
        for (int i = 0; i < array.arraySize; i++) { var row = array.GetArrayElementAtIndex(i); var k = row.FindPropertyRelative("key"); if (key is int n ? k.intValue == n : k.stringValue == (string)key) return row.FindPropertyRelative("value"); }
        int index = array.arraySize; array.arraySize++; var entry = array.GetArrayElementAtIndex(index);
        if (key is int number) entry.FindPropertyRelative("key").intValue = number; else entry.FindPropertyRelative("key").stringValue = (string)key;
        return entry.FindPropertyRelative("value");
    }
    static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    static void Save(SerializedObject data) { data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data.targetObject); }
}
