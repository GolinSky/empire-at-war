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
        private readonly SpaceStationFacade _spaceStationViewFacade;
        private readonly LazyInject<IMapModelObserver> _mapModel;
        private readonly EnemyProductionStrategy _productionStrategy;

        [Inject(Id = PlayerType.Opponent)]
        public FactionType FactionType { get; }

        public EnemyService(
            LazyInject<IMapModelObserver> mapModel,
            SpaceStationFacade spaceStationViewFacade,
            EnemyProductionStrategy productionStrategy)
        {
            _mapModel = mapModel;
            _spaceStationViewFacade = spaceStationViewFacade;
            _productionStrategy = productionStrategy;
        }
        
        public void Initialize()
        {
            _stationPosition = _mapModel.Value.GetStationPosition(FactionType);
            _spaceStation = _spaceStationViewFacade.Create(PlayerType.Opponent, FactionType, _stationPosition);
            _productionStrategy.Start();
        }
        
        public void Tick()
        {
            _productionStrategy.Tick(Time.deltaTime);
        }
    }
}
