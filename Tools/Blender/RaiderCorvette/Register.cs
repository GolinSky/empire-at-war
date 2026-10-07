using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.ShipAbilities.Abilities;

public static class RegisterRaiderCorvette
{
    const string VIEW = "Assets/Prefabs/Models/Ships/RaiderCorvetteShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/RaiderCorvetteShipData.asset";
    const string MATCHUPS = "Assets/Settings/Data/Tooltip/Matchups/RaiderCorvetteMatchups.asset";
    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Raider asset registration requires Edit Mode.");
        var weapons = Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        Profile(weapons, 11, 60, "Heavy 2-Burst Ion Blaster", 8, 1.5f, 3.25f, 125, 4, 2);
        Profile(weapons, 13, 61, "Heavy 5-Burst Concussion Missile", 20, .75f, 15, 250, 5, 5);
        Profile(weapons, 14, 62, "Dual Repeating Point Defense", 8, .2f, 5.58f, 175, 1, 6);
        Save(weapons);
        var audio = Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        CloneRow(audio.FindProperty("weapons"), "weaponType", 11, 60);
        CloneRow(audio.FindProperty("weapons"), "weaponType", 13, 61);
        CloneRow(audio.FindProperty("weapons"), "weaponType", 14, 62);
        CloneRow(audio.FindProperty("abilities"), "abilityId", 6, 30);
        Save(audio);
        var abilities = Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        var ability = CloneRow(abilities.FindProperty("definitions.keyValue"), "key", 6, 30).FindPropertyRelative("value");
        ability.FindPropertyRelative("settings").managedReferenceValue = new BoostEnginePowerSettings();
        abilities.ApplyModifiedPropertiesWithoutUndo(); abilities.Update();
        ability = Entry(abilities.FindProperty("definitions.keyValue"), 30);
        ability.FindPropertyRelative("displayName").stringValue = "Pursuit";
        ability.FindPropertyRelative("description").stringValue = "For 15 s, speed increases to 250%, damage doubles and fire rate doubles. Shield regeneration stops. Recovers in 60 s.";
        ability.FindPropertyRelative("duration").floatValue = 15;
        ability.FindPropertyRelative("recoveryDelay").floatValue = 60;
        ability.FindPropertyRelative("requiresEnemyTarget").boolValue = false;
        var modifier = ability.FindPropertyRelative("settings.statModifier");
        modifier.FindPropertyRelative("damageMultiplier").floatValue = 2;
        modifier.FindPropertyRelative("fireDelayMultiplier").floatValue = .5f;
        modifier.FindPropertyRelative("speedMultiplier").floatValue = 2.5f;
        modifier.FindPropertyRelative("shieldRegenMultiplier").floatValue = 0;
        modifier.FindPropertyRelative("damageTakenMultiplier").floatValue = 1;
        Save(abilities);
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/RaiderCorvetteIcon.png");
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long localId)) throw new InvalidOperationException("Missing Raider sprite");
        string iconKey = iconGuid + ":" + localId;
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var mapping = Load("Assets/Settings/AssetMappingData.asset");
        foreach (var path in new[] {VIEW, DATA})
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            Entry(mapping.FindProperty("assetMappings.keyValue"), Path.GetFileNameWithoutExtension(path)).FindPropertyRelative("m_AssetGUID").stringValue = guid;
            var donor = path == VIEW ? "Assets/Prefabs/Models/Ships/VictoryShipView.prefab" : "Assets/Settings/Data/Ship/VictoryShipData.asset";
            var entry = settings.CreateOrMoveEntry(guid, settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(donor)).parentGroup);
            entry.address = Path.GetFileNameWithoutExtension(path); EditorUtility.SetDirty(entry.parentGroup);
        }
        Save(mapping); EditorUtility.SetDirty(settings);
        var ships = Load("Assets/Settings/Data/Ship/ShipsData.asset");
        Entry(ships.FindProperty("shipsData.keyValue"), 208).FindPropertyRelative("m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(DATA); Save(ships);
        var ui = Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset"); Entry(ui.FindProperty("shipIconWrapper.keyValue"), 208).objectReferenceValue = icon; Save(ui);
        var tooltip = Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset"); var icons = tooltip.FindProperty("icons");
        int index = Enumerable.Range(0, icons.arraySize).Where(i => icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == iconKey).DefaultIfEmpty(-1).First();
        if (index < 0) { index = icons.arraySize; icons.arraySize++; }
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue = iconKey;
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue = icon; Save(tooltip);
        var preview = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/RaiderCorvetteReinforcementView.prefab");
        var spawn = preview.GetComponents<MonoBehaviour>().Single(m => m.GetType().Name == "UnitSpawnView");
        var reinforcement = Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset"); Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"), 208).objectReferenceValue = spawn; Save(reinforcement);
        if (!File.Exists(MATCHUPS) && !AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/CorellianCorvetteMatchups.asset", MATCHUPS)) throw new InvalidOperationException(MATCHUPS);
        var matchups = Load(MATCHUPS); matchups.targetObject.name = "RaiderCorvetteMatchups"; Save(matchups);
        var faction = Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");
        var unit = Entry(faction.FindProperty("ships.keyValue"), 208);
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue = "Raider Corvette";
        unit.FindPropertyRelative("role").stringValue = "Screener / Corvette";
        unit.FindPropertyRelative("description").stringValue = "600 hull; 800 shields; speed 35. Four 2-burst laser cannons, two dual repeating point-defense mounts, two heavy 2-burst ion blasters and two heavy 5-burst concussion-missile launchers. No targetable hardpoints, hangar or fighter complement. Pursuit boosts speed, damage and fire rate for 15 s while stopping shield regeneration; recovers in 60 s.";
        unit.FindPropertyRelative("iconKey").stringValue = iconKey; unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = icon;
        unit.FindPropertyRelative("matchups").objectReferenceValue = matchups.targetObject; unit.FindPropertyRelative("isHero").boolValue = false;
        foreach (var field in new[] {new {key = "MaxCount", value = 10}, new {key = "AvailableLevel", value = 3}, new {key = "Price", value = 2800}, new {key = "BuildTime", value = 56}, new {key = "UnitCapacity", value = 2}})
            unit.FindPropertyRelative("<" + field.key + ">k__BackingField").intValue = field.value;
        Save(faction); AssetDatabase.SaveAssets();
        return "Registered Empire ship 208, own icon/preview/data/Addressables/roster, three dedicated weapon/audio profiles and self-activated Pursuit.";
    }
    static void Profile(SerializedObject data, int source, int type, string name, float damage, float interval, float reload, float range, int damageType, int shots)
    {
        var row = CloneRow(data.FindProperty("weapons"), "weaponType", source, type);
        row.FindPropertyRelative("displayName").stringValue = name; row.FindPropertyRelative("damage").floatValue = damage;
        row.FindPropertyRelative("damageType").intValue = damageType; row.FindPropertyRelative("shotsPerSalvo").intValue = shots;
        row.FindPropertyRelative("shotInterval").floatValue = interval; row.FindPropertyRelative("reload").floatValue = reload;
        row.FindPropertyRelative("range").floatValue = range; row.FindPropertyRelative("projectileSpeed").floatValue = type == 61 ? 80 : 133;
        row.FindPropertyRelative("strikecraftOnly").boolValue = type == 62; row.FindPropertyRelative("interceptable").boolValue = type == 61;
        row.FindPropertyRelative("color").colorValue = type == 60 ? Color.white : new Color(.2f, 1, .25f);
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
