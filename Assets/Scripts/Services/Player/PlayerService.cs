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
        private readonly SpaceStationViewFacade spaceStationViewFacade;
        private readonly LazyInject<IMapModelObserver> mapModel;

        [Inject(Id = PlayerType.Player)]
        private FactionType FactionType { get; }
        
        public PlayerService(
            SpaceStationViewFacade spaceStationViewFacade,
            LazyInject<IMapModelObserver> mapModel)
        {
            this.spaceStationViewFacade = spaceStationViewFacade;
            this.mapModel = mapModel;
        }

        public void Initialize()
        {
            spaceStationViewFacade.Create(
                PlayerType.Player,
                FactionType,
                mapModel.Value.GetStationPosition(PlayerType.Player));
            

        }
    }
}