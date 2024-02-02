using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Skirmish;
using EmpireAtWar.Views.Ship;
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
        private readonly ShipFacadeFactory shipFacadeFactory;

        public EnemyService(SkirmishGameData skirmishGameData, ShipFacadeFactory shipFacadeFactory)
        {
            factionType = skirmishGameData.EnemyFactionType;
            this.shipFacadeFactory = shipFacadeFactory;
        }
        public void Initialize()
        {
            
        }
    }
}