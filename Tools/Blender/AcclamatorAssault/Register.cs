using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.ShipAbilities.Abilities;

public static class RegisterAcclamatorAssault
{
    const string VIEW = "Assets/Prefabs/Models/Ships/AcclamatorAssaultShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/AcclamatorAssaultShipData.asset";
    const string MATCHUPS = "Assets/Settings/Data/Tooltip/Matchups/AcclamatorAssaultMatchups.asset";
    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Acclamator asset registration requires Edit Mode.");
        var weapons = Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        Profile(weapons, 13, 63, "4-Burst Assault-Missile Launcher", 30, 1, 15, 250, 5);
        Save(weapons);
        var audio = Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        CloneRow(audio.FindProperty("weapons"), "weaponType", 13, 63);
        CloneRow(audio.FindProperty("abilities"), "abilityId", 23, 31);
        Save(audio);
        var abilities = Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        var ability = CloneRow(abilities.FindProperty("definitions.keyValue"), "key", 23, 31).FindPropertyRelative("value");
        ability.FindPropertyRelative("settings").managedReferenceValue = new BoostWeaponPowerSettings();
        abilities.ApplyModifiedPropertiesWithoutUndo(); abilities.Update();
        ability = Entry(abilities.FindProperty("definitions.keyValue"), 31);
        ability.FindPropertyRelative("displayName").stringValue = "Boost Weapon Power";
        ability.FindPropertyRelative("description").stringValue = "Double fire rate for 20 s. Shield regeneration stops, speed falls to 25%, and incoming damage increases by 50%. Recovers in 60 s.";
        ability.FindPropertyRelative("duration").floatValue = 20;
        ability.FindPropertyRelative("recoveryDelay").floatValue = 60;
        var modifier = ability.FindPropertyRelative("settings.statModifier");
        modifier.FindPropertyRelative("damageMultiplier").floatValue = 1;
        modifier.FindPropertyRelative("fireDelayMultiplier").floatValue = .5f;
        modifier.FindPropertyRelative("speedMultiplier").floatValue = .25f;
        modifier.FindPropertyRelative("shieldRegenMultiplier").floatValue = 0;
        modifier.FindPropertyRelative("damageTakenMultiplier").floatValue = 1.5f;
        Save(abilities);
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/AcclamatorAssaultIcon.png");
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long localId)) throw new InvalidOperationException("Missing Acclamator sprite");
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
        Entry(ships.FindProperty("shipsData.keyValue"), 209).FindPropertyRelative("m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(DATA); Save(ships);
        var ui = Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset"); Entry(ui.FindProperty("shipIconWrapper.keyValue"), 209).objectReferenceValue = icon; Save(ui);
        var tooltip = Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset"); var icons = tooltip.FindProperty("icons");
        int index = Enumerable.Range(0, icons.arraySize).Where(i => icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == iconKey).DefaultIfEmpty(-1).First();
        if (index < 0) { index = icons.arraySize; icons.arraySize++; }
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue = iconKey;
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue = icon; Save(tooltip);
        var preview = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/AcclamatorAssaultReinforcementView.prefab");
        var spawn = preview.GetComponents<MonoBehaviour>().Single(m => m.GetType().Name == "UnitSpawnView");
        var reinforcement = Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset"); Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"), 209).objectReferenceValue = spawn; Save(reinforcement);
        if (!File.Exists(MATCHUPS) && !AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/AcclamatorMatchups.asset", MATCHUPS)) throw new InvalidOperationException(MATCHUPS);
        var matchups = Load(MATCHUPS); matchups.targetObject.name = "AcclamatorAssaultMatchups"; Save(matchups);
        var faction = Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");
        var unit = Entry(faction.FindProperty("ships.keyValue"), 209);
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue = "AcclamatorAssault";
        unit.FindPropertyRelative("role").stringValue = "Battle Carrier / Cruiser";
        unit.FindPropertyRelative("description").stringValue = "5,000 hull; 2,500 shields; speed 40. Two targetable 4-burst assault-missile launchers, shield generator and engine. Four non-targetable heavy lasers and six non-targetable missile-defense mounts. TIE-Fighters and TIE-Bombers; no hangar hardpoint. Boost Weapon Power doubles fire rate for 20 s, stops shield regeneration, reduces speed to 25% and increases incoming damage by 50%.";
        unit.FindPropertyRelative("iconKey").stringValue = iconKey; unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = icon;
        unit.FindPropertyRelative("matchups").objectReferenceValue = matchups.targetObject; unit.FindPropertyRelative("isHero").boolValue = false;
        foreach (var field in new[] {new {key = "MaxCount", value = 10}, new {key = "AvailableLevel", value = 3}, new {key = "Price", value = 4300}, new {key = "BuildTime", value = 86}, new {key = "UnitCapacity", value = 4}})
            unit.FindPropertyRelative("<" + field.key + ">k__BackingField").intValue = field.value;
        Save(faction); AssetDatabase.SaveAssets();
        return "Registered Empire ship 209, own icon/preview/data/Addressables/roster, four-burst missile profile and Boost Weapon Power.";
    }
    static void Profile(SerializedObject data, int source, int type, string name, float damage, float interval, float reload, float range, int damageType)
    {
        var row = CloneRow(data.FindProperty("weapons"), "weaponType", source, type);
        row.FindPropertyRelative("displayName").stringValue = name; row.FindPropertyRelative("damage").floatValue = damage;
        row.FindPropertyRelative("damageType").intValue = damageType; row.FindPropertyRelative("shotsPerSalvo").intValue = 4;
        row.FindPropertyRelative("shotInterval").floatValue = interval; row.FindPropertyRelative("reload").floatValue = reload;
        row.FindPropertyRelative("range").floatValue = range; row.FindPropertyRelative("projectileSpeed").floatValue = 35;
        row.FindPropertyRelative("strikecraftOnly").boolValue = false; row.FindPropertyRelative("interceptable").boolValue = true;
        row.FindPropertyRelative("color").colorValue = new Color(1, .55f, .1f);
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
