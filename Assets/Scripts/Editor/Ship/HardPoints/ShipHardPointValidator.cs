using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Editor.AI;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;

namespace EmpireAtWar.Editor.Ship.HardPoints
{
    /// <summary>Reports hardpoint wiring problems in a ship view: list mismatches, ID gaps and missing health data.</summary>
    public static class ShipHardPointValidator
    {
        private const string SHIP_VIEW_FOLDER = "Assets/Prefabs/Models/Ships/";
        private const string SHIP_VIEW_SUFFIX = "ShipView.prefab";
        private const string SHIP_DATA_SUFFIX = "ShipData.asset";

        /// <summary>Hardpoints in the hierarchy that HealthComponent.ShipUnits does not list.</summary>
        public static List<HardPoint> FindUnregistered(ShipHardPointRegistry registry) =>
            // Editor-only audit of a prefab hierarchy.
            registry.Root.GetComponentsInChildren<HardPoint>(true)
                .Where(hardPoint => !registry.HardPoints.Contains(hardPoint)).ToList();

        public static List<string> Validate(ShipHardPointRegistry registry, EntityBindings bindings, ShipData data)
        {
            List<string> issues = new List<string>();
            IReadOnlyList<HardPoint> units = registry.HardPoints;

            if (units.Any(unit => unit == null)) issues.Add("HealthComponent.ShipUnits has empty slots.");
            if (bindings.Injectables.Any(injectable => injectable == null))
                issues.Add("EntityBindings.Injectables has empty slots.");

            foreach (HardPoint hardPoint in FindUnregistered(registry))
                issues.Add($"{hardPoint.name} is not in HealthComponent.ShipUnits.");

            List<WeaponHardPoint> fired = new List<WeaponHardPoint>();
            foreach (WeaponComponent weapons in registry.WeaponComponents)
            {
                SerializedProperty list = new SerializedObject(weapons).FindProperty("hardPoints");
                for (int index = 0; index < list.arraySize; index++)
                {
                    WeaponHardPoint weapon = (WeaponHardPoint)list.GetArrayElementAtIndex(index).objectReferenceValue;
                    if (weapon == null) issues.Add($"{weapons.name}.WeaponComponent has an empty slot {index}.");
                    else fired.Add(weapon);
                }
            }

            for (int index = 0; index < units.Count; index++)
            {
                HardPoint unit = units[index];
                if (unit == null) continue;
                if (unit.Id != index) issues.Add($"{unit.name} has ID {unit.Id}, expected {index}.");
                if (!bindings.Injectables.Contains(unit)) issues.Add($"{unit.name} is not in EntityBindings.Injectables.");
                if (unit is WeaponHardPoint weapon)
                {
                    if (!fired.Contains(weapon)) issues.Add($"{unit.name} is not fired by any WeaponComponent.");
                    if (weapon.MaxYaw < weapon.MinYaw) issues.Add($"{unit.name} has an inverted firing arc.");
                }
            }

            foreach (WeaponHardPoint weapon in fired.Where(weapon => !units.Contains(weapon)))
                issues.Add($"{weapon.name} fires but is not targetable (missing from ShipUnits).");

            if (data == null)
            {
                issues.Add("No matching ShipData found; hardPointHealth was not checked.");
                return issues;
            }

            foreach (HardPointType type in units.Where(unit => unit != null).Select(unit => unit.HardPointType).Distinct())
            {
                if (data.HardPointHealth.All(health => health.HardPointType != type))
                    issues.Add($"{data.name}.hardPointHealth has no entry for {type}.");
            }
            return issues;
        }

        /// <summary>
        /// ShipData for a view by the project convention:
        /// Prefabs/Models/Ships/&lt;Folder&gt;/&lt;Name&gt;ShipView ↔ Settings/Data/Ship/&lt;Folder&gt;/&lt;Name&gt;ShipData.
        /// </summary>
        public static ShipData FindShipData(string viewPath)
        {
            if (!viewPath.StartsWith(SHIP_VIEW_FOLDER) || !viewPath.EndsWith(SHIP_VIEW_SUFFIX)) return null;
            string relative = viewPath.Substring(SHIP_VIEW_FOLDER.Length);
            string dataPath = $"{WeaponLoadoutBaker.SHIP_DATA_FOLDER}/" +
                relative.Substring(0, relative.Length - SHIP_VIEW_SUFFIX.Length) + SHIP_DATA_SUFFIX;
            return AssetDatabase.LoadAssetAtPath<ShipData>(dataPath);
        }
    }
}
