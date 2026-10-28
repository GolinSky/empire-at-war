using EmpireAtWar.Entities.Capture;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.ReinforcementZones;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class CaptureControlTests
    {
        [Test]
        public void AlliedLeadChange_CarriesProgressForBothConsumers()
        {
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            var site = new CaptureSiteModel(roster, 10f, 1f);
            var relay = new ReinforcementZoneModel(roster, PlayerId.None, 10f, 1f, true);
            CaptureStrength human = TestPlayers.Tally(roster, (TestPlayers.Human, 1f));
            CaptureStrength ally = TestPlayers.Tally(roster, (TestPlayers.Ally, 1f));
            site.TickCapture(6f, human);
            relay.Tick(6f, human);
            Assert.That(site.TickCapture(4f, ally), Is.True);
            Assert.That(relay.Tick(4f, ally), Is.True);
            Assert.That(site.Owner, Is.EqualTo(TestPlayers.Ally));
            Assert.That(relay.Owner, Is.EqualTo(TestPlayers.Ally));
        }

        [Test]
        public void NonCapturableRelay_PreservesProgressAndReportsContested()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            var relay = new ReinforcementZoneModel(roster, TestPlayers.Human, 10f, 1f, false);
            relay.Restore(TestPlayers.Human, TestPlayers.Enemy, 0.4f, false);
            Assert.That(relay.Tick(10f, TestPlayers.DuelTally(roster, 1f, 1f)), Is.False);
            Assert.That(relay.IsContested, Is.True);
            Assert.That(relay.CaptureProgress, Is.EqualTo(0.4f));
        }

        [Test]
        public void EnemyLeadChange_ResetsProgress_AlliedOwnerCannotBeTaken()
        {
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            var control = new CaptureControl(roster, PlayerId.None, 10f, 1f);
            control.Tick(6f, TestPlayers.Tally(roster, (TestPlayers.Human, 1f)), true);
            control.Tick(2f, TestPlayers.Tally(roster, (TestPlayers.Enemy, 1f)), true);
            Assert.That(control.CaptureProgress, Is.EqualTo(0.2f).Within(0.001f));
            control.SetOwner(TestPlayers.Ally);
            Assert.That(control.Tick(100f, TestPlayers.Tally(roster, (TestPlayers.Human, 5f)), true), Is.False);
            Assert.That(control.Owner, Is.EqualTo(TestPlayers.Ally));
            Assert.That(control.CaptureProgress, Is.Zero);
        }
    }
}
