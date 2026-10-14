using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.Game
{
    public static class SimulationPreset
    {
        public static BattleDefinitionBuilder Create(SimulationSetup setup)
        {
            var builder = new BattleDefinitionBuilder
            {
                Mode = GameMode.BattleSimulation, Map = new MapSpec(MapType.Empty, PlanetType.Coruscant, MapSize.Large),
                Outcome = OutcomeRule.Simulation, Features = BattleFeatures.None, StartingMoney = 0, Simulation = setup
            };
            for (int i = 0; i < 2; i++)
                builder.Players.Add(new BattlePlayer(new PlayerSlot(new PlayerId(i), new TeamId(i),
                    i == 0 ? setup.FactionA : setup.FactionB, PlayerController.Ai, EnemyAiDifficulty.Medium, i),
                    PlayerRole.SimulationSide, BattleFeatures.None));
            return builder;
        }
    }
}
