using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiningFacility;
using LightWeightFramework.Command;

namespace EmpireAtWar.Commands.Reinforcement
{
    public interface IReinforcementCommand:ICommand
    {
        void TrySpawnShip(ShipType shipType);
        void TrySpawnMiningFacility(MiningFacilityType miningFacilityType);
    }
}