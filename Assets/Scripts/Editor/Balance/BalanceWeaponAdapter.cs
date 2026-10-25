using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceWeaponAdapter
    {
        // Every weapon definition registers this stat; its presence means the weapon exists.
        public const string DEFINITION_MARKER = "damageMultiplier";

        private static readonly string[] CLASSES =
            { "fighter", "bomber", "interceptor", "corvette", "frigate", "cruiser", "capital", "heavyCapital", "structure" };

        public static void Register(BalanceRegistration registry)
        {
            WeaponsData weapons = BalanceRegistration.One<WeaponsData>();
            using (SerializedObject serialized = new SerializedObject(weapons))
            {
                Dictionary<WeaponType, List<BalanceUnit>> users = weapons.Weapons.ToDictionary(definition => definition.WeaponType,
                    definition => registry.Units.Where(unit => unit.Mounts.OfType<WeaponHardPoint>()
                        .Any(mount => mount.WeaponType == definition.WeaponType)).ToList());
                RegisterDefinitions(registry, weapons, serialized, users);
                RegisterFamilies(registry, weapons, serialized, users);
                RegisterCalibers(registry, weapons, serialized, users);
            }
        }

        // Inline profiles of abilities and super weapons are edited field by field.
        public static void Profile(BalanceRegistration registry, Object target, SerializedProperty profile, string key,
            BalanceFieldGroup group, string context, List<BalanceUnit> users, string alias = "", string dependency = "")
        {
            foreach (string name in new[] { "damageType", "damage", "shotsPerSalvo", "shotInterval", "reload", "range", "strikecraftOnly", "interceptable", "projectileSpeed" })
            {
                BalanceField field = registry.Add(target, key + name, profile.propertyPath + "." + name, group, context, BalanceFieldOwner.SharedProfile, true, users,
                    name == "damageType" ? typeof(DamageType) : null, minimum: name == "shotsPerSalvo" ? 1 : 0,
                    alias: alias, dependency: dependency, serializedTarget: profile.serializedObject);
                if (name == "range") field.Label = "Profile range";
            }
        }

        private static void RegisterDefinitions(BalanceRegistration registry, WeaponsData weapons, SerializedObject serialized,
            Dictionary<WeaponType, List<BalanceUnit>> users)
        {
            HashSet<int> ids = new HashSet<int>();
            foreach (SerializedProperty definition in BalanceRegistration.Elements(serialized.FindProperty("weapons")))
            {
                int id = definition.FindPropertyRelative("weaponType").intValue;
                if (!ids.Add(id)) throw new InvalidOperationException($"Duplicate weapon ID: {id}");
                List<BalanceUnit> consumers = users[(WeaponType)id];
                string key = "weapon/" + id + "/";
                string context = definition.FindPropertyRelative("displayName").stringValue;
                Add(registry, weapons, serialized, definition, key, context, consumers, "family", typeof(WeaponFamily));
                Add(registry, weapons, serialized, definition, key, context, consumers, "caliber", typeof(WeaponCaliber));
                Add(registry, weapons, serialized, definition, key, context, consumers, "mount", typeof(WeaponMount));
                Add(registry, weapons, serialized, definition, key, context, consumers, DEFINITION_MARKER);
                Add(registry, weapons, serialized, definition, key, context, consumers, "reloadMultiplier", positive: true);
                Add(registry, weapons, serialized, definition, key, context, consumers, "rangeMultiplier");
                // A linked shared definition cell appears alongside each consuming unit in Compare.
                foreach (BalanceField field in registry.FieldsFor(weapons).Where(field => field.Stat.StartsWith(key)))
                    foreach (BalanceUnit unit in consumers) unit.Fields[field.Stat] = field.Key;
            }
        }

        private static void RegisterFamilies(BalanceRegistration registry, WeaponsData weapons, SerializedObject serialized,
            Dictionary<WeaponType, List<BalanceUnit>> users)
        {
            foreach (SerializedProperty rule in BalanceRegistration.Elements(serialized.FindProperty("families")))
            {
                WeaponFamily family = (WeaponFamily)rule.FindPropertyRelative("family").intValue;
                List<BalanceUnit> consumers = Users(weapons, users, definition => definition.Family == family);
                string key = "weaponfamily/" + family + "/";
                string context = ObjectNames.NicifyVariableName(family.ToString()) + " family";
                Add(registry, weapons, serialized, rule, key, context, consumers, "damageType", typeof(DamageType));
                Add(registry, weapons, serialized, rule, key, context, consumers, "damage");
                Add(registry, weapons, serialized, rule, key, context, consumers, "shotsPerSalvo", minimum: 1);
                Add(registry, weapons, serialized, rule, key, context, consumers, "shotInterval");
                Add(registry, weapons, serialized, rule, key, context, consumers, "reload", positive: true);
                Add(registry, weapons, serialized, rule, key, context, consumers, "range");
                Add(registry, weapons, serialized, rule, key, context, consumers, "strikecraftOnly");
                Add(registry, weapons, serialized, rule, key, context, consumers, "interceptable");
                Add(registry, weapons, serialized, rule, key, context, consumers, "targetSizeBias");
            }
        }

        private static void RegisterCalibers(BalanceRegistration registry, WeaponsData weapons, SerializedObject serialized,
            Dictionary<WeaponType, List<BalanceUnit>> users)
        {
            foreach (SerializedProperty rule in BalanceRegistration.Elements(serialized.FindProperty("calibers")))
            {
                WeaponCaliber caliber = (WeaponCaliber)rule.FindPropertyRelative("caliber").intValue;
                List<BalanceUnit> consumers = Users(weapons, users, definition => definition.Caliber == caliber);
                string key = "weaponcaliber/" + caliber + "/";
                string context = caliber + " caliber";
                Add(registry, weapons, serialized, rule, key, context, consumers, "damageMultiplier");
                Add(registry, weapons, serialized, rule, key, context, consumers, "rangeMultiplier");
                foreach (string shipClass in CLASSES)
                {
                    Add(registry, weapons, serialized, rule, key, context, consumers, "damageBias." + shipClass);
                    Add(registry, weapons, serialized, rule, key, context, consumers, "accuracyBias." + shipClass);
                }
            }
        }

        private static List<BalanceUnit> Users(WeaponsData weapons, Dictionary<WeaponType, List<BalanceUnit>> users,
            Func<WeaponDefinition, bool> predicate) =>
            weapons.Weapons.Where(predicate).SelectMany(definition => users[definition.WeaponType])
                .GroupBy(unit => unit.Id).Select(group => group.First()).ToList();

        private static void Add(BalanceRegistration registry, WeaponsData weapons, SerializedObject serialized,
            SerializedProperty owner, string key, string context, List<BalanceUnit> users, string name, Type enumType = null,
            double minimum = 0, bool positive = false)
        {
            registry.Add(weapons, key + name, owner.propertyPath + "." + name, BalanceFieldGroup.Weapons, context,
                BalanceFieldOwner.SharedProfile, true, users, enumType, minimum, positive: positive, serializedTarget: serialized);
        }
    }
}
