using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Capture")]
    public sealed class CaptureStrengthBuilderTests
    {
        [Test]
        public void Build_EighthPlayerContributesToTeamAndClearRemovesItsStrength()
        {
            var slots = new PlayerSlot[8];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = new PlayerSlot(new PlayerId(i), new TeamId(i % 2), FactionType.Republic,
                    i == 0 ? PlayerController.Human : PlayerController.Ai, EnemyAiDifficulty.Medium, i);
            var builder = new CaptureStrengthBuilder(new PlayerRoster(slots));
            builder.Add(new PlayerId(0), 4f);
            builder.Add(new PlayerId(1), 2f);
            builder.Add(new PlayerId(7), 3f);

            CaptureStrength tally = builder.Build();
            Assert.That(tally.LeadingPlayer, Is.EqualTo(new PlayerId(7)));
            Assert.That(tally.OpposingPlayer, Is.EqualTo(new PlayerId(0)));
            Assert.That(tally.Advantage, Is.EqualTo(1f));
            Assert.That(tally.PresentTeamCount, Is.EqualTo(2));

            builder.Clear();
            builder.Add(new PlayerId(0), 1f);
            tally = builder.Build();
            Assert.That(tally.LeadingPlayer, Is.EqualTo(new PlayerId(0)));
            Assert.That(tally.Advantage, Is.EqualTo(1f));
            Assert.That(tally.PresentTeamCount, Is.EqualTo(1));
        }

        [Test]
        public void Build_ThreeTeams_AdvantageIsLeadOverRunnerUp()
        {
            PlayerRoster freeForAll = CreateFreeForAll();

            CaptureStrength tally = TestPlayers.Tally(freeForAll,
                (new PlayerId(0), 1f), (new PlayerId(1), 4f), (new PlayerId(2), 3f));

            Assert.That(tally.LeadingPlayer, Is.EqualTo(new PlayerId(1)));
            Assert.That(tally.OpposingPlayer, Is.EqualTo(new PlayerId(2)));
            Assert.That(tally.Advantage, Is.EqualTo(1f).Within(0.001f));
            Assert.That(tally.PresentTeamCount, Is.EqualTo(3));
            Assert.That(tally.IsContested, Is.False);
        }

        [Test]
        public void Build_TwoTeamsTied_IsContested()
        {
            PlayerRoster freeForAll = CreateFreeForAll();

            CaptureStrength tally = TestPlayers.Tally(freeForAll, (new PlayerId(0), 2f), (new PlayerId(2), 2f));

            Assert.That(tally.IsTied, Is.True);
            Assert.That(tally.LeadingPlayer, Is.EqualTo(new PlayerId(0)));
            Assert.That(tally.OpposingPlayer, Is.EqualTo(new PlayerId(2)));
            Assert.That(tally.IsContested, Is.True);
        }

        [Test]
        public void Build_EmptyCircle_HasNoUnitsAndNoLeader()
        {
            CaptureStrength tally = TestPlayers.Tally(CreateFreeForAll());

            Assert.That(tally.HasUnits, Is.False);
            Assert.That(tally.LeadingPlayer, Is.EqualTo(PlayerId.None));
            Assert.That(tally.OpposingPlayer, Is.EqualTo(PlayerId.None));
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
