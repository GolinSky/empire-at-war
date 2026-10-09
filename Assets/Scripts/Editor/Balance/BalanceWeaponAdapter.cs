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
        public static void Register(BalanceRegistration registry)
        {
            WeaponsData weapons = BalanceRegistration.One<WeaponsData>();
            using (SerializedObject serialized = new SerializedObject(weapons))
            {
                HashSet<int> ids = new HashSet<int>();
                foreach (SerializedProperty profile in BalanceRegistration.Elements(serialized.FindProperty("weapons")))
                {
                    int id = profile.FindPropertyRelative("weaponType").intValue;
                    if (!ids.Add(id)) throw new System.InvalidOperationException($"Duplicate weapon ID: {id}");
                    List<BalanceUnit> users = registry.Units.Where(unit => unit.Mounts.OfType<WeaponHardPoint>().Any(mount => (int)mount.WeaponType == id)).ToList();
                    Profile(registry, weapons, profile, "weapon/" + id + "/", "Weapons", profile.FindPropertyRelative("displayName").stringValue, users);
                    // A linked shared profile cell appears alongside each consuming unit in Compare.
                    foreach (BalanceField field in registry.Fields.Values.Where(field => field.Target == weapons && field.Stat.StartsWith("weapon/" + id + "/")))
                        foreach (BalanceUnit unit in users) unit.Fields[field.Stat] = field.Key;
                }
            }
        }

        public static void Profile(BalanceRegistration registry, Object target, SerializedProperty profile, string key,
            string group, string context, List<BalanceUnit> users, string alias = "", string dependency = "")
        {
            foreach (string name in new[] { "damageType", "damage", "shotsPerSalvo", "shotInterval", "reload", "range", "strikecraftOnly", "interceptable", "projectileSpeed" })
            {
                BalanceField field = registry.Add(target, key + name, profile.propertyPath + "." + name, group, context, "Shared Profile", true, users,
                    name == "damageType" ? typeof(DamageType) : null, minimum: name == "shotsPerSalvo" ? 1 : 0,
                    positive: name == "reload" && target is WeaponsData, alias: alias, dependency: dependency);
                if (name == "range") field.Label = "Profile range (ordinary units use Unit Data fire range)";
            }
        }
    }
}
