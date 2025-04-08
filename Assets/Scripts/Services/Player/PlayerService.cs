using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Map;
using EmpireAtWar.Views.SpaceStation;
using LightWeightFramework.Components.Service;
using Zenject;

namespace EmpireAtWar.Services.Player
{
    public interface IPlayerService : IService
    {
    }

    public class PlayerService : Service, IInitializable, IPlayerService
    {
        private readonly SpaceStationViewFacade _spaceStationViewFacade;
        private readonly LazyInject<IMapModelObserver> _mapModel;

        [Inject(Id = PlayerType.Player)]
        private FactionType FactionType { get; }
        
        public PlayerService(
            SpaceStationViewFacade spaceStationViewFacade,
            LazyInject<IMapModelObserver> mapModel)
        {
            _spaceStationViewFacade = spaceStationViewFacade;
            _mapModel = mapModel;
        }

        public void Initialize()
        {
            _spaceStationViewFacade.Create(
                PlayerType.Player,
                FactionType,
                _mapModel.Value.GetStationPosition(PlayerType.Player));
            

        }
    }
}