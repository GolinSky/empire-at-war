using EmpireAtWar.Patterns.ChainOfResponsibility;

namespace EmpireAtWar.Controllers.Factions
{
    public interface IEnemyPurchaseProcessor: IChainHandler<UnitRequest>
    {

    }
}
