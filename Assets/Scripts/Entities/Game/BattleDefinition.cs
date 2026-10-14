using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.Game
{
    public sealed class BattleDefinition
    {
        public GameMode Mode { get; }
        public MapSpec Map { get; }
        public IReadOnlyList<BattlePlayer> Players { get; }
        public IReadOnlyList<PlayerSlot> Slots { get; }
        public BattleFeatures Features { get; }
        public OutcomeRule Outcome { get; }
        public BattleVictoryCondition VictoryCondition { get; }
        public float StartingMoney { get; }
        public SurvivalSettings Survival { get; }
        public SimulationSetup Simulation { get; }
        public int InitialStationLevel { get; }
        public BattlePlayer Human => Players.Single(player => player.Role == PlayerRole.Human);

        public BattleDefinition(GameMode mode, MapSpec map, IReadOnlyList<BattlePlayer> players,
            BattleFeatures features, OutcomeRule outcome, BattleVictoryCondition victoryCondition, float startingMoney,
            SurvivalSettings survival = null, SimulationSetup simulation = null, int initialStationLevel = 1)
        {
            Mode = mode;
            Map = map;
            // Each player keeps only the features the whole battle enables.
            Players = System.Array.AsReadOnly(players
                .Select(player => new BattlePlayer(player.Slot, player.Role, player.Features & features)).ToArray());
            Slots = System.Array.AsReadOnly(players.Select(player => player.Slot).ToArray());
            Features = features;
            Outcome = outcome;
            VictoryCondition = victoryCondition;
            StartingMoney = startingMoney;
            Survival = survival;
            Simulation = simulation;
            InitialStationLevel = initialStationLevel;
        }

        public BattlePlayer GetPlayer(PlayerId id) => Players[id.Index];
        /// <summary>The player whose production bases <paramref name="player"/> buys from.</summary>
        public BattlePlayer GetProductionHost(BattlePlayer player) => player.IsHosted ? Human : player;
        public bool HasFeature(BattleFeatures feature) => (Features & feature) == feature;
    }
}
