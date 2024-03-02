using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Controllers.Reinforcement;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Patterns.ChainOfResponsibility;

namespace EmpireAtWar.Controllers.Factions
{
    public interface IPurchaseFlow : IChainHandler<ShipType>
    {
        void Add(IBuildShipChain buildShipChain);
        // void Add(IPurchaseChain buildShipChain);
        // void Add(IReinforcementChain buildShipChain);
    }

    public class PurchaseFlow : IPurchaseFlow
    {
        private readonly IPurchaseChain purchaseChain;
        private readonly IReinforcementChain reinforcementChain;
        private IChainHandler<ShipType> next;

        public PurchaseFlow(IPurchaseChain purchaseChain, IReinforcementChain reinforcementChain)
        {
            this.purchaseChain = purchaseChain;
            this.reinforcementChain = reinforcementChain;
        }
        
        public void Add(IBuildShipChain buildShipChain)
        {
            ConstructChains(buildShipChain);
        }

        private void ConstructChains(IBuildShipChain buildShipChain)
        {
            SetNext(purchaseChain)
                .SetNext(buildShipChain)
                .SetNext(reinforcementChain);
        }

        public IChainHandler<ShipType> SetNext(IChainHandler<ShipType> chainHandler)
        {
            next = chainHandler;
            return next;
        }

        public void Handle(ShipType request)
        {
            if (next != null)
            {
                next.Handle(request);
            }
        }
    }
}