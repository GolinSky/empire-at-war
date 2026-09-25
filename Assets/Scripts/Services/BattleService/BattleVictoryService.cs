using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Controllers;
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
    public sealed class BattleVictoryService : IService, INotifier<BattleResult>, ITickable
    {
        private readonly IGameModelObserver _gameModel;
        private readonly IShipService _shipService;
        private readonly IEntityLocator _entityLocator;
        private readonly BattleVictoryModel _victoryModel;
        private readonly LazyInject<IEnemyReinforcementObserver> _enemyReinforcement;
        private readonly List<IObserver<BattleResult>> _observers = new List<IObserver<BattleResult>>();
        private BattleResult _finalResult;

        public BattleVictoryService(
            IGameModelObserver gameModel,
            IShipService shipService,
            IEntityLocator entityLocator,
            BattleVictoryModel victoryModel,
            LazyInject<IEnemyReinforcementObserver> enemyReinforcement)
        {
            _gameModel = gameModel;
            _shipService = shipService;
            _entityLocator = entityLocator;
            _victoryModel = victoryModel;
            _enemyReinforcement = enemyReinforcement;
        }

        public string Id => nameof(BattleVictoryService);

        public void AddObserver(IObserver<BattleResult> observer)
        {
            if (_observers.Contains(observer))
            {
                return;
            }

            _observers.Add(observer);
            if (_finalResult != null)
            {
                observer.UpdateState(_finalResult);
            }
        }

        public void RemoveObserver(IObserver<BattleResult> observer)
        {
            _observers.Remove(observer);
        }

        public void Tick()
        {
            if (_finalResult != null)
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
                isEnemyBaseAlive,
                _enemyReinforcement.Value.HasPendingReinforcement);
            if (outcome == BattleOutcome.None)
            {
                return;
            }

            _finalResult = new BattleResult(
                outcome,
                _gameModel.VictoryCondition,
                _gameModel.PlanetType,
                _gameModel.PlayerFactionType,
                _gameModel.EnemyFactionType,
                playerShipCount,
                enemyShipCount,
                isPlayerBaseAlive,
                isEnemyBaseAlive);
            Debug.Log($"[Battle] Outcome={outcome}, VictoryCondition={_gameModel.VictoryCondition}");
            foreach (IObserver<BattleResult> observer in _observers)
            {
                observer.UpdateState(_finalResult);
            }
        }
    }
}
