using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.Game
{
    public sealed class BattleDefinitionBuilder
    {
        public GameMode Mode { get; set; } = GameMode.Skirmish;
        public MapSpec Map { get; set; } = new MapSpec(MapType.Full, PlanetType.Coruscant, MapSize.Small);
        public List<BattlePlayer> Players { get; } = new List<BattlePlayer>();
        public BattleFeatures Features { get; set; } = BattleFeatures.All;
        public OutcomeRule Outcome { get; set; } = OutcomeRule.Skirmish;
        public BattleVictoryCondition VictoryCondition { get; set; }
        public float StartingMoney { get; set; } = 1000f;
        public SurvivalSettings Survival { get; set; }
        public SimulationSetup Simulation { get; set; }
        public int InitialStationLevel { get; set; } = 1;

        public BattleDefinition Build()
        {
            BattleDefinition definition = new BattleDefinition(Mode, Map, Players, Features,
                Outcome, VictoryCondition, StartingMoney, Survival, Simulation, InitialStationLevel);
            MatchRules.Validate(definition);
            return definition;
        }
    }
}
