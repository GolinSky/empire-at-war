using System;
using System.Linq;
using System.Globalization;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Editor.BattleSimulator;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Simulation")]
    public sealed class BattleBindingTests
    {
        [Test]
        public void RosterAudit_BuildsEveryUnit_WithEveryWeaponTargetable()
        {
            BalanceRegistration registry = BalanceInventory.BuildCombat();
            int ships = 0, squadrons = 0;
            using (var factory = new SimUnitFactory(registry, new BalanceDraft()))
                foreach (BalanceUnit unit in registry.Units)
                {
                    // Throws when a weapon mount is missing from its health bindings (non-targetable weapon).
                    SimUnitDefinition definition = factory.Build(unit);
                    Assert.That(definition.HardPoints.Count, Is.GreaterThan(0), unit.Caption);
                    foreach (SimMountDefinition mount in definition.Mounts)
                    {
                        Assert.That(mount.Profile, Is.Not.Null, unit.Caption);
                        Assert.That(mount.HardPointId, Is.InRange(0, definition.HardPoints.Count - 1), unit.Caption);
                    }
                    if (definition.IsSquadron) squadrons++; else ships++;
                }
            TestContext.WriteLine($"Roster {registry.Units.Count}; ships {ships}; squadrons {squadrons}");
            Assert.That(ships, Is.GreaterThanOrEqualTo(3)); Assert.That(squadrons, Is.GreaterThanOrEqualTo(1));
            Assert.That(registry.Units.Select(u => u.Faction).Distinct().Count(), Is.EqualTo(4));
        }

        [Test]
        public void DraftHealth_IsCopiedWithoutChangingLiveAssets()
        {
            BalanceRegistration registry = BalanceInventory.BuildCombat();
            BalanceUnit unit = registry.Units.First(u => u.Kind == BalanceUnitKind.Ship);
            BalanceField field = registry.FieldsFor(unit.Data).Single(f => f.Stat == "Hull");
            string before = field.Read();
            var draft = new BalanceDraft(); draft.Set(field.Snapshot(), "12345");
            using (var factory = new SimUnitFactory(registry, draft)) Assert.That(factory.Build(unit).Hull, Is.EqualTo(12345));
            Assert.That(field.Read(), Is.EqualTo(before));
        }

        [Test]
        public void DraftWeaponProfile_ChangesSimulationDamageWithoutWritingSource()
        {
            var registry = BalanceInventory.BuildCombat();
            var unit = registry.Units.First(u => u.Kind == BalanceUnitKind.Ship);
            using (var live = new SimUnitFactory(registry, new BalanceDraft()))
            {
                var original = live.Build(unit).Mounts[0].Profile;
                var field = registry.FieldsFor(BalanceRegistration.One<WeaponsData>()).Single(f =>
                    f.Stat == "weapon/" + (int)original.WeaponType + "/damageMultiplier");
                string before = field.Read();
                var draft = new BalanceDraft();
                draft.Set(field.Snapshot(), (double.Parse(before, CultureInfo.InvariantCulture) * 2).ToString(CultureInfo.InvariantCulture));
                using (var changed = new SimUnitFactory(registry, draft))
                    Assert.That(changed.Build(unit).Mounts[0].Profile.Damage, Is.EqualTo(original.Damage * 2).Within(0.001));
                Assert.That(field.Read(), Is.EqualTo(before));
            }
        }
    }
}
