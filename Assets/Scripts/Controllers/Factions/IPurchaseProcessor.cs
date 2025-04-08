using EmpireAtWar.Patterns.ChainOfResponsibility;

namespace EmpireAtWar.Controllers.Factions
{
    public interface IPurchaseProcessor : IChainHandler<UnitRequest>
    {
        void Add(IBuildShipChain buildShipChain);
        void RevertFlow(UnitRequest result);
    }

    public interface IEnemyPurchaseProcessor: IChainHandler<UnitRequest>
    {
        
    }
}