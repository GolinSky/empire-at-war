using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceValidation
    {
        public static List<string> Preflight(BalanceRegistration current, BalanceDraft draft)
        {
            List<string> errors = new List<string>(current.Errors);
            foreach (BalanceChange change in draft.Changes)
            {
                if (!current.Fields.TryGetValue(change.Key, out BalanceField field)) { errors.Add("Missing target: " + change.Key); continue; }
                if (field.Schema != change.Schema) errors.Add("Ownership/schema changed: " + field.Context + "/" + field.Label);
                if (field.Read() != change.Before) errors.Add("External edit conflict: " + field.Context + "/" + field.Label);
                string error = field.Validate(change.After);
                if (error.Length != 0) errors.Add(field.Context + "/" + field.Label + ": " + error);
                BalanceChange sourceChange = field.InheritsSource ? draft.Changes.FirstOrDefault(entry => entry.Key == field.SourceKey) : null;
                if (sourceChange != null && sourceChange.After != change.After)
                    errors.Add(field.Context + ": apply the shared source before staging a distinct owning override for this field.");
                if (field.Stat == "WeaponType")
                {
                    if (!current.Fields.Values.Any(profile => profile.Stat == "weapon/" + change.After + "/damage")) errors.Add("Weapon profile missing: " + change.After);
                    foreach (BalanceUnit unit in field.Users.Where(unit => unit.Data is ShipData))
                        if (!((ShipData)unit.Data).HardPointHealth.Any(health => health.HardPointType == ((HardPoint)field.Target).HardPointType))
                            errors.Add(unit.Name + ": assignment requires existing hardpoint health data.");
                }
            }
            if (errors.Count != 0) return errors;
            foreach (BalanceUnit unit in current.Units)
            {
                if (unit.Fields.TryGetValue("Abilities", out string abilityKey) && draft.Changes.Any(change => change.Key == abilityKey))
                    ValidateAbilities(current, draft, unit, errors);
            }
            foreach (BalanceField min in current.Fields.Values.Where(field => field.Stat == "MinYaw"))
            {
                BalanceField max = current.Fields.Values.Single(field => field.Target == min.Target && field.Stat == "MaxYaw");
                if (float.Parse(Value(min, draft), System.Globalization.CultureInfo.InvariantCulture) > float.Parse(Value(max, draft), System.Globalization.CultureInfo.InvariantCulture))
                    errors.Add(min.Context + ": minimum yaw exceeds maximum yaw.");
            }
            return errors.Distinct().ToList();
        }

        private static string Value(BalanceField field, BalanceDraft draft) => field.DraftValue(draft);

        private static void ValidateAbilities(BalanceRegistration current, BalanceDraft draft, BalanceUnit unit, List<string> errors)
        {
            BalanceField assignment = current.Fields[unit.Fields["Abilities"]];
            string value = Value(assignment, draft);
            ShipAbilityCatalog catalog = BalanceRegistration.One<ShipAbilityCatalog>();
            foreach (int id in value.Length == 0 ? Array.Empty<int>() : value.Split(',').Select(int.Parse))
            {
                if (!current.Fields.Values.Any(field => field.Stat == "ability/" + id + "/duration")) { errors.Add("Ability definition missing: " + id); continue; }
                string subtype = catalog.Get((ShipAbilityId)id).Settings.GetType().Name;
                if (subtype == "LockSFoilsSettings" && (unit.Kind != BalanceUnitKind.Squadron || !unit.Components.Any(component => component.GetType().Name == "SFoilsView")))
                    errors.Add(unit.Name + ": Lock S-foils requires an existing squadron SFoilsView.");
                if (subtype == "IonPulseSettings" && (unit.Kind != BalanceUnitKind.Ship || !unit.Mounts.OfType<HardPoint>().Any(mount => mount.HardPointType == HardPointType.IonPulseCannon)
                    || !((ShipData)unit.Data).HardPointHealth.Any(health => health.HardPointType == HardPointType.IonPulseCannon)))
                    errors.Add(unit.Name + ": Ion Pulse requires existing facing and cannon bindings.");
                if (subtype == "CompositeBeamSettings")
                {
                    bool muzzle = false;
                    foreach (UnityEngine.Object component in unit.Components.Where(component => component.GetType().Name == "Ship"))
                        using (SerializedObject serialized = new SerializedObject(component))
                        {
                            SerializedProperty property = serialized.FindProperty("compositeBeamMuzzle");
                            muzzle = property != null && property.objectReferenceValue != null;
                        }
                    if (unit.Kind != BalanceUnitKind.Ship || !muzzle) errors.Add(unit.Name + ": Composite Beam requires an existing bound muzzle.");
                }
                if ((subtype == "PowerToMainBatteriesSettings" || subtype == "FullSalvoSettings" || subtype == "AssaultSettings")
                    && !unit.Mounts.OfType<WeaponHardPoint>().Any()) errors.Add(unit.Name + ": ability requires an existing weapon component and mounts.");
            }
        }
    }
}
