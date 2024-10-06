using EmpireAtWar.Patterns.ChainOfResponsibility;

namespace EmpireAtWar.Controllers.Factions
{
    public interface IPurchaseMediator : IChainHandler<UnitRequest>
    {
        void Add(IBuildShipChain buildShipChain);
        void RevertFlow(UnitRequest result);
    }

    public interface IEnemyPurchaseMediator: IChainHandler<UnitRequest>
    {
        
    }
}