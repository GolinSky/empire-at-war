using System;
using System.IO;
using System.Linq;
using EmpireAtWar.Entities.Reinforcement;
using EmpireAtWar.Models.Factions;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class RegisterInvincible
{
    const int SHIP_ID = (int)ShipType.Invincible;
    const string VIEW = "Assets/Prefabs/Models/Ships/Separatist/InvincibleShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Separatist/InvincibleShipData.asset";
    const string WRECK = "Assets/Prefabs/Models/Wrecks/InvincibleWreckView.prefab";
    const string WRECK_DATA = "Assets/Settings/Data/Models/Wrecks/Separatist/InvincibleWreckData.asset";
    const string MATCHUPS = "Assets/Settings/Data/Tooltip/Matchups/InvincibleMatchups.asset";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Invincible registration requires Edit Mode.");

        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/Separatist/InvincibleIcon.png");
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long localId))
            throw new InvalidOperationException("Invincible's rendered sprite must be imported before registration.");
        string iconKey = iconGuid + ":" + localId;
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var mapping = Load("Assets/Settings/AssetMappingData.asset");
        foreach (var pair in new[]
        {
            (Path: VIEW, Donor: "Assets/Prefabs/Models/Ships/Separatist/ProvidenceShipView.prefab"),
            (Path: DATA, Donor: "Assets/Settings/Data/Ship/Separatist/ProvidenceShipData.asset"),
            (Path: WRECK, Donor: "Assets/Prefabs/Models/Wrecks/ProvidenceWreckView.prefab")
        })
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(pair.Path);
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long assetId))
                throw new InvalidOperationException("Missing Invincible asset: " + pair.Path);
            Entry(mapping.FindProperty("assetMappings.keyValue"), Path.GetFileNameWithoutExtension(pair.Path))
                .FindPropertyRelative("m_AssetGUID").stringValue = guid;
            var group = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(pair.Donor)).parentGroup;
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = Path.GetFileNameWithoutExtension(pair.Path);
            EditorUtility.SetDirty(group);
        }
        Save(mapping);
        EditorUtility.SetDirty(settings);

        var ships = Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
        Entry(ships.FindProperty("shipsData.keyValue"), SHIP_ID).FindPropertyRelative("m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(DATA);
        Save(ships);
        var ui = Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");
        Entry(ui.FindProperty("shipIconWrapper.keyValue"), SHIP_ID).objectReferenceValue = icon;
        Save(ui);

        var tooltip = Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");
        var icons = tooltip.FindProperty("icons");
        int iconIndex = Enumerable.Range(0, icons.arraySize)
            .Where(i => icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == iconKey)
            .DefaultIfEmpty(-1).First();
        if (iconIndex < 0) { iconIndex = icons.arraySize; icons.arraySize++; }
        icons.GetArrayElementAtIndex(iconIndex).FindPropertyRelative("key").stringValue = iconKey;
        icons.GetArrayElementAtIndex(iconIndex).FindPropertyRelative("sprite").objectReferenceValue = icon;
        Save(tooltip);

        var preview = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/InvincibleReinforcementView.prefab");
        var previewComponents = new SerializedObject(preview).FindProperty("m_Component");
        var spawn = Enumerable.Range(0, previewComponents.arraySize)
            .Select(i => previewComponents.GetArrayElementAtIndex(i).FindPropertyRelative("component").objectReferenceValue)
            .OfType<UnitSpawnView>().Single();
        var reinforcement = Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"), SHIP_ID).objectReferenceValue = spawn;
        Save(reinforcement);

        if (!File.Exists(MATCHUPS) && !AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/ProvidenceMatchups.asset", MATCHUPS))
            throw new InvalidOperationException("Could not create Invincible's matchup data.");
        var matchups = Load(MATCHUPS);
        matchups.targetObject.name = "InvincibleMatchups";
        Save(matchups);

        var faction = Load("Assets/Settings/Data/Factions/Separatist/SeparatistFaction.asset");
        var roster = faction.FindProperty("ships.keyValue");
        var donor = Entry(roster, (int)ShipType.Providence);
        int[] rosterValues = new[] {"MaxCount", "AvailableLevel", "Price", "BuildTime", "UnitCapacity"}
            .Select(field => donor.FindPropertyRelative("<" + field + ">k__BackingField").intValue).ToArray();
        bool isHero = donor.FindPropertyRelative("isHero").boolValue;
        var unit = Entry(roster, SHIP_ID);
        string[] rosterFields = {"MaxCount", "AvailableLevel", "Price", "BuildTime", "UnitCapacity"};
        for (int i = 0; i < rosterFields.Length; i++)
            unit.FindPropertyRelative("<" + rosterFields[i] + ">k__BackingField").intValue = rosterValues[i] * (rosterFields[i] == "Price" || rosterFields[i] == "UnitCapacity" ? 2 : 1);
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue = "Invincible";
        unit.FindPropertyRelative("role").stringValue = "Capital / Providence-class dreadnought / carrier";
        unit.FindPropertyRelative("description").stringValue = "A larger Providence-class hull with 10,000 hull, 6,000 shields and 28 weapon hardpoints. Retains Providence's abilities and fighter complement.";
        unit.FindPropertyRelative("iconKey").stringValue = iconKey;
        unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = icon;
        unit.FindPropertyRelative("matchups").objectReferenceValue = matchups.targetObject;
        unit.FindPropertyRelative("isHero").boolValue = isHero;
        Save(faction);

        var wreckData = Load(WRECK_DATA);
        wreckData.FindProperty("prefab.m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(WRECK);
        Save(wreckData);
        AssetDatabase.SaveAssets();
        return JsonConvert.SerializeObject(new {shipType = SHIP_ID, faction = "Separatist", view = VIEW, data = DATA, iconKey, price = unit.FindPropertyRelative("<Price>k__BackingField").intValue, population = unit.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue, isHero});
    }

    static SerializedProperty Entry(SerializedProperty array, object key)
    {
        for (int i = 0; i < array.arraySize; i++)
        {
            var row = array.GetArrayElementAtIndex(i);
            var candidate = row.FindPropertyRelative("key");
            if (key is int number ? candidate.intValue == number : candidate.stringValue == (string)key)
                return row.FindPropertyRelative("value");
        }
        int index = array.arraySize;
        array.arraySize++;
        var entry = array.GetArrayElementAtIndex(index);
        if (key is int integer) entry.FindPropertyRelative("key").intValue = integer;
        else entry.FindPropertyRelative("key").stringValue = (string)key;
        return entry.FindPropertyRelative("value");
    }

    static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    static void Save(SerializedObject data)
    {
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data.targetObject);
    }
}
