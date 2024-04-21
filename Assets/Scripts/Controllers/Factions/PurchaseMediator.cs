using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Reinforcement;
using EmpireAtWar.Models.Factions;
using Zenject;

namespace EmpireAtWar.Controllers.Factions
{
    public sealed class PurchaseMediator : BasePurchaseMediator, IPurchaseMediator
    {
        private readonly IPurchaseChain purchaseChain;
        private readonly IReinforcementChain reinforcementChain;
        
        public PurchaseMediator(
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