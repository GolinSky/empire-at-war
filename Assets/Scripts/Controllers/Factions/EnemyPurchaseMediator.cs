using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Models.Factions;
using Zenject;

namespace EmpireAtWar.Controllers.Factions
{
    public sealed class EnemyPurchaseMediator : BasePurchaseMediator
    {
        private readonly IPurchaseChain purchaseChain;

       
        public EnemyPurchaseMediator(
            [Inject(Id = PlayerType.Opponent)] IPurchaseChain purchaseChain,
            [Inject(Id = PlayerType.Opponent)] IBuildShipChain buildShipChain)

        {
            this.purchaseChain = purchaseChain;
            Add(buildShipChain);
        }

        public override void RevertFlow(UnitRequest result)
        {
            purchaseChain.Revert(result);
        }

        protected override void ConstructChains(IBuildShipChain buildShipChain)
        {
            SetNext(purchaseChain)
                .SetNext(buildShipChain);
        }
    }
}