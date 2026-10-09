using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Repository.Data;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceStructureAdapter
    {
        public static void Register(BalanceRegistration registry)
        {
            AssetMappingData mapping = BalanceRegistration.One<AssetMappingData>();
            foreach (FactionDefinition faction in BalanceRegistration.One<FactionCatalog>().Factions)
            {
                Structure(registry, faction, BalanceRegistration.One<SpaceStationData>(), "Station", "Space station", faction.FactionType + "SpaceStationView", mapping);
                foreach (MiningFacilityType id in faction.MiningFacilities)
                    Buildable(registry, faction, BalanceRegistration.One<MiningFacilityCatalog>(), (int)id, BalanceAssetResolver.Load<MiningFacilityData>(mapping.GetAssetKey(nameof(MiningFacilityData))), "Mining facility", "MiningFacilityView", mapping);
                foreach (DefendPlatformType id in faction.DefendPlatforms)
                    Buildable(registry, faction, BalanceRegistration.One<DefendPlatformCatalog>(), (int)id, BalanceAssetResolver.Load<DefendPlatformData>(mapping.GetAssetKey(nameof(DefendPlatformData))), "Defense platform", "DefendPlatformView", mapping);
                foreach (SuperWeaponType id in faction.SuperWeapons)
                    Buildable(registry, faction, BalanceRegistration.One<SuperWeaponCatalog>(), (int)id, BalanceRegistration.One<SuperWeaponData>(), "Super weapon", "", mapping);
            }
            StationLevels(registry);
            SuperWeapons(registry);
        }

        private static BalanceUnit Structure(BalanceRegistration registry, FactionDefinition faction, Object data, string id,
            string name, string prefabKey, AssetMappingData mapping)
        {
            BalanceUnit unit = new BalanceUnit { Id = faction.FactionType + "/" + id, Name = name, Faction = faction.FactionType.ToString(), Class = "Structure", Kind = "Structure", Data = data };
            registry.Units.Add(unit);
            if (prefabKey.Length != 0)
            {
                unit.Prefab = BalanceAssetResolver.Load<GameObject>(mapping.GetAssetKey(prefabKey));
                if (unit.Prefab == null) throw new MissingReferenceException($"Unmapped structure prefab: {prefabKey}");
                BalancePrefabBindings.Register(registry, unit);
                string prefix = BalanceRegistration.Auto("ComponentData") + ".";
                registry.AutoFields(data, prefix, "", "Hull Shields ShieldRegenerateValue ShieldRegenerateDelay Range Delay WeaponRange VisionRange SpawnBlockRadius",
                    "Combat", name, "Shared Profile", true, new[] { unit });
                using (SerializedObject serialized = new SerializedObject(data))
                    BalanceUnitAdapter.Health(registry, data, serialized.FindProperty(prefix + "hardPointHealth"), "hardPointHealth", unit);
            }
            if (data is MiningFacilityData)
                registry.AutoFields(data, "", "", "Income", "Global Data", name, "Shared Profile", true, new[] { unit });
            foreach (BalanceField field in registry.Fields.Values.Where(field => field.Target == data)) unit.Fields[field.Stat] = field.Key;
            return unit;
        }

        private static void Buildable(BalanceRegistration registry, FactionDefinition faction, Object catalog, int id,
            Object data, string name, string prefabKey, AssetMappingData mapping)
        {
            using (SerializedObject serialized = new SerializedObject(catalog))
            {
                SerializedProperty entry = BalanceRegistration.Keyed(serialized, "entries.keyValue").Single(row => row.FindPropertyRelative("key").intValue == id);
                SerializedProperty value = entry.FindPropertyRelative("value");
                BalanceUnit unit = Structure(registry, faction, data, catalog.GetType().Name + "/" + id,
                    value.FindPropertyRelative(BalanceRegistration.Auto("Name")).stringValue, prefabKey, mapping);
                unit.Icon = (Sprite)value.FindPropertyRelative(BalanceRegistration.Auto("Icon")).objectReferenceValue;
                BalanceUnitAdapter.Economy(registry, catalog, value.propertyPath + ".", "entry/" + id + "/", unit, true);
            }
        }

        private static void StationLevels(BalanceRegistration registry)
        {
            Object levels = BalanceRegistration.One<StationLevelData>();
            SpaceStationData station = BalanceRegistration.One<SpaceStationData>();
            List<BalanceUnit> users = registry.Units.Where(unit => unit.Data == station).ToList();
            foreach (BalanceUnit unit in users)
                registry.AutoFields(station, "", "", "HangarInitialDelay HangarLaunchInterval", "Hangar", "Station hangar", "Shared Profile", true, new[] { unit });
            using (SerializedObject serialized = new SerializedObject(levels))
            {
                SerializedProperty entries = serialized.FindProperty("levels");
                for (int level = 1; level <= entries.arraySize; level++)
                    foreach (string name in new[] { "Price", "BuildTime", "UnitCapacity", "MaxCount", "AvailableLevel" })
                        registry.Add(levels, "level/" + level + "/" + name, entries.GetArrayElementAtIndex(level - 1).propertyPath + "." + BalanceRegistration.Auto(name),
                            "Global Data", "Station upgrade " + level, "Shared Profile", true, users);
            }
            using (SerializedObject serialized = new SerializedObject(station))
            {
                SerializedProperty stats = serialized.FindProperty("levelStats");
                for (int level = 1; level <= stats.arraySize; level++)
                    foreach (string name in new[] { "hullMultiplier", "shieldsMultiplier", "shieldRegenerateMultiplier" })
                        registry.Add(station, "level/" + level + "/" + name, stats.GetArrayElementAtIndex(level - 1).propertyPath + "." + name,
                            "Global Data", "Station level " + level, "Shared Profile", true, users, positive: true);
                foreach (SerializedProperty bay in BalanceRegistration.Keyed(serialized, "hangarBays.keyValue"))
                {
                    int faction = bay.FindPropertyRelative("key").intValue;
                    foreach (string name in new[] { "reserve", "maxActive" })
                        registry.Add(station, "hangar/" + faction + "/" + name, bay.propertyPath + ".value." + name, "Hangar", "Station " + (FactionType)faction,
                            "Faction Entry", false, users.Where(unit => unit.Faction == ((FactionType)faction).ToString()));
                }
            }
        }

        private static void SuperWeapons(BalanceRegistration registry)
        {
            SuperWeaponData data = BalanceRegistration.One<SuperWeaponData>();
            using (SerializedObject serialized = new SerializedObject(data))
                foreach (SerializedProperty entry in BalanceRegistration.Keyed(serialized, "profiles.keyValue"))
                {
                    int id = entry.FindPropertyRelative("key").intValue;
                    List<BalanceUnit> users = registry.Units.Where(unit => unit.Id.EndsWith("SuperWeaponCatalog/" + id)).ToList();
                    string key = "super/" + id + "/";
                    SerializedProperty value = entry.FindPropertyRelative("value");
                    BalanceWeaponAdapter.Profile(registry, data, value.FindPropertyRelative("weapon"), key + "weapon/", "Weapons", ((SuperWeaponType)id).ToString(), users);
                    foreach (string name in new[] { "firingDelay", "stunDuration", "areaDamage", "areaRadius" })
                        registry.Add(data, key + name, value.propertyPath + "." + name, "Global Data", ((SuperWeaponType)id).ToString(), "Shared Profile", true, users);
                }
        }
    }
}
