using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using Zenject;

namespace EmpireAtWar.Services.Player
{
    public interface IPlayerService : IService
    {
    }

    public class PlayerService : Service, IInitializable, ILateDisposable, IPlayerService, IStationSpawner
    {
        private readonly SpaceStationFactory _spaceStationFactory;
        private readonly IMapModelObserver _mapModel;
        private readonly IPlayerRegistry _playerRegistry;
        private readonly PlayerSlot _owner;

        public PlayerService(
            SpaceStationFactory spaceStationFactory,
            IMapModelObserver mapModel,
            IPlayerRegistry playerRegistry,
            PlayerSlot owner)
        {
            _owner = owner;
            _spaceStationFactory = spaceStationFactory;
            _mapModel = mapModel;
            _playerRegistry = playerRegistry;
        }

        public void Initialize()
        {
            _playerRegistry.RegisterStationSpawner(_owner.Id, this);
        }

        public void LateDispose()
        {
            _playerRegistry.UnregisterStationSpawner(_owner.Id);
        }

        public void Spawn()
        {
            _spaceStationFactory.Create(
                _owner.Id,
                _owner.Faction,
                _mapModel.GetStationPosition(_owner.Id));
        }
    }
}
