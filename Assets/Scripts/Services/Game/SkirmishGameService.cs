using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Skirmish;
using WorkShop.LightWeightFramework.Service;

namespace EmpireAtWar.Services.Game
{
    public interface ISkirmishGameService : IService
    {
    }
    
    public class SkirmishGameService:Service, ISkirmishGameService
    {
        private readonly SkirmishGameData skirmishGameData;

        public SkirmishGameService(SkirmishGameData skirmishGameData)
        {
            this.skirmishGameData = skirmishGameData;
        }

      
    }
}