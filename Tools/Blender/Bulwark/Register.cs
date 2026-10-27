using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class RegisterBulwark
{
    const int SHIP_ID = 112;
    const string VIEW = "Assets/Prefabs/Models/Ships/Separatist/BulwarkShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Separatist/BulwarkShipData.asset";
    const string MATCHUPS = "Assets/Settings/Data/Tooltip/Matchups/BulwarkMatchups.asset";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Bulwark registration requires Edit Mode.");
        var audio = Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        CloneRow(audio.FindProperty("ships.keyValue"), "key", 100, SHIP_ID); Save(audio);
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/Separatist/BulwarkIcon.png");
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long localId)) throw new InvalidOperationException("Missing Bulwark sprite.");
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
        var preview = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/BulwarkReinforcementView.prefab");
        string previewPath = AssetDatabase.GetAssetPath(preview);
        var previewEntry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(previewPath), settings.FindGroup("View"));
        previewEntry.address = "BulwarkReinforcementView"; EditorUtility.SetDirty(previewEntry.parentGroup); EditorUtility.SetDirty(settings);
        var reinforcement = Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"), SHIP_ID).FindPropertyRelative("m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(previewPath); Save(reinforcement);
        if (!File.Exists(MATCHUPS) && !AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/ProvidenceMatchups.asset", MATCHUPS)) throw new InvalidOperationException(MATCHUPS);
        var matchups = Load(MATCHUPS); matchups.targetObject.name = "BulwarkMatchups"; Save(matchups);
        var faction = Load("Assets/Settings/Data/Factions/Separatist/SeparatistFaction.asset");
        var unit = Entry(faction.FindProperty("ships.keyValue"), SHIP_ID);
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue = "Bulwark Mk1";
        unit.FindPropertyRelative("role").stringValue = "Capital / Battle line carrier";
        unit.FindPropertyRelative("description").stringValue = "8,500 hull and 2,500 shields. Three quad turbolaser batteries, eight turbolaser batteries and two proton torpedo launchers use medium caliber. Sixteen targetable hardpoints include engines, shields and hangar. Deploys two Vulture fighter squadrons and one Advanced Droid Bomber squadron, with matching replacement reserves.";
        unit.FindPropertyRelative("iconKey").stringValue = iconKey; unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = icon;
        unit.FindPropertyRelative("matchups").objectReferenceValue = matchups.targetObject; unit.FindPropertyRelative("isHero").boolValue = false;
        foreach (var field in new[] {new {key = "MaxCount", value = 10}, new {key = "AvailableLevel", value = 4}, new {key = "Price", value = 6500}, new {key = "BuildTime", value = 85}, new {key = "UnitCapacity", value = 3}})
            unit.FindPropertyRelative("<" + field.key + ">k__BackingField").intValue = field.value;
        Save(faction); AssetDatabase.SaveAssets();
        return "Registered CIS Bulwark (112): data/view/icon/placement/audio; 13 medium weapons; cost6500, population3.";
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
