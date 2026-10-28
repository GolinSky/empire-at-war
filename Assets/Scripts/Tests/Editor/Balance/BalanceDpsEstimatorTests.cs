using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Editor.Balance;
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
