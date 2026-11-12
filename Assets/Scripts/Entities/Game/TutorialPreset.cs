using System;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.Game
{
    /// <summary>
    /// Guided tutorial battle: the human against an AI with no economy and no strategic commander, so the enemy never
    /// builds and its units move only on the tutorial director's orders. Victory = destroy the enemy station.
    /// </summary>
    public static class TutorialPreset
    {
        public const PlanetType PLANET = PlanetType.Coruscant;
        public const MapSize MAP_SIZE = MapSize.Small;
        public const BattleVictoryCondition VICTORY_CONDITION = BattleVictoryCondition.DestroyOpponentBase;
        public const EnemyAiDifficulty OPPONENT_DIFFICULTY = EnemyAiDifficulty.Easy;
        public const float STARTING_MONEY = 1000f;

        public static BattleDefinition Create(FactionType playerFaction, int playerColorIndex, int opponentColorIndex)
        {
            BattleDefinitionBuilder builder = new BattleDefinitionBuilder
            {
                Mode = GameMode.Tutorial,
                Map = new MapSpec(MapType.Full, PLANET, MAP_SIZE, SkirmishPreset.NewMapSeed()),
                VictoryCondition = VICTORY_CONDITION,
                StartingMoney = STARTING_MONEY
            };
            builder.Players.Add(new BattlePlayer(new PlayerSlot(new PlayerId(0), new TeamId(0), playerFaction,
                PlayerController.Human, EnemyAiDifficulty.Easy, playerColorIndex), PlayerRole.Human, BattleFeatures.All));
            builder.Players.Add(new BattlePlayer(new PlayerSlot(new PlayerId(1), new TeamId(1), Opponent(playerFaction),
                PlayerController.Ai, OPPONENT_DIFFICULTY, opponentColorIndex), PlayerRole.AiOpponent, BattleFeatures.None));
            return builder.Build();
        }

        public static FactionType Opponent(FactionType faction) => faction switch
        {
            FactionType.Republic => FactionType.Separatist,
            FactionType.Separatist => FactionType.Republic,
            FactionType.Rebellion => FactionType.Empire,
            FactionType.Empire => FactionType.Rebellion,
            _ => throw new ArgumentOutOfRangeException(nameof(faction), faction, null)
        };
    }
}
