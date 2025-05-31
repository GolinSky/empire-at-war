using EmpireAtWar.Controllers.Economy;

namespace EmpireAtWar.Controllers.Factions
{
    public sealed class EnemyPurchaseProcessor : BasePurchaseProcessor,IEnemyPurchaseProcessor
    {
        public EnemyPurchaseProcessor(
                IPurchaseChain purchaseChain,
                IBuildShipChain buildShipChain)
        {
                SetNext(purchaseChain)
                .SetNext(buildShipChain);
        }
    }
}