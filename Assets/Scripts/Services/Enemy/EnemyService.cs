using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Player;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyService : IService
    {
    }

    public class EnemyService : Service, IInitializable, ILateDisposable, IEnemyService, ITickable,
        IStationSpawner, IObserver<BattleState>
    {
        private readonly SpaceStationFactory _spaceStationFactory;
        private readonly IMapModelObserver _mapModel;
        private readonly IPlayerRegistry _playerRegistry;
        private readonly INotifier<BattleState> _battleState;
        private readonly EnemyProductionStrategy _productionStrategy;
        private readonly PlayerSlot _owner;

        private bool _isRunning;
        private bool _hasStartedProduction;

        public EnemyService(
            IMapModelObserver mapModel,
            SpaceStationFactory spaceStationFactory,
            IPlayerRegistry playerRegistry,
            INotifier<BattleState> battleState,
            EnemyProductionStrategy productionStrategy,
            PlayerSlot owner)
        {
            _owner = owner;
            _mapModel = mapModel;
            _spaceStationFactory = spaceStationFactory;
            _playerRegistry = playerRegistry;
            _battleState = battleState;
            _productionStrategy = productionStrategy;
        }

        public void Initialize()
        {
            _playerRegistry.RegisterStationSpawner(_owner.Id, this);
            _battleState.AddObserver(this);
        }

        public void LateDispose()
        {
            _playerRegistry.UnregisterStationSpawner(_owner.Id);
            _battleState.RemoveObserver(this);
        }

        public void Spawn()
        {
            _spaceStationFactory.Create(_owner.Id, _owner.Faction, _mapModel.GetStationPosition(_owner.Id));
        }

        public void UpdateState(BattleState state)
        {
            _isRunning = state == BattleState.Running;
            if (_isRunning && !_hasStartedProduction)
            {
                _hasStartedProduction = true;
                _productionStrategy.Start();
            }
        }

        public void Tick()
        {
            if (_isRunning)
            {
                _productionStrategy.Tick(Time.deltaTime);
            }
        }
    }
}
