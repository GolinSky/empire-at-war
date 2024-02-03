using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Skirmish;
using EmpireAtWar.Views.Ship;
using EmpireAtWar.Views.SpaceStation;
using WorkShop.LightWeightFramework.Service;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyService : IService
    {
    }

    public class EnemyService : Service, IInitializable, IEnemyService
    {
        private readonly FactionType factionType;
        private readonly SkirmishGameData skirmishGameData;
        private readonly ShipFacadeFactory shipFacadeFactory;
        private readonly SpaceStationViewFacade spaceStationViewFacade;

        public EnemyService(SkirmishGameData skirmishGameData, ShipFacadeFactory shipFacadeFactory, SpaceStationViewFacade spaceStationViewFacade)
        {
            factionType = skirmishGameData.EnemyFactionType;
            this.skirmishGameData = skirmishGameData;
            this.shipFacadeFactory = shipFacadeFactory;
            this.spaceStationViewFacade = spaceStationViewFacade;
        }
        public void Initialize()
        {
            spaceStationViewFacade.Create(PlayerType.Opponent, factionType,  skirmishGameData.GetStationPosition(PlayerType.Opponent));
        }
    }
}