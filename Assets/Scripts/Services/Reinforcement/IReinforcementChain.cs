using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Patterns.ChainOfResponsibility;

namespace EmpireAtWar.Services.Reinforcement
{
    public interface IReinforcementChain : IChainHandler<UnitRequest>
    {
    }
}
