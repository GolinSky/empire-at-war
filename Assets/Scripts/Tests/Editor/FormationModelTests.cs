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
    }
}
