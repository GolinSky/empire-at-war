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

    //todo: this is more facade than mediator - rebuild
    public class EconomyMediator : Service, IEconomyMediator
    {
        [Inject(Id = PlayerType.Player)] 
        private LazyInject<IEconomyProvider> PlayerEconomyProvider { get; }
        
        [Inject(Id = PlayerType.Opponent)] 
        private LazyInject<IEconomyProvider> OpponentEconomyProvider { get; }


        public IEconomyProvider GetProvider(PlayerType playerType)
        {
            switch (playerType)
            {
                case PlayerType.Player:
                    return PlayerEconomyProvider.Value;
                case PlayerType.Opponent:
                    return OpponentEconomyProvider.Value;
            }

            return null;
        }
        
    }
}