using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Movement
{
    public sealed class FormationModelTests
    {
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
