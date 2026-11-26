using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Hit-and-run safe point: repairer, own station, main fleet body, held zone; never cut off or under fire.</summary>
    [Category("EnemyAi")]
    public sealed class EnemyStrikeFallbackPointTests
    {
        [Test]
        public void Select_PrefersRepairerThenStationThenMainBodyThenHeldZone()
        {
            EnemyStrikeFallbackCandidate[] candidates =
            {
                Valid(EnemyStrikeFallbackKind.HeldZone), Valid(EnemyStrikeFallbackKind.MainBody),
                Valid(EnemyStrikeFallbackKind.Station), Valid(EnemyStrikeFallbackKind.Repairer)
            };

            Assert.That(EnemyStrikeFallbackRules.Select(candidates, defendingStation: false), Is.EqualTo(3));
            Assert.That(EnemyStrikeFallbackRules.Select(new[] { candidates[0], candidates[1], candidates[2] }, false),
                Is.EqualTo(2));
            Assert.That(EnemyStrikeFallbackRules.Select(new[] { candidates[0], candidates[1] }, false), Is.EqualTo(1));
            Assert.That(EnemyStrikeFallbackRules.Select(new[] { candidates[0] }, false), Is.EqualTo(0));
        }

        [Test]
        public void Select_SkipsAPointThatIsCutOffOrUnderHostileFire()
        {
            EnemyStrikeFallbackCandidate[] candidates =
            {
                new EnemyStrikeFallbackCandidate(EnemyStrikeFallbackKind.Repairer, isCutOff: true, isInHostileReach: false),
                new EnemyStrikeFallbackCandidate(EnemyStrikeFallbackKind.Station, isCutOff: false, isInHostileReach: true),
                Valid(EnemyStrikeFallbackKind.HeldZone)
            };

            Assert.That(EnemyStrikeFallbackRules.Select(candidates, defendingStation: false), Is.EqualTo(2));
        }

        [Test]
        public void Select_WhileDefendingTheStation_OnlyTheStationPointCounts()
        {
            EnemyStrikeFallbackCandidate[] candidates =
                { Valid(EnemyStrikeFallbackKind.Repairer), Valid(EnemyStrikeFallbackKind.Station) };

            Assert.That(EnemyStrikeFallbackRules.Select(candidates, defendingStation: true), Is.EqualTo(1));
            Assert.That(EnemyStrikeFallbackRules.Select(new[] { candidates[0] }, defendingStation: true), Is.EqualTo(-1));
        }

        [Test]
        public void Select_WithoutAValidPoint_FindsNone()
        {
            Assert.That(EnemyStrikeFallbackRules.Select(new EnemyStrikeFallbackCandidate[0], false), Is.EqualTo(-1));
        }

        [Test]
        public void StationCoverPoint_LiesBehindTheStationClearOfItsHull()
        {
            const float STATION_RADIUS = 300f;
            const float SHIP_RADIUS = 80f;
            var station = new FormationPoint(1000f, 500f);
            var threat = new FormationPoint(1000f, 2500f);

            FormationPoint point = EnemyStrikeFallbackRules.GetStationCoverPoint(station, threat, STATION_RADIUS, SHIP_RADIUS);

            Assert.That(point.X, Is.EqualTo(station.X).Within(0.01f));
            Assert.That(point.Z, Is.LessThan(station.Z), "The point is on the far side from the threat.");
            Assert.That(station.Z - point.Z, Is.EqualTo(STATION_RADIUS + SHIP_RADIUS +
                EnemyStrikeFallbackRules.STATION_COVER_MARGIN).Within(0.01f), "The ship hull stays outside the station hull.");
        }

        [Test]
        public void StationCoverPoint_ThreatOnTheStation_StillGivesAPointOutsideTheHull()
        {
            var station = new FormationPoint(10f, 20f);

            FormationPoint point = EnemyStrikeFallbackRules.GetStationCoverPoint(station, station, 300f, 80f);

            float x = point.X - station.X;
            float z = point.Z - station.Z;
            Assert.That(x * x + z * z, Is.GreaterThan(380f * 380f));
        }

        private static EnemyStrikeFallbackCandidate Valid(EnemyStrikeFallbackKind kind) =>
            new EnemyStrikeFallbackCandidate(kind, isCutOff: false, isInHostileReach: false);
    }
}
