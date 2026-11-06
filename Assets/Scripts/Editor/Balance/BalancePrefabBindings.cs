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
            var parents = unit.Components.OfType<Transform>().Select(transform => transform.parent).Where(parent => parent != null);
            Object[] objects = unit.Components.Concat(unit.Mounts).Concat(parents).Distinct().ToArray();
            GlobalObjectId[] identifiers = new GlobalObjectId[objects.Length];
            GlobalObjectId.GetGlobalObjectIdsSlow(objects, identifiers);
            var ids = new Dictionary<Object, string>(objects.Length);
            for (int i = 0; i < objects.Length; i++) ids.Add(objects[i], identifiers[i].ToString());

            string topology = string.Join(";", OrderIds(unit.Components.Select(component =>
                ids[component] + (component is Transform transform
                    ? "/parent:" + (transform.parent != null ? ids[transform.parent] : "root") : "")))) + "|mounts:"
                + string.Join(";", OrderIds(unit.Mounts.Select(mount => ids[mount])));
            using (SHA256 hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(topology)));
        }

        private static IEnumerable<string> OrderIds(IEnumerable<string> ids)
        {
            // Existing draft and preset hashes use culture ordering. Most prefab IDs have the same ordinal order.
            string[] original = ids.ToArray();
            string[] ordered = original.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            for (int i = 1; i < ordered.Length; i++)
                if (ordered[i - 1] != ordered[i] && string.Compare(ordered[i - 1], ordered[i], StringComparison.CurrentCulture) >= 0)
                    return original.OrderBy(id => id);
            return ordered;
        }

        public static string Register(BalanceRegistration registry, BalanceUnit unit)
        {
            string topology;
            if (registry.PrefabBindings.TryGetValue(unit.Prefab, out var bindings))
            {
                unit.Components.AddRange(bindings.Unit.Components);
                unit.Mounts.AddRange(bindings.Unit.Mounts);
                topology = bindings.Topology;
            }
            else
            {
                unit.Components.AddRange(Components(unit.Prefab));
                foreach (Object health in unit.Components.Where(component => component is HealthComponent))
                    unit.Mounts.AddRange(References(health, BalanceRegistration.Auto("ShipUnits")));
                foreach (Object weapons in unit.Components.Where(component => component is WeaponComponent))
                    unit.Mounts.AddRange(References(weapons, "hardPoints"));
                topology = Topology(unit);
            }

            foreach (Object mount in unit.Mounts.Distinct())
            {
                using SerializedObject serialized = new SerializedObject(mount);
                string context = unit.Name + " / " + ((Component)mount).gameObject.name;
                registry.Add(mount, "unlockLevel", "unlockLevel", BalanceFieldGroup.Hardpoints, context, BalanceFieldOwner.PrefabOverride, false,
                    new[] { unit }, minimum: 1, dependency: topology, serializedTarget: serialized);
                if (unit.Kind == BalanceUnitKind.Ship)
                    registry.Add(mount, "healthWeight", "healthWeight", BalanceFieldGroup.Hardpoints, context, BalanceFieldOwner.PrefabOverride, false,
                        new[] { unit }, positive: true, dependency: topology, serializedTarget: serialized);
                if (mount is WeaponHardPoint)
                {
                    registry.Add(mount, "WeaponType", BalanceRegistration.Auto("WeaponType"), BalanceFieldGroup.Hardpoints, context,
                        BalanceFieldOwner.PrefabOverride, false, new[] { unit }, typeof(WeaponType), dependency: topology, serializedTarget: serialized);
                    registry.Add(mount, "MinYaw", "yAxisRange." + BalanceRegistration.Auto("Min"), BalanceFieldGroup.Hardpoints, context,
                        BalanceFieldOwner.PrefabOverride, false, new[] { unit }, minimum: double.NegativeInfinity, dependency: topology, serializedTarget: serialized);
                    registry.Add(mount, "MaxYaw", "yAxisRange." + BalanceRegistration.Auto("Max"), BalanceFieldGroup.Hardpoints, context,
                        BalanceFieldOwner.PrefabOverride, false, new[] { unit }, minimum: double.NegativeInfinity, dependency: topology, serializedTarget: serialized);
                    registry.Add(mount, "mainBattery", "mainBattery", BalanceFieldGroup.Hardpoints, context, BalanceFieldOwner.PrefabOverride, false, new[] { unit }, dependency: topology, serializedTarget: serialized);
                }
                if (mount is MissileInterceptorHardPoint)
                {
                    foreach (string name in new[] { "range", "reload", "interceptChance" })
                        registry.Add(mount, name, name, BalanceFieldGroup.Hardpoints, context, BalanceFieldOwner.PrefabOverride, false, new[] { unit }, positive: name == "reload",
                            maximum: name == "interceptChance" ? 1 : double.PositiveInfinity, dependency: topology, serializedTarget: serialized);
                }
                foreach (BalanceField field in registry.FieldsFor(mount))
                    unit.Fields["mount/" + field.Key] = field.Key;
            }
            registry.PrefabBindings.TryAdd(unit.Prefab, (unit, topology));
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
