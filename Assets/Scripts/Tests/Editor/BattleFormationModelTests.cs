using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BattleFormationModelTests
    {
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(12)]
        public void Perimeter_ClearsObjectiveAndOtherShips(int count)
        {
            List<FormationPoint> positions = new List<FormationPoint>();
            List<float> radii = new List<float>();
            List<FormationPoint> destinations = new List<FormationPoint>();
            FormationPoint objective = new FormationPoint(160f, -170f);
            for (int i = 0; i < count; i++)
            {
                positions.Add(objective);
                radii.Add(i % 2 == 0 ? 28f : 5f);
            }

            BattleFormationModel.CalculateDestinations(positions, radii, objective, destinations);

            Assert.That(destinations.Count, Is.EqualTo(count));
            for (int i = 0; i < count; i++)
            {
                Assert.That(FormationModel.HasClearance(destinations[i], radii[i], objective, 28f), Is.True);
                for (int j = i + 1; j < count; j++)
                {
                    Assert.That(FormationModel.HasClearance(
                        destinations[i], radii[i], destinations[j], radii[j]), Is.True);
                }
            }
        }

        [Test]
        public void ReissuedFormation_KeepsArrivedShipsInTheirSlots()
        {
            FormationPoint[] positions = { new FormationPoint(-20f, 0f), new FormationPoint(20f, 0f) };
            float[] radii = { 5f, 10f };
            List<FormationPoint> first = new List<FormationPoint>();
            List<FormationPoint> second = new List<FormationPoint>();

            BattleFormationModel.CalculateDestinations(positions, radii, default, first);
            BattleFormationModel.CalculateDestinations(first, radii, default, second);

            Assert.That(second, Is.EqualTo(first));
        }
    }
}
