using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Editor.BattleSimulator;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BattleBindingTests
    {
        [Test]
        public void RosterAudit_ReportsEveryShip_AndBuildsThreeShipsAndOneSquadron()
        {
            BalanceRegistration registry = BalanceInventory.BuildCombat();
            int ships = 0, squadrons = 0;
            var errors = new List<string>();
            using (var factory = new SimUnitFactory(registry, new BalanceDraft()))
                foreach (BalanceUnit unit in registry.Units)
                {
                    try
                    {
                        SimUnitDefinition definition = factory.Build(unit);
                        Assert.That(definition.HardPoints.Count, Is.GreaterThan(0), unit.Caption);
                        foreach (SimMountDefinition mount in definition.Mounts)
                        {
                            Assert.That(mount.Profile, Is.Not.Null, unit.Caption);
                            Assert.That(mount.HardPointId, Is.InRange(0, definition.HardPoints.Count - 1), unit.Caption);
                        }
                        if (definition.IsSquadron) squadrons++; else ships++;
                    }
                    catch (InvalidOperationException exception)
                    {
                        Assert.That(exception.Message, Does.Contain("no explicit health binding"), unit.Caption);
                        errors.Add(unit.Caption + ": " + exception.Message);
                    }
                }
            Directory.CreateDirectory("Library/BattleSimulator");
            File.WriteAllLines("Library/BattleSimulator/binding-errors.txt", errors);
            TestContext.WriteLine($"Roster {registry.Units.Count}; valid ships {ships}; valid squadrons {squadrons}; blocked {errors.Count}");
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
    }
}
