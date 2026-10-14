using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Camera
{
    public sealed class SimulationMapNavigationTests
    {
        [TestCase(1, 2000f, 200f, 1350f)]
        [TestCase(100, 2000f, 600f, 6950f)]
        [TestCase(1, 10000f, 2000f, 6250f)]
        public void Layout_FitsFormationWithoutTheHundredKilometreMinimap(
            int count, float distance, float spacing, float expectedExtent)
        {
            var fleet = new[] { new SimulationFleetEntry(UnitTypeId.Ship(ShipType.Venator), count) };
            var setup = new SimulationSetup(FactionType.Republic, FactionType.Separatist,
                fleet, fleet, distance, 300f, false);

            var layout = EmptyLayoutGenerator.Generate(setup, spacing, spacing);

            Assert.That(layout.SizeRange.Min, Is.EqualTo(Vector2.one * -expectedExtent));
            Assert.That(layout.SizeRange.Max, Is.EqualTo(Vector2.one * expectedExtent));
        }

        [Test]
        public void Restart_UpdatesSharedRangeWithoutChangingThePreviousLayoutBounds()
        {
            var fleet = new[] { new SimulationFleetEntry(UnitTypeId.Ship(ShipType.Venator), 1) };
            var small = new SimulationSetup(FactionType.Republic, FactionType.Separatist,
                fleet, fleet, 2000f, 300f, false);
            var large = new SimulationSetup(FactionType.Republic, FactionType.Separatist,
                fleet, fleet, 10000f, 300f, false);
            var initial = EmptyLayoutGenerator.Generate(small, 200f, 200f);
            var next = EmptyLayoutGenerator.Generate(large, 200f, 200f);
            var minimapRange = initial.SizeRange;

            initial.SizeRange.SetValue(next.SizeRange);

            Assert.That(minimapRange.Max.x, Is.EqualTo(5350f));
            initial.SizeRange.SetValue(EmptyLayoutGenerator.Generate(small, 200f, 200f).SizeRange);
            Assert.That(minimapRange.Max.x, Is.EqualTo(1350f));
            Assert.That(next.SizeRange.Max.x, Is.EqualTo(5350f));
        }
    }
}
