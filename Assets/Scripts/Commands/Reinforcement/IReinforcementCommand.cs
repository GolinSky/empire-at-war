using EmpireAtWar.Mvc;

namespace EmpireAtWar.Commands.Reinforcement
{
    public interface IReinforcementCommand:ICommand
    {
        void TrySpawnReinforcement(string id);
    }
}