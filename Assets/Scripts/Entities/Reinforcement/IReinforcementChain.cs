using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Patterns.ChainOfResponsibility;

namespace EmpireAtWar.Controllers.Reinforcement
{
    public interface IReinforcementChain:IChainHandler<UnitRequest> {}
}