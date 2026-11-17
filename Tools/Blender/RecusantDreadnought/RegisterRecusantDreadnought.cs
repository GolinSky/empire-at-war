using System;
using System.IO;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Editor.AI;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.ShipAbilities;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class RegisterRecusantDreadnought
{
    const int SHIP_ID = (int)ShipType.RecusantDreadnought;
    const string SHIP_NAME = "Recusant Dreadnought";
    const string VIEW = "Assets/Prefabs/Models/Ships/Separatist/View/RecusantDreadnoughtShipView.prefab";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/RecusantDreadnoughtReinforcementView.prefab";
    const string WRECK = "Assets/Prefabs/Models/Wrecks/RecusantDreadnoughtWreckView.prefab";
    const string ICON = "Assets/Art/Textures/Ui/Icons/ShipIcon/Separatist/RecusantDreadnoughtIcon.png";
    const string DATA = "Assets/Settings/Data/Ship/Separatist/RecusantDreadnoughtShipData.asset";
    const string WRECK_DATA = "Assets/Settings/Data/Models/Wrecks/Separatist/RecusantDreadnoughtWreckData.asset";
    const string MATCHUPS = "Assets/Settings/Data/Tooltip/Matchups/RecusantDreadnoughtMatchups.asset";

    public static string Main()
    {
        var bounds = JObject.Parse(File.ReadAllText("Temp/RecusantDreadnoughtImport/GameplayBounds.json"));
        float bottom = bounds.Value<float>("bottom");
        float top = bounds.Value<float>("top");
        float radius = bounds.Value<float>("radius");
        if (!(bottom < top && radius > 0 && float.IsFinite(bottom) && float.IsFinite(top) && float.IsFinite(radius)))
            throw new InvalidOperationException("The gameplay hull bounds are missing or invalid.");

        var loadout = WeaponLoadoutBaker.DeriveLoadout(Load<GameObject>(VIEW));
        var expected = new[]
        {
            (WeaponType.DualHeavyTurboLaser, 12), (WeaponType.HeavyOctupleTurbolaser, 1),
            (WeaponType.MediumQuadTurbolaser, 12), (WeaponType.HeavyDualIonCannon, 2),
            (WeaponType.MediumDualLaser, 2)
        };
        if (!loadout.Select(x => (x.WeaponType, x.Count)).SequenceEqual(expected))
            throw new InvalidOperationException("The saved weapon component does not have the approved 29-mount loadout.");

        Load<GameObject>(PREVIEW);
        Load<GameObject>(WRECK);
        var icon = Load<Sprite>(ICON);
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long iconId))
            throw new InvalidOperationException("The icon needs an imported Sprite local identifier.");
        string iconKey = iconGuid + ":" + iconId;

        var wreck = CopyData("Assets/Settings/Data/Models/Wrecks/Separatist/InvincibleWreckData.asset", WRECK_DATA);
        SetGuid(wreck.FindProperty("prefab"), WRECK);
        Save(wreck);
        var data = CopyData("Assets/Settings/Data/Ship/Separatist/InvincibleShipData.asset", DATA);
        Field(data, "ShipClass").intValue = (int)ShipClass.BattleCruiser;
        Field(data, "Hull").floatValue = 30000;
        Field(data, "Shields").floatValue = 35000;
        Field(data, "HullBottom").floatValue = bottom;
        Field(data, "HullTop").floatValue = top;
        Field(data, "NavigationRadius").floatValue = radius;
        Field(data, "Wreck").objectReferenceValue = wreck.targetObject;
        Field(data, "HangarLaunchInterval").floatValue = 15;
        var abilities = data.FindProperty("abilities");
        abilities.arraySize = 1;
        abilities.GetArrayElementAtIndex(0).intValue = (int)ShipAbilityId.BoostWeaponPower;
        var bays = data.FindProperty("hangarBays");
        var squadrons = new[] { SquadronType.Vulture, SquadronType.Belbullab22 };
        bays.arraySize = squadrons.Length;
        for (int i = 0; i < squadrons.Length; i++)
        {
            var bay = bays.GetArrayElementAtIndex(i);
            bay.FindPropertyRelative("squadronType").intValue = (int)squadrons[i];
            bay.FindPropertyRelative("reserve").intValue = 3;
            bay.FindPropertyRelative("maxActive").intValue = 1;
        }
        var weapons = data.FindProperty("weaponLoadout");
        weapons.arraySize = loadout.Count;
        for (int i = 0; i < loadout.Count; i++)
        {
            var weapon = weapons.GetArrayElementAtIndex(i);
            weapon.FindPropertyRelative("weaponType").intValue = (int)loadout[i].WeaponType;
            weapon.FindPropertyRelative("count").intValue = loadout[i].Count;
        }
        Save(data);

        var mapping = Serialized("Assets/Settings/AssetMappingData.asset");
        var assets = mapping.FindProperty("assetMappings.keyValue");
        foreach (string path in new[] { VIEW, DATA, WRECK })
        {
            string name = Path.GetFileNameWithoutExtension(path);
            SetGuid(Row(assets, name).FindPropertyRelative("value"), path);
        }
        Save(mapping);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (var item in new[] { (VIEW, "SeparatistUnitViews"), (PREVIEW, "SeparatistUnitViews"),
            (WRECK, "SeparatistUnitViews"), (DATA, "Data"), (ICON, "SharedIcons") })
        {
            string path = item.Item1;
            var group = settings.FindGroup(item.Item2);
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
            entry.address = path == ICON ? path : Path.GetFileNameWithoutExtension(path);
            EditorUtility.SetDirty(group);
        }
        EditorUtility.SetDirty(settings);

        var ships = Serialized("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
        SetGuid(Row(ships.FindProperty("shipsData.keyValue"), SHIP_ID).FindPropertyRelative("value"), DATA);
        Save(ships);
        var reinforcement = Serialized("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        SetGuid(Row(reinforcement.FindProperty("spawnShipWrapper.keyValue"), SHIP_ID).FindPropertyRelative("value"), PREVIEW);
        Save(reinforcement);
        var ui = Serialized("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");
        Row(ui.FindProperty("shipIconWrapper.keyValue"), SHIP_ID).FindPropertyRelative("value").objectReferenceValue = icon;
        Save(ui);
        var tooltip = Serialized("Assets/Settings/Data/Tooltip/TooltipIconData.asset");
        Row(tooltip.FindProperty("icons"), iconKey).FindPropertyRelative("sprite").objectReferenceValue = icon;
        Save(tooltip);

        var matchups = CopyData("Assets/Settings/Data/Tooltip/Matchups/InvincibleMatchups.asset", MATCHUPS);
        matchups.FindProperty("strongAgainst").arraySize = 0;
        matchups.FindProperty("weakAgainst").arraySize = 0;
        Save(matchups);
        var faction = Serialized("Assets/Settings/Data/Factions/Separatist/SeparatistFaction.asset");
        var unit = Row(faction.FindProperty("ships.keyValue"), SHIP_ID).FindPropertyRelative("value");
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue = SHIP_NAME;
        unit.FindPropertyRelative("displayName").stringValue = SHIP_NAME;
        unit.FindPropertyRelative("role").stringValue = "Heavy Capital / Forward assault";
        unit.FindPropertyRelative("description").stringValue = "A forward-assault dreadnought with 29 weapon batteries, including an octuple heavy turbolaser and two heavy dual ion cannons. Boost Weapon Power increases its firing rate at the cost of speed and shield regeneration.";
        unit.FindPropertyRelative("iconKey").stringValue = iconKey;
        unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue = icon;
        unit.FindPropertyRelative("matchups").objectReferenceValue = matchups.targetObject;
        unit.FindPropertyRelative("isHero").boolValue = false;
        foreach (var (field, value) in new[] { ("MaxCount", 20), ("AvailableLevel", 5), ("Price", 18000),
            ("BuildTime", 75), ("UnitCapacity", 25) })
            unit.FindPropertyRelative("<" + field + ">k__BackingField").intValue = value;
        Save(faction);

        var audio = Load<ShipSfxData>("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        var audioData = new SerializedObject(audio);
        Row(audioData.FindProperty("ships.keyValue"), SHIP_ID).FindPropertyRelative("value").boxedValue =
            audio.GetUnit(ShipType.Providence);
        Save(audioData);
        AssetDatabase.SaveAssets();
        return "Registered CIS Recusant Dreadnought (114), with 29 baked weapon mounts, Heavy Capital data, and faction view, wreck, preview, icon and audio bindings.";
    }

    static SerializedProperty Row(SerializedProperty array, object key)
    {
        for (int i = 0; i < array.arraySize; i++)
        {
            var candidate = array.GetArrayElementAtIndex(i);
            var candidateKey = candidate.FindPropertyRelative("key");
            if (key is int number ? candidateKey.intValue == number : candidateKey.stringValue == (string)key)
                return candidate;
        }
        int index = array.arraySize;
        array.arraySize++;
        var row = array.GetArrayElementAtIndex(index);
        var newKey = row.FindPropertyRelative("key");
        if (key is int id) newKey.intValue = id;
        else newKey.stringValue = (string)key;
        return row;
    }

    static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path)
        ?? throw new InvalidOperationException("Missing registration asset: " + path);

    static SerializedObject Serialized(string path) => new SerializedObject(Load<ScriptableObject>(path));

    static SerializedObject CopyData(string source, string destination)
    {
        if (!File.Exists(destination) && !AssetDatabase.CopyAsset(source, destination))
            throw new InvalidOperationException("Could not create " + destination);
        var result = Serialized(destination);
        result.targetObject.name = Path.GetFileNameWithoutExtension(destination);
        return result;
    }

    static SerializedProperty Field(SerializedObject data, string field) =>
        data.FindProperty("<" + field + ">k__BackingField");

    static void SetGuid(SerializedProperty reference, string path)
    {
        reference.FindPropertyRelative("m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(path);
        reference.FindPropertyRelative("m_SubObjectName").stringValue = "";
        reference.FindPropertyRelative("m_SubObjectType").stringValue = "";
        reference.FindPropertyRelative("m_SubObjectGUID").stringValue = "";
        reference.FindPropertyRelative("m_EditorAssetChanged").boolValue = false;
    }

    static void Save(SerializedObject data)
    {
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data.targetObject);
    }
}
