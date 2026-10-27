using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;

public static class AddWavecrestPowerToShields
{
    const int ABILITY_ID = 53;
    const string CATALOG = "Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset";
    const string DATA = "Assets/Settings/Data/Ship/Separatist/WavecrestShipData.asset";
    const string AUDIO = "Assets/Settings/Data/Models/Audio/ShipSfxData.asset";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Wavecrest ability setup requires Edit Mode.");
        var catalog = Load(CATALOG);
        var definitions = catalog.FindProperty("definitions.keyValue");
        var donor = Row(definitions, "key", 3).FindPropertyRelative("value");
        var icon = donor.FindPropertyRelative("icon").objectReferenceValue;
        var entry = Enumerable.Range(0, definitions.arraySize).Select(i => definitions.GetArrayElementAtIndex(i))
            .SingleOrDefault(row => row.FindPropertyRelative("key").intValue == ABILITY_ID);
        if (entry == null)
        {
            int index = definitions.arraySize;
            definitions.arraySize++;
            entry = definitions.GetArrayElementAtIndex(index);
        }
        entry.FindPropertyRelative("key").intValue = ABILITY_ID;
        var value = entry.FindPropertyRelative("value");
        value.FindPropertyRelative("icon").objectReferenceValue = icon;
        value.FindPropertyRelative("displayName").stringValue = "Power to Shields";
        value.FindPropertyRelative("description").stringValue = "Shield regeneration increases tenfold to 300/s and movement speed falls by 20% for 20 seconds. Weapon damage and fire rate are unchanged. Recovers 60 seconds after expiry.";
        value.FindPropertyRelative("duration").floatValue = 20;
        value.FindPropertyRelative("recoveryDelay").floatValue = 60;
        value.FindPropertyRelative("canCancel").boolValue = false;
        value.FindPropertyRelative("isToggle").boolValue = false;
        value.FindPropertyRelative("requiresEnemyTarget").boolValue = false;
        value.FindPropertyRelative("range").floatValue = 0;
        value.FindPropertyRelative("aiUse").intValue = (int)ShipAbilityAiUse.Defensive;
        var settings = value.FindPropertyRelative("settings");
        settings.managedReferenceValue = new BoostShieldPowerSettings();
        var modifier = settings.FindPropertyRelative("statModifier");
        modifier.FindPropertyRelative("damageMultiplier").floatValue = 1;
        modifier.FindPropertyRelative("fireDelayMultiplier").floatValue = 1;
        modifier.FindPropertyRelative("speedMultiplier").floatValue = .8f;
        modifier.FindPropertyRelative("shieldRegenMultiplier").floatValue = 10;
        modifier.FindPropertyRelative("damageTakenMultiplier").floatValue = 1;
        Save(catalog);
        var data = Load(DATA);
        var abilities = data.FindProperty("abilities");
        abilities.arraySize = 1;
        abilities.GetArrayElementAtIndex(0).intValue = ABILITY_ID;
        Save(data);
        var audio = Load(AUDIO);
        var sounds = audio.FindProperty("abilities");
        if (!Enumerable.Range(0, sounds.arraySize).Any(i => sounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue == ABILITY_ID))
        {
            int index = Enumerable.Range(0, sounds.arraySize).Single(i => sounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue == 3);
            sounds.InsertArrayElementAtIndex(index);
            sounds.GetArrayElementAtIndex(index).FindPropertyRelative("abilityId").intValue = ABILITY_ID;
        }
        Save(audio);
        AssetDatabase.SaveAssets();
        return Verify();
    }

    public static string Verify()
    {
        var data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
        var definition = AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>(CATALOG).Get((ShipAbilityId)ABILITY_ID);
        var modifier = ((BoostShieldPowerSettings)definition.Settings).StatModifier;
        Check(data.Abilities.Count == 1 && (int)data.Abilities.Single() == ABILITY_ID, "Wavecrest ability binding");
        Check(definition.Duration == 20 && definition.RecoveryDelay == 60, "Source ability timing");
        Check(definition.Icon != null && definition.AiUse == ShipAbilityAiUse.Defensive && !definition.RequiresEnemyTarget, "Shield icon and defensive AI");
        Check(modifier.ShieldRegenMultiplier == 10 && modifier.SpeedMultiplier == .8f && modifier.DamageMultiplier == 1
            && modifier.FireDelayMultiplier == 1 && modifier.DamageTakenMultiplier == 1, "Source modifiers");
        Check(data.ShieldRegenerateValue * modifier.ShieldRegenMultiplier == 300, "300 shields/s while active");
        var sounds = Load(AUDIO).FindProperty("abilities");
        var audio = Row(sounds, "abilityId", ABILITY_ID);
        Check(audio.FindPropertyRelative("start.clip").objectReferenceValue != null, "Ability activation audio");
        string report = JsonConvert.SerializeObject(new { abilityId = ABILITY_ID, duration = definition.Duration,
            recovery = definition.RecoveryDelay, shieldRegen = 300, speedMultiplier = modifier.SpeedMultiplier,
            damageMultiplier = modifier.DamageMultiplier, fireDelayMultiplier = modifier.FireDelayMultiplier, passed = true }, Formatting.Indented);
        File.WriteAllText("Tools/Blender/Wavecrest/Evidence/PowerToShieldsVerification.json", report + Environment.NewLine);
        return report;
    }

    static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    static SerializedProperty Row(SerializedProperty rows, string key, int id) => Enumerable.Range(0, rows.arraySize)
        .Select(i => rows.GetArrayElementAtIndex(i)).Single(row => row.FindPropertyRelative(key).intValue == id);
    static void Save(SerializedObject data) { data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data.targetObject); }
    static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
