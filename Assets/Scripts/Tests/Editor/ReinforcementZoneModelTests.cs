using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.ReinforcementZones;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public class ReinforcementZoneModelTests
    {
        private static readonly PlayerRoster ROSTER = TestPlayers.CreateDuel();
        [Test]
        public void Tick_UncontestedEnemyPresence_CapturesAfterDuration()
        {
            ReinforcementZoneModel model = new ReinforcementZoneModel(PlayerId.None, true, 10f, 1f, ROSTER);

            bool changedEarly = model.Tick(9f, TestPlayers.DuelTally(ROSTER, 0, 1));
            bool changedAtDuration = model.Tick(1f, TestPlayers.DuelTally(ROSTER, 0, 1));

            Assert.That(changedEarly, Is.False);
            Assert.That(changedAtDuration, Is.True);
            Assert.That(model.Owner, Is.EqualTo(TestPlayers.Enemy));
        }

        [Test]
        public void Tick_EqualFleets_PausesCaptureProgress()
        {
            ReinforcementZoneModel model = new ReinforcementZoneModel(PlayerId.None, true, 10f, 1f, ROSTER);
            model.Tick(5f, TestPlayers.DuelTally(ROSTER, 1, 0));

            model.Tick(4f, TestPlayers.DuelTally(ROSTER, 1, 1));

            Assert.That(model.IsContested, Is.True);
            Assert.That(model.CaptureProgress, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(model.Owner, Is.EqualTo(PlayerId.None));
        }

        [Test]
        public void Tick_CapturingFleetLeaves_ResetsCaptureProgress()
        {
            ReinforcementZoneModel model = new ReinforcementZoneModel(PlayerId.None, true, 10f, 1f, ROSTER);
            model.Tick(5f, TestPlayers.DuelTally(ROSTER, 1, 0));

            model.Tick(1f, TestPlayers.DuelTally(ROSTER, 0, 0));

            Assert.That(model.CapturingPlayer, Is.EqualTo(PlayerId.None));
            Assert.That(model.CaptureProgress, Is.Zero);
        }

        [Test]
        public void Tick_LockedDefaultZone_NeverChangesOwner()
        {
            ReinforcementZoneModel model = new ReinforcementZoneModel(TestPlayers.Human, false, 1f, 1f, ROSTER);

            bool changed = model.Tick(100f, TestPlayers.DuelTally(ROSTER, 0, 10));

            Assert.That(changed, Is.False);
            Assert.That(model.Owner, Is.EqualTo(TestPlayers.Human));
        }

        [Test]
        public void Tick_UnequalFleets_CapturesUsingNetShipAdvantage()
        {
            ReinforcementZoneModel model = new ReinforcementZoneModel(PlayerId.None, true, 10f, 1f, ROSTER);

            bool changed = model.Tick(10f, TestPlayers.DuelTally(ROSTER, 5, 4));

            Assert.That(changed, Is.True);
            Assert.That(model.Owner, Is.EqualTo(TestPlayers.Human));
        }

        [Test]
        public void Tick_LargerNetFleet_CapturesFaster()
        {
            ReinforcementZoneModel oneShip = new ReinforcementZoneModel(PlayerId.None, true, 10f, 1f, ROSTER);
            ReinforcementZoneModel fiveShips = new ReinforcementZoneModel(PlayerId.None, true, 10f, 1f, ROSTER);

            oneShip.Tick(1f, TestPlayers.DuelTally(ROSTER, 1, 0));
            fiveShips.Tick(1f, TestPlayers.DuelTally(ROSTER, 5, 0));

            Assert.That(oneShip.CaptureProgress, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(fiveShips.CaptureProgress, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void Tick_AlliedFleets_CombineStrengthAgainstEnemy()
        {
            PlayerRoster teamGame = TestPlayers.CreateTeamGame();
            ReinforcementZoneModel model = new ReinforcementZoneModel(PlayerId.None, true, 10f, 1f, teamGame);

            model.Tick(1f, TestPlayers.Tally(teamGame,
                (TestPlayers.Human, 1f), (TestPlayers.Ally, 2f), (TestPlayers.Enemy, 2f)));

            Assert.That(model.CapturingPlayer, Is.EqualTo(TestPlayers.Ally));
            Assert.That(model.CaptureProgress, Is.EqualTo(0.1f).Within(0.001f));
        }

        [Test]
        public void Tick_AllyOwnedZone_IsNeverTakenByItsTeammate()
        {
            PlayerRoster teamGame = TestPlayers.CreateTeamGame();
            ReinforcementZoneModel model = new ReinforcementZoneModel(TestPlayers.Ally, true, 10f, 1f, teamGame);

            bool changed = model.Tick(100f, TestPlayers.Tally(teamGame, (TestPlayers.Human, 5f)));

            Assert.That(changed, Is.False);
            Assert.That(model.Owner, Is.EqualTo(TestPlayers.Ally));
            Assert.That(model.CaptureProgress, Is.Zero);
        }

    }
}
