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
    }
}
