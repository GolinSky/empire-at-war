using System.Linq;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class HostileIntelModelTests
    {
        [TestCase(0f, 1f)]
        [TestCase(IntelConfidence.FRESH_AGE, 1f)]
        [TestCase(55f, 0.5f)]
        [TestCase(IntelConfidence.STALE_AGE, 0.5f)]
        [TestCase(165f, 0.25f)]
        [TestCase(IntelConfidence.FORGET_AGE, 0f)]
        public void Confidence_FadesFromFreshThroughStaleToForgotten(float age, float expected)
        {
            Assert.That(IntelConfidence.Evaluate(age), Is.EqualTo(expected).Within(1e-4f));
        }

        [Test]
        public void OlderReport_DoesNotOverwriteNewerAllyReport()
        {
            HostileIntelModel intel = new HostileIntelModel();
            intel.Report(Sighting(seenAt: 50f, hull: 100f));
            intel.Report(Sighting(seenAt: 40f, hull: 900f));

            Assert.That(intel.Sightings.Single().Hull, Is.EqualTo(100f));
        }

        [Test]
        public void ForgottenRecords_AreDropped()
        {
            HostileIntelModel intel = new HostileIntelModel();
            intel.Report(Sighting(seenAt: 0f, hull: 100f));

            intel.ForgetExpired(IntelConfidence.FORGET_AGE + 1f);

            Assert.That(intel.Sightings, Is.Empty);
        }

        [Test]
        public void ScoutTarget_IsTheStaleRecordWorthMost()
        {
            HostileIntelModel intel = new HostileIntelModel();
            intel.Report(Sighting(1, seenAt: 0f, hull: 100f, x: 10f));
            intel.Report(Sighting(2, seenAt: 0f, hull: 5000f, x: 20f));
            intel.Report(Sighting(3, seenAt: 95f, hull: 90000f, x: 30f));

            Assert.That(intel.TryGetScoutTarget(100f, out FormationPoint target), Is.True);
            Assert.That(target.X, Is.EqualTo(20f));
        }

        [Test]
        public void FreshIntel_NeedsNoScout()
        {
            HostileIntelModel intel = new HostileIntelModel();
            intel.Report(Sighting(seenAt: 95f, hull: 100f));

            Assert.That(intel.TryGetScoutTarget(100f, out _), Is.False);
        }

        private static HostileSighting Sighting(long id = 1, float seenAt = 0f, float hull = 100f, float x = 0f) =>
            new HostileSighting(id, UnitTypeId.Ship(ShipType.Venator), TestPlayers.Human, new FormationPoint(x, 0f),
                hull, 0f, 1f, seenAt);
    }
}
