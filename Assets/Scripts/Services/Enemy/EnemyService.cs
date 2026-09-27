using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;
using SpaceStationEntity = EmpireAtWar.Entities.SpaceStation.SpaceStation;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyService : IService
    {
    }

    public class EnemyService : Service, IInitializable, IEnemyService, ITickable
    {
        
        private Vector3 _stationPosition;
        private SpaceStationEntity _spaceStation;
        private readonly SpaceStationFactory _spaceStationFactory;
        private readonly LazyInject<IMapModelObserver> _mapModel;
        private readonly EnemyProductionStrategy _productionStrategy;

        [Inject(Id = PlayerType.Opponent)]
        public FactionType FactionType { get; }

        public EnemyService(
            LazyInject<IMapModelObserver> mapModel,
            SpaceStationFactory spaceStationFactory,
            EnemyProductionStrategy productionStrategy)
        {
            _mapModel = mapModel;
            _spaceStationFactory = spaceStationFactory;
            _productionStrategy = productionStrategy;
        }
        
        public void Initialize()
        {
            _stationPosition = _mapModel.Value.GetStationPosition(FactionType);
            _spaceStation = _spaceStationFactory.Create(PlayerType.Opponent, FactionType, _stationPosition);
            _productionStrategy.Start();
        }
        
        public void Tick()
        {
            _productionStrategy.Tick(Time.deltaTime);
        }
    }
}
