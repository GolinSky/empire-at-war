using System;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.ShipAbilities.Abilities;

public static class RegisterAssaultFrigateMkII
{
    const int SHIP_ID = 309;
    const int ABILITY_ID = 45;
    const string NAME = "AssaultFrigateMkII";
    const string DATA = "Assets/Settings/Data/Ship/AssaultFrigateMkIIShipData.asset";
    const string VIEW = "Assets/Prefabs/Models/Ships/AssaultFrigateMkIIShipView.prefab";
    const string ICON = "Assets/Art/Textures/Ui/Icons/ShipIcon/AssaultFrigateMkIIIcon.png";
    const string MATCHUPS = "Assets/Settings/Data/Tooltip/Matchups/AssaultFrigateMkIIMatchups.asset";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Ship registration requires Edit Mode.");
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>(ICON);
        string iconKey = IconKey(icon);
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MATCHUPS) == null)
            AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/MonCalCruiserMatchups.asset", MATCHUPS);
        var matchups = Load(MATCHUPS); matchups.targetObject.name = NAME+"Matchups";
        var strong = matchups.FindProperty("strongAgainst"); strong.arraySize = 2;
        Matchup(strong.GetArrayElementAtIndex(0), "Corvettes", "Assets/Art/Textures/Ui/Icons/ShipIcon/CorellianCorvetteIcon.png");
        Matchup(strong.GetArrayElementAtIndex(1), "Frigates", "Assets/Art/Textures/Ui/Icons/ShipIcon/ArquitensIcon.png");
        var weak = matchups.FindProperty("weakAgainst"); weak.arraySize = 1;
        Matchup(weak.GetArrayElementAtIndex(0), "Heavy capital ships", "Assets/Art/Textures/Ui/Icons/ShipIcon/ISDIIIcon.png"); Save(matchups);
        var faction = Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset");
        var value = Upsert(faction.FindProperty("ships.keyValue"), SHIP_ID).FindPropertyRelative("value");
        value.FindPropertyRelative("matchups").objectReferenceValue = matchups.targetObject;
        value.FindPropertyRelative("description").stringValue = "General-purpose combat frigate for fleet escort and medium-range combat. Four medium turbolasers, two light quad turbolasers, four heavy laser cannons and two point-defense lasers. Power to Shields increases shield regeneration by 60%, reducing movement speed and weapon damage by 20% for 8 seconds; 35-second recovery. Strong against corvettes and frigates; limited against heavy capitals.";
        value.FindPropertyRelative("role").stringValue = "Fleet escort / Medium-range combat";
        value.FindPropertyRelative("iconKey").stringValue = iconKey;
        value.FindPropertyRelative("isHero").boolValue = false;
        value.FindPropertyRelative("<Name>k__BackingField").stringValue = "Assault Frigate Mk II";
        value.FindPropertyRelative("<MaxCount>k__BackingField").intValue = 10;
        value.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue = 3;
        value.FindPropertyRelative("<Price>k__BackingField").intValue = 3200;
        value.FindPropertyRelative("<BuildTime>k__BackingField").intValue = 40;
        value.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue = 3;
        value.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = icon; Save(faction);
        var ships = Load("Assets/Settings/Data/Ship/ShipsData.asset");
        Upsert(ships.FindProperty("shipsData.keyValue"), SHIP_ID).FindPropertyRelative("value.m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(DATA); Save(ships);
        var ui = Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");
        Upsert(ui.FindProperty("shipIconWrapper.keyValue"), SHIP_ID).FindPropertyRelative("value").objectReferenceValue = icon; Save(ui);
        var tooltips = Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");
        var icons = tooltips.FindProperty("icons");
        var row = Enumerable.Range(0, icons.arraySize).Select(i => icons.GetArrayElementAtIndex(i)).SingleOrDefault(p => p.FindPropertyRelative("key").stringValue == iconKey) ?? Append(icons);
        row.FindPropertyRelative("key").stringValue = iconKey; row.FindPropertyRelative("sprite").objectReferenceValue = icon; Save(tooltips);
        var reinforcement = Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        var preview = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/AssaultFrigateMkIIReinforcementView.prefab");
        Upsert(reinforcement.FindProperty("spawnShipWrapper.keyValue"), SHIP_ID).FindPropertyRelative("value").objectReferenceValue = preview.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "UnitSpawnView"); Save(reinforcement);
        var mapping = Load("Assets/Settings/AssetMappingData.asset");
        foreach (var asset in new[] { VIEW, DATA })
        {
            var dictionary = mapping.FindProperty("assetMappings.keyValue"); string key = System.IO.Path.GetFileNameWithoutExtension(asset);
            row = Enumerable.Range(0, dictionary.arraySize).Select(i => dictionary.GetArrayElementAtIndex(i)).SingleOrDefault(p => p.FindPropertyRelative("key").stringValue == key) ?? Append(dictionary);
            row.FindPropertyRelative("key").stringValue = key; row.FindPropertyRelative("value.m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(asset);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(asset), settings.FindGroup(asset == VIEW ? "View" : "Data")); entry.address = key; EditorUtility.SetDirty(settings);
        }
        Save(mapping);
        var catalog = Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        var definitions = catalog.FindProperty("definitions.keyValue");
        row = Enumerable.Range(0, definitions.arraySize).Select(i => definitions.GetArrayElementAtIndex(i)).SingleOrDefault(p => p.FindPropertyRelative("key").intValue == ABILITY_ID);
        if (row == null)
        {
            int donor = Enumerable.Range(0, definitions.arraySize).Single(i => definitions.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue == 3);
            definitions.InsertArrayElementAtIndex(donor); row = definitions.GetArrayElementAtIndex(donor); row.FindPropertyRelative("key").intValue = ABILITY_ID;
        }
        value = row.FindPropertyRelative("value");
        value.FindPropertyRelative("displayName").stringValue = "Power to Shields";
        value.FindPropertyRelative("description").stringValue = "+60% shield regeneration, -20% movement speed and weapon damage for 8 seconds; 35-second recovery.";
        value.FindPropertyRelative("duration").floatValue = 8;
        value.FindPropertyRelative("recoveryDelay").floatValue = 35;
        value.FindPropertyRelative("settings").managedReferenceValue = new BoostShieldPowerSettings();
        catalog.ApplyModifiedPropertiesWithoutUndo(); catalog.Update();
        value = Upsert(definitions, ABILITY_ID).FindPropertyRelative("value");
        var modifier = value.FindPropertyRelative("settings.statModifier");
        modifier.FindPropertyRelative("damageMultiplier").floatValue = .8f;
        modifier.FindPropertyRelative("fireDelayMultiplier").floatValue = 1;
        modifier.FindPropertyRelative("speedMultiplier").floatValue = .8f;
        modifier.FindPropertyRelative("shieldRegenMultiplier").floatValue = 1.6f;
        modifier.FindPropertyRelative("damageTakenMultiplier").floatValue = 1; Save(catalog);
        var weapons = Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        var profiles = weapons.FindProperty("weapons");
        row = Enumerable.Range(0, profiles.arraySize).Select(i => profiles.GetArrayElementAtIndex(i)).SingleOrDefault(p => p.FindPropertyRelative("weaponType").intValue == 82);
        if (row == null)
        {
            int donor = Enumerable.Range(0, profiles.arraySize).Single(i => profiles.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue == 36);
            profiles.InsertArrayElementAtIndex(donor); row = profiles.GetArrayElementAtIndex(donor);
        }
        row.FindPropertyRelative("weaponType").intValue = 82;
        row.FindPropertyRelative("displayName").stringValue = "Light Quad Turbolaser";
        row.FindPropertyRelative("mount").intValue = 4; row.FindPropertyRelative("rangeClass").intValue = 0; Save(weapons);
        var audio = Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        var sounds = audio.FindProperty("abilities");
        if (!Enumerable.Range(0, sounds.arraySize).Any(i => sounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue == ABILITY_ID))
        {
            int donor = Enumerable.Range(0, sounds.arraySize).Single(i => sounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue == 3);
            sounds.InsertArrayElementAtIndex(donor); sounds.GetArrayElementAtIndex(donor).FindPropertyRelative("abilityId").intValue = ABILITY_ID;
        }
        Save(audio); AssetDatabase.SaveAssets();
        return "Assault Frigate Mk II=309 registered: Rebellion level 3/population 3, view/data/preview/icons/matchups; exact Power to Shields=45 and Light Quad Turbolaser=82.";
    }
    static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Append(SerializedProperty array) { array.InsertArrayElementAtIndex(array.arraySize); return array.GetArrayElementAtIndex(array.arraySize-1); }
    static SerializedProperty Upsert(SerializedProperty array, int id) { var row = Enumerable.Range(0, array.arraySize).Select(i => array.GetArrayElementAtIndex(i)).SingleOrDefault(p => p.FindPropertyRelative("key").intValue == id) ?? Append(array); row.FindPropertyRelative("key").intValue = id; return row; }
    static void Save(SerializedObject so) { so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(so.targetObject); }
    static string IconKey(Sprite sprite) { AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long id); return guid+":"+id; }
    static void Matchup(SerializedProperty row, string label, string path) { row.FindPropertyRelative("label").stringValue = label; row.FindPropertyRelative("iconKey").stringValue = IconKey(AssetDatabase.LoadAssetAtPath<Sprite>(path)); }
}
