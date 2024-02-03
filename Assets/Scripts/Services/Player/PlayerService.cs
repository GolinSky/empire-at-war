using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Skirmish;
using EmpireAtWar.Views.SpaceStation;
using WorkShop.LightWeightFramework.Service;
using Zenject;

namespace EmpireAtWar.Services.Player
{
    public interface IPlayerService:IService
    {
        
    }
    public class PlayerService:Service, IInitializable
    {
        private readonly SkirmishGameData skirmishGameData;
        private readonly SpaceStationViewFacade spaceStationViewFacade;

        public PlayerService(SkirmishGameData skirmishGameData, SpaceStationViewFacade spaceStationViewFacade)
        {
            this.skirmishGameData = skirmishGameData;
            this.spaceStationViewFacade = spaceStationViewFacade;
        }

        public void Initialize()
        {
            spaceStationViewFacade.Create(PlayerType.Player, skirmishGameData.PlayerFactionType, skirmishGameData.GetStationPosition(PlayerType.Player));
        }
    }
}