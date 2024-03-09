using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Patterns.ChainOfResponsibility;

namespace EmpireAtWar.Controllers.Economy
{
    public interface IPurchaseChain:IChainHandler<UnitRequest>
    {
        void Revert(UnitRequest result);
    }
}