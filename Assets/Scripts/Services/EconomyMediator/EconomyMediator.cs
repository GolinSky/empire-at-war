using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Models.Factions;
using LightWeightFramework.Components.Service;
using Zenject;

namespace EmpireAtWar.Services.EconomyMediator
{
    public interface IEconomyMediator
    {
        IEconomyProvider GetProvider(PlayerType playerType);
    }

    public class EconomyMediator : Service, IEconomyMediator
    {
        [Inject(Id = PlayerType.Player)] 
        private IEconomyProvider PlayerEconomyProvider { get; }
        
        public IEconomyProvider GetProvider(PlayerType playerType)
        {
            switch (playerType)
            {
                case PlayerType.Player:
                    return PlayerEconomyProvider;
                case PlayerType.Opponent:
                    break;
            }

            return null;
        }
    }
}