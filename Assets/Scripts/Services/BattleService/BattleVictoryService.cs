using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Services.Battle
{
    public sealed class BattleVictoryService : IService, INotifier<BattleResult>, ITickable
    {
        private readonly IGameModelObserver _gameModel;
        private readonly IShipService _shipService;
        private readonly IEntityLocator _entityLocator;
        private readonly BattleVictoryModel _victoryModel;
        private readonly IPlayerRoster _roster;
        private readonly ILocalPlayer _localPlayer;
        private readonly IPlayerRegistry _playerRegistry;
        private readonly List<IObserver<BattleResult>> _observers = new List<IObserver<BattleResult>>();
        private readonly int[] _shipCounts = new int[MatchRules.MAX_PLAYERS];
        private readonly bool[] _aliveBases = new bool[MatchRules.MAX_PLAYERS];
        private readonly List<PlayerBattleState> _states = new List<PlayerBattleState>();
        private BattleResult _finalResult;

        public BattleVictoryService(
            IGameModelObserver gameModel,
            IShipService shipService,
            IEntityLocator entityLocator,
            BattleVictoryModel victoryModel,
            IPlayerRoster roster,
            ILocalPlayer localPlayer,
            IPlayerRegistry playerRegistry)
        {
            _gameModel = gameModel;
            _shipService = shipService;
            _entityLocator = entityLocator;
            _victoryModel = victoryModel;
            _roster = roster;
            _localPlayer = localPlayer;
            _playerRegistry = playerRegistry;
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

            CollectStates();
            BattleOutcome outcome = _victoryModel.Evaluate(
                _gameModel.VictoryCondition,
                _states,
                _localPlayer.Slot.Team);
            if (outcome == BattleOutcome.None)
            {
                return;
            }

            _finalResult = CreateResult(outcome);
            Debug.Log($"[Battle] Outcome={outcome}, VictoryCondition={_gameModel.VictoryCondition}");
            foreach (IObserver<BattleResult> observer in _observers)
            {
                observer.UpdateState(_finalResult);
            }
        }

        private void CollectStates()
        {
            for (int i = 0; i < _shipCounts.Length; i++)
            {
                _shipCounts[i] = 0;
                _aliveBases[i] = false;
            }

            foreach (IShipEntity ship in _shipService.Ships)
            {
                _shipCounts[ship.Owner.Index]++;
            }

            foreach (GameEntity entity in _entityLocator.Entities)
            {
                if (entity.IsPlayerBase())
                {
                    _aliveBases[entity.Owner.Index] |= !entity.HealthModel.IsDestroyed && entity.HealthModel.HasUnits;
                }
            }

            _states.Clear();
            foreach (PlayerSlot player in _roster.Players)
            {
                int index = player.Id.Index;
                _states.Add(new PlayerBattleState(
                    player.Id,
                    player.Team,
                    _shipCounts[index],
                    _aliveBases[index],
                    _playerRegistry.HasPendingReinforcement(player.Id)));
            }
        }

        private BattleResult CreateResult(BattleOutcome outcome)
        {
            int playerShipCount = 0;
            int enemyShipCount = 0;
            bool isPlayerBaseAlive = false;
            bool isEnemyBaseAlive = false;
            List<FactionType> enemyFactions = new List<FactionType>();
            foreach (PlayerBattleState state in _states)
            {
                if (_localPlayer.IsFriendly(state.Player))
                {
                    playerShipCount += state.ShipCount;
                    isPlayerBaseAlive |= state.IsBaseAlive;
                    continue;
                }

                enemyShipCount += state.ShipCount;
                isEnemyBaseAlive |= state.IsBaseAlive;
                FactionType faction = _roster.Get(state.Player).Faction;
                if (!enemyFactions.Contains(faction))
                {
                    enemyFactions.Add(faction);
                }
            }

            return new BattleResult(
                outcome,
                _gameModel.VictoryCondition,
                _gameModel.PlanetType,
                _localPlayer.Slot.Faction,
                enemyFactions,
                playerShipCount,
                enemyShipCount,
                isPlayerBaseAlive,
                isEnemyBaseAlive);
        }
    }
}
