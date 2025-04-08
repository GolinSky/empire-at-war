using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Reinforcement;
using EmpireAtWar.Models.Factions;
using Zenject;

namespace EmpireAtWar.Controllers.Factions
{
    public sealed class PurchaseProcessor : BasePurchaseMediator, IPurchaseProcessor
    {
        private readonly IPurchaseChain _purchaseChain;
        private readonly IReinforcementChain _reinforcementChain;
        
        public PurchaseProcessor(
            [Inject(Id = PlayerType.Player)] IPurchaseChain purchaseChain,
            [Inject(Id = PlayerType.Player)] IReinforcementChain reinforcementChain)

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