using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Reinforcement;

namespace EmpireAtWar.Controllers.Factions
{
    public sealed class PurchaseProcessor : BasePurchaseProcessor, IPurchaseProcessor
    {
        private readonly IPurchaseChain _purchaseChain;
        private readonly IReinforcementChain _reinforcementChain;
        
        public PurchaseProcessor(IPurchaseChain purchaseChain, IReinforcementChain reinforcementChain)
        {
            _purchaseChain = purchaseChain;
            _reinforcementChain = reinforcementChain;
        }


        public void Add(IBuildShipChain buildShipChain)
        {
            ConstructChains(buildShipChain);
        }

        public void RevertFlow(UnitRequest result)
        {
            _purchaseChain.Revert(result);
        }

        private void ConstructChains(IBuildShipChain buildShipChain)
        {
            SetNext(_purchaseChain)
                .SetNext(buildShipChain)
                .SetNext(_reinforcementChain);
        }
    }
}