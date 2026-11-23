using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.EnemyFaction.Models;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Navigation")]
    public sealed class FleetCohesionTests
    {
        private const float RADIUS = 800f;

        // Point-sized hulls: the distances below are hull gaps.
        private static List<float> PointHulls(int count) => Enumerable.Repeat(0f, count).ToList();

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
            List<float> hulls = PointHulls(positions.Count);

            int anchor = FleetCohesion.FindAnchor(positions, hulls, RADIUS);

            Assert.That(anchor, Is.Not.EqualTo(0));
            Assert.That(FleetCohesion.IsInBody(positions[anchor], 0f, positions[0], 0f, RADIUS, false), Is.False);
            for (int i = 1; i < positions.Count; i++)
                Assert.That(FleetCohesion.IsInBody(positions[anchor], 0f, positions[i], 0f, RADIUS, false), Is.True);
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
            Assert.That(FleetCohesion.FindAnchor(positions, PointHulls(positions.Count), RADIUS, heldIndex: 2),
                Is.EqualTo(2));

            positions.Add(new FormationPoint(100f, 100f));
            positions.Add(new FormationPoint(200f, 100f));
            Assert.That(FleetCohesion.FindAnchor(positions, PointHulls(positions.Count), RADIUS, heldIndex: 3),
                Is.Not.EqualTo(3));
        }

        [Test]
        public void IsTogether_FailsWhileAnyShipIsOutsideTheBody()
        {
            List<FormationPoint> positions = new List<FormationPoint>
            {
                new FormationPoint(0f, 0f), new FormationPoint(300f, 0f), new FormationPoint(0f, 300f)
            };
            Assert.That(FleetCohesion.IsTogether(positions, PointHulls(positions.Count), RADIUS), Is.True);

            positions.Add(new FormationPoint(5000f, 0f));
            Assert.That(FleetCohesion.IsTogether(positions, PointHulls(positions.Count), RADIUS), Is.False);
            Assert.That(FleetCohesion.IsTogether(new List<FormationPoint>(), new List<float>(), RADIUS), Is.True);
        }

        [Test]
        public void IsInBody_MembersKeepTheirPlaceBeyondTheJoinRadius()
        {
            FormationPoint anchor = new FormationPoint(0f, 0f);
            FormationPoint edge = new FormationPoint(RADIUS * 1.2f, 0f);

            Assert.That(FleetCohesion.IsInBody(anchor, 0f, edge, 0f, RADIUS, wasInBody: false), Is.False);
            Assert.That(FleetCohesion.IsInBody(anchor, 0f, edge, 0f, RADIUS, wasInBody: true), Is.True);
            Assert.That(FleetCohesion.IsInBody(anchor, 0f, new FormationPoint(RADIUS * 2f, 0f), 0f, RADIUS,
                wasInBody: true), Is.False);
        }

        // W55: a hull wider than the cohesion radius (Viscount NavigationRadius > RADIUS) parked beside its escorts
        // counts as regrouped; before, the force rallied forever.
        [Test]
        public void IsTogether_CountsHullGaps_SoAHullWiderThanTheRadiusCanRegroup()
        {
            float big = RADIUS + 100f;
            const float ESCORT = 50f;
            float touching = big + ESCORT + 10f;
            List<FormationPoint> positions = new List<FormationPoint>
            {
                new FormationPoint(0f, 0f), new FormationPoint(touching, 0f), new FormationPoint(-touching, 0f)
            };
            List<float> hulls = new List<float> { big, ESCORT, ESCORT };

            Assert.That(FleetCohesion.IsTogether(positions, PointHulls(positions.Count), RADIUS), Is.False,
                "Measured centre to centre the escorts look like stragglers.");
            Assert.That(FleetCohesion.IsTogether(positions, hulls, RADIUS), Is.True);

            positions.Add(new FormationPoint(touching + big + RADIUS * 2f, 0f));
            hulls.Add(ESCORT);
            Assert.That(FleetCohesion.IsTogether(positions, hulls, RADIUS), Is.False,
                "A hull gap beyond the radius is still a straggler.");
        }
    }
}
