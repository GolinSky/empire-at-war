using EmpireAtWar.Models.Factions;
using LightWeightFramework.Command;

namespace EmpireAtWar.Commands.Reinforcement
{
    public interface IReinforcementCommand:ICommand
    {
        void TrySpawnShip(ShipType shipType);
    }
}