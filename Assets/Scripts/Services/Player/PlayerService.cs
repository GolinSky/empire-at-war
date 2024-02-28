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
        private readonly MapModel mapModel;

        [Inject(Id = PlayerType.Player)]
        private FactionType FactionType { get; }
        
        public PlayerService(
            SpaceStationViewFacade spaceStationViewFacade,
            MapModel mapModel)
        {
            this.spaceStationViewFacade = spaceStationViewFacade;
            this.mapModel = mapModel;
        }

        public void Initialize()
        {
            spaceStationViewFacade.Create(
                PlayerType.Player,
                FactionType,
                mapModel.GetStationPosition(PlayerType.Player));
            

        }
    }
}