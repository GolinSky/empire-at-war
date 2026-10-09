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
            registry.AutoFields(economy, "", "", "IncomeDelay StartMoneyAmount", BalanceFieldGroup.GlobalData, "Economy", BalanceFieldOwner.SharedProfile, true, registry.Units);
            Object matchups = BalanceRegistration.One<ShipClassMatchupData>();
            registry.Add(matchups, "strongBonus", "strongBonus", BalanceFieldGroup.GlobalData, "Class production preference", BalanceFieldOwner.SharedProfile, true, registry.Units);
            registry.Add(matchups, "weakPenalty", "weakPenalty", BalanceFieldGroup.GlobalData, "Class production preference",
                BalanceFieldOwner.SharedProfile, true, registry.Units, maximum: 1);
            using (SerializedObject serialized = new SerializedObject(matchups))
                foreach (SerializedProperty row in BalanceRegistration.Elements(serialized.FindProperty("matchups")))
                {
                    int id = row.FindPropertyRelative("shipClass").intValue;
                    foreach (string name in new[] { "strongAgainst", "weakAgainst" })
                        registry.Add(matchups, "class/" + id + "/" + name, row.propertyPath + "." + name, BalanceFieldGroup.GlobalData,
                            ((ShipClass)id) + " production preference", BalanceFieldOwner.SharedProfile, true,
                            registry.Units.Where(unit => unit.Class == ((ShipClass)id).ToString()), typeof(ShipClass));
                }
            Matrix(registry);
            foreach (FactionDefinition faction in BalanceRegistration.One<FactionCatalog>().Factions) Research(registry, faction);
        }

        private static void Matrix(BalanceRegistration registry)
        {
            DamageMatrixData matrix = BalanceRegistration.One<DamageMatrixData>();
            registry.Add(matrix, "missSpread", "missSpread", BalanceFieldGroup.GlobalData, "Damage matrix", BalanceFieldOwner.SharedProfile, true, registry.Units);
            using (SerializedObject serialized = new SerializedObject(matrix))
                foreach (SerializedProperty row in BalanceRegistration.Elements(serialized.FindProperty("damageTypes")))
                {
                    int id = row.FindPropertyRelative("damageType").intValue;
                    string key = "matrix/" + id + "/";
                    string context = ((DamageType)id).ToString();
                    foreach (string name in new[] { "vsShield", "shieldPiercing" })
                        registry.Add(matrix, key + name, row.propertyPath + "." + name, BalanceFieldGroup.GlobalData, context, BalanceFieldOwner.SharedProfile, true, registry.Units);
                    foreach (string kind in new[] { "damage", "accuracy" })
                        foreach (ShipClass shipClass in Enum.GetValues(typeof(ShipClass)))
                        {
                            string member = char.ToLowerInvariant(shipClass.ToString()[0]) + shipClass.ToString().Substring(1);
                            registry.Add(matrix, key + kind + "/" + shipClass, row.propertyPath + "." + kind + "." + member,
                                BalanceFieldGroup.GlobalData, context + " → " + shipClass + " " + kind, BalanceFieldOwner.SharedProfile, true, registry.Units,
                                maximum: kind == "accuracy" ? 1 : double.PositiveInfinity, serializedTarget: serialized);
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
                            Name = faction.FactionType + " / " + (ResearchType)id + " / Tier " + level, Faction = faction.FactionType.ToString(), Kind = BalanceUnitKind.Research };
                        BalanceUnitAdapter.Economy(registry, faction, tier.propertyPath + ".factionData.", key, owner, false);
                        foreach (SerializedProperty effect in BalanceRegistration.Elements(tier.FindPropertyRelative("effects")))
                        {
                            int stat = effect.FindPropertyRelative("stat").intValue;
                            string classes = BalanceValue.Read(effect.FindPropertyRelative("shipClasses"), BalanceValueKind.EnumSet);
                            string effectKey = key + "effect/" + stat + "/" + string.Join("+", classes.Split(',').OrderBy(value => value)) + "/multiplier";
                            registry.Add(faction, effectKey, effect.propertyPath + ".multiplier", BalanceFieldGroup.GlobalData, owner.Name + " / " + (ResearchStat)stat,
                                BalanceFieldOwner.FactionEntry, false, new[] { owner }, positive: true, dependency: "classes:" + classes);
                        }
                    }
                }
        }
    }
}
