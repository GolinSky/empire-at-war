using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Editor.AI;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Squadrons;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// One-time migration. Reuses existing weapon definitions; one stronger gun replaces each fighter's gun battery.
public static class ConsolidateFighterGuns
{
    private const string VIEWS = "Assets/Prefabs/Models/Squadrons";
    private const string DATA = "Assets/Settings/Data/Squadron";
    private const string WEAPONS = "Assets/Settings/Data/Models/Weapon/WeaponsData.asset";

    public static object Apply()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
            throw new InvalidOperationException("Gun consolidation requires an idle Editor.");

        var weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS);
        var paths = Directory.GetFiles(VIEWS, "*SquadronView.prefab").Select(path => path.Replace('\\', '/'))
            .OrderBy(path => path).ToArray();
        var multipliers = new Dictionary<WeaponType, float>();
        var before = new Dictionary<string, float>();
        var reports = new List<object>();
        foreach (string path in paths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            before.Add(path, Dps(prefab, weapons));
            foreach (FighterHardPoint fighter in prefab.GetComponentsInChildren<FighterHardPoint>(true))
            {
                foreach (var channel in Channels(fighter, weapons))
                {
                    WeaponHardPoint retained = Retained(fighter, channel);
                    float multiplier = channel.Sum(gun => weapons.GetProfile(gun.WeaponType).DamagePerSecond) /
                        weapons.GetProfile(retained.WeaponType).DamagePerSecond;
                    if (multipliers.TryGetValue(retained.WeaponType, out float existing) &&
                        Mathf.Abs(existing - multiplier) > 0.0001f)
                        throw new InvalidOperationException("Inconsistent battery for shared weapon " + retained.WeaponType);
                    multipliers[retained.WeaponType] = multiplier;
                }
            }
        }

        foreach (string path in paths)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var removed = new HashSet<WeaponHardPoint>();
                foreach (FighterHardPoint fighter in root.GetComponentsInChildren<FighterHardPoint>(true))
                {
                    foreach (var channel in Channels(fighter, weapons))
                    {
                        WeaponHardPoint retained = Retained(fighter, channel);
                        foreach (WeaponHardPoint gun in channel.Where(gun => gun != retained)) removed.Add(gun);
                    }
                    var serializedFighter = new SerializedObject(fighter);
                    Filter(serializedFighter.FindProperty("additionalGuns"), removed);
                    serializedFighter.ApplyModifiedPropertiesWithoutUndo();
                }
                foreach (WeaponComponent component in root.GetComponentsInChildren<WeaponComponent>(true))
                {
                    var serializedWeapons = new SerializedObject(component);
                    Filter(serializedWeapons.FindProperty("hardPoints"), removed);
                    serializedWeapons.ApplyModifiedPropertiesWithoutUndo();
                }
                foreach (EntityBindings bindings in root.GetComponentsInChildren<EntityBindings>(true))
                {
                    var serializedBindings = new SerializedObject(bindings);
                    Filter(serializedBindings.FindProperty("<Injectables>k__BackingField"), removed);
                    serializedBindings.ApplyModifiedPropertiesWithoutUndo();
                }
                // Preserve the model's muzzle transforms and any children; only the unused firing components go.
                foreach (WeaponHardPoint gun in removed) Object.DestroyImmediate(gun);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                reports.Add(new { prefab = path, removedGuns = removed.Count });
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        var serializedData = new SerializedObject(weapons);
        SerializedProperty definitions = serializedData.FindProperty("weapons");
        for (int i = 0; i < definitions.arraySize; i++)
        {
            SerializedProperty definition = definitions.GetArrayElementAtIndex(i);
            var type = (WeaponType)definition.FindPropertyRelative("weaponType").intValue;
            if (!multipliers.TryGetValue(type, out float multiplier) || multiplier == 1f) continue;
            definition.FindPropertyRelative("damageMultiplier").floatValue *= multiplier;
            SerializedProperty reason = definition.FindPropertyRelative("reason");
            reason.stringValue = (string.IsNullOrWhiteSpace(reason.stringValue) ? "" :
                "Previous battery: " + reason.stringValue + " ") +
                "Consolidated fighter weapon channel; damage per shot includes the former battery (" +
                multiplier.ToString("G9", System.Globalization.CultureInfo.InvariantCulture) + "x).";
        }
        serializedData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(weapons);

        var changed = new List<string>(paths) { WEAPONS };
        foreach (SquadronData data in WeaponLoadoutBaker.LoadAll<SquadronData>(DATA))
        {
            GameObject view = WeaponLoadoutBaker.LoadSquadronView(data);
            var loadout = WeaponLoadoutBaker.DeriveLoadout(view);
            var serialized = new SerializedObject(data);
            SerializedProperty entries = serialized.FindProperty("weaponLoadout");
            entries.arraySize = loadout.Count;
            for (int i = 0; i < loadout.Count; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("weaponType").intValue = (int)loadout[i].WeaponType;
                entry.FindPropertyRelative("count").intValue = loadout[i].Count;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            changed.Add(AssetDatabase.GetAssetPath(data));
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.ForceReserializeAssets(changed);
        AssetDatabase.SaveAssets();
        var after = paths.ToDictionary(path => path,
            path => Dps(AssetDatabase.LoadAssetAtPath<GameObject>(path), weapons));
        return new { reports, beforeDps = before, afterDps = after,
            damageMultipliers = multipliers.ToDictionary(p => p.Key.ToString(), p => p.Value) };
    }

    private static IEnumerable<IGrouping<int, WeaponHardPoint>> Channels(FighterHardPoint fighter, WeaponsData weapons) =>
        fighter.GetComponentsInChildren<WeaponHardPoint>(true).GroupBy(gun =>
            weapons.GetProfile(gun.WeaponType).Family is WeaponFamily.FighterLaser or
                WeaponFamily.InterceptorLaser or WeaponFamily.HeroLaser ? -1 : (int)gun.WeaponType);

    private static WeaponHardPoint Retained(FighterHardPoint fighter, IEnumerable<WeaponHardPoint> channel) =>
        channel.Contains(fighter.Gun) ? fighter.Gun : channel.First();

    private static float Dps(GameObject view, WeaponsData weapons) =>
        view.GetComponentsInChildren<WeaponHardPoint>(true).Sum(gun =>
        {
            WeaponDefinition definition = weapons.GetDefinition(gun.WeaponType);
            return WeaponProfileFactory.Create(definition, weapons.GetFamily(definition.Family),
                weapons.GetCaliber(definition.Caliber), weapons.GetMount(definition.Mount)).DamagePerSecond;
        });

    private static void Filter(SerializedProperty entries, HashSet<WeaponHardPoint> removed)
    {
        var retained = Enumerable.Range(0, entries.arraySize)
            .Select(i => entries.GetArrayElementAtIndex(i).objectReferenceValue)
            .Where(reference => !(reference is WeaponHardPoint gun) || !removed.Contains(gun)).ToArray();
        entries.arraySize = retained.Length;
        for (int i = 0; i < retained.Length; i++) entries.GetArrayElementAtIndex(i).objectReferenceValue = retained[i];
    }
}
