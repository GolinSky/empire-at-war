using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using UnityEditor;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceAbilityAdapter
    {
        private static readonly Dictionary<Type, string[]> SETTINGS = new Dictionary<Type, string[]>
        {
            [typeof(AssaultSettings)] = new[] { "statModifier" },
            [typeof(BoostEnginePowerSettings)] = new[] { "statModifier" },
            [typeof(BoostShieldPowerSettings)] = new[] { "statModifier" },
            [typeof(BoostWeaponPowerSettings)] = new[] { "statModifier" },
            [typeof(InvulnerabilitySettings)] = new[] { "statModifier" },
            [typeof(LockSFoilsSettings)] = new[] { "statModifier" },
            [typeof(PowerToMainBatteriesSettings)] = new[] { "fireDelayMultiplier", "statModifier" },
            [typeof(CloakSettings)] = Array.Empty<string>(),
            [typeof(FullSalvoSettings)] = new[] { "projectileFireDelayMultiplier", "otherFireDelayMultiplier" },
            [typeof(CompositeBeamSettings)] = new[] { "damage" },
            [typeof(ProtonBeamSettings)] = new[] { "damage", "damageType" },
            [typeof(TractorBeamSettings)] = new[] { "speedMultiplier", "targetClasses", "beam" },
            [typeof(IonShotSettings)] = new[] { "disableDuration", "projectile" },
            [typeof(ConcentrateFireSettings)] = new[] { "allyStatModifier", "commandRadius", "targetDamageMultiplier" },
            [typeof(IonPulseSettings)] = new[] { "chargeDuration", "alignmentTimeout", "alignmentTolerance", "waveSpeed", "waveRadius", "waveThickness", "disableDuration", "malfunctionChance" }
        };

        public static void Register(BalanceRegistration registry)
        {
            ShipAbilityCatalog catalog = BalanceRegistration.One<ShipAbilityCatalog>();
            using (SerializedObject serialized = new SerializedObject(catalog))
            {
                List<SerializedProperty> definitions = BalanceRegistration.Keyed(serialized, "definitions.keyValue").ToList();
                foreach (SerializedProperty entry in definitions)
                {
                    int id = entry.FindPropertyRelative("key").intValue;
                    SerializedProperty value = entry.FindPropertyRelative("value");
                    string context = value.FindPropertyRelative("displayName").stringValue;
                    List<BalanceUnit> users = Users(registry, id);
                    string key = "ability/" + id + "/";
                    foreach (string field in new[] { "duration", "recoveryDelay", "range", "aiUse" })
                        registry.Add(catalog, key + field, value.propertyPath + "." + field, BalanceFieldGroup.Abilities, context, BalanceFieldOwner.SharedProfile, true, users,
                            field == "aiUse" ? typeof(ShipAbilityAiUse) : null);
                    SerializedProperty settings = value.FindPropertyRelative("settings");
                    if (settings.managedReferenceValue == null) throw new InvalidOperationException($"Missing settings: {context}");
                    Type settingsType = settings.managedReferenceValue.GetType();
                    if (!SETTINGS.TryGetValue(settingsType, out string[] allowed))
                        throw new InvalidOperationException($"Unapproved ability subtype: {settingsType.Name}");
                    // The type name is part of each field's preset schema, so it stays the dependency key.
                    string subtype = settingsType.Name;
                    // Reference IDs are used for this scan only. Durable identity is the sorted canonical ability ID set.
                    List<SerializedProperty> aliases = definitions.Where(other => other.FindPropertyRelative("value.settings").managedReferenceId == settings.managedReferenceId)
                        .OrderBy(other => other.FindPropertyRelative("key").intValue).ToList();
                    string aliasIds = string.Join("+", aliases.Select(other => other.FindPropertyRelative("key").intValue));
                    string warning = aliases.Count > 1 ? "Shared settings — " + string.Join(" + ", aliases.Select(other => other.FindPropertyRelative("value.displayName").stringValue)) : "";
                    List<BalanceUnit> aliasUsers = aliases.SelectMany(other => Users(registry, other.FindPropertyRelative("key").intValue)).Distinct().ToList();
                    foreach (string field in allowed)
                        RegisterSetting(registry, catalog, settings, "settings/" + aliasIds + "/", field, context, aliasUsers, warning, subtype);
                }
            }
            foreach (BalanceField field in registry.Fields.Values.Where(field => field.Group == BalanceFieldGroup.Abilities))
                foreach (BalanceUnit unit in field.Users) unit.Fields[field.Stat] = field.Key;
        }

        private static List<BalanceUnit> Users(BalanceRegistration registry, int id) => registry.Units.Where(unit =>
            unit.Fields.TryGetValue("Abilities", out string key) && registry.Fields[key].Read().Split(',').Contains(id.ToString())).ToList();

        private static void RegisterSetting(BalanceRegistration registry, ShipAbilityCatalog catalog, SerializedProperty settings,
            string key, string name, string context, List<BalanceUnit> users, string alias, string subtype)
        {
            SerializedProperty property = settings.FindPropertyRelative(name);
            if (name.EndsWith("tatModifier"))
            {
                foreach (string member in new[] { "damageMultiplier", "fireDelayMultiplier", "speedMultiplier", "shieldRegenMultiplier", "damageTakenMultiplier" })
                    registry.Add(catalog, key + name + "/" + member, property.propertyPath + "." + member, BalanceFieldGroup.Abilities, context, BalanceFieldOwner.SharedProfile, true, users,
                        positive: member == "fireDelayMultiplier", alias: alias, dependency: subtype);
            }
            else if (name == "beam" || name == "projectile")
                BalanceWeaponAdapter.Profile(registry, catalog, property, key + name + "/", BalanceFieldGroup.Abilities, context, users, alias, subtype);
            else
                registry.Add(catalog, key + name, property.propertyPath, BalanceFieldGroup.Abilities, context, BalanceFieldOwner.SharedProfile, true, users,
                    name == "damageType" ? typeof(EmpireAtWar.Components.AttackComponent.DamageType)
                    : name == "targetClasses" ? typeof(EmpireAtWar.Components.Ship.Health.ShipClass) : null,
                    maximum: name == "malfunctionChance" ? 1 : double.PositiveInfinity,
                    positive: name == "waveSpeed" || name.EndsWith("FireDelayMultiplier"), alias: alias, dependency: subtype);
        }
    }
}
