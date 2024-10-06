using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Models.Factions;
using Zenject;

namespace EmpireAtWar.Controllers.Factions
{
    public sealed class EnemyPurchaseMediator : BasePurchaseMediator,IEnemyPurchaseMediator
    {
        public EnemyPurchaseMediator(
            [Inject(Id = PlayerType.Opponent)] IPurchaseChain purchaseChain,
            [Inject(Id = PlayerType.Opponent)] IBuildShipChain buildShipChain)
        {
                SetNext(purchaseChain)
                .SetNext(buildShipChain);
        }
    }
}