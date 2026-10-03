using EmpireAtWar.Controllers.Factions;

namespace EmpireAtWar.Services.Reinforcement
{
    public interface IReinforcementPool
    {
        void Add(UnitRequest request);
    }
}
