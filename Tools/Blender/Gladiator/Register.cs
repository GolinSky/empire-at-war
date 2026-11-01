using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class RegisterGladiator
{
    const int SHIP_ID = 216;
    const string VIEW = "Assets/Prefabs/Models/Ships/Empire/View/GladiatorShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Empire/GladiatorShipData.asset";
    const string MATCHUPS = "Assets/Settings/Data/Tooltip/Matchups/GladiatorMatchups.asset";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Gladiator registration requires Edit Mode.");
        var audio = Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        CloneRow(audio.FindProperty("ships.keyValue"), "key", 201, SHIP_ID); Save(audio);
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/Empire/GladiatorIcon.png");
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long localId)) throw new InvalidOperationException("Missing Gladiator sprite.");
        string iconKey = iconGuid + ":" + localId;
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var mapping = Load("Assets/Settings/AssetMappingData.asset");
        foreach (var path in new[] {VIEW, DATA})
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            Entry(mapping.FindProperty("assetMappings.keyValue"), Path.GetFileNameWithoutExtension(path)).FindPropertyRelative("m_AssetGUID").stringValue = guid;
            var donor = path == VIEW ? "Assets/Prefabs/Models/Ships/Empire/View/ISDIShipView.prefab" : "Assets/Settings/Data/Ship/Empire/ISDIShipData.asset";
            var group = path == VIEW ? settings.FindGroup("EmpireUnitViews") : settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(donor)).parentGroup;
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
        var preview = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/GladiatorReinforcementView.prefab");
        string previewPath = AssetDatabase.GetAssetPath(preview);
        var previewEntry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(previewPath), settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(VIEW)).parentGroup);
        previewEntry.address = "GladiatorReinforcementView"; EditorUtility.SetDirty(previewEntry.parentGroup); EditorUtility.SetDirty(settings);
        var reinforcement = Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"), SHIP_ID).FindPropertyRelative("m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(previewPath); Save(reinforcement);
        if (!File.Exists(MATCHUPS) && !AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/RecusantMatchups.asset", MATCHUPS)) throw new InvalidOperationException(MATCHUPS);
        var matchups = Load(MATCHUPS); matchups.targetObject.name = "GladiatorMatchups";
        matchups.FindProperty("strongAgainst").arraySize = 0; matchups.FindProperty("weakAgainst").arraySize = 0; Save(matchups);
        var faction = Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");
        var unit = Entry(faction.FindProperty("ships.keyValue"), SHIP_ID);
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue = "Gladiator-class Star Destroyer";
        unit.FindPropertyRelative("role").stringValue = "Capital / Assault carrier";
        unit.FindPropertyRelative("description").stringValue = "6,000 hull and 5,000 shields with capital shield regeneration. Four dual medium turbolasers, two concussion missile launchers and two point defense lasers. Deploys TIE Fighter, TIE Bomber, TIE Avenger, TIE Interceptor, TIE Brute and TIE Punisher squadrons. One squadron of each type can be active, with ten carried per type and sixty total.";
        unit.FindPropertyRelative("iconKey").stringValue = iconKey; unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = icon;
        unit.FindPropertyRelative("matchups").objectReferenceValue = matchups.targetObject; unit.FindPropertyRelative("isHero").boolValue = false;
        foreach (var field in new[] {new {key = "MaxCount", value = 10}, new {key = "AvailableLevel", value = 4}, new {key = "Price", value = 4000}, new {key = "BuildTime", value = 40}, new {key = "UnitCapacity", value = 8}})
            unit.FindPropertyRelative("<" + field.key + ">k__BackingField").intValue = field.value;
        Save(faction);
        string wreckPath = "Assets/Prefabs/Models/Wrecks/GladiatorWreckView.prefab";
        var wreckEntry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(wreckPath), settings.FindGroup("EmpireUnitViews"));
        wreckEntry.address = "GladiatorWreckView"; EditorUtility.SetDirty(wreckEntry.parentGroup); EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        return "Registered Empire Gladiator (216): own data/view/icon/placement/audio; cost4000, build40, level4, population8.";
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
    static void Save(SerializedObject data)
    {
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data.targetObject);
        // Serialized row edits must also invalidate the wrapper's lazy Editor lookup.
        foreach (var field in data.targetObject.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
            .Where(f => f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(Utilities.ScriptUtils.EditorSerialization.DictionaryWrapper<,>)))
            field.FieldType.GetField("dictionary", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(field.GetValue(data.targetObject), null);
    }
}
