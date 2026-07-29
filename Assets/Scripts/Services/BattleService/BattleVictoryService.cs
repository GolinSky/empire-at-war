using System;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;

namespace EmpireAtWar.Services.Battle
{
    public interface IBattleVictoryService : IService
    {
        event Action<BattleOutcome> OutcomeChanged;
        BattleOutcome CurrentOutcome { get; }
    }

    public sealed class BattleVictoryService : IBattleVictoryService, ITickable
    {
        private readonly IGameModelObserver _gameModel;
        private readonly IShipService _shipService;
        private readonly IEntityLocator _entityLocator;
        private readonly BattleVictoryModel _victoryModel;

        public BattleVictoryService(
            IGameModelObserver gameModel,
            IShipService shipService,
            IEntityLocator entityLocator,
            BattleVictoryModel victoryModel)
        {
            _gameModel = gameModel ?? throw new ArgumentNullException(nameof(gameModel));
            _shipService = shipService ?? throw new ArgumentNullException(nameof(shipService));
            _entityLocator = entityLocator ?? throw new ArgumentNullException(nameof(entityLocator));
            _victoryModel = victoryModel ?? throw new ArgumentNullException(nameof(victoryModel));
        }

        public event Action<BattleOutcome> OutcomeChanged;

        public string Id => nameof(BattleVictoryService);
        public BattleOutcome CurrentOutcome { get; private set; }

        public void Tick()
        {
            if (CurrentOutcome != BattleOutcome.None)
            {
                return;
            }

            int playerShipCount = 0;
            int enemyShipCount = 0;
            foreach (IShipEntity ship in _shipService.Ships)
            {
                if (ship.PlayerType == PlayerType.Player)
                {
                    playerShipCount++;
                }
                else if (ship.PlayerType == PlayerType.Opponent)
                {
                    enemyShipCount++;
                }
            }

            bool isPlayerBaseAlive = false;
            bool isEnemyBaseAlive = false;
            foreach (GameEntity entity in _entityLocator.Entities)
            {
                if (entity.Model is not ISpaceStationModelObserver)
                {
                    continue;
                }

                bool isAlive = !entity.HealthModel.IsDestroyed && entity.HealthModel.HasUnits;
                if (entity.PlayerType == PlayerType.Player)
                {
                    isPlayerBaseAlive |= isAlive;
                }
                else if (entity.PlayerType == PlayerType.Opponent)
                {
                    isEnemyBaseAlive |= isAlive;
                }
            }

            BattleOutcome outcome = _victoryModel.Evaluate(
                _gameModel.VictoryCondition,
                playerShipCount,
                enemyShipCount,
                isPlayerBaseAlive,
                isEnemyBaseAlive);
            if (outcome == BattleOutcome.None)
            {
                return;
            }

            CurrentOutcome = outcome;
            Debug.Log($"[Battle] Outcome={outcome}, VictoryCondition={_gameModel.VictoryCondition}");
            OutcomeChanged?.Invoke(outcome);
        }
    }
}
