using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Capture")]
    public sealed class CaptureSiteModelTests
    {
        private const float CAPTURE_DURATION = 10f;

        private static readonly PlayerRoster ROSTER = TestPlayers.CreateDuel();

        [Test]
        public void TickCapture_UncontestedShips_CaptureNeutralSite()
        {
            CaptureSiteModel model = new CaptureSiteModel(captureDuration: CAPTURE_DURATION, captureSpeedPerNetShip: 1f, relations: ROSTER);

            Assert.That(model.TickCapture(5f, TestPlayers.DuelTally(ROSTER, 1, 0)), Is.False);
            Assert.That(model.TickCapture(5f, TestPlayers.DuelTally(ROSTER, 1, 0)), Is.True);

            Assert.That(model.Owner, Is.EqualTo(TestPlayers.Human));
            Assert.That(model.State, Is.EqualTo(CaptureSiteState.Owned));
            Assert.That(model.CanStartConstruction, Is.True);
        }

        [Test]
        public void TickCapture_EqualOpposingShips_IsContestedWithoutProgress()
        {
            CaptureSiteModel model = new CaptureSiteModel(captureDuration: CAPTURE_DURATION, captureSpeedPerNetShip: 1f, relations: ROSTER);

            model.TickCapture(CAPTURE_DURATION, TestPlayers.DuelTally(ROSTER, 2, 2));

            Assert.That(model.IsContested, Is.True);
            Assert.That(model.CaptureProgress, Is.Zero);
            Assert.That(model.Owner, Is.EqualTo(PlayerId.None));
        }

        [Test]
        public void ConstructionProgress_CompletedFacility_BecomesOperationalAndLocksCapture()
        {
            CaptureSiteModel model = CreateOwnedSite(TestPlayers.Human);
            model.StartConstruction(SiteFacilityType.Mining);

            Assert.That(model.SetConstructionProgress(0.5f), Is.False);
            Assert.That(model.SetConstructionProgress(1f), Is.True);
            Assert.That(model.State, Is.EqualTo(CaptureSiteState.Operational));

            Assert.That(model.TickCapture(CAPTURE_DURATION, TestPlayers.DuelTally(ROSTER, 0, 5)), Is.False);
            Assert.That(model.Owner, Is.EqualTo(TestPlayers.Human));
            Assert.That(model.CaptureProgress, Is.Zero);
        }

        [Test]
        public void TickCapture_EnemyAtSiteUnderConstruction_CannotCaptureFacility()
        {
            CaptureSiteModel model = CreateOwnedSite(TestPlayers.Human);
            model.StartConstruction(SiteFacilityType.Mining);
            model.SetConstructionProgress(0.5f);

            Assert.That(model.TickCapture(CAPTURE_DURATION, TestPlayers.DuelTally(ROSTER, 0, 1)), Is.False);

            Assert.That(model.Owner, Is.EqualTo(TestPlayers.Human));
            Assert.That(model.State, Is.EqualTo(CaptureSiteState.Constructing));
            Assert.That(model.ConstructionProgress, Is.EqualTo(0.5f));
        }

        [TestCase(0.5f)]
        [TestCase(1f)]
        public void ReleaseFacility_DestroyedFacility_ReturnsSiteToNeutral(float progress)
        {
            CaptureSiteModel model = CreateOwnedSite(TestPlayers.Enemy);
            model.StartConstruction(SiteFacilityType.Mining);
            model.SetConstructionProgress(progress);

            model.ReleaseFacility();

            Assert.That(model.Owner, Is.EqualTo(PlayerId.None));
            Assert.That(model.State, Is.EqualTo(CaptureSiteState.Neutral));
            Assert.That(model.TickCapture(CAPTURE_DURATION, TestPlayers.DuelTally(ROSTER, 1, 0)), Is.True);
            Assert.That(model.Owner, Is.EqualTo(TestPlayers.Human));
        }

        [Test]
        public void Tie_PreservesProgress_EmptyAreaResetsIt()
        {
            CaptureSiteModel model = new CaptureSiteModel(ROSTER, CAPTURE_DURATION, 1f);
            model.TickCapture(4f, TestPlayers.DuelTally(ROSTER, 1, 0));
            model.TickCapture(2f, TestPlayers.DuelTally(ROSTER, 1, 1));
            Assert.That(model.CaptureProgress, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(model.IsContested, Is.True);
            model.TickCapture(1f, TestPlayers.DuelTally(ROSTER, 0, 0));
            Assert.That(model.CaptureProgress, Is.Zero);
            Assert.That(model.CapturingPlayer, Is.EqualTo(PlayerId.None));
        }

        [Test]
        public void OperationalSite_ResetsCaptureAndContestedState()
        {
            CaptureSiteModel model = new CaptureSiteModel(ROSTER, CAPTURE_DURATION, 1f);
            model.Restore(TestPlayers.Human, CaptureSiteState.Operational, TestPlayers.Enemy, 0.4f,
                SiteFacilityType.Mining, 1f);
            model.TickCapture(1f, TestPlayers.DuelTally(ROSTER, 1, 2));
            Assert.That(model.IsContested, Is.False);
            Assert.That(model.CaptureProgress, Is.Zero);
            Assert.That(model.Owner, Is.EqualTo(TestPlayers.Human));
        }

        [Test]
        public void StartingOwner_ProducesEmptyOwnedSite()
        {
            CaptureSiteModel model = new CaptureSiteModel(ROSTER, CAPTURE_DURATION, 1f, TestPlayers.Human);
            Assert.That(model.Owner, Is.EqualTo(TestPlayers.Human));
            Assert.That(model.State, Is.EqualTo(CaptureSiteState.Owned));
            Assert.That(model.CanStartConstruction, Is.True);
        }

        [TestCase(SiteFacilityType.QuadLaserSatellite)]
        [TestCase(SiteFacilityType.TurbolaserSatellite)]
        [TestCase(SiteFacilityType.IonSatellite)]
        [TestCase(SiteFacilityType.MassDriverSatellite)]
        [TestCase(SiteFacilityType.MissileSatellite)]
        public void Satellite_RestoresConstructionAndReleasesAfterDestruction(SiteFacilityType type)
        {
            CaptureSiteModel model = new CaptureSiteModel(ROSTER, CAPTURE_DURATION, 1f);
            model.Restore(TestPlayers.Human, CaptureSiteState.Constructing, PlayerId.None, 0f, type, 0.5f);
            Assert.That(model.SetConstructionProgress(1f), Is.True);
            Assert.That(model.FacilityType, Is.EqualTo(type));
            Assert.That(model.TickCapture(100f, TestPlayers.DuelTally(ROSTER, 0, 5)), Is.False);
            model.ReleaseFacility();
            Assert.That(model.State, Is.EqualTo(CaptureSiteState.Neutral));
            Assert.That(model.Owner, Is.EqualTo(PlayerId.None));
        }

        private static CaptureSiteModel CreateOwnedSite(PlayerId owner)
        {
            CaptureSiteModel model = new CaptureSiteModel(captureDuration: CAPTURE_DURATION, captureSpeedPerNetShip: 1f, relations: ROSTER);
            int playerShips = owner == TestPlayers.Human ? 1 : 0;
            model.TickCapture(CAPTURE_DURATION, TestPlayers.DuelTally(ROSTER, playerShips, 1 - playerShips));
            return model;
        }

        [Test]
        public void Restore_ConstructingSite_TracksFacilityProgress()
        {
            CaptureSiteModel model = new CaptureSiteModel(captureDuration: CAPTURE_DURATION, captureSpeedPerNetShip: 1f, relations: ROSTER);

            model.Restore(TestPlayers.Human, CaptureSiteState.Constructing, PlayerId.None, 0f,
                SiteFacilityType.BattleAsteroid, 0.75f);

            Assert.That(model.Owner, Is.EqualTo(TestPlayers.Human));
            Assert.That(model.FacilityType, Is.EqualTo(SiteFacilityType.BattleAsteroid));
            Assert.That(model.ConstructionProgress, Is.EqualTo(0.75f));
            Assert.That(model.SetConstructionProgress(0.95f), Is.False);
            Assert.That(model.SetConstructionProgress(1f), Is.True);
            Assert.That(model.State, Is.EqualTo(CaptureSiteState.Operational));
        }

        [Test]
        public void Restore_CaptureInProgress_ContinuesForTheSameTeam()
        {
            CaptureSiteModel model = new CaptureSiteModel(captureDuration: CAPTURE_DURATION, captureSpeedPerNetShip: 1f, relations: ROSTER);

            model.Restore(PlayerId.None, CaptureSiteState.Neutral, TestPlayers.Human, 0.5f, SiteFacilityType.Mining, 0f);

            Assert.That(model.TickCapture(CAPTURE_DURATION / 2f, TestPlayers.DuelTally(ROSTER, 1, 0)), Is.True);
            Assert.That(model.Owner, Is.EqualTo(TestPlayers.Human));
        }
    }
}
