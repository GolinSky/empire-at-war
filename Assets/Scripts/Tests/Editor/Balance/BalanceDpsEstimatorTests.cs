using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Balance")]
    public sealed class BalanceDpsEstimatorTests
    {
        [TestCase(1, 0.5f, 2.0f, 2.0f)]
        [TestCase(4, 0.25f, 3.0f, 3.0f)]
        [TestCase(8, 3.0f, 15.0f, 21.0f)]
        public void Cycle_IsReloadOrSalvoDurationWhicheverIsLonger(int shots, float interval, float reload, float expected)
        {
            Assert.That(Profile(21f, shots, interval, reload).CycleSeconds, Is.EqualTo(expected).Within(1e-5));
        }

        [Test]
        public void DamagePerSecond_LongSalvoIsLimitedBySalvoDuration()
        {
            Assert.That(Profile(21f, 8, 3f, 15f).DamagePerSecond, Is.EqualTo(8f).Within(1e-5));
        }

        [Test]
        public void Estimate_UsesDraftWeaponRules()
        {
            BalanceRegistration registry = BalanceInventory.Build();
            Assert.That(registry.Errors, Is.Empty);
            BalanceUnit unit = registry.Units.First(entry => entry.Kind == BalanceUnitKind.Ship && entry.Mounts.Any());
            List<BalanceField> mounts = unit.Mounts.Distinct().SelectMany(registry.FieldsFor)
                .Where(field => !field.SharedMountSource && field.Stat == "WeaponType").ToList();
            Assert.That(mounts, Is.Not.Empty);

            double baseline;
            using (BalanceDpsEstimator estimator = new BalanceDpsEstimator(registry))
                baseline = estimator.Estimate(unit, new BalanceDraft()).Value;

            // Doubling every mounted weapon's variant damage doubles the estimate before anything is applied.
            BalanceDraft draft = new BalanceDraft();
            List<BalanceField> multipliers = mounts.Select(mount => mount.Read()).Distinct()
                .Select(id => registry.Fields.Values.Single(field =>
                    field.Stat == "weapon/" + id + "/" + BalanceWeaponAdapter.DEFINITION_MARKER))
                .ToList();
            draft.SetMany(multipliers.Select(field => field.Snapshot()).ToList(),
                multipliers.Select(field => (double.Parse(field.Read(), CultureInfo.InvariantCulture) * 2)
                    .ToString("R", CultureInfo.InvariantCulture)).ToList());

            using (BalanceDpsEstimator estimator = new BalanceDpsEstimator(registry))
            {
                BalanceDpsEstimate estimate = estimator.Estimate(unit, draft);
                Assert.That(estimate.HasValue, Is.True, estimate.Problem);
                Assert.That(estimate.Value, Is.EqualTo(baseline * 2).Within(baseline * 1e-5));
            }

            WeaponsData saved = AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            Assert.That(multipliers.Select(field => field.Read()), Is.EqualTo(multipliers.Select(field =>
                saved.GetDefinition((WeaponType)int.Parse(field.Stat.Split('/')[1])).DamageMultiplier
                    .ToString("R", CultureInfo.InvariantCulture))), "The preview must not write the asset.");
        }

        [Test]
        public void EstimateAgainst_MatchesRuntimeEffectiveDamage_AndFollowsDraftMatrix()
        {
            BalanceRegistration registry = BalanceInventory.BuildCombat();
            BalanceUnit unit = registry.Units.First(entry => entry.Kind == BalanceUnitKind.Ship && entry.Mounts.Any());
            List<WeaponType> mounted = unit.Mounts.Distinct().SelectMany(registry.FieldsFor)
                .Where(field => !field.SharedMountSource && field.Stat == "WeaponType")
                .Select(field => (WeaponType)int.Parse(field.Read(), CultureInfo.InvariantCulture)).ToList();
            WeaponsData weapons = registry.Resolve<WeaponsData>();
            DamageMatrixData matrix = registry.Resolve<DamageMatrixData>();
            const ShipClass TARGET = ShipClass.Capital;
            double expected = mounted.Sum(type =>
                (double)WeaponEngagementRange.EffectiveDamagePerSecond(weapons.GetProfile(type), TARGET, matrix));

            using (BalanceDpsEstimator estimator = new BalanceDpsEstimator(registry))
            {
                BalanceDpsEstimate saved = estimator.EstimateAgainst(unit, new BalanceDraft(), TARGET);
                Assert.That(saved.HasValue, Is.True, saved.Problem);
                Assert.That(saved.Value, Is.EqualTo(expected).Within(expected * 1e-5 + 1e-5));

                // Doubling the hull damage multiplier of every mounted damage type doubles damage against that class.
                List<BalanceField> multipliers = mounted.Select(type => weapons.GetProfile(type).DamageType).Distinct()
                    .Select(damageType => registry.Fields.Values.Single(field =>
                        field.Stat == "matrix/" + (int)damageType + "/damage/" + TARGET))
                    .ToList();
                BalanceDraft draft = new BalanceDraft();
                draft.SetMany(multipliers.Select(field => field.Snapshot()).ToList(),
                    multipliers.Select(field => (double.Parse(field.Read(), CultureInfo.InvariantCulture) * 2)
                        .ToString("R", CultureInfo.InvariantCulture)).ToList());
                BalanceDpsEstimate doubled = estimator.EstimateAgainst(unit, draft, TARGET);
                Assert.That(doubled.Value, Is.EqualTo(saved.Value * 2).Within(saved.Value * 1e-5 + 1e-5));
            }
        }

        [Test]
        public void EstimateIonVsShields_MatchesRuntimeShieldDamage_AndFollowsDraftMatrix()
        {
            BalanceRegistration registry = BalanceInventory.BuildCombat();
            WeaponsData weapons = registry.Resolve<WeaponsData>();
            DamageMatrixData matrix = registry.Resolve<DamageMatrixData>();
            List<WeaponType> Mounted(BalanceUnit candidate) => candidate.Mounts.Distinct().SelectMany(registry.FieldsFor)
                .Where(field => !field.SharedMountSource && field.Stat == "WeaponType")
                .Select(field => (WeaponType)int.Parse(field.Read(), CultureInfo.InvariantCulture)).ToList();
            BalanceUnit unit = registry.Units.First(entry => entry.Kind == BalanceUnitKind.Ship &&
                Mounted(entry).Any(type => weapons.GetProfile(type).DamageType == DamageType.Ion));
            FactionType faction = (FactionType)System.Enum.Parse(typeof(FactionType), unit.Faction);
            double expected = Mounted(unit).Select(weapons.GetProfile).Where(profile => profile.DamageType == DamageType.Ion)
                .Sum(profile => (double)profile.DamagePerSecond) * matrix.GetShipShieldMultiplier(DamageType.Ion) *
                matrix.GetFactionDamageMultiplier(faction, DamageType.Ion);

            using (BalanceDpsEstimator estimator = new BalanceDpsEstimator(registry))
            {
                BalanceDpsEstimate saved = estimator.EstimateIonVsShields(unit, new BalanceDraft());
                Assert.That(saved.HasValue, Is.True, saved.Problem);
                Assert.That(saved.Value, Is.GreaterThan(0));
                Assert.That(saved.Value, Is.EqualTo(expected).Within(expected * 1e-5));

                // Doubling the draft Ion vs shield value doubles the estimate.
                BalanceField vsShield = registry.Fields.Values.Single(field => field.Stat == "matrix/" + (int)DamageType.Ion + "/vsShield");
                BalanceDraft draft = new BalanceDraft();
                draft.SetMany(new List<BalanceSnapshot> { vsShield.Snapshot() }, new List<string>
                    { (double.Parse(vsShield.Read(), CultureInfo.InvariantCulture) * 2).ToString("R", CultureInfo.InvariantCulture) });
                Assert.That(estimator.EstimateIonVsShields(unit, draft).Value, Is.EqualTo(saved.Value * 2).Within(saved.Value * 1e-5));
            }
        }

        [Test]
        public void Estimate_UnarmedUnitHasNoValue()
        {
            BalanceRegistration registry = new BalanceRegistration();
            BalanceUnit unit = new BalanceUnit { Id = "test/unarmed", Kind = BalanceUnitKind.Ship };

            using (BalanceDpsEstimator estimator = new BalanceDpsEstimator(registry))
            {
                BalanceDpsEstimate estimate = estimator.Estimate(unit, new BalanceDraft());
                Assert.That(estimate.HasValue, Is.False);
                Assert.That(estimate.ToString(), Is.EqualTo(BalanceDpsEstimate.NOT_ARMED));
            }
        }

        private static WeaponProfile Profile(float damage, int shots, float interval, float reload) =>
            new WeaponProfile(WeaponType.Laser, "Test", WeaponFamily.Laser, DamageType.Laser, damage, shots, interval,
                reload, 100f, false, false, null, 100f, Color.white, Vector3.one, false, false, default, default, 0f);
    }
}
