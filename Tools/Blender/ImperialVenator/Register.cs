using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.ShipAbilities.Abilities;

public static class RegisterImperialVenator
{
    const int SHIP_ID = 211;
    const int ABILITY_ID = 35;
    const string VIEW = "Assets/Prefabs/Models/Ships/ImperialVenatorShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/ImperialVenatorShipData.asset";
    public static string Main()
    {
        var catalog = Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        var definitions = catalog.FindProperty("definitions.keyValue");
        if (!Enumerable.Range(0, definitions.arraySize).Any(i => definitions.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue == ABILITY_ID))
        {
            int donor = Enumerable.Range(0, definitions.arraySize).Single(i => definitions.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue == 19);
            definitions.InsertArrayElementAtIndex(donor); definitions.GetArrayElementAtIndex(donor).FindPropertyRelative("key").intValue = ABILITY_ID;
        }
        var ability = Entry(definitions, ABILITY_ID);
        ability.FindPropertyRelative("displayName").stringValue = "Intensify Firepower";
        ability.FindPropertyRelative("description").stringValue = "Turbolasers, ion cannons and the SPHAT beam fire about three times faster for 15 s. Damage, speed and shield regeneration are unchanged. 60 s recovery.";
        ability.FindPropertyRelative("duration").floatValue = 15; ability.FindPropertyRelative("recoveryDelay").floatValue = 60;
        ability.FindPropertyRelative("settings").managedReferenceValue = new FullSalvoSettings();
        ability.FindPropertyRelative("settings.projectileFireDelayMultiplier").floatValue = 1;
        ability.FindPropertyRelative("settings.otherFireDelayMultiplier").floatValue = .33f;
        Save(catalog);
        var audio = Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        var audioRows = audio.FindProperty("abilities");
        if (!Enumerable.Range(0, audioRows.arraySize).Any(i => audioRows.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue == ABILITY_ID))
        {
            int donor = Enumerable.Range(0, audioRows.arraySize).Single(i => audioRows.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue == 19);
            audioRows.InsertArrayElementAtIndex(donor); audioRows.GetArrayElementAtIndex(donor).FindPropertyRelative("abilityId").intValue = ABILITY_ID; Save(audio);
        }
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/ImperialVenatorIcon.png");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long iconId); string iconKey = iconGuid + ":" + iconId;
        var mapping = Load("Assets/Settings/AssetMappingData.asset"); var settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (var path in new[] { VIEW, DATA })
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            Entry(mapping.FindProperty("assetMappings.keyValue"), Path.GetFileNameWithoutExtension(path)).FindPropertyRelative("m_AssetGUID").stringValue = guid;
            string donor = path == VIEW ? "Assets/Prefabs/Models/Ships/VenatorShipView.prefab" : "Assets/Settings/Data/Ship/VenatorShipData.asset";
            var entry = settings.CreateOrMoveEntry(guid, settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(donor)).parentGroup);
            entry.address = Path.GetFileNameWithoutExtension(path); EditorUtility.SetDirty(entry.parentGroup);
        }
        Save(mapping); EditorUtility.SetDirty(settings);
        var ships = Load("Assets/Settings/Data/Ship/ShipsData.asset"); Entry(ships.FindProperty("shipsData.keyValue"), SHIP_ID).FindPropertyRelative("m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(DATA); Save(ships);
        var ui = Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset"); Entry(ui.FindProperty("shipIconWrapper.keyValue"), SHIP_ID).objectReferenceValue = icon; Save(ui);
        var tooltip = Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset"); var icons = tooltip.FindProperty("icons");
        int index = Enumerable.Range(0, icons.arraySize).Where(i => icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == iconKey).DefaultIfEmpty(-1).First();
        if (index < 0) { index = icons.arraySize; icons.arraySize++; }
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue = iconKey; icons.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue = icon; Save(tooltip);
        var preview = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/ImperialVenatorReinforcementView.prefab");
        var reinforcement = Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset"); Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"), SHIP_ID).objectReferenceValue = preview.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "UnitSpawnView"); Save(reinforcement);
        const string MATCHUPS = "Assets/Settings/Data/Tooltip/Matchups/ImperialVenatorMatchups.asset";
        if (!File.Exists(MATCHUPS)) AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/VenatorMatchups.asset", MATCHUPS);
        var matchups = Load(MATCHUPS); matchups.targetObject.name = "ImperialVenatorMatchups"; Save(matchups);
        var faction = Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset"); var unit = Entry(faction.FindProperty("ships.keyValue"), SHIP_ID);
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue = "Imperial Venator";
        unit.FindPropertyRelative("role").stringValue = "Capital carrier / Anti-frigate";
        unit.FindPropertyRelative("description").stringValue = "15,000 hull and shields. Republic Venator hardpoint layout: ten Imperial green heavy dual turbolasers, two heavy turbolasers, two heavy ion cannons and one SPHAT beam. Intensify Firepower increases firing rate for 15 s. Launches TIE Fighters and TIE Bombers.";
        unit.FindPropertyRelative("iconKey").stringValue = iconKey; unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = icon; unit.FindPropertyRelative("matchups").objectReferenceValue = matchups.targetObject;
        unit.FindPropertyRelative("isHero").boolValue = false; unit.FindPropertyRelative("<MaxCount>k__BackingField").intValue = 3;
        unit.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue = 2; unit.FindPropertyRelative("<Price>k__BackingField").intValue = 17200;
        unit.FindPropertyRelative("<BuildTime>k__BackingField").intValue = 70; unit.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue = 26; Save(faction);
        AssetDatabase.SaveAssets();
        return "Imperial Venator 211 registered only in Empire; ability 35; ship/data/addressable/icon/tooltip/placement mappings saved.";
    }
    static SerializedProperty Entry(SerializedProperty array, object key)
    {
        for (int i = 0; i < array.arraySize; i++) { var row = array.GetArrayElementAtIndex(i); var item = row.FindPropertyRelative("key"); if (key is int ? item.intValue == (int)key : item.stringValue == (string)key) return row.FindPropertyRelative("value"); }
        int index = array.arraySize; array.arraySize++; var added = array.GetArrayElementAtIndex(index);
        if (key is int) added.FindPropertyRelative("key").intValue = (int)key; else added.FindPropertyRelative("key").stringValue = (string)key;
        return added.FindPropertyRelative("value");
    }
    static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    static void Save(SerializedObject so) { so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(so.targetObject); }
}
