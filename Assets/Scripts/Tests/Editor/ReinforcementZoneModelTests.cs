using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ReinforcementZones;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public class ReinforcementZoneModelTests
    {
        [Test]
        public void Tick_UncontestedEnemyPresence_CapturesAfterDuration()
        {
            ReinforcementZoneModel model = new ReinforcementZoneModel(PlayerType.None, true, 10f, 1f);

            bool changedEarly = model.Tick(9f, 0, 1);
            bool changedAtDuration = model.Tick(1f, 0, 1);

            Assert.That(changedEarly, Is.False);
            Assert.That(changedAtDuration, Is.True);
            Assert.That(model.Owner, Is.EqualTo(PlayerType.Opponent));
        }

        [Test]
        public void Tick_EqualFleets_PausesCaptureProgress()
        {
            ReinforcementZoneModel model = new ReinforcementZoneModel(PlayerType.None, true, 10f, 1f);
            model.Tick(5f, 1, 0);

            model.Tick(4f, 1, 1);

            Assert.That(model.IsContested, Is.True);
            Assert.That(model.CaptureProgress, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(model.Owner, Is.EqualTo(PlayerType.None));
        }

        [Test]
        public void Tick_CapturingFleetLeaves_ResetsCaptureProgress()
        {
            ReinforcementZoneModel model = new ReinforcementZoneModel(PlayerType.None, true, 10f, 1f);
            model.Tick(5f, 1, 0);

            model.Tick(1f, 0, 0);

            Assert.That(model.CapturingPlayer, Is.EqualTo(PlayerType.None));
            Assert.That(model.CaptureProgress, Is.Zero);
        }

        [Test]
        public void Tick_LockedDefaultZone_NeverChangesOwner()
        {
            ReinforcementZoneModel model = new ReinforcementZoneModel(PlayerType.Player, false, 1f, 1f);

            bool changed = model.Tick(100f, 0, 10);

            Assert.That(changed, Is.False);
            Assert.That(model.Owner, Is.EqualTo(PlayerType.Player));
        }

        [Test]
        public void Tick_UnequalFleets_CapturesUsingNetShipAdvantage()
        {
            ReinforcementZoneModel model = new ReinforcementZoneModel(PlayerType.None, true, 10f, 1f);

            bool changed = model.Tick(10f, 5, 4);

            Assert.That(changed, Is.True);
            Assert.That(model.Owner, Is.EqualTo(PlayerType.Player));
        }

        [Test]
        public void Tick_LargerNetFleet_CapturesFaster()
        {
            ReinforcementZoneModel oneShip = new ReinforcementZoneModel(PlayerType.None, true, 10f, 1f);
            ReinforcementZoneModel fiveShips = new ReinforcementZoneModel(PlayerType.None, true, 10f, 1f);

            oneShip.Tick(1f, 1, 0);
            fiveShips.Tick(1f, 5, 0);

            Assert.That(oneShip.CaptureProgress, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(fiveShips.CaptureProgress, Is.EqualTo(0.5f).Within(0.001f));
        }
    }
}
