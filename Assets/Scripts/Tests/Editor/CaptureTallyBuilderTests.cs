using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class CaptureTallyBuilderTests
    {
        [Test]
        public void Build_ThreeTeams_AdvantageIsLeadOverRunnerUp()
        {
            PlayerRoster freeForAll = CreateFreeForAll();

            CaptureTally tally = TestPlayers.Tally(freeForAll,
                (new PlayerId(0), 1f), (new PlayerId(1), 4f), (new PlayerId(2), 3f));

            Assert.That(tally.LeadingPlayer, Is.EqualTo(new PlayerId(1)));
            Assert.That(tally.Advantage, Is.EqualTo(1f).Within(0.001f));
            Assert.That(tally.PresentTeamCount, Is.EqualTo(3));
            Assert.That(tally.IsContested, Is.False);
        }

        [Test]
        public void Build_TwoTeamsTied_IsContested()
        {
            PlayerRoster freeForAll = CreateFreeForAll();

            CaptureTally tally = TestPlayers.Tally(freeForAll, (new PlayerId(0), 2f), (new PlayerId(2), 2f));

            Assert.That(tally.IsTied, Is.True);
            Assert.That(tally.IsContested, Is.True);
        }

        [Test]
        public void Build_EmptyCircle_HasNoUnitsAndNoLeader()
        {
            CaptureTally tally = TestPlayers.Tally(CreateFreeForAll());

            Assert.That(tally.HasUnits, Is.False);
            Assert.That(tally.LeadingPlayer, Is.EqualTo(PlayerId.None));
            Assert.That(tally.IsContested, Is.False);
        }

        private static PlayerRoster CreateFreeForAll()
        {
            return new PlayerRoster(new[]
            {
                new PlayerSlot(new PlayerId(0), new TeamId(0), FactionType.Republic,
                    PlayerController.Human, EnemyAiDifficulty.Medium, 0),
                new PlayerSlot(new PlayerId(1), new TeamId(1), FactionType.Separatist,
                    PlayerController.Ai, EnemyAiDifficulty.Medium, 1),
                new PlayerSlot(new PlayerId(2), new TeamId(2), FactionType.Republic,
                    PlayerController.Ai, EnemyAiDifficulty.Medium, 2)
            });
        }
    }
}
