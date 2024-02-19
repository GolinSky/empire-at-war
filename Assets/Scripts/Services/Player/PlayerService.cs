using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Map;
using EmpireAtWar.Models.Skirmish;
using EmpireAtWar.Views.SpaceStation;
using WorkShop.LightWeightFramework.Service;
using Zenject;

namespace EmpireAtWar.Services.Player
{
    public interface IPlayerService : IService
    {
    }

    public class PlayerService : Service, IInitializable, IPlayerService
    {
        private readonly SkirmishGameData skirmishGameData;
        private readonly SpaceStationViewFacade spaceStationViewFacade;
        private readonly MapModel mapModel;

        public PlayerService(
            SkirmishGameData skirmishGameData, 
            SpaceStationViewFacade spaceStationViewFacade,
            MapModel mapModel)
        {
            this.skirmishGameData = skirmishGameData;
            this.spaceStationViewFacade = spaceStationViewFacade;
            this.mapModel = mapModel;
        }

        public void Initialize()
        {
            spaceStationViewFacade.Create(
                PlayerType.Player,
                skirmishGameData.PlayerFactionType,
                mapModel.GetStationPosition(PlayerType.Player));
        }
    }
}