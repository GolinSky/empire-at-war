using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Controllers.Reinforcement;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Patterns.ChainOfResponsibility;

namespace EmpireAtWar.Controllers.Factions
{
    public interface IPurchaseMediator : IChainHandler<ShipType>
    {
        void Add(IBuildShipChain buildShipChain);
        void RevertFlow(ShipType result);
    }

    public class PurchaseMediator : IPurchaseMediator
    {
        private readonly IPurchaseChain purchaseChain;
        private readonly IReinforcementChain reinforcementChain;
        private IChainHandler<ShipType> next;

        public PurchaseMediator(IPurchaseChain purchaseChain, IReinforcementChain reinforcementChain)
        {
            this.purchaseChain = purchaseChain;
            this.reinforcementChain = reinforcementChain;
        }
        
        public void Add(IBuildShipChain buildShipChain)
        {
            ConstructChains(buildShipChain);
        }

        public void RevertFlow(ShipType result)
        {
            purchaseChain.Revert(result);
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