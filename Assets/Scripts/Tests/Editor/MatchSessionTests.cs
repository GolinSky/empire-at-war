using System;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Networking.Online;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("GameFlow")]
    public sealed class MatchSessionTests
    {
        private static PlayerSlot[] TwoHumans() => new[]
        {
            new PlayerSlot(new PlayerId(0), new TeamId(0), FactionType.Republic, PlayerController.Human, EnemyAiDifficulty.Medium, 0),
            new PlayerSlot(new PlayerId(1), new TeamId(1), FactionType.Separatist, PlayerController.Human, EnemyAiDifficulty.Medium, 1)
        };

        [Test]
        public void MatchRules_AllowsSeveralHumans_ButStillNeedsOne()
        {
            Assert.DoesNotThrow(() => MatchRules.Validate(TwoHumans()));
            PlayerSlot[] noHuman =
            {
                new PlayerSlot(new PlayerId(0), new TeamId(0), FactionType.Republic, PlayerController.Ai, EnemyAiDifficulty.Medium, 0),
                new PlayerSlot(new PlayerId(1), new TeamId(1), FactionType.Separatist, PlayerController.Ai, EnemyAiDifficulty.Medium, 1)
            };
            Assert.Throws<ArgumentException>(() => MatchRules.Validate(noHuman));
        }

        [Test]
        public void OfflineSession_RequiresExactlyOneHuman()
        {
            Assert.Throws<InvalidOperationException>(() => OfflineSession.ForHuman(new PlayerRoster(TwoHumans())));
            OfflineSession session = OfflineSession.ForHuman(TestPlayers.CreateDuel());
            Assert.That(session.LocalPlayerId, Is.EqualTo(TestPlayers.Human));
            Assert.That(session.IsAuthority && !session.IsOnline, Is.True);
        }

        [Test]
        [Category("Networking")]
        public void OnlineLocalSlot_OutsideBattle_ReturnsFalseAndNoPlayer()
        {
            IOnlineLocalSlot online = new OnlineMatchService(null);

            Assert.That(online.TryGetLocalSlot(out PlayerId player), Is.False);
            Assert.That(player, Is.EqualTo(PlayerId.None));
        }

        [Test]
        public void LocalPlayer_TakesItsSlotFromTheSession()
        {
            PlayerRoster roster = new PlayerRoster(TwoHumans());
            LocalPlayer second = new LocalPlayer(roster, new OfflineSession(new PlayerId(1)));

            Assert.That(second.Id, Is.EqualTo(new PlayerId(1)));
            Assert.That(second.IsLocal(new PlayerId(0)), Is.False);
            Assert.That(second.IsHostile(new PlayerId(0)), Is.True);
        }

        [Test]
        public void BattleDefinition_ListsEveryHuman()
        {
            BattleDefinition battle = SkirmishPreset.Create(TwoHumans(), PlanetType.Coruscant, MapSize.Small,
                BattleVictoryCondition.DestroyEnemyFleet, 1000f, true, 1).Build();

            Assert.That(battle.Humans, Has.Count.EqualTo(2));
            Assert.That(battle.GetProductionHost(battle.Players[1]), Is.SameAs(battle.Players[1]));
        }

        [Test]
        public void AiAllyWithoutHumanHost_IsRejected()
        {
            BattleDefinitionBuilder builder = SurvivalPreset.Create(new SurvivalSettings(FactionType.Republic, null,
                FactionType.Separatist, MapSize.Small, EnemyAiDifficulty.Medium, true));
            BattlePlayer attacker = builder.Players[1];
            builder.Players[1] = new BattlePlayer(new PlayerSlot(new PlayerId(1), new TeamId(0), FactionType.Republic,
                PlayerController.Ai, EnemyAiDifficulty.Medium, 1), PlayerRole.AiAlly, BattleFeatures.All);
            builder.Players.Add(new BattlePlayer(new PlayerSlot(new PlayerId(2), attacker.Slot.Team, attacker.Slot.Faction,
                PlayerController.Ai, EnemyAiDifficulty.Medium, 2), PlayerRole.WaveAttacker, BattleFeatures.None));

            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        [Test]
        public void VictoryModel_TeamsAreDecidedOnce_AndEachViewerGetsItsOwnOutcome()
        {
            BattleVictoryModel model = new BattleVictoryModel();
            PlayerBattleState[] alive =
            {
                new PlayerBattleState(new PlayerId(0), new TeamId(0), 1, true, false),
                new PlayerBattleState(new PlayerId(1), new TeamId(1), 1, true, false)
            };
            model.EvaluateTeams(BattleVictoryCondition.DestroyOpponentBase, alive);
            PlayerBattleState[] secondFell =
            {
                new PlayerBattleState(new PlayerId(0), new TeamId(0), 1, true, false),
                new PlayerBattleState(new PlayerId(1), new TeamId(1), 1, false, false)
            };

            Assert.That(model.EvaluateTeams(BattleVictoryCondition.DestroyOpponentBase, secondFell),
                Is.EquivalentTo(new[] { new TeamId(0) }));
            Assert.That(model.OutcomeFor(new TeamId(0)), Is.EqualTo(BattleOutcome.PlayerVictory));
            Assert.That(model.OutcomeFor(new TeamId(1)), Is.EqualTo(BattleOutcome.EnemyVictory));
        }
    }
}
