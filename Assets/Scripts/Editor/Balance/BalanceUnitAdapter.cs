using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Repository.Data;
using EmpireAtWar.Services.ShipAbilities;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceUnitAdapter
    {
        public static void Register(BalanceRegistration registry)
        {
            FactionCatalog catalog = BalanceRegistration.One<FactionCatalog>();
            ShipsData ships = BalanceRegistration.One<ShipsData>();
            AssetMappingData mapping = BalanceRegistration.One<AssetMappingData>();
            foreach (FactionDefinition faction in catalog.Factions)
            {
                using (SerializedObject serialized = new SerializedObject(faction))
                {
                    RegisterRoster(registry, faction, serialized, "ships", typeof(ShipType), id =>
                        AssetDatabase.LoadAssetAtPath<ShipData>(AssetDatabase.GUIDToAssetPath(ships.GetShipDataPath((ShipType)id))), mapping);
                    RegisterRoster(registry, faction, serialized, "squadrons", typeof(SquadronType), id =>
                        BalanceAssetResolver.Load<SquadronData>(mapping.GetAssetKey(((SquadronType)id) + "SquadronData")), mapping);
                }
            }
        }

        private static void RegisterRoster(BalanceRegistration registry, FactionDefinition faction, SerializedObject serialized,
            string roster, Type idType, Func<int, Object> resolve, AssetMappingData mapping)
        {
            foreach (SerializedProperty entry in BalanceRegistration.Keyed(serialized, roster + ".keyValue"))
            {
                int id = entry.FindPropertyRelative("key").intValue;
                if (!Enum.IsDefined(idType, id)) throw new InvalidOperationException($"Unknown roster ID: {faction.name}/{id}");
                Object data = resolve(id);
                if (data == null) throw new MissingReferenceException($"Unmapped roster data: {faction.name}/{roster}/{id}");
                string kind = roster == "ships" ? "Ship" : "Squadron";
                string prefabKey = Enum.GetName(idType, id) + kind + "View";
                GameObject prefab = BalanceAssetResolver.Load<GameObject>(mapping.GetAssetKey(prefabKey));
                if (prefab == null) throw new MissingReferenceException($"Unmapped prefab: {prefabKey}");
                SerializedProperty value = entry.FindPropertyRelative("value");
                BalanceUnit unit = new BalanceUnit
                {
                    Id = faction.FactionType + "/" + roster + "/" + id,
                    Name = value.FindPropertyRelative(BalanceRegistration.Auto("Name")).stringValue,
                    Icon = (Sprite)value.FindPropertyRelative(BalanceRegistration.Auto("Icon")).objectReferenceValue,
                    Faction = faction.FactionType.ToString(), Kind = kind, Data = data, Prefab = prefab,
                    Class = data is ShipData ship ? ship.ShipClass.ToString() : ((SquadronData)data).ShipClass.ToString()
                };
                registry.Units.Add(unit);
                Economy(registry, faction, value.propertyPath + ".", roster + "/" + id + "/", unit, false);
                RegisterData(registry, unit);
                string topology = BalancePrefabBindings.Register(registry, unit);
                foreach (BalanceField field in registry.FieldsFor(data))
                    field.Dependency = topology;
            }
        }

        public static void Economy(BalanceRegistration registry, Object target, string path, string key, BalanceUnit unit, bool shared)
        {
            using (SerializedObject serialized = new SerializedObject(target))
            foreach (string name in new[] { "Price", "BuildTime", "UnitCapacity", "MaxCount", "AvailableLevel" })
            {
                BalanceField field = registry.Add(target, key + name, path + BalanceRegistration.Auto(name), "Economy", unit.Name,
                    shared ? "Shared Profile" : "Faction Entry", shared, new[] { unit }, serializedTarget: serialized);
                unit.Fields[name] = field.Key;
            }
        }

        private static void RegisterData(BalanceRegistration registry, BalanceUnit unit)
        {
            Object data = unit.Data;
            bool ship = data is ShipData;
            string combat = ship ? "Hull Shields ShieldRegenerateValue ShieldRegenerateDelay Range Delay"
                : "MemberHull MemberShields ShieldRegenerateValue ShieldRegenerateDelay LaserShieldDamageMultiplier HullRepairPerSecond Range Delay WeaponRange VisionRange";
            string movement = ship ? "Speed RotationSpeed TurnAcceleration HyperSpaceDuration BodyRotationMaxAngle NavigationRadius MinMoveCoefficient"
                : "CruiseSpeed CombatSpeed Acceleration TurnRate MaxBankAngle BankResponse Height FormationSpacing LoiterRadius NavigationRadius GuardRadius BreakDistance ExtendDistance";
            registry.AutoFields(data, "", "", combat, "Combat", unit.Name, "Unit Data", false, new[] { unit });
            registry.AutoFields(data, "", "", movement, "Movement", unit.Name, "Unit Data", false, new[] { unit });
            registry.AutoFields(data, "", "", ship ? "HangarInitialDelay HangarLaunchInterval" : "SeekerWarheadRange SeekerWarheadRecharge SeekerWarheadMinimumTravel", "Hangar", unit.Name, "Unit Data", false, new[] { unit });
            registry.Add(data, "Abilities", ship ? "abilities" : BalanceRegistration.Auto("Abilities"), "Advanced", unit.Name, "Unit Data", false, new[] { unit }, typeof(ShipAbilityId));
            if (ship)
            {
                registry.Add(data, "HeightTier", BalanceRegistration.Auto("HeightTier"), "Movement", unit.Name, "Unit Data", false, new[] { unit }, typeof(ShipHeightTier));
                using (SerializedObject serialized = new SerializedObject(data))
                {
                    Health(registry, data, serialized.FindProperty("hardPointHealth"), "hardPointHealth", unit);
                    Hangar(registry, data, serialized.FindProperty("hangarBays"), "hangarBays", unit);
                }
            }
            foreach (BalanceField field in registry.FieldsFor(data)) unit.Fields[field.Stat] = field.Key;
        }

        public static void Health(BalanceRegistration registry, Object data, SerializedProperty entries, string key, BalanceUnit unit)
        {
            HashSet<int> types = new HashSet<int>();
            foreach (SerializedProperty entry in BalanceRegistration.Elements(entries))
            {
                int id = entry.FindPropertyRelative("hardPointType").intValue;
                if (!types.Add(id)) throw new InvalidOperationException($"Duplicate hardpoint health type: {data.name}/{id}");
                foreach (string name in new[] { "health", "hullDamageMultiplier" })
                    registry.Add(data, key + "/" + id + "/" + name, entry.propertyPath + "." + name,
                        "Hardpoints", unit.Name + " / " + ((EmpireAtWar.Components.Ship.Health.HardPointType)id), "Unit Data", unit.Kind == "Structure", new[] { unit }, positive: name == "health");
            }
        }

        public static void Hangar(BalanceRegistration registry, Object data, SerializedProperty entries, string key, BalanceUnit unit)
        {
            HashSet<int> ids = new HashSet<int>();
            foreach (SerializedProperty entry in BalanceRegistration.Elements(entries))
            {
                int id = entry.FindPropertyRelative("squadronType").intValue;
                if (!ids.Add(id)) throw new InvalidOperationException($"Ambiguous hangar bay identity: {data.name}/{id}");
                foreach (string name in new[] { "reserve", "maxActive" })
                    registry.Add(data, key + "/" + id + "/" + name, entry.propertyPath + "." + name,
                        "Hangar", unit.Name + " / " + ((SquadronType)id), "Unit Data", unit.Kind == "Structure", new[] { unit });
            }
        }
    }
}
