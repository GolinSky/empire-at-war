using System;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.Game
{
    /// <summary>
    /// Guided tutorial battle: the human against an Easy AI with no economy, so the enemy never builds and only
    /// commands the units the tutorial gives it. Victory = destroy the enemy station.
    /// </summary>
    public static class TutorialPreset
    {
        public static BattleDefinition Create(FactionType playerFaction)
        {
            BattleDefinitionBuilder builder = new BattleDefinitionBuilder
            {
                Mode = GameMode.Tutorial,
                Map = new MapSpec(MapType.Full, PlanetType.Coruscant, MapSize.Small, SkirmishPreset.NewMapSeed()),
                VictoryCondition = BattleVictoryCondition.DestroyOpponentBase
            };
            builder.Players.Add(new BattlePlayer(new PlayerSlot(new PlayerId(0), new TeamId(0), playerFaction,
                PlayerController.Human, EnemyAiDifficulty.Easy, 0), PlayerRole.Human, BattleFeatures.All));
            builder.Players.Add(new BattlePlayer(new PlayerSlot(new PlayerId(1), new TeamId(1), Opponent(playerFaction),
                PlayerController.Ai, EnemyAiDifficulty.Easy, 1), PlayerRole.AiOpponent, BattleFeatures.None));
            return builder.Build();
        }

        private static FactionType Opponent(FactionType faction) => faction switch
        {
            FactionType.Republic => FactionType.Separatist,
            FactionType.Separatist => FactionType.Republic,
            FactionType.Rebellion => FactionType.Empire,
            FactionType.Empire => FactionType.Rebellion,
            _ => throw new ArgumentOutOfRangeException(nameof(faction), faction, null)
        };
    }
}
