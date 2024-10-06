using LightWeightFramework.Command;

namespace EmpireAtWar.Commands.Reinforcement
{
    public interface IReinforcementCommand:ICommand
    {
        void TrySpawnReinforcement(string id);
    }
}