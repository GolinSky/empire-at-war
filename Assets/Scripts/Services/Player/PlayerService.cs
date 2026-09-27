using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Factions;
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

        [Inject(Id = PlayerType.Player)]
        private FactionType FactionType { get; }
        
        public PlayerService(
            SpaceStationFactory spaceStationFactory,
            LazyInject<IMapModelObserver> mapModel)
        {
            _spaceStationFactory = spaceStationFactory;
            _mapModel = mapModel;
        }

        public void Initialize()
        {
            _spaceStationFactory.Create(
                PlayerType.Player,
                FactionType,
                _mapModel.Value.GetStationPosition(FactionType));
            
        }
    }
}
