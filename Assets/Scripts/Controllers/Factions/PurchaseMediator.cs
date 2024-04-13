using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Reinforcement;
using EmpireAtWar.Models.Factions;
using Zenject;

namespace EmpireAtWar.Controllers.Factions
{
    public sealed class PurchaseMediator : BasePurchaseMediator
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

        
        public override void RevertFlow(UnitRequest result)
        {
            purchaseChain.Revert(result);

        }

        protected override void ConstructChains(IBuildShipChain buildShipChain)
        {
            SetNext(purchaseChain)
                .SetNext(buildShipChain)
                .SetNext(reinforcementChain);
        }
    }
}