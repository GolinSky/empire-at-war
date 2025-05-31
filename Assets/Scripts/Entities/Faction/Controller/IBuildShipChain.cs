using EmpireAtWar.Patterns.ChainOfResponsibility;

namespace EmpireAtWar.Controllers.Factions
{
    public interface IBuildShipChain : IChainHandler<UnitRequest>
    {
        
    }
}