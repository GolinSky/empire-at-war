using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalancePrefabBindings
    {
        // Inspect serialized component tables, then follow the same explicit mount lists used by gameplay.
        public static IEnumerable<Object> Components(GameObject root)
        {
            using (SerializedObject serialized = new SerializedObject(root))
                foreach (SerializedProperty entry in BalanceRegistration.Elements(serialized.FindProperty("m_Component")))
                {
                    Object component = entry.FindPropertyRelative("component").objectReferenceValue;
                    if (component == null) throw new MissingReferenceException($"Missing component in {root.name}.");
                    yield return component;
                }
            foreach (Transform child in root.transform)
                foreach (Object component in Components(child.gameObject)) yield return component;
        }

        public static List<Object> References(Object component, string property)
        {
            using (SerializedObject serialized = new SerializedObject(component))
                return BalanceRegistration.Elements(serialized.FindProperty(property)).Select(entry =>
                    entry.objectReferenceValue != null ? entry.objectReferenceValue
                    : throw new MissingReferenceException($"Empty binding {component.name}/{property}.")).ToList();
        }

        public static string Topology(BalanceUnit unit)
        {
            string topology = string.Join(";", unit.Components.Select(component =>
                GlobalObjectId.GetGlobalObjectIdSlow(component).ToString() + (component is Transform transform
                    ? "/parent:" + (transform.parent != null ? GlobalObjectId.GetGlobalObjectIdSlow(transform.parent).ToString() : "root") : "")).OrderBy(id => id)) + "|mounts:"
                + string.Join(";", unit.Mounts.Select(mount => GlobalObjectId.GetGlobalObjectIdSlow(mount).ToString()).OrderBy(id => id));
            using (SHA256 hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(topology)));
        }

        public static string Register(BalanceRegistration registry, BalanceUnit unit)
        {
            unit.Components.AddRange(Components(unit.Prefab));
            foreach (Object health in unit.Components.Where(component => component is HealthComponent))
                unit.Mounts.AddRange(References(health, BalanceRegistration.Auto("ShipUnits")));
            foreach (Object weapons in unit.Components.Where(component => component is WeaponComponent))
                unit.Mounts.AddRange(References(weapons, "hardPoints"));
            string topology = Topology(unit);
            foreach (Object mount in unit.Mounts.Distinct())
            {
                string context = unit.Name + " / " + ((Component)mount).gameObject.name;
                registry.Add(mount, "unlockLevel", "unlockLevel", BalanceFieldGroup.Hardpoints, context, BalanceFieldOwner.PrefabOverride, false, new[] { unit }, minimum: 1, dependency: topology);
                if (mount is WeaponHardPoint)
                {
                    registry.Add(mount, "WeaponType", BalanceRegistration.Auto("WeaponType"), BalanceFieldGroup.Hardpoints, context, BalanceFieldOwner.PrefabOverride, false, new[] { unit }, typeof(WeaponType), dependency: topology);
                    registry.Add(mount, "MinYaw", "yAxisRange." + BalanceRegistration.Auto("Min"), BalanceFieldGroup.Hardpoints, context, BalanceFieldOwner.PrefabOverride, false, new[] { unit }, minimum: double.NegativeInfinity, dependency: topology);
                    registry.Add(mount, "MaxYaw", "yAxisRange." + BalanceRegistration.Auto("Max"), BalanceFieldGroup.Hardpoints, context, BalanceFieldOwner.PrefabOverride, false, new[] { unit }, minimum: double.NegativeInfinity, dependency: topology);
                    registry.Add(mount, "mainBattery", "mainBattery", BalanceFieldGroup.Hardpoints, context, BalanceFieldOwner.PrefabOverride, false, new[] { unit }, dependency: topology);
                }
                if (mount is MissileInterceptorHardPoint)
                {
                    foreach (string name in new[] { "range", "reload", "interceptChance" })
                        registry.Add(mount, name, name, BalanceFieldGroup.Hardpoints, context, BalanceFieldOwner.PrefabOverride, false, new[] { unit }, positive: name == "reload",
                            maximum: name == "interceptChance" ? 1 : double.PositiveInfinity, dependency: topology);
                }
                foreach (BalanceField field in registry.FieldsFor(mount))
                    unit.Fields["mount/" + field.Key] = field.Key;
            }
            return topology;
        }

        public static Object InContents(Object persistent, GameObject contents)
        {
            Component component = (Component)persistent;
            Transform owner = component.transform;
            // Resolve positions from the current verified asset for this operation only.
            // Drafts and presets retain persistent object IDs, never hierarchy/array positions.
            List<int> childSlots = new List<int>();
            while (owner.parent != null)
            {
                childSlots.Add(owner.GetSiblingIndex());
                owner = owner.parent;
            }
            Transform target = contents.transform;
            foreach (int slot in childSlots.AsEnumerable().Reverse()) target = target.GetChild(slot);
            using (SerializedObject assetObject = new SerializedObject(component.gameObject))
            using (SerializedObject loadedObject = new SerializedObject(target.gameObject))
            {
                SerializedProperty assetTable = assetObject.FindProperty("m_Component");
                SerializedProperty loadedTable = loadedObject.FindProperty("m_Component");
                for (int i = 0; i < assetTable.arraySize; i++)
                    if (assetTable.GetArrayElementAtIndex(i).FindPropertyRelative("component").objectReferenceValue == persistent)
                    {
                        Object loaded = loadedTable.GetArrayElementAtIndex(i).FindPropertyRelative("component").objectReferenceValue;
                        if (loaded == null || loaded.GetType() != persistent.GetType()) throw new InvalidOperationException("Prefab component topology changed during apply.");
                        return loaded;
                    }
            }
            throw new InvalidOperationException($"Prefab component identity no longer resolves: {GlobalObjectId.GetGlobalObjectIdSlow(persistent)}");
        }
    }
}
