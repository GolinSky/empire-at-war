using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.ShipAbilities.Abilities;

public static class RegisterArquitensImperialCruiser
{
    const string VIEW = "Assets/Prefabs/Models/Ships/ArquitensImperialCruiserShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/ArquitensImperialCruiserShipData.asset";
    const string MATCHUPS = "Assets/Settings/Data/Tooltip/Matchups/ArquitensImperialCruiserMatchups.asset";
    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Arquitens asset registration requires Edit Mode.");
        var weapons = Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        Profile(weapons, 33, 39, "Light Long-Range 2-Burst Turbolaser", 67.5f, 4.75f, 9.75f, 562.5f, 2);
        Profile(weapons, 8, 40, "2-Burst Laser Cannon", 7.5f, .5f, 1.25f, 200, 0);
        Save(weapons);
        var audio = Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        CloneRow(audio.FindProperty("weapons"), "weaponType", 33, 39);
        CloneRow(audio.FindProperty("weapons"), "weaponType", 8, 40);
        CloneRow(audio.FindProperty("abilities"), "abilityId", 5, 23);
        Save(audio);
        var abilities = Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        var ability = CloneRow(abilities.FindProperty("definitions.keyValue"), "key", 5, 23).FindPropertyRelative("value");
        ability.FindPropertyRelative("settings").managedReferenceValue = new BoostWeaponPowerSettings();
        abilities.ApplyModifiedPropertiesWithoutUndo(); abilities.Update();
        ability = Entry(abilities.FindProperty("definitions.keyValue"), 23);
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
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/ArquitensImperialCruiserIcon.png");
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long localId)) throw new InvalidOperationException("Missing Arquitens sprite");
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
        Entry(ships.FindProperty("shipsData.keyValue"), 204).FindPropertyRelative("m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(DATA); Save(ships);
        var ui = Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset"); Entry(ui.FindProperty("shipIconWrapper.keyValue"), 204).objectReferenceValue = icon; Save(ui);
        var tooltip = Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset"); var icons = tooltip.FindProperty("icons");
        int index = Enumerable.Range(0, icons.arraySize).Where(i => icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == iconKey).DefaultIfEmpty(-1).First();
        if (index < 0) { index = icons.arraySize; icons.arraySize++; }
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue = iconKey;
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue = icon; Save(tooltip);
        var preview = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/ArquitensImperialCruiserReinforcementView.prefab");
        var spawn = preview.GetComponents<MonoBehaviour>().Single(m => m.GetType().Name == "UnitSpawnView");
        var reinforcement = Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset"); Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"), 204).objectReferenceValue = spawn; Save(reinforcement);
        if (!File.Exists(MATCHUPS) && !AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/ArquitensMatchups.asset", MATCHUPS)) throw new InvalidOperationException(MATCHUPS);
        var matchups = Load(MATCHUPS); matchups.targetObject.name = "ArquitensImperialCruiserMatchups"; Save(matchups);
        var faction = Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");
        var unit = Entry(faction.FindProperty("ships.keyValue"), 204);
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue = "ArquitensImperialCruiser";
        unit.FindPropertyRelative("role").stringValue = "Artillery / Light Cruiser";
        unit.FindPropertyRelative("description").stringValue = "1,300 hull; 1,200 shields; speed 30. Four light long-range 2-burst turbolasers and four 2-burst laser cannons. Weapons, engines and shields cannot be targeted separately. No hangar or fighter complement. Boost Weapon Power doubles fire rate for 20 s, stops shield regeneration, reduces speed to 25% and increases incoming damage by 50%.";
        unit.FindPropertyRelative("iconKey").stringValue = iconKey; unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = icon;
        unit.FindPropertyRelative("matchups").objectReferenceValue = matchups.targetObject; unit.FindPropertyRelative("isHero").boolValue = false;
        foreach (var field in new[] {new {key = "MaxCount", value = 10}, new {key = "AvailableLevel", value = 1}, new {key = "Price", value = 3250}, new {key = "BuildTime", value = 65}, new {key = "UnitCapacity", value = 3}})
            unit.FindPropertyRelative("<" + field.key + ">k__BackingField").intValue = field.value;
        Save(faction); AssetDatabase.SaveAssets();
        return "Registered Empire ship 204, own icon/preview/data/Addressables/roster, two dedicated weapon/audio profiles and source-specific Boost Weapon Power.";
    }
    static void Profile(SerializedObject data, int source, int type, string name, float damage, float interval, float reload, float range, int damageType)
    {
        var row = CloneRow(data.FindProperty("weapons"), "weaponType", source, type);
        row.FindPropertyRelative("displayName").stringValue = name; row.FindPropertyRelative("damage").floatValue = damage;
        row.FindPropertyRelative("damageType").intValue = damageType; row.FindPropertyRelative("shotsPerSalvo").intValue = 2;
        row.FindPropertyRelative("shotInterval").floatValue = interval; row.FindPropertyRelative("reload").floatValue = reload;
        row.FindPropertyRelative("range").floatValue = range; row.FindPropertyRelative("projectileSpeed").floatValue = 133;
        row.FindPropertyRelative("strikecraftOnly").boolValue = false; row.FindPropertyRelative("interceptable").boolValue = false;
        row.FindPropertyRelative("color").colorValue = new Color(.2f, 1, .25f);
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
