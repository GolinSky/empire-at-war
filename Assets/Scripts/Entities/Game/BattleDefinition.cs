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

        public BattleDefinition(GameMode mode, MapSpec map, IReadOnlyList<BattlePlayer> players,
            BattleFeatures features, OutcomeRule outcome, BattleVictoryCondition victoryCondition, float startingMoney,
            SurvivalSettings survival = null)
        {
            Mode = mode;
            Map = map;
            Players = System.Array.AsReadOnly(players.ToArray());
            Slots = System.Array.AsReadOnly(players.Select(player => player.Slot).ToArray());
            Features = features;
            Outcome = outcome;
            VictoryCondition = victoryCondition;
            StartingMoney = startingMoney;
            Survival = survival;
        }

        public BattlePlayer GetPlayer(PlayerId id) => Players[id.Index];
        public bool HasFeature(BattleFeatures feature) => (Features & feature) == feature;
    }
}
