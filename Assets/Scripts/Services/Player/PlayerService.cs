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

    public class PlayerService : Service, IInitializable, IPlayerService
    {
        private readonly SpaceStationFactory _spaceStationFactory;
        private readonly LazyInject<IMapModelObserver> _mapModel;

        private readonly PlayerSlot _owner;

        public PlayerService(
            SpaceStationFactory spaceStationFactory,
            LazyInject<IMapModelObserver> mapModel,
            PlayerSlot owner)
        {
            _owner = owner;
            _spaceStationFactory = spaceStationFactory;
            _mapModel = mapModel;
        }

        public void Initialize()
        {
            _spaceStationFactory.Create(
                _owner.Id,
                _owner.Faction,
                _mapModel.Value.GetStationPosition(_owner.Id));
            
        }
    }
}
