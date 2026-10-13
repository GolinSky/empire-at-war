using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Squadrons;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.BattleSimulator
{
    // All Unity asset access ends here. Definitions are snapshots; no prefab is instantiated or edited.
    public sealed class SimUnitFactory : IDisposable
    {
        private readonly BalanceRegistration _registry;
        private readonly BalanceDraft _draft;
        private readonly BalanceWeaponPreview _weapons;
        private readonly IReadOnlyDictionary<WeaponType, WeaponProfile> _profiles;
        public DamageMatrixData Matrix { get; }

        public SimUnitFactory(BalanceRegistration registry, BalanceDraft draft)
        {
            _registry = registry;
            _draft = draft;
            _weapons = new BalanceWeaponPreview(registry);
            _profiles = _weapons.CaptureProfiles(draft);
            Matrix = Copy(BalanceRegistration.One<DamageMatrixData>());
        }

        public SimUnitDefinition Build(BalanceUnit unit)
        {
            var definition = new SimUnitDefinition { Id = unit.Id, Name = unit.Name, Faction = unit.Faction };
            Object copy = Copy(unit.Data);
            var bindings = new Dictionary<Object, int>();
            try
            {
                if (copy is ShipData ship)
                {
                    definition.ShipClass = ship.ShipClass;
                    definition.Hull = ship.Hull;
                    definition.Shields = ship.Shields;
                    definition.ShieldRegenerateDelay = ship.ShieldRegenerateDelay;
                    definition.ShieldRegenerateValue = ship.ShieldRegenerateValue;
                    definition.HardPointHealth = ship.HardPointHealth.ToArray();
                    definition.Range = ship.Range;
                    if (ship.HangarBays.Count > 0) definition.Warning = "Hangar not simulated";
                    var health = unit.Components.OfType<HealthComponent>().Single();
                    foreach (HardPoint point in BalancePrefabBindings.References(health, BalanceRegistration.Auto("ShipUnits")))
                    {
                        bindings.Add(point, definition.HardPoints.Count);
                        definition.HardPoints.Add(new SimHardPointDefinition { Type = point.HardPointType,
                            UnlockLevel = Integer(point, "unlockLevel"), Name = point.name });
                        if (!definition.HardPointHealth.Any(value => value.HardPointType == point.HardPointType))
                            throw new InvalidOperationException($"{unit.Caption}: missing HardPointHealth for {point.HardPointType}.");
                    }
                }
                else if (copy is SquadronData squadron)
                {
                    definition.IsSquadron = true;
                    definition.ShipClass = squadron.ShipClass;
                    definition.MemberHull = squadron.MemberHull;
                    definition.MemberShields = squadron.MemberShields;
                    definition.ShieldRegenerateDelay = squadron.ShieldRegenerateDelay;
                    definition.ShieldRegenerateValue = squadron.ShieldRegenerateValue;
                    definition.LaserShieldDamageMultiplier = squadron.LaserShieldDamageMultiplier;
                    definition.HullRepairPerSecond = squadron.HullRepairPerSecond;
                    definition.Range = squadron.Range;
                    var health = unit.Components.OfType<SquadronHealthComponent>().Single();
                    foreach (FighterView fighter in BalancePrefabBindings.References(health, "fighters"))
                    {
                        int id = definition.HardPoints.Count;
                        definition.HardPoints.Add(new SimHardPointDefinition { Type = fighter.HardPointType, Name = fighter.name });
                        using (var serialized = new SerializedObject(fighter))
                        {
                            Object gun = serialized.FindProperty("gun").objectReferenceValue;
                            if (gun == null) throw new InvalidOperationException($"{unit.Caption}: fighter has no gun.");
                            bindings.Add(gun, id);
                        }
                        foreach (Object gun in BalancePrefabBindings.References(fighter, "additionalGuns")) bindings.Add(gun, id);
                    }
                    if (definition.HardPoints.Count == 0) throw new InvalidOperationException($"{unit.Caption}: no squadron members.");
                    definition.Warning = "Stationary fighters; attack-run restrictions and missile interception not simulated";
                }
                else throw new InvalidOperationException($"{unit.Caption}: unsupported combat data.");

                foreach (WeaponHardPoint mount in unit.Components.OfType<WeaponComponent>()
                             .SelectMany(component => BalancePrefabBindings.References(component, "hardPoints")))
                {
                    if (!bindings.TryGetValue(mount, out int id))
                        throw new InvalidOperationException($"{unit.Caption}: weapon {mount.name} has no explicit health binding.");
                    WeaponType type = (WeaponType)Integer(mount, "WeaponType");
                    if (!_profiles.TryGetValue(type, out WeaponProfile profile))
                        throw new InvalidOperationException($"{unit.Caption}: missing weapon profile {type}.");
                    if (!float.IsFinite(profile.CycleSeconds) || profile.CycleSeconds <= 0 || profile.ShotsPerSalvo < 1 ||
                        profile.ShotInterval < 0 || !float.IsFinite(profile.Damage) || profile.Damage < 0)
                        throw new InvalidOperationException($"{unit.Caption}: invalid weapon cycle/damage for {type}.");
                    if (profile.ShotPrefab == null) throw new InvalidOperationException($"{unit.Caption}: missing shot prefab for {type}.");
                    string fixedFlight = FixedFlightProperty(profile.ShotPrefab);
                    float flight = 0;
                    if (fixedFlight != null)
                    {
                        using (var serialized = new SerializedObject(profile.ShotPrefab))
                            flight = serialized.FindProperty(fixedFlight).floatValue;
                    }
                    else if (!(profile.ProjectileSpeed > 0)) throw new InvalidOperationException($"{unit.Caption}: invalid projectile speed for {type}.");
                    definition.Mounts.Add(new SimMountDefinition { Profile = profile, HardPointId = id,
                        UnlockLevel = Integer(mount, "unlockLevel"), HasFixedFlight = fixedFlight != null,
                        FixedFlightSeconds = flight, DamageDuration = profile.ShotPrefab.DamageDuration });
                }
                float hull = definition.IsSquadron ? definition.MemberHull : definition.Hull;
                if (!(hull > 0) || !float.IsFinite(hull) || !(definition.ShieldRegenerateDelay > 0))
                    throw new InvalidOperationException($"{unit.Caption}: invalid hull or shield regeneration delay.");
                return definition;
            }
            finally { Object.DestroyImmediate(copy); }
        }

        // Serialized time each shot type returns from ShotEffect.Play; null when it flies distance / projectile speed.
        private static string FixedFlightProperty(ShotEffect shot) => shot switch
        {
            BeamShot _ => "growthDuration",
            IonPulseShot _ => "flightDuration",
            _ => null
        };

        private int Integer(Object target, string stat) => int.Parse(_registry.FieldsFor(target)
            .Single(field => field.Stat == stat && !field.SharedMountSource).DraftValue(_draft), CultureInfo.InvariantCulture);

        private T Copy<T>(T source) where T : Object
        {
            T copy = Object.Instantiate(source);
            copy.hideFlags = HideFlags.HideAndDontSave;
            using (var serialized = new SerializedObject(copy))
            {
                foreach (BalanceField field in _registry.FieldsFor(source))
                    BalanceValue.Write(field.Property(serialized), field.Kind, field.DraftValue(_draft));
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            return copy;
        }

        public void Dispose()
        {
            _weapons.Dispose();
            Object.DestroyImmediate(Matrix);
        }
    }
}
