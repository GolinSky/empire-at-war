using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Reinforcement;
using EmpireAtWar.Models.Factions;
using Zenject;

namespace EmpireAtWar.Controllers.Factions
{
    public sealed class PurchaseProcessor : BasePurchaseMediator, IPurchaseProcessor
    {
        private readonly IPurchaseChain purchaseChain;
        private readonly IReinforcementChain reinforcementChain;
        
        public PurchaseProcessor(
            [Inject(Id = PlayerType.Player)] IPurchaseChain purchaseChain,
            [Inject(Id = PlayerType.Player)] IReinforcementChain reinforcementChain)

        {
            this.purchaseChain = purchaseChain;
            this.reinforcementChain = reinforcementChain;
        }


        public void Add(IBuildShipChain buildShipChain)
        {
            ConstructChains(buildShipChain);
        }

        public void RevertFlow(UnitRequest result)
        {
            purchaseChain.Revert(result);
        }

        private void ConstructChains(IBuildShipChain buildShipChain)
        {
            SetNext(purchaseChain)
                .SetNext(buildShipChain)
                .SetNext(reinforcementChain);
        }
    }
}