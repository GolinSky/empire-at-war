using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceDerivedData
    {
        public static string Preview(BalanceUnit unit, BalanceRegistration registry, BalanceDraft draft)
        {
            return string.Join(", ", unit.Components.Where(component => component.GetType().Name == "WeaponComponent")
                .SelectMany(component => BalancePrefabBindings.References(component, "hardPoints")).Cast<WeaponHardPoint>().Select(mount =>
                registry.Fields.Values.Single(field => field.Target == mount && field.Stat == "WeaponType"))
                .GroupBy(field => (WeaponType)int.Parse(field.DraftValue(draft))).OrderBy(group => group.Key)
                .Select(group => group.Key + " × " + group.Count()));
        }

        public static void Rebuild(BalanceUnit unit)
        {
            UnityEngine.GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(AssetDatabase.GetAssetPath(unit.Prefab));
            List<UnityEngine.Object> components = BalancePrefabBindings.Components(prefab).ToList();
            List<WeaponHardPoint> mounts = components.Where(component => component.GetType().Name == "WeaponComponent")
                .SelectMany(component => BalancePrefabBindings.References(component, "hardPoints")).Cast<WeaponHardPoint>().ToList();
            using (SerializedObject serialized = new SerializedObject(unit.Data))
            {
                SerializedProperty loadout = serialized.FindProperty("weaponLoadout");
                List<IGrouping<WeaponType, WeaponHardPoint>> groups = mounts.GroupBy(mount => mount.WeaponType).OrderBy(group => group.Key).ToList();
                loadout.arraySize = groups.Count;
                for (int i = 0; i < groups.Count; i++)
                {
                    SerializedProperty entry = loadout.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("weaponType").intValue = (int)groups[i].Key;
                    entry.FindPropertyRelative("count").intValue = groups[i].Count();
                }
                if (unit.Kind == "Squadron")
                {
                    UnityEngine.Object health = components.Single(component => component.GetType().Name == "SquadronHealthComponent");
                    serialized.FindProperty(BalanceRegistration.Auto("MemberCount")).intValue = BalancePrefabBindings.References(health, "fighters").Count;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(unit.Data);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
