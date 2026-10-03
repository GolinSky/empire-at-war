using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Players;
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
        private SpaceStationEntity _spaceStation;
        private readonly SpaceStationFactory _spaceStationFactory;
        private readonly LazyInject<IMapModelObserver> _mapModel;
        private readonly EnemyProductionStrategy _productionStrategy;
        private readonly PlayerSlot _owner;

        private Vector3 _stationPosition;

        public EnemyService(
            LazyInject<IMapModelObserver> mapModel,
            SpaceStationFactory spaceStationFactory,
            EnemyProductionStrategy productionStrategy,
            PlayerSlot owner)
        {
            _owner = owner;
            _mapModel = mapModel;
            _spaceStationFactory = spaceStationFactory;
            _productionStrategy = productionStrategy;
        }

        public void Initialize()
        {
            _stationPosition = _mapModel.Value.GetStationPosition(_owner.Id);
            _spaceStation = _spaceStationFactory.Create(_owner.Id, _owner.Faction, _stationPosition);
            _productionStrategy.Start();
        }

        public void Tick()
        {
            _productionStrategy.Tick(Time.deltaTime);
        }
    }
}
