using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Squadrons;
using ShipEntity = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceValidation
    {
        public static List<string> Preflight(BalanceRegistration current, BalanceDraft draft)
        {
            List<string> errors = new List<string>(current.Errors);
            foreach (BalanceChange change in draft.Changes)
            {
                if (!current.Fields.TryGetValue(change.Key, out BalanceField field))
                {
                    errors.Add("Missing target: " + change.Key);
                    continue;
                }

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
                if (!current.Fields.Values.Any(field => field.Stat == "ability/" + id + "/duration"))
                {
                    errors.Add("Ability definition missing: " + id);
                    continue;
                }

                ShipAbilitySettings settings = catalog.Get((ShipAbilityId)id).Settings;
                bool ship = unit.Kind == BalanceUnitKind.Ship;
                if (settings is LockSFoilsSettings
                    && (unit.Kind != BalanceUnitKind.Squadron || !unit.Components.OfType<SFoilsView>().Any()))
                    errors.Add(unit.Name + ": Lock S-foils requires an existing squadron SFoilsView.");
                if (settings is IonPulseSettings && (!ship || !HasIonPulseCannon(unit)))
                    errors.Add(unit.Name + ": Ion Pulse requires existing facing and cannon bindings.");
                if (settings is CompositeBeamSettings
                    && (!ship || !unit.Components.OfType<ShipEntity>().Any(entity => entity.CompositeBeamMuzzle != null)))
                    errors.Add(unit.Name + ": Composite Beam requires an existing bound muzzle.");
                if ((settings is PowerToMainBatteriesSettings || settings is FullSalvoSettings || settings is AssaultSettings)
                    && !unit.Mounts.OfType<WeaponHardPoint>().Any())
                    errors.Add(unit.Name + ": ability requires an existing weapon component and mounts.");
            }
        }

        private static bool HasIonPulseCannon(BalanceUnit unit) =>
            unit.Mounts.OfType<HardPoint>().Any(mount => mount.HardPointType == HardPointType.IonPulseCannon)
            && ((ShipData)unit.Data).HardPointHealth.Any(health => health.HardPointType == HardPointType.IonPulseCannon);
    }
}
