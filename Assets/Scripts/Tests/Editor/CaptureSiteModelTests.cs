using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class CaptureSiteModelTests
    {
        private static readonly PlayerRoster ROSTER = TestPlayers.CreateDuel();
        private const float CAPTURE_DURATION = 10f;

        [Test]
        public void TickCapture_UncontestedShips_CaptureNeutralSite()
        {
            CaptureSiteModel model = new CaptureSiteModel(CAPTURE_DURATION, 1f, ROSTER);

            Assert.That(model.TickCapture(5f, TestPlayers.DuelTally(ROSTER, 1, 0)), Is.False);
            Assert.That(model.TickCapture(5f, TestPlayers.DuelTally(ROSTER, 1, 0)), Is.True);

            Assert.That(model.Owner, Is.EqualTo(TestPlayers.Human));
            Assert.That(model.State, Is.EqualTo(CaptureSiteState.Owned));
            Assert.That(model.CanStartConstruction, Is.True);
        }

        [Test]
        public void TickCapture_EqualOpposingShips_IsContestedWithoutProgress()
        {
            CaptureSiteModel model = new CaptureSiteModel(CAPTURE_DURATION, 1f, ROSTER);

            model.TickCapture(CAPTURE_DURATION, TestPlayers.DuelTally(ROSTER, 2, 2));

            Assert.That(model.IsContested, Is.True);
            Assert.That(model.CaptureProgress, Is.Zero);
            Assert.That(model.Owner, Is.EqualTo(PlayerId.None));
        }

        [Test]
        public void TickConstruction_AfterBuildTime_BecomesOperationalAndLocksCapture()
        {
            CaptureSiteModel model = CreateOwnedSite(TestPlayers.Human);
            model.StartConstruction(SiteFacilityType.Mining, 20f);

            Assert.That(model.TickConstruction(10f), Is.False);
            Assert.That(model.TickConstruction(10f), Is.True);
            Assert.That(model.State, Is.EqualTo(CaptureSiteState.Operational));

            Assert.That(model.TickCapture(CAPTURE_DURATION, TestPlayers.DuelTally(ROSTER, 0, 5)), Is.False);
            Assert.That(model.Owner, Is.EqualTo(TestPlayers.Human));
            Assert.That(model.CaptureProgress, Is.Zero);
        }

        [Test]
        public void TickCapture_EnemyTakesSiteUnderConstruction_CancelsBuild()
        {
            CaptureSiteModel model = CreateOwnedSite(TestPlayers.Human);
            model.StartConstruction(SiteFacilityType.Mining, 20f);
            model.TickConstruction(10f);

            Assert.That(model.TickCapture(CAPTURE_DURATION, TestPlayers.DuelTally(ROSTER, 0, 1)), Is.True);

            Assert.That(model.Owner, Is.EqualTo(TestPlayers.Enemy));
            Assert.That(model.State, Is.EqualTo(CaptureSiteState.Owned));
            Assert.That(model.ConstructionProgress, Is.Zero);
        }

        [Test]
        public void ReleaseFacility_DestroyedFacility_ReturnsSiteToNeutral()
        {
            CaptureSiteModel model = CreateOwnedSite(TestPlayers.Enemy);
            model.StartConstruction(SiteFacilityType.Mining, 1f);
            model.TickConstruction(1f);

            model.ReleaseFacility();

            Assert.That(model.Owner, Is.EqualTo(PlayerId.None));
            Assert.That(model.State, Is.EqualTo(CaptureSiteState.Neutral));
            Assert.That(model.TickCapture(CAPTURE_DURATION, TestPlayers.DuelTally(ROSTER, 1, 0)), Is.True);
            Assert.That(model.Owner, Is.EqualTo(TestPlayers.Human));
        }

        private static CaptureSiteModel CreateOwnedSite(PlayerId owner)
        {
            CaptureSiteModel model = new CaptureSiteModel(CAPTURE_DURATION, 1f, ROSTER);
            int playerShips = owner == TestPlayers.Human ? 1 : 0;
            model.TickCapture(CAPTURE_DURATION, TestPlayers.DuelTally(ROSTER, playerShips, 1 - playerShips));
            return model;
        }
    }
}
