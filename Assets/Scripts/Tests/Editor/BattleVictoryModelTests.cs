using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.Ships;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BattleVictoryModelTests
    {
        private static readonly TeamId HUMAN_TEAM = new TeamId(0);
        private static readonly TeamId ENEMY_TEAM = new TeamId(1);

        [Test]
        public void FleetCondition_DoesNotFinishBeforeBaseWasObserved()
        {
            BattleVictoryModel model = new BattleVictoryModel();

            BattleOutcome outcome = Evaluate(
                model,
                BattleVictoryCondition.DestroyEnemyFleet,
                0,
                0,
                false,
                false,
                false);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.None));
        }

        [Test]
        public void FleetCondition_DoesNotFinishWhileEnemyStationIsAlive()
        {
            BattleVictoryModel model = new BattleVictoryModel();
            Evaluate(model, BattleVictoryCondition.DestroyEnemyFleet, 2, 2, true, true, false);

            BattleOutcome outcome = Evaluate(
                model,
                BattleVictoryCondition.DestroyEnemyFleet,
                2,
                0,
                true,
                true,
                false);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.None));
        }

        [Test]
        public void FleetCondition_DoesNotFinishWhileEnemyHasPendingReinforcement()
        {
            BattleVictoryModel model = new BattleVictoryModel();
            Evaluate(model, BattleVictoryCondition.DestroyEnemyFleet, 2, 2, true, true, false);

            BattleOutcome outcome = Evaluate(
                model,
                BattleVictoryCondition.DestroyEnemyFleet,
                2,
                0,
                true,
                false,
                true);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.None));
        }

        [Test]
        public void FleetCondition_ReturnsPlayerVictoryAfterEnemyShipsStationAndReinforcementsAreGone()
        {
            BattleVictoryModel model = new BattleVictoryModel();
            Evaluate(model, BattleVictoryCondition.DestroyEnemyFleet, 2, 2, true, true, false);

            BattleOutcome outcome = Evaluate(
                model,
                BattleVictoryCondition.DestroyEnemyFleet,
                2,
                0,
                true,
                false,
                false);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
        }

        [Test]
        public void FleetCondition_ReturnsEnemyVictoryAfterPlayerShipsAndStationAreGone()
        {
            BattleVictoryModel model = new BattleVictoryModel();
            Evaluate(model, BattleVictoryCondition.DestroyEnemyFleet, 2, 2, true, true, false);

            BattleOutcome outcome = Evaluate(
                model,
                BattleVictoryCondition.DestroyEnemyFleet,
                0,
                2,
                false,
                true,
                false);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.EnemyVictory));
        }

        [Test]
        public void BaseCondition_IgnoresFleetCount()
        {
            BattleVictoryModel model = new BattleVictoryModel();
            Evaluate(model, BattleVictoryCondition.DestroyOpponentBase, 0, 4, true, true, false);

            BattleOutcome outcome = Evaluate(
                model,
                BattleVictoryCondition.DestroyOpponentBase,
                0,
                4,
                true,
                false,
                true);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
        }

        [Test]
        public void TeamGame_FallenPlayerWithLivingAlly_KeepsBattleRunning()
        {
            BattleVictoryModel model = new BattleVictoryModel();
            model.Evaluate(BattleVictoryCondition.DestroyOpponentBase, TeamStates(true, true, true, true), HUMAN_TEAM);

            BattleOutcome outcome = model.Evaluate(
                BattleVictoryCondition.DestroyOpponentBase, TeamStates(false, true, true, true), HUMAN_TEAM);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.None));
        }

        [Test]
        public void TeamGame_BothEnemiesFallen_ReturnsPlayerVictoryEvenIfHumanFell()
        {
            BattleVictoryModel model = new BattleVictoryModel();
            model.Evaluate(BattleVictoryCondition.DestroyOpponentBase, TeamStates(true, true, true, true), HUMAN_TEAM);

            BattleOutcome outcome = model.Evaluate(
                BattleVictoryCondition.DestroyOpponentBase, TeamStates(false, false, true, false), HUMAN_TEAM);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
        }

        [Test]
        public void FreeForAll_WinsOnlyAfterEveryOtherTeamFalls()
        {
            BattleVictoryModel model = new BattleVictoryModel();
            model.Evaluate(BattleVictoryCondition.DestroyOpponentBase, FreeForAllStates(true, true, true), HUMAN_TEAM);

            BattleOutcome afterFirst = model.Evaluate(
                BattleVictoryCondition.DestroyOpponentBase, FreeForAllStates(true, false, true), HUMAN_TEAM);
            BattleOutcome afterSecond = model.Evaluate(
                BattleVictoryCondition.DestroyOpponentBase, FreeForAllStates(true, false, false), HUMAN_TEAM);

            Assert.That(afterFirst, Is.EqualTo(BattleOutcome.None));
            Assert.That(afterSecond, Is.EqualTo(BattleOutcome.PlayerVictory));
        }

        [Test]
        public void FleetCondition_ReserveKeepsPlayerAliveUntilLastUnitIsConsumed()
        {
            ReinforcementModel reserve = new ReinforcementModel(null);
            ShipUnitRequest request = new ShipUnitRequest(new FactionData(), ShipType.Venator);
            PlayerRegistry registry = new PlayerRegistry();
            registry.RegisterReinforcementReserve(TestPlayers.Human, reserve);
            BattleVictoryModel model = new BattleVictoryModel();
            Evaluate(model, BattleVictoryCondition.DestroyEnemyFleet, 1, 1, true, true, false);
            reserve.AddReinforcement(request);
            reserve.AddReinforcement(request);
            reserve.ConsumeReinforcement(request);
            PlayerBattleState[] states =
            {
                new PlayerBattleState(TestPlayers.Human, HUMAN_TEAM, 0, false,
                    registry.HasPendingReinforcement(TestPlayers.Human)),
                new PlayerBattleState(TestPlayers.Enemy, ENEMY_TEAM, 1, true, false)
            };
            Assert.That(model.Evaluate(BattleVictoryCondition.DestroyEnemyFleet, states, HUMAN_TEAM),
                Is.EqualTo(BattleOutcome.None));
            Assert.That(registry.HasPendingReinforcement(TestPlayers.Enemy), Is.False);
            reserve.ConsumeReinforcement(request);
            Assert.That(registry.HasPendingReinforcement(TestPlayers.Human), Is.False);
            states[0] = new PlayerBattleState(TestPlayers.Human, HUMAN_TEAM, 0, false, false);
            Assert.That(model.Evaluate(BattleVictoryCondition.DestroyEnemyFleet, states, HUMAN_TEAM),
                Is.EqualTo(BattleOutcome.EnemyVictory));
            registry.UnregisterReinforcementReserve(TestPlayers.Human);
            Assert.That(registry.HasPendingReinforcement(TestPlayers.Human), Is.False);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void FleetCondition_ConstructionCountsForHumanAndAi(int ownerIndex)
        {
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            ConstructionSite sites = new ConstructionSite { Owner = new PlayerId(ownerIndex) };
            BattleVictoryService service = new BattleVictoryService(null, new ShipService(), new EntityLocator(),
                roster, TestPlayers.CreateLocalPlayer(roster), new PlayerRegistry(), null,
                new BattleVictoryModel(), sites);
            typeof(BattleVictoryService).GetMethod("CollectStates", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(service, null);
            List<PlayerBattleState> states = (List<PlayerBattleState>)typeof(BattleVictoryService)
                .GetField("_states", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service);
            Assert.That(states.Find(state => state.Player == sites.Owner).UnitCount, Is.EqualTo(1));
        }

        private sealed class ConstructionSite : ICaptureSite, ICaptureSitesSystem
        {
            public CaptureSiteKind Kind => CaptureSiteKind.Mining;
            public int PointId => -1;

            public event System.Action<PlayerId, PlayerId, SiteFacilityType> SiteCaptured { add { } remove { } }
            public event System.Action<PlayerId, SiteFacilityType> SiteLost { add { } remove { } }
            public event System.Action<PlayerId, SiteFacilityType> ConstructionStarted { add { } remove { } }
            public PlayerId Owner { get; set; }
            public CaptureSiteState State => CaptureSiteState.Constructing;
            public SiteFacilityType FacilityType => default;
            public float CaptureProgress => 0f;
            public float ConstructionProgress => 0.5f;
            public Vector3 Center => Vector3.zero;
            public bool IsRevealed => false;
            public bool IsOperational => false;
            public IReadOnlyList<ICaptureSite> Sites => new[] { this };
            public bool Contains(Vector3 position, float clearance = 0f) => false;
            public bool IsPositionInAnySite(Vector3 position, float clearance = 0f) => false;
            public bool TryGetCaptureTarget(PlayerId owner, Vector3 origin, out Vector3 position)
            { position = default; return false; }
            public bool TryGetThreatenedSite(PlayerId owner, out Vector3 position)
            { position = default; return false; }
            public bool TryGetRaidTarget(PlayerId attacker, Vector3 origin, out Vector3 position)
            { position = default; return false; }
            public bool HasHostileUnits(ICaptureSite site, PlayerId owner) => false;
            public bool TryStartConstruction(int siteIndex, PlayerId owner, SiteFacilityType type) => false;
            public bool TryBuildOnOwnedSite(PlayerId owner) => false;
        }

        // Keeps the duel cases readable: the human is team 0, the AI team 1.
        private static BattleOutcome Evaluate(
            BattleVictoryModel model,
            BattleVictoryCondition condition,
            int playerShipCount,
            int enemyShipCount,
            bool isPlayerBaseAlive,
            bool isEnemyBaseAlive,
            bool hasEnemyPendingReinforcement)
        {
            return model.Evaluate(condition, new[]
            {
                new PlayerBattleState(TestPlayers.Human, HUMAN_TEAM, playerShipCount, isPlayerBaseAlive, false),
                new PlayerBattleState(TestPlayers.Enemy, ENEMY_TEAM, enemyShipCount, isEnemyBaseAlive,
                    hasEnemyPendingReinforcement)
            }, HUMAN_TEAM);
        }

        private static PlayerBattleState[] TeamStates(bool human, bool enemy, bool ally, bool secondEnemy)
        {
            return new[]
            {
                new PlayerBattleState(TestPlayers.Human, HUMAN_TEAM, 0, human, false),
                new PlayerBattleState(TestPlayers.Enemy, ENEMY_TEAM, 0, enemy, false),
                new PlayerBattleState(TestPlayers.Ally, HUMAN_TEAM, 0, ally, false),
                new PlayerBattleState(TestPlayers.SecondEnemy, ENEMY_TEAM, 0, secondEnemy, false)
            };
        }

        private static PlayerBattleState[] FreeForAllStates(bool human, bool first, bool second)
        {
            return new[]
            {
                new PlayerBattleState(TestPlayers.Human, HUMAN_TEAM, 0, human, false),
                new PlayerBattleState(TestPlayers.Enemy, ENEMY_TEAM, 0, first, false),
                new PlayerBattleState(TestPlayers.Ally, new TeamId(2), 0, second, false)
            };
        }

    }
}
