using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Movement
{
    [Category("Navigation")]
    public sealed class FormationModelTests
    {
        [Test]
        public void CompactDestinations_RotateWithTravelDirectionAndClearOccupiedCenter()
        {
            var east = new List<FormationPoint>();
            var north = new List<FormationPoint>();
            var positions = new[] { new FormationPoint(0f, 0f), new FormationPoint(2f, 0f) };
            var rotated = new[] { new FormationPoint(0f, 0f), new FormationPoint(0f, 2f) };
            var radii = new[] { 5f, 5f };
            FormationModel.CalculateCompactDestinations(positions, radii,
                new FormationPoint(100f, 0f), east, 50f);
            FormationModel.CalculateCompactDestinations(rotated, radii,
                new FormationPoint(0f, 100f), north, 50f);
            for (int i = 0; i < east.Count; i++)
            {
                Assert.That(north[i].X, Is.EqualTo(-east[i].Z).Within(0.001f));
                Assert.That(north[i].Z, Is.EqualTo(east[i].X).Within(0.001f));
                Assert.That(FormationModel.HasClearance(east[i], 5f,
                    new FormationPoint(100f, 0f), 50f), Is.True);
            }
            Assert.That(FormationModel.HasClearance(east[0], 5f, east[1], 5f), Is.True);
        }

        private const int HEAVY_CRUISER = 4;
        private const int CAPITAL = 5;

        [Test]
        public void RowDestinations_HeavyCruiserLeadsWithoutCapitalsAndFollowsThem()
        {
            var positions = new[] { new FormationPoint(-20f, 0f), new FormationPoint(20f, 0f) };
            var radii = new[] { 5f, 5f };
            var backRow = new[] { false, false };
            var target = new FormationPoint(0f, 500f);
            var destinations = new List<FormationPoint>();
            var rows = new List<FormationRow>();

            FormationModel.CalculateRowDestinations(positions, radii, new[] { HEAVY_CRUISER, HEAVY_CRUISER }, backRow,
                target, destinations, rows);
            Assert.That(rows, Is.All.EqualTo(FormationRow.Front));

            FormationModel.CalculateRowDestinations(positions, radii, new[] { HEAVY_CRUISER, CAPITAL }, backRow,
                target, destinations, rows);
            Assert.That(rows[0], Is.EqualTo(FormationRow.Middle));
            Assert.That(rows[1], Is.EqualTo(FormationRow.Front));
            Assert.That(destinations[1].Z, Is.GreaterThan(destinations[0].Z), "The strongest tier present leads.");
            Assert.That(FormationModel.HasClearance(destinations[0], 5f, destinations[1], 5f), Is.True);
        }

        [Test]
        public void RowDestinations_BackRowShipsNeverLeadWhateverTheirTier()
        {
            var positions = new[]
            {
                new FormationPoint(0f, 40f), new FormationPoint(-30f, 0f), new FormationPoint(30f, 0f)
            };
            var radii = new[] { 8f, 5f, 5f };
            var tiers = new[] { CAPITAL, HEAVY_CRUISER, 2 };
            var backRow = new[] { true, false, false };
            var target = new FormationPoint(0f, 800f);
            var destinations = new List<FormationPoint>();
            var rows = new List<FormationRow>();

            FormationModel.CalculateRowDestinations(positions, radii, tiers, backRow, target, destinations, rows);

            Assert.That(rows, Is.EqualTo(new[] { FormationRow.Back, FormationRow.Front, FormationRow.Middle }));
            Assert.That(destinations[1].Z, Is.GreaterThan(destinations[2].Z));
            Assert.That(destinations[2].Z, Is.GreaterThan(destinations[0].Z));
            for (int i = 0; i < destinations.Count; i++)
            for (int j = i + 1; j < destinations.Count; j++)
                Assert.That(FormationModel.HasClearance(destinations[i], radii[i], destinations[j], radii[j]), Is.True);
        }

        [Test]
        public void RowDestinations_ShipsKeepTheirSideWithinARow()
        {
            var positions = new[] { new FormationPoint(40f, 0f), new FormationPoint(-40f, 0f), new FormationPoint(0f, -60f) };
            var radii = new[] { 5f, 5f, 5f };
            var tiers = new[] { CAPITAL, CAPITAL, 2 };
            var backRow = new[] { false, false, false };
            var destinations = new List<FormationPoint>();
            var rows = new List<FormationRow>();

            FormationModel.CalculateRowDestinations(positions, radii, tiers, backRow, new FormationPoint(0f, 500f),
                destinations, rows);

            Assert.That(destinations[0].X, Is.GreaterThan(destinations[1].X), "No crossing paths inside the row.");
        }

        [Test]
        public void RowDestinations_OneTierMatchesTheCompactClump()
        {
            var positions = new[] { new FormationPoint(0f, 0f), new FormationPoint(10f, 0f), new FormationPoint(20f, 5f) };
            var radii = new[] { 5f, 6f, 5f };
            var target = new FormationPoint(300f, 300f);
            var compact = new List<FormationPoint>();
            var destinations = new List<FormationPoint>();
            var rows = new List<FormationRow>();

            FormationModel.CalculateCompactDestinations(positions, radii, target, compact);
            FormationModel.CalculateRowDestinations(positions, radii, new[] { 3, 3, 3 },
                new[] { false, false, false }, target, destinations, rows);

            Assert.That(destinations, Is.EqualTo(compact));
            Assert.That(rows, Is.All.EqualTo(FormationRow.Front));
        }

        [Test]
        public void CalculateDestination_PreservesOffsetsAroundTargetCenter()
        {
            List<FormationPoint> positions = new List<FormationPoint>
            {
                new FormationPoint(10f, 20f),
                new FormationPoint(14f, 24f)
            };
            FormationPoint center = FormationModel.CalculateCenter(positions);
            FormationPoint targetCenter = new FormationPoint(100f, 200f);

            FormationPoint first = FormationModel.CalculateDestination(positions[0], center, targetCenter);
            FormationPoint second = FormationModel.CalculateDestination(positions[1], center, targetCenter);

            Assert.That(first.X, Is.EqualTo(98f));
            Assert.That(first.Z, Is.EqualTo(198f));
            Assert.That(second.X, Is.EqualTo(102f));
            Assert.That(second.Z, Is.EqualTo(202f));
        }

        [Test]
        public void HasClearance_RejectsOverlappingShipRadii()
        {
            FormationPoint position = new FormationPoint(10f, -5f);

            Assert.That(
                FormationModel.HasClearance(
                    position,
                    5f,
                    position,
                    5f),
                Is.False);
            Assert.That(
                FormationModel.HasClearance(
                    position,
                    5f,
                    new FormationPoint(20f, -5f),
                    5f),
                Is.True);
        }

        [Test]
        public void CalculateCenter_ReturnsOriginForEmptyFormation()
        {
            FormationPoint center = FormationModel.CalculateCenter(new List<FormationPoint>());

            Assert.That(center.X, Is.Zero);
            Assert.That(center.Z, Is.Zero);
        }

        [Test]
        public void CalculateDestinations_PreservesDistinctOffsetsForSharedTarget()
        {
            List<FormationPoint> positions = new List<FormationPoint>
            {
                new FormationPoint(-6f, 4f),
                new FormationPoint(6f, -4f)
            };
            List<FormationPoint> destinations = new List<FormationPoint>();

            FormationModel.CalculateDestinations(
                positions,
                new FormationPoint(100f, 200f),
                destinations);

            Assert.That(destinations, Has.Count.EqualTo(2));
            Assert.That(destinations[0].X, Is.EqualTo(94f));
            Assert.That(destinations[0].Z, Is.EqualTo(204f));
            Assert.That(destinations[1].X, Is.EqualTo(106f));
            Assert.That(destinations[1].Z, Is.EqualTo(196f));
        }

        [Test]
        public void CalculateGridDestination_AssignsDistinctCenteredSlots()
        {
            FormationPoint target = new FormationPoint(100f, 200f);

            FormationPoint first = FormationModel.CalculateGridDestination(0, 4, target, 12f);
            FormationPoint fourth = FormationModel.CalculateGridDestination(3, 4, target, 12f);

            Assert.That(first.X, Is.EqualTo(94f));
            Assert.That(first.Z, Is.EqualTo(194f));
            Assert.That(fourth.X, Is.EqualTo(106f));
            Assert.That(fourth.Z, Is.EqualTo(206f));
        }

        [Test]
        public void CalculateCompactDestinations_AssignsClosestShipToCenter()
        {
            List<FormationPoint> positions = new List<FormationPoint>
            {
                new FormationPoint(-100f, 0f),
                new FormationPoint(8f, 0f)
            };
            List<float> radii = new List<float> { 5f, 5f };
            List<FormationPoint> destinations = new List<FormationPoint>();
            FormationPoint target = new FormationPoint(10f, 0f);

            FormationModel.CalculateCompactDestinations(
                positions,
                radii,
                target,
                destinations);

            Assert.That(destinations, Has.Count.EqualTo(2));
            Assert.That(destinations[1].X, Is.EqualTo(target.X));
            Assert.That(destinations[1].Z, Is.EqualTo(target.Z));
            Assert.That(
                Distance(destinations[0], target),
                Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void CalculateCompactDestinations_SeparatesEveryShip()
        {
            List<FormationPoint> positions = new List<FormationPoint>
            {
                new FormationPoint(-50f, 0f),
                new FormationPoint(0f, 50f),
                new FormationPoint(50f, 0f),
                new FormationPoint(0f, -50f)
            };
            List<float> radii = new List<float> { 4f, 6f, 8f, 5f };
            List<FormationPoint> destinations = new List<FormationPoint>();

            FormationModel.CalculateCompactDestinations(
                positions,
                radii,
                default,
                destinations);

            for (int first = 0; first < destinations.Count; first++)
            {
                for (int second = first + 1;
                     second < destinations.Count;
                     second++)
                {
                    Assert.That(
                        Distance(
                            destinations[first],
                            destinations[second]),
                        Is.GreaterThanOrEqualTo(
                            radii[first] + radii[second] - 0.001f));
                }
            }
        }

        private static float Distance(
            FormationPoint first,
            FormationPoint second)
        {
            float x = first.X - second.X;
            float z = first.Z - second.Z;
            return (float)System.Math.Sqrt(x * x + z * z);
        }
    }
}
