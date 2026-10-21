using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Services.Ships;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Services.CaptureSites;

namespace EmpireAtWar.Services.Battle
{
    public sealed class BattleVictoryService : IService, INotifier<BattleResult>, IInitializable, ILateDisposable,
        ITickable, IObserver<BattleState>
    {
        private readonly IGameModelObserver _gameModel;
        private readonly IShipService _shipService;
        private readonly IEntityLocator _entityLocator;
        private readonly IPlayerRoster _playerRoster;
        private readonly ILocalPlayer _localPlayer;
        private readonly IPlayerRegistry _playerRegistry;
        private readonly ICaptureSitesSystem _captureSites;
        private readonly INotifier<BattleState> _battleState;

        private readonly IBattleOutcomeRule _victoryModel;
        private readonly List<IObserver<BattleResult>> _observers = new List<IObserver<BattleResult>>();
        private readonly int[] _shipCounts = new int[MatchRules.MAX_PLAYERS];
        private readonly int[] _unitCounts = new int[MatchRules.MAX_PLAYERS];
        private readonly bool[] _aliveBases = new bool[MatchRules.MAX_PLAYERS];
        private readonly List<PlayerBattleState> _states = new List<PlayerBattleState>();
        private BattleResult _finalResult;
        private bool _isRunning;

        public string Id => nameof(BattleVictoryService);
        public void Reset() => _finalResult = null;

        public BattleVictoryService(
            IGameModelObserver gameModel,
            IShipService shipService,
            IEntityLocator entityLocator,
            IPlayerRoster playerRoster,
            ILocalPlayer localPlayer,
            IPlayerRegistry playerRegistry,
            INotifier<BattleState> battleState,
            IBattleOutcomeRule victoryModel,
            ICaptureSitesSystem captureSites)
        {
            _captureSites = captureSites;
            _gameModel = gameModel;
            _shipService = shipService;
            _entityLocator = entityLocator;
            _victoryModel = victoryModel;
            _playerRoster = playerRoster;
            _localPlayer = localPlayer;
            _playerRegistry = playerRegistry;
            _battleState = battleState;
        }

        public void Initialize()
        {
            _battleState.AddObserver(this);
        }

        public void LateDispose()
        {
            _battleState.RemoveObserver(this);
        }

        // Before the battle runs no station exists, so evaluating would end it at once.
        public void UpdateState(BattleState state)
        {
            _isRunning = state == BattleState.Running;
        }

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
            if (!_isRunning || _finalResult != null)
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
                _unitCounts[i] = 0;
                _aliveBases[i] = false;
            }

            IReadOnlyList<IShipEntity> ships = _shipService.Ships;
            for (int i = 0; i < ships.Count; i++)
            {
                IShipEntity ship = ships[i];
                _shipCounts[ship.Owner.Index]++;
            }

            foreach (GameEntity entity in _entityLocator.Entities)
            {
                if (entity.IsPlayerBase())
                {
                    _aliveBases[entity.Owner.Index] |= !entity.HealthModel.IsDestroyed && entity.HealthModel.HasUnits;
                }
                else if (!entity.HealthModel.IsDestroyed)
                {
                    // Ships, squadrons, defense platforms and mining facilities all keep a player in the battle.
                    _unitCounts[entity.Owner.Index]++;
                }
            }

            // Construction survives station loss and becomes an entity when it completes.
            IReadOnlyList<ICaptureSite> sites = _captureSites.Sites;
            for (int i = 0; i < sites.Count; i++)
            {
                ICaptureSite site = sites[i];
                if (site.State == CaptureSiteState.Constructing)
                    _unitCounts[site.Owner.Index]++;
            }

            _states.Clear();
            IReadOnlyList<PlayerSlot> players = _playerRoster.Players;
            for (int i = 0; i < players.Count; i++)
            {
                PlayerSlot player = players[i];
                int index = player.Id.Index;
                _states.Add(new PlayerBattleState(
                    player.Id,
                    player.Team,
                    _unitCounts[index],
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
                    playerShipCount += _shipCounts[state.Player.Index];
                    isPlayerBaseAlive |= state.IsBaseAlive;
                    continue;
                }

                enemyShipCount += _shipCounts[state.Player.Index];
                isEnemyBaseAlive |= state.IsBaseAlive;
                FactionType faction = _playerRoster.Get(state.Player).Faction;
                if (!enemyFactions.Contains(faction))
                {
                    enemyFactions.Add(faction);
                }
            }

            return new BattleResult(
                outcome: outcome,
                victoryCondition: _gameModel.VictoryCondition,
                planet: _gameModel.PlanetType,
                playerFaction: _localPlayer.Slot.Faction,
                enemyFactions: enemyFactions,
                playerShipCount: playerShipCount,
                enemyShipCount: enemyShipCount,
                isPlayerBaseAlive: isPlayerBaseAlive,
                isEnemyBaseAlive: isEnemyBaseAlive, mode: _gameModel.GameMode);
        }
    }
}
