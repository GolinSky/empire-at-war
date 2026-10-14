using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.ShipAbilities.Abilities;

public static class RegisterTartan
{
    const string VIEW = "Assets/Prefabs/Models/Ships/Empire/TartanShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Empire/TartanShipData.asset";
    const string MATCHUPS = "Assets/Settings/Data/Tooltip/Matchups/TartanMatchups.asset";
    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Tartan asset registration requires Edit Mode.");
        var weapons = Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        var profile = CloneRow(weapons.FindProperty("weapons"), "weaponType", 28, 90);
        profile.FindPropertyRelative("displayName").stringValue = "Large Repeating Laser Cannon";
        profile.FindPropertyRelative("pattern").intValue = 1;
        profile.FindPropertyRelative("burstShots").intValue = 6;
        profile.FindPropertyRelative("faction").intValue = 3;
        profile.FindPropertyRelative("reason").stringValue = "Tartan anti-squadron battery; project balance, six-shot source burst.";
        Save(weapons);
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/TartanIcon.png");
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long localId)) throw new InvalidOperationException("Missing Tartan sprite");
        string iconKey = iconGuid + ":" + localId;
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var mapping = Load("Assets/Settings/AssetMappingData.asset");
        foreach (var path in new[] {VIEW, DATA})
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            Entry(mapping.FindProperty("assetMappings.keyValue"), Path.GetFileNameWithoutExtension(path)).FindPropertyRelative("m_AssetGUID").stringValue = guid;
            var donor = path == VIEW ? "Assets/Prefabs/Models/Ships/Empire/VictoryShipView.prefab" : "Assets/Settings/Data/Ship/Empire/VictoryShipData.asset";
            var entry = settings.CreateOrMoveEntry(guid, settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(donor)).parentGroup);
            entry.address = Path.GetFileNameWithoutExtension(path); EditorUtility.SetDirty(entry.parentGroup);
        }
        Save(mapping); EditorUtility.SetDirty(settings);
        var ships = Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
        Entry(ships.FindProperty("shipsData.keyValue"), 215).FindPropertyRelative("m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(DATA); Save(ships);
        var ui = Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset"); Entry(ui.FindProperty("shipIconWrapper.keyValue"), 215).objectReferenceValue = icon; Save(ui);
        var tooltip = Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset"); var icons = tooltip.FindProperty("icons");
        int index = Enumerable.Range(0, icons.arraySize).Where(i => icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == iconKey).DefaultIfEmpty(-1).First();
        if (index < 0) { index = icons.arraySize; icons.arraySize++; }
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue = iconKey;
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue = icon; Save(tooltip);
        var preview = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/TartanReinforcementView.prefab");
        var spawn = preview.GetComponents<MonoBehaviour>().Single(m => m.GetType().Name == "UnitSpawnView");
        var reinforcement = Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset"); Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"), 215).objectReferenceValue = spawn; Save(reinforcement);
        if (!File.Exists(MATCHUPS) && !AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/CorellianCorvetteMatchups.asset", MATCHUPS)) throw new InvalidOperationException(MATCHUPS);
        var matchups = Load(MATCHUPS); matchups.targetObject.name = "TartanMatchups"; Save(matchups);
        var faction = Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");
        var unit = Entry(faction.FindProperty("ships.keyValue"), 215);
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue = "Tartan-class Patrol Cruiser";
        unit.FindPropertyRelative("role").stringValue = "Screener / Corvette";
        unit.FindPropertyRelative("description").stringValue = "Anti-squadron escort corvette with four large repeating lasers, one heavy laser and four point-defense mounts. 650 hull, 850 shields, speed 70. No engine, shield-generator or hangar hardpoint; no squadrons. Power to Weapons doubles fire rate for 10 s, reduces speed by 25% and increases damage taken by 50%; 60 s recovery.";
        unit.FindPropertyRelative("iconKey").stringValue = iconKey; unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = icon;
        unit.FindPropertyRelative("matchups").objectReferenceValue = matchups.targetObject; unit.FindPropertyRelative("isHero").boolValue = false;
        foreach (var field in new[] {new {key = "MaxCount", value = 20}, new {key = "AvailableLevel", value = 1}, new {key = "Price", value = 1450}, new {key = "BuildTime", value = 28}, new {key = "UnitCapacity", value = 1}})
            unit.FindPropertyRelative("<" + field.key + ">k__BackingField").intValue = field.value;
        Save(faction); AssetDatabase.SaveAssets();
        return "Registered Empire ship 215, own icon/preview/data/Addressables/roster, repeating laser profile and existing Power to Weapons.";
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
