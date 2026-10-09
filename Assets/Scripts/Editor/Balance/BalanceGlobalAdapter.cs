using System;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.EnemyFaction.Data;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using UnityEditor;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceGlobalAdapter
    {
        public static void Register(BalanceRegistration registry)
        {
            BalanceStructureAdapter.Register(registry);
            Object economy = BalanceRegistration.One<EconomyData>();
            registry.AutoFields(economy, "", "", "IncomeDelay StartMoneyAmount", "Global Data", "Economy", "Shared Profile", true, registry.Units);
            Object matchups = BalanceRegistration.One<ShipClassMatchupData>();
            registry.Add(matchups, "strongBonus", "strongBonus", "Global Data", "Class production preference", "Shared Profile", true, registry.Units);
            registry.Add(matchups, "weakPenalty", "weakPenalty", "Global Data", "Class production preference", "Shared Profile", true, registry.Units, maximum: 1);
            using (SerializedObject serialized = new SerializedObject(matchups))
                foreach (SerializedProperty row in BalanceRegistration.Elements(serialized.FindProperty("matchups")))
                {
                    int id = row.FindPropertyRelative("shipClass").intValue;
                    foreach (string name in new[] { "strongAgainst", "weakAgainst" })
                        registry.Add(matchups, "class/" + id + "/" + name, row.propertyPath + "." + name, "Global Data",
                            ((ShipClass)id) + " production preference", "Shared Profile", true, registry.Units.Where(unit => unit.Class == ((ShipClass)id).ToString()), typeof(ShipClass));
                }
            Matrix(registry);
            foreach (FactionDefinition faction in BalanceRegistration.One<FactionCatalog>().Factions) Research(registry, faction);
        }

        private static void Matrix(BalanceRegistration registry)
        {
            DamageMatrixData matrix = BalanceRegistration.One<DamageMatrixData>();
            registry.Add(matrix, "missSpread", "missSpread", "Global Data", "Damage matrix", "Shared Profile", true, registry.Units);
            using (SerializedObject serialized = new SerializedObject(matrix))
                foreach (SerializedProperty row in BalanceRegistration.Elements(serialized.FindProperty("damageTypes")))
                {
                    int id = row.FindPropertyRelative("damageType").intValue;
                    string key = "matrix/" + id + "/";
                    string context = ((DamageType)id).ToString();
                    foreach (string name in new[] { "vsShield", "shieldPiercing" })
                        registry.Add(matrix, key + name, row.propertyPath + "." + name, "Global Data", context, "Shared Profile", true, registry.Units);
                    foreach (string kind in new[] { "damage", "accuracy" })
                        foreach (ShipClass shipClass in Enum.GetValues(typeof(ShipClass)))
                        {
                            string member = char.ToLowerInvariant(shipClass.ToString()[0]) + shipClass.ToString().Substring(1);
                            registry.Add(matrix, key + kind + "/" + shipClass, row.propertyPath + "." + kind + "." + member,
                                "Global Data", context + " → " + shipClass + " " + kind, "Shared Profile", true, registry.Units,
                                maximum: kind == "accuracy" ? 1 : double.PositiveInfinity);
                        }
                }
        }

        private static void Research(BalanceRegistration registry, FactionDefinition faction)
        {
            using (SerializedObject serialized = new SerializedObject(faction))
                foreach (SerializedProperty line in BalanceRegistration.Keyed(serialized, "research.keyValue"))
                {
                    int id = line.FindPropertyRelative("key").intValue;
                    SerializedProperty tiers = line.FindPropertyRelative("value.tiers");
                    for (int level = 1; level <= tiers.arraySize; level++)
                    {
                        SerializedProperty tier = tiers.GetArrayElementAtIndex(level - 1);
                        string key = "research/" + id + "/level/" + level + "/";
                        BalanceUnit owner = new BalanceUnit { Id = faction.FactionType + "/research/" + id + "/level/" + level,
                            Name = faction.FactionType + " / " + (ResearchType)id + " / Tier " + level, Faction = faction.FactionType.ToString(), Kind = "Research" };
                        BalanceUnitAdapter.Economy(registry, faction, tier.propertyPath + ".factionData.", key, owner, false);
                        foreach (SerializedProperty effect in BalanceRegistration.Elements(tier.FindPropertyRelative("effects")))
                        {
                            int stat = effect.FindPropertyRelative("stat").intValue;
                            string classes = BalanceValue.Read(effect.FindPropertyRelative("shipClasses"), BalanceValueKind.EnumSet);
                            string effectKey = key + "effect/" + stat + "/" + string.Join("+", classes.Split(',').OrderBy(value => value)) + "/multiplier";
                            registry.Add(faction, effectKey, effect.propertyPath + ".multiplier", "Global Data", owner.Name + " / " + (ResearchStat)stat,
                                "Faction Entry", false, new[] { owner }, positive: true, dependency: "classes:" + classes);
                        }
                    }
                }
        }
    }
}
