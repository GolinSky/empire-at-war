using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Factions;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class CaptureSiteModelTests
    {
        private const float CAPTURE_DURATION = 10f;

        [Test]
        public void TickCapture_UncontestedShips_CaptureNeutralSite()
        {
            CaptureSiteModel model = new CaptureSiteModel(CAPTURE_DURATION, 1f);

            Assert.That(model.TickCapture(5f, 1, 0), Is.False);
            Assert.That(model.TickCapture(5f, 1, 0), Is.True);

            Assert.That(model.Owner, Is.EqualTo(PlayerType.Player));
            Assert.That(model.State, Is.EqualTo(CaptureSiteState.Owned));
            Assert.That(model.CanStartConstruction, Is.True);
        }

        [Test]
        public void TickCapture_EqualOpposingShips_IsContestedWithoutProgress()
        {
            CaptureSiteModel model = new CaptureSiteModel(CAPTURE_DURATION, 1f);

            model.TickCapture(CAPTURE_DURATION, 2, 2);

            Assert.That(model.IsContested, Is.True);
            Assert.That(model.CaptureProgress, Is.Zero);
            Assert.That(model.Owner, Is.EqualTo(PlayerType.None));
        }

        [Test]
        public void TickConstruction_AfterBuildTime_BecomesOperationalAndLocksCapture()
        {
            CaptureSiteModel model = CreateOwnedSite(PlayerType.Player);
            model.StartConstruction(20f);

            Assert.That(model.TickConstruction(10f), Is.False);
            Assert.That(model.TickConstruction(10f), Is.True);
            Assert.That(model.State, Is.EqualTo(CaptureSiteState.Operational));

            Assert.That(model.TickCapture(CAPTURE_DURATION, 0, 5), Is.False);
            Assert.That(model.Owner, Is.EqualTo(PlayerType.Player));
            Assert.That(model.CaptureProgress, Is.Zero);
        }

        [Test]
        public void TickCapture_EnemyTakesSiteUnderConstruction_CancelsBuild()
        {
            CaptureSiteModel model = CreateOwnedSite(PlayerType.Player);
            model.StartConstruction(20f);
            model.TickConstruction(10f);

            Assert.That(model.TickCapture(CAPTURE_DURATION, 0, 1), Is.True);

            Assert.That(model.Owner, Is.EqualTo(PlayerType.Opponent));
            Assert.That(model.State, Is.EqualTo(CaptureSiteState.Owned));
            Assert.That(model.ConstructionProgress, Is.Zero);
        }

        [Test]
        public void ReleaseFacility_DestroyedFacility_ReturnsSiteToNeutral()
        {
            CaptureSiteModel model = CreateOwnedSite(PlayerType.Opponent);
            model.StartConstruction(1f);
            model.TickConstruction(1f);

            model.ReleaseFacility();

            Assert.That(model.Owner, Is.EqualTo(PlayerType.None));
            Assert.That(model.State, Is.EqualTo(CaptureSiteState.Neutral));
            Assert.That(model.TickCapture(CAPTURE_DURATION, 1, 0), Is.True);
            Assert.That(model.Owner, Is.EqualTo(PlayerType.Player));
        }

        private static CaptureSiteModel CreateOwnedSite(PlayerType owner)
        {
            CaptureSiteModel model = new CaptureSiteModel(CAPTURE_DURATION, 1f);
            int playerShips = owner == PlayerType.Player ? 1 : 0;
            model.TickCapture(CAPTURE_DURATION, playerShips, 1 - playerShips);
            return model;
        }
    }
}
