using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.EnemyFaction.Models;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Navigation")]
    public sealed class FleetCohesionTests
    {
        private const float RADIUS = 800f;

        [Test]
        public void FindAnchor_PicksTheShipInTheLargestGroup_NotAFarStraggler()
        {
            List<FormationPoint> positions = new List<FormationPoint>
            {
                new FormationPoint(5000f, 0f),
                new FormationPoint(0f, 0f),
                new FormationPoint(300f, 0f),
                new FormationPoint(0f, 300f),
            };

            int anchor = FleetCohesion.FindAnchor(positions, RADIUS);

            Assert.That(anchor, Is.Not.EqualTo(0));
            Assert.That(FleetCohesion.IsInBody(positions[anchor], positions[0], RADIUS, false), Is.False);
            for (int i = 1; i < positions.Count; i++)
                Assert.That(FleetCohesion.IsInBody(positions[anchor], positions[i], RADIUS, false), Is.True);
        }

        [Test]
        public void FindAnchor_KeepsHeldAnchorUnlessAnotherHasClearlyMoreNeighbours()
        {
            List<FormationPoint> positions = new List<FormationPoint>
            {
                new FormationPoint(0f, 0f), new FormationPoint(300f, 0f), new FormationPoint(0f, 300f),
                new FormationPoint(5000f, 0f)
            };
            // Ship 0 and 1 and 2 each have 2 neighbours; the earlier index wins the plain search.
            Assert.That(FleetCohesion.FindAnchor(positions, RADIUS, heldIndex: 2), Is.EqualTo(2));

            positions.Add(new FormationPoint(100f, 100f));
            positions.Add(new FormationPoint(200f, 100f));
            Assert.That(FleetCohesion.FindAnchor(positions, RADIUS, heldIndex: 3), Is.Not.EqualTo(3));
        }

        [Test]
        public void IsTogether_FailsWhileAnyShipIsOutsideTheBody()
        {
            List<FormationPoint> positions = new List<FormationPoint>
            {
                new FormationPoint(0f, 0f), new FormationPoint(300f, 0f), new FormationPoint(0f, 300f)
            };
            Assert.That(FleetCohesion.IsTogether(positions, RADIUS), Is.True);

            positions.Add(new FormationPoint(5000f, 0f));
            Assert.That(FleetCohesion.IsTogether(positions, RADIUS), Is.False);
            Assert.That(FleetCohesion.IsTogether(new List<FormationPoint>(), RADIUS), Is.True);
        }

        [Test]
        public void IsInBody_MembersKeepTheirPlaceBeyondTheJoinRadius()
        {
            FormationPoint anchor = new FormationPoint(0f, 0f);
            FormationPoint edge = new FormationPoint(RADIUS * 1.2f, 0f);

            Assert.That(FleetCohesion.IsInBody(anchor, edge, RADIUS, wasInBody: false), Is.False);
            Assert.That(FleetCohesion.IsInBody(anchor, edge, RADIUS, wasInBody: true), Is.True);
            Assert.That(FleetCohesion.IsInBody(anchor, new FormationPoint(RADIUS * 2f, 0f), RADIUS, wasInBody: true),
                Is.False);
        }
    }
}
