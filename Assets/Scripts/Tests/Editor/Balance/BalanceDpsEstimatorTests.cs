using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Editor.Balance;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BalanceDpsEstimatorTests
    {
        [TestCase(1, 0.5, 2.0, 2.0)]
        [TestCase(4, 0.25, 3.0, 3.0)]
        [TestCase(8, 3.0, 15.0, 21.0)]
        public void Cycle_IsReloadOrSalvoDurationWhicheverIsLonger(int shots, double interval, double reload, double expected)
        {
            Assert.That(BalanceWeaponMath.CycleSeconds(shots, interval, reload), Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void SalvoDps_LongSalvoIsLimitedBySalvoDuration()
        {
            Assert.That(BalanceWeaponMath.SalvoDps(21, 8, 3, 15), Is.EqualTo(8).Within(1e-9));
        }

        [Test]
        public void Estimate_UsesDraftProfilesAndSalvoDuration()
        {
            BalanceRegistration registry = BalanceInventory.Build();
            Assert.That(registry.Errors, Is.Empty);
            BalanceUnit unit = registry.Units.First(entry => entry.Kind == "Ship" && entry.Mounts.Any());
            List<BalanceField> mounts = unit.Mounts.Distinct().SelectMany(registry.FieldsFor)
                .Where(field => !field.SharedMountSource && field.Stat == "WeaponType").ToList();
            Assert.That(mounts, Is.Not.Empty);

            BalanceDraft draft = new BalanceDraft();
            var values = new Dictionary<string, string> { ["damage"] = "21", ["shotsPerSalvo"] = "8", ["shotInterval"] = "3", ["reload"] = "15" };
            List<BalanceField> profiles = mounts.Select(mount => mount.Read()).Distinct()
                .SelectMany(id => values.Keys.Select(stat => registry.Fields.Values.Single(field => field.Stat == "weapon/" + id + "/" + stat)))
                .ToList();
            draft.SetMany(profiles.Select(field => field.Snapshot()).ToList(), profiles.Select(field => values[field.Stat.Split('/')[2]]).ToList());

            BalanceDpsEstimate estimate = new BalanceDpsEstimator(registry).Estimate(unit, draft);

            Assert.That(estimate.HasValue, Is.True, estimate.Problem);
            Assert.That(estimate.Value, Is.EqualTo(mounts.Count * 8.0).Within(1e-6));
        }

        [Test]
        public void Estimate_UnarmedUnitHasNoValue()
        {
            BalanceRegistration registry = new BalanceRegistration();
            BalanceUnit unit = new BalanceUnit { Id = "test/unarmed", Kind = "Ship" };

            BalanceDpsEstimate estimate = new BalanceDpsEstimator(registry).Estimate(unit, new BalanceDraft());

            Assert.That(estimate.HasValue, Is.False);
            Assert.That(estimate.ToString(), Is.EqualTo(BalanceDpsEstimate.NOT_ARMED));
        }
    }
}
