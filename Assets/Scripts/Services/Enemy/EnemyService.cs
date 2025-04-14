using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Map;
using EmpireAtWar.Patterns.Strategy;
using EmpireAtWar.Views.SpaceStation;
using LightWeightFramework.Components.Service;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyService : IService
    {
    }

    public class EnemyService : Service, IInitializable, IEnemyService, ITickable
    {
        
        private IUnitSpawnStrategy _currentStrategy;
        private Vector3 _stationPosition;
        private SpaceStationView _spaceStationView;
        private readonly SpaceStationViewFacade _spaceStationViewFacade;
        private readonly EnemyPurchaseProcessor _enemyPurchaseProcessor;
        private readonly LazyInject<IMapModelObserver> _mapModel;
        private readonly IUnitRequestFactory _unitRequestFactory;
        private readonly EnemyFactionModel _enemyFactionModel;

        [Inject(Id = PlayerType.Opponent)]
        public FactionType FactionType { get; }

        public EnemyService(
            LazyInject<IMapModelObserver> mapModel,
            SpaceStationViewFacade spaceStationViewFacade,
            EnemyPurchaseProcessor enemyPurchaseProcessor, 
            IUnitRequestFactory unitRequestFactory,
            EnemyFactionModel enemyFactionModel)
        {
            _mapModel = mapModel;
            _spaceStationViewFacade = spaceStationViewFacade;
            _enemyPurchaseProcessor = enemyPurchaseProcessor;
            _unitRequestFactory = unitRequestFactory;
            _enemyFactionModel = enemyFactionModel;
        }
        
        public void Initialize()
        {
            SetStrategy(UnitSpawnStrategyType.LevelUpFast);
            _stationPosition = _mapModel.Value.GetStationPosition(PlayerType.Opponent);
            _spaceStationView = _spaceStationViewFacade.Create(PlayerType.Opponent, FactionType,  _stationPosition);
        }
        
        public void Tick()
        {
            _currentStrategy?.Update();
        }
        
        
        public void SetStrategy(UnitSpawnStrategyType strategyType)
        {
            if (_currentStrategy != null)
            {
                _currentStrategy.Stop();
            }
            //todo: refactor this - get rid of new - use factory
            _currentStrategy = new TempStrategy(_enemyFactionModel, _enemyPurchaseProcessor, _unitRequestFactory);
            _currentStrategy.Start();
        }
    }
}