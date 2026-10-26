using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Editor.AI;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class WeaponTaxonomyTests
    {
        private const string WEAPONS = "Assets/Settings/Data/Models/Weapon/WeaponsData.asset";
        private const string MATRIX = "Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset";
        private const string SFX = "Assets/Settings/Data/Models/Audio/ShipSfxData.asset";
        private const string FACTIONS = "Assets/Settings/Data/Factions";

        // Orbital super weapons fire from SuperWeaponData, not from hardpoints.
        private static readonly WeaponType[] SUPER_WEAPONS =
            { WeaponType.PlanetaryIonCannon, WeaponType.HypervelocityGun, WeaponType.PlasmaCannon };

        private static WeaponsData Weapons => AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS);

        [TestCase(ShipClass.Corvette)]
        [TestCase(ShipClass.Frigate)]
        public void SavedShipWeapons_RespectClassCaliberRules(ShipClass shipClass)
        {
            WeaponsData weapons = Weapons;
            foreach (ShipData data in LoadAll<ShipData>().Where(data => data.ShipClass == shipClass))
            {
                foreach (WeaponLoadoutEntry entry in WeaponLoadoutBaker.DeriveLoadout(WeaponLoadoutBaker.LoadShipView(data)))
                {
                    WeaponDefinition definition = weapons.GetDefinition(entry.WeaponType);
                    string context = $"{data.name} mounts {definition.DisplayName}";
                    // The approved Procursator loadout explicitly includes triple support batteries.
                    if (data.name == "ProcursatorShipData" && entry.WeaponType == WeaponType.ProcursatorMediumTripleTurbolaser)
                    {
                        Assert.That(definition.Caliber, Is.EqualTo(WeaponCaliber.Medium), context);
                        Assert.That(definition.Mount, Is.EqualTo(WeaponMount.Triple), context);
                        continue;
                    }
                    if (shipClass == ShipClass.Corvette)
                    {
                        Assert.That(definition.Family, Is.Not.EqualTo(WeaponFamily.Turbolaser), context);
                        Assert.That(definition.Caliber, Is.Not.EqualTo(WeaponCaliber.Heavy), context);
                    }
                    else if (shipClass == ShipClass.Frigate && definition.Family == WeaponFamily.Turbolaser)
                        Assert.That(definition.Caliber, Is.Not.EqualTo(WeaponCaliber.Heavy), context);
                }
            }
        }

        [Test]
        public void SavedShipLoadouts_MatchTheirFiringHardpoints()
        {
            foreach (ShipData data in LoadAll<ShipData>())
            {
                var mounted = WeaponLoadoutBaker.DeriveLoadout(WeaponLoadoutBaker.LoadShipView(data));
                Assert.That(data.WeaponLoadout.Select(entry => (entry.WeaponType, entry.Count)),
                    Is.EquivalentTo(mounted.Select(entry => (entry.WeaponType, entry.Count))), data.name);
            }
        }

        [Test]
        public void EveryHardpointWeapon_HasOneDefinitionAndBuildsAProfile()
        {
            WeaponsData weapons = Weapons;
            WeaponType[] expected = ((WeaponType[])Enum.GetValues(typeof(WeaponType))).Except(SUPER_WEAPONS).ToArray();

            Assert.That(weapons.Weapons.Select(definition => definition.WeaponType), Is.EquivalentTo(expected));
            foreach (WeaponType type in expected)
                Assert.That(weapons.GetProfile(type).DamagePerSecond, Is.GreaterThan(0f), type.ToString());
        }

        [Test]
        public void EveryFamilyCaliberAndMount_HasExactlyOneRule()
        {
            WeaponsData weapons = Weapons;
            Assert.That(weapons.Families.Select(rule => rule.Family), Is.EquivalentTo(Enum.GetValues(typeof(WeaponFamily))));
            Assert.That(weapons.Calibers.Select(rule => rule.Caliber), Is.EquivalentTo(Enum.GetValues(typeof(WeaponCaliber))));
            Assert.That(weapons.Mounts.Select(rule => rule.Mount), Is.EquivalentTo(Enum.GetValues(typeof(WeaponMount))));
        }

        [Test]
        public void NoTwoWeaponDefinitionsShareTaxonomy()
        {
            foreach (var group in Weapons.Weapons.GroupBy(definition => (definition.Family, definition.Caliber,
                         definition.Mount, definition.Scope, definition.Faction, definition.Unit,
                         definition.DamageMultiplier, definition.ReloadMultiplier, definition.RangeMultiplier)))
                Assert.That(group.Count(), Is.EqualTo(1), string.Join(", ", group.Select(definition => definition.WeaponType)));
        }

        [Test]
        public void AntiSquadronWeapons_PenetrateSquadronShieldsWithoutBuffingShipShieldDamage()
        {
            DamageMatrixData matrix = AssetDatabase.LoadAssetAtPath<DamageMatrixData>(MATRIX);

            Assert.That(matrix.GetSquadronShieldMultiplier(DamageType.Laser), Is.EqualTo(1f));
            Assert.That(matrix.GetSquadronShieldMultiplier(DamageType.PointDefense), Is.EqualTo(1f));
            Assert.That(matrix.GetShieldMultiplier(DamageType.Laser), Is.EqualTo(0.5f));
            Assert.That(matrix.GetShieldMultiplier(DamageType.PointDefense), Is.EqualTo(0.25f));
            foreach (ShipClass target in new[] { ShipClass.Fighter, ShipClass.Bomber, ShipClass.Interceptor })
            {
                Assert.That(matrix.GetDamageMultiplier(DamageType.Laser, target), Is.EqualTo(2f));
                Assert.That(matrix.GetDamageMultiplier(DamageType.PointDefense, target), Is.EqualTo(2.5f));
            }
        }

        [Test]
        public void HeavyLasers_CanDefendAgainstSquadrons_WhileTurbolasersRemainPoorCounters()
        {
            WeaponsData weapons = Weapons;
            DamageMatrixData matrix = AssetDatabase.LoadAssetAtPath<DamageMatrixData>(MATRIX);
            WeaponCaliberRule heavy = weapons.GetCaliber(WeaponCaliber.Heavy);
            foreach (ShipClass target in new[] { ShipClass.Fighter, ShipClass.Bomber, ShipClass.Interceptor })
            {
                float laserEffectiveness = heavy.DamageMultiplier * heavy.DamageBias[target] *
                    heavy.AccuracyBias[target] * matrix.GetDamageMultiplier(DamageType.Laser, target) *
                    matrix.GetAccuracy(DamageType.Laser, target);
                float turboEffectiveness = heavy.DamageMultiplier * heavy.DamageBias[target] *
                    heavy.AccuracyBias[target] * matrix.GetDamageMultiplier(DamageType.Turbolaser, target) *
                    matrix.GetAccuracy(DamageType.Turbolaser, target);

                Assert.That(laserEffectiveness, Is.GreaterThanOrEqualTo(0.8f));
                Assert.That(turboEffectiveness, Is.LessThan(0.1f));
            }
        }

        [Test]
        public void BiggerCalibers_DealMoreDamageAndReachFarther()
        {
            WeaponsData weapons = Weapons;
            WeaponCaliberRule light = weapons.GetCaliber(WeaponCaliber.Light);
            WeaponCaliberRule medium = weapons.GetCaliber(WeaponCaliber.Medium);
            WeaponCaliberRule heavy = weapons.GetCaliber(WeaponCaliber.Heavy);

            Assert.That(medium.DamageMultiplier, Is.GreaterThan(light.DamageMultiplier));
            Assert.That(heavy.DamageMultiplier, Is.GreaterThan(medium.DamageMultiplier));
            Assert.That(medium.RangeMultiplier, Is.GreaterThan(light.RangeMultiplier));
            Assert.That(heavy.RangeMultiplier, Is.GreaterThan(medium.RangeMultiplier));
            // Heavy guns favour big targets; light guns favour small ones.
            Assert.That(heavy.DamageBias[ShipClass.Capital], Is.GreaterThan(light.DamageBias[ShipClass.Capital]));
            Assert.That(heavy.AccuracyBias[ShipClass.Fighter], Is.LessThan(light.AccuracyBias[ShipClass.Fighter]));
        }

        // Phase 0 baseline (2f45f1371): the former Long range class gave the Long guns ×1.2 accuracy, now folded into damage.
        [TestCase(WeaponType.HeavyArtilleryRocket, 93.75f, 1.2f, 4, 20f, 1072.5f)]
        [TestCase(WeaponType.ExecutorHeavyTurbolaser, 37.5f, 1.2f, 3, 8f, 877.5f)]
        [TestCase(WeaponType.ExecutorHeavyDualTurbolaser, 75f, 1.2f, 3, 8f, 877.5f)]
        [TestCase(WeaponType.ExecutorHeavyDualIonCannon, 140.625f, 1.2f, 2, 12f, 975f)]
        [TestCase(WeaponType.ExecutorMediumDualTurbolaser, 60f, 1.2f, 3, 8f, 776.25f)]
        [TestCase(WeaponType.HeavyLaser, 9.375f, 1f, 3, 1.5f, 390f)]
        [TestCase(WeaponType.ExecutorPointDefense, 5f, 1f, 8, 2f, 230f)]
        public void Executor_WeaponProfilesMatchBaseline(WeaponType type, float baselineDamage, float foldedAccuracy,
            int shots, float reload, float range)
        {
            WeaponDefinition definition = Weapons.GetDefinition(type);
            WeaponProfile profile = Weapons.GetProfile(type);
            if (definition.Scope == WeaponScope.Unit) Assert.That(definition.Unit, Is.EqualTo(nameof(ShipType.Executor)));

            Assert.That(profile.Damage, Is.EqualTo(baselineDamage * foldedAccuracy).Within(0.001f));
            Assert.That(profile.ShotsPerSalvo, Is.EqualTo(shots));
            Assert.That(profile.Reload, Is.EqualTo(reload).Within(0.001f));
            Assert.That(profile.Range, Is.EqualTo(range).Within(0.01f));
        }

        // RaW 1.5.5 red/blue turbolaser projectiles: twin 1.67×, quad 2.0–2.67× the single bolt.
        [TestCase(WeaponMount.Dual, 1.6f)]
        [TestCase(WeaponMount.Triple, 2f)]
        [TestCase(WeaponMount.Quad, 2.4f)]
        [TestCase(WeaponMount.Octuple, 4f)]
        public void Mounts_MultiplyShotDamageByMountRule_AndKeepShots(WeaponMount mount, float multiplier)
        {
            WeaponsData weapons = Weapons;
            WeaponFamilyRule family = weapons.GetFamily(WeaponFamily.Turbolaser);
            WeaponProfile single = Build(weapons, WeaponMount.Single);
            WeaponProfile mounted = Build(weapons, mount);

            Assert.That(weapons.GetMount(mount).DamageMultiplier, Is.EqualTo(multiplier).Within(0.001f));
            Assert.That(mounted.Damage, Is.EqualTo(single.Damage * multiplier).Within(0.001f));
            Assert.That(mounted.ShotsPerSalvo, Is.EqualTo(single.ShotsPerSalvo));
            var expectedPrefab = mount == WeaponMount.Quad ? family.QuadShotPrefab :
                mount == WeaponMount.Octuple ? family.OctupleShotPrefab : family.MultiBarrelShotPrefab;
            Assert.That(expectedPrefab, Is.Not.Null);
            Assert.That(mounted.ShotPrefab, Is.EqualTo(expectedPrefab));
        }

        [Test]
        public void SquadronMounts_KeepFullDamagePerBarrel()
        {
            WeaponsData weapons = Weapons;
            WeaponDefinition definition = weapons.GetDefinition(WeaponType.LightDualLaser);
            float single = weapons.GetFamily(definition.Family).Damage * weapons.GetCaliber(definition.Caliber).DamageMultiplier;

            Assert.That(definition.Mount, Is.EqualTo(WeaponMount.Dual));
            Assert.That(weapons.GetProfile(definition.WeaponType).Damage, Is.EqualTo(single * 2).Within(0.001f));
        }

        [TestCase(WeaponMount.Single, 1, "Projectile")]
        [TestCase(WeaponMount.Dual, 2, "DualProjectile")]
        [TestCase(WeaponMount.Quad, 4, "QuadProjectile")]
        [TestCase(WeaponMount.Octuple, 8, "OctupleProjectile")]
        public void MountProjectiles_EmitDistinctBoltsInCompactRows(WeaponMount mount, int count, string prefabName)
        {
            WeaponProfile profile = Build(Weapons, mount);
            Assert.That(profile.ShotPrefab.name, Is.EqualTo(prefabName));
            var shot = UnityEngine.Object.Instantiate(profile.ShotPrefab);
            var muzzle = new GameObject("Test muzzle");
            var target = new GameObject("Test target");
            var flights = new EmpireAtWar.ViewComponents.Weapon.BoltFlightBatch();
            try
            {
                target.transform.position = Vector3.forward * 133f;
                var serialized = new SerializedObject(shot);
                var particles = (ParticleSystem)serialized.FindProperty("vfx").objectReferenceValue;
                for (int reuse = 0; reuse < 2; reuse++)
                {
                    // Exercise the runtime setup without a damage target, then advance particle simulation.
                    if (reuse == 0)
                        shot.Fire(muzzle.transform, target.transform, target.transform, Vector3.zero, profile, flights);
                    else
                        typeof(EmpireAtWar.ViewComponents.Weapon.BoltShot).GetMethod("Play",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                            .Invoke(shot, new object[] { muzzle.transform, target.transform, Vector3.zero, profile });
                    particles.Simulate(0.05f, true, false, true);
                    var buffer = new ParticleSystem.Particle[count + 1];
                    Assert.That(particles.GetParticles(buffer), Is.EqualTo(count));
                    foreach (var particle in buffer.Take(count))
                        Assert.That(Vector3.Dot(particle.velocity.normalized, Vector3.forward),
                            Is.GreaterThan(0.999f), "Stretched bolts must point along flight");
                    var positions = buffer.Take(count).Select(particle => particle.position).ToArray();
                    Assert.That(positions.Distinct().Count(), Is.EqualTo(count));
                    if (count >= 4)
                    {
                        Assert.That(positions.Count(position => position.y > 0f), Is.EqualTo(count / 2));
                        Assert.That(positions.Count(position => position.y < 0f), Is.EqualTo(count / 2));
                        Assert.That(positions.Max(position => position.x) - positions.Min(position => position.x),
                            Is.LessThanOrEqualTo(5.3f));
                    }
                    particles.Simulate(0.25f, true, false, true);
                    Assert.That(particles.particleCount, Is.EqualTo(count), "Only one burst per shot");
                }
            }
            finally
            {
                flights.Dispose();
                UnityEngine.Object.DestroyImmediate(shot.gameObject);
                UnityEngine.Object.DestroyImmediate(muzzle);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void Variants_ExplainTheirMultipliers_AndNameTheirOwner()
        {
            foreach (WeaponDefinition definition in Weapons.Weapons)
            {
                string name = definition.WeaponType.ToString();
                bool modified = !Mathf.Approximately(definition.DamageMultiplier, 1f) ||
                                !Mathf.Approximately(definition.ReloadMultiplier, 1f) ||
                                !Mathf.Approximately(definition.RangeMultiplier, 1f);
                if (modified || definition.Scope != WeaponScope.Shared)
                    Assert.That(definition.Reason, Is.Not.Empty, name);
                if (definition.Scope == WeaponScope.Unit)
                    Assert.That(definition.Unit, Is.Not.Empty, name);
            }
        }

        [Test]
        public void ScopedWeapons_AreMountedOnlyByTheirOwners()
        {
            WeaponsData weapons = Weapons;
            Dictionary<string, FactionType[]> unitFactions = UnitFactions();
            foreach ((string unit, IReadOnlyList<WeaponLoadoutEntry> loadout) in Loadouts())
            {
                // Retired data remains available for authoring, but has no playable faction.
                if (unit == nameof(ShipType.ISDIII)) continue;
                foreach (WeaponLoadoutEntry entry in loadout)
                {
                    WeaponDefinition definition = weapons.GetDefinition(entry.WeaponType);
                    string context = $"{unit} mounts {definition.WeaponType}";
                    if (definition.Scope == WeaponScope.Unit)
                        Assert.That(unit, Is.EqualTo(definition.Unit), context);
                    if (definition.Scope == WeaponScope.Faction)
                        Assert.That(unitFactions[unit], Is.EqualTo(new[] { definition.Faction }), context);
                }
            }
        }

        [Test]
        public void ShipsMountShipWeapons_AndSquadronsMountSquadronWeapons()
        {
            WeaponsData weapons = Weapons;
            foreach (ShipData data in LoadAll<ShipData>())
                foreach (WeaponLoadoutEntry entry in data.WeaponLoadout)
                    Assert.That(weapons.GetFamily(weapons.GetDefinition(entry.WeaponType).Family).Platform,
                        Is.EqualTo(WeaponPlatform.Ship), $"{data.name} mounts {entry.WeaponType}");
            foreach (SquadronData data in LoadAll<SquadronData>())
                foreach (WeaponLoadoutEntry entry in data.WeaponLoadout)
                    Assert.That(weapons.GetFamily(weapons.GetDefinition(entry.WeaponType).Family).Platform,
                        Is.EqualTo(WeaponPlatform.Squadron), $"{data.name} mounts {entry.WeaponType}");
        }

        [Test]
        public void EveryWeapon_HasASound()
        {
            ShipSfxData sfx = AssetDatabase.LoadAssetAtPath<ShipSfxData>(SFX);
            foreach (WeaponDefinition definition in Weapons.Weapons)
                Assert.DoesNotThrow(() => sfx.GetWeapon(definition.WeaponType, definition.Family), definition.WeaponType.ToString());
        }

        [Test]
        public void EveryUnitRadar_ReachesItsLongestGun()
        {
            WeaponsData weapons = Weapons;
            foreach (ShipData data in LoadAll<ShipData>())
                Assert.That(data.Range, Is.GreaterThanOrEqualTo(LongestGun(weapons, data.WeaponLoadout)), data.name);
            foreach (SquadronData data in LoadAll<SquadronData>())
                Assert.That(data.Range, Is.GreaterThanOrEqualTo(LongestGun(weapons, data.WeaponLoadout)), data.name);
        }

        [Test]
        public void EngagementRange_HoldsHeavyGunsBackFromCapitals_AndClosesLasersOnCorvettes()
        {
            WeaponsData weapons = Weapons;
            DamageMatrixData matrix = AssetDatabase.LoadAssetAtPath<DamageMatrixData>(MATRIX);
            WeaponProfile heavyTurbolaser = weapons.GetProfile(WeaponType.HeavyTurboLaser);
            WeaponProfile laser = weapons.GetProfile(WeaponType.Laser);
            List<WeaponProfile> loadout = new List<WeaponProfile> { heavyTurbolaser, heavyTurbolaser, laser, laser, laser, laser };

            Assert.That(WeaponEngagementRange.Calculate(loadout, ShipClass.Capital, matrix), Is.EqualTo(heavyTurbolaser.Range));
            Assert.That(WeaponEngagementRange.Calculate(loadout, ShipClass.Corvette, matrix), Is.EqualTo(laser.Range));
        }

        [Test]
        public void PointDefenseOnly_ApproachesShipsToItsReach()
        {
            DamageMatrixData matrix = AssetDatabase.LoadAssetAtPath<DamageMatrixData>(MATRIX);
            WeaponProfile pointDefense = Weapons.GetProfile(WeaponType.PointDefense);

            Assert.That(WeaponEngagementRange.Calculate(new[] { pointDefense }, ShipClass.Capital, matrix),
                Is.EqualTo(pointDefense.Range));
        }

        private static WeaponProfile Build(WeaponsData weapons, WeaponMount mount)
        {
            WeaponDefinition definition = new WeaponDefinition();
            SerializedDefinition.Set(definition, mount);
            return WeaponProfileFactory.Create(definition, weapons.GetFamily(WeaponFamily.Turbolaser),
                weapons.GetCaliber(WeaponCaliber.Heavy), weapons.GetMount(mount));
        }

        private static float LongestGun(WeaponsData weapons, IEnumerable<WeaponLoadoutEntry> loadout) =>
            loadout.Select(entry => weapons.GetProfile(entry.WeaponType).Range).DefaultIfEmpty(0f).Max();

        private static IEnumerable<(string unit, IReadOnlyList<WeaponLoadoutEntry> loadout)> Loadouts()
        {
            foreach (ShipData data in LoadAll<ShipData>())
                yield return (data.name.Replace("ShipData", ""), data.WeaponLoadout);
            foreach (SquadronData data in LoadAll<SquadronData>())
                yield return (data.name.Replace("SquadronData", ""), data.WeaponLoadout);
        }

        private static Dictionary<string, FactionType[]> UnitFactions()
        {
            Dictionary<string, List<FactionType>> owners = new Dictionary<string, List<FactionType>>();
            foreach (FactionDefinition faction in LoadAll<FactionDefinition>(FACTIONS))
            {
                foreach (string unit in faction.Ships.Keys.Select(type => type.ToString())
                             .Concat(faction.Squadrons.Keys.Select(type => type.ToString())))
                {
                    if (!owners.TryGetValue(unit, out List<FactionType> factions))
                        owners.Add(unit, factions = new List<FactionType>());
                    factions.Add(faction.FactionType);
                }
            }

            return owners.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
        }

        private static IEnumerable<T> LoadAll<T>(string folder = "Assets/Settings/Data") where T : UnityEngine.Object =>
            AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)));

        // WeaponDefinition has no public setters; tests build one through its serialized fields.
        private static class SerializedDefinition
        {
            public static void Set(WeaponDefinition definition, WeaponMount mount)
            {
                const System.Reflection.BindingFlags FIELDS =
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(WeaponDefinition).GetField("mount", FIELDS).SetValue(definition, mount);
            }
        }
    }
}
