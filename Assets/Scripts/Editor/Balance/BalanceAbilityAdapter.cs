using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
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
            [typeof(BoostWeaponPowerSettings)] = new[] { "statModifier", "shotIntervalMultiplier" },
            [typeof(InvulnerabilitySettings)] = new[] { "statModifier" },
            [typeof(LockSFoilsSettings)] = new[] { "statModifier" },
            [typeof(PowerToMainBatteriesSettings)] = new[] { "fireDelayMultiplier", "statModifier" },
            [typeof(CloakSettings)] = Array.Empty<string>(),
            [typeof(SensorJammingSettings)] = Array.Empty<string>(),
            [typeof(FighterReinforcementsSettings)] = Array.Empty<string>(),
            [typeof(RapidDeploymentSettings)] = Array.Empty<string>(),
            [typeof(TorpedoSalvoSettings)] = new[] { "fireDelayMultiplier" },
            [typeof(FighterCoordinationSettings)] = new[] { "commandRadius" },
            [typeof(FullSalvoSettings)] = new[] { "projectileFireDelayMultiplier", "otherFireDelayMultiplier" },
            [typeof(CompositeBeamSettings)] = new[] { "damage" },
            [typeof(HarmonicBombSettings)] = new[] { "radius", "minimumTargets" },
            [typeof(ProtonBeamSettings)] = new[] { "damage", "damageType" },
            [typeof(TractorBeamSettings)] = new[] { "speedMultiplier", "targetClasses", "beam" },
            [typeof(IonShotSettings)] = new[] { "disableDuration", "projectile" },
            [typeof(ConcentrateFireSettings)] = new[] { "allyStatModifier", "commandRadius", "targetDamageMultiplier" },
            [typeof(IonPulseSettings)] = new[] { "alignmentTolerance", "waveSpeed", "waveRadius",
                "waveThickness", "disableDuration" }
        };

        public static void Register(BalanceRegistration registry)
        {
            ShipAbilityCatalog catalog = BalanceRegistration.One<ShipAbilityCatalog>();
            var usersByAbility = registry.Units.Where(unit => unit.Fields.ContainsKey("Abilities"))
                .SelectMany(unit => registry.Fields[unit.Fields["Abilities"]].Read().Split(',')
                    .Where(id => id.Length != 0).Distinct().Select(id => (Id: id, Unit: unit)))
                .ToLookup(entry => entry.Id, entry => entry.Unit);
            using (SerializedObject serialized = new SerializedObject(catalog))
            {
                List<SerializedProperty> definitions = BalanceRegistration.Keyed(serialized, "definitions.keyValue").ToList();
                foreach (SerializedProperty entry in definitions)
                {
                    int id = entry.FindPropertyRelative("key").intValue;
                    SerializedProperty value = entry.FindPropertyRelative("value");
                    string context = value.FindPropertyRelative("displayName").stringValue;
                    IEnumerable<BalanceUnit> users = usersByAbility[id.ToString()];
                    string key = "ability/" + id + "/";
                    foreach (string field in new[] { "duration", "recoveryDelay", "range", "aiUse" })
                        registry.Add(catalog, key + field, value.propertyPath + "." + field, BalanceFieldGroup.Abilities, context, BalanceFieldOwner.SharedProfile, true, users,
                            field == "aiUse" ? typeof(ShipAbilityAiUse) : null, serializedTarget: serialized);
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
                    IEnumerable<string> aliasNames = aliases.Select(other => other.FindPropertyRelative("value.displayName").stringValue);
                    string warning = aliases.Count > 1 ? "Shared settings — " + string.Join(" + ", aliasNames) : "";
                    List<BalanceUnit> aliasUsers = aliases.SelectMany(other => usersByAbility[other.FindPropertyRelative("key").intValue.ToString()]).Distinct().ToList();
                    foreach (string field in allowed)
                        RegisterSetting(registry, catalog, settings, "settings/" + aliasIds + "/", field, context, aliasUsers, warning, subtype);
                }
            }
            foreach (BalanceField field in registry.Fields.Values.Where(field => field.Group == BalanceFieldGroup.Abilities))
                foreach (BalanceUnit unit in field.Users) unit.Fields[field.Stat] = field.Key;
        }

        private static void RegisterSetting(BalanceRegistration registry, ShipAbilityCatalog catalog, SerializedProperty settings,
            string key, string name, string context, List<BalanceUnit> users, string alias, string subtype)
        {
            SerializedProperty property = settings.FindPropertyRelative(name);
            if (name.EndsWith("tatModifier"))
            {
                foreach (string member in new[] { "damageMultiplier", "fireDelayMultiplier", "speedMultiplier", "shieldRegenMultiplier", "damageTakenMultiplier", "shotIntervalMultiplier" })
                    registry.Add(catalog, key + name + "/" + member, property.propertyPath + "." + member, BalanceFieldGroup.Abilities, context, BalanceFieldOwner.SharedProfile, true, users,
                        positive: member is "fireDelayMultiplier" or "shotIntervalMultiplier", alias: alias, dependency: subtype, serializedTarget: settings.serializedObject);
            }
            else if (name == "beam" || name == "projectile")
                BalanceWeaponAdapter.Profile(registry, catalog, property, key + name + "/", BalanceFieldGroup.Abilities, context, users, alias, subtype);
            else
                registry.Add(catalog, key + name, property.propertyPath, BalanceFieldGroup.Abilities, context, BalanceFieldOwner.SharedProfile, true, users,
                    name == "damageType" ? typeof(DamageType)
                    : name == "targetClasses" ? typeof(ShipClass) : null,
                    minimum: name == "minimumTargets" ? 1 : 0, positive: name == "radius" || name == "waveSpeed" || name == "shotIntervalMultiplier" || name.EndsWith("FireDelayMultiplier"),
                    alias: alias, dependency: subtype, serializedTarget: settings.serializedObject);
        }
    }
}
