using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.Game
{
    public static class SkirmishPreset
    {
        public static BattleDefinitionBuilder Create(IReadOnlyList<PlayerSlot> players, PlanetType planet,
            MapSize size, BattleVictoryCondition victoryCondition, float startingMoney)
        {
            BattleDefinitionBuilder builder = new BattleDefinitionBuilder
            {
                Map = new MapSpec(MapType.Full, planet, size),
                VictoryCondition = victoryCondition,
                StartingMoney = startingMoney
            };
            foreach (PlayerSlot slot in players)
                builder.Players.Add(new BattlePlayer(slot,
                    slot.IsAi ? PlayerRole.AiOpponent : PlayerRole.Human, BattleFeatures.All));
            return builder;
        }

        public static BattleDefinition Default() => Create(new[]
        {
            new PlayerSlot(new PlayerId(0), new TeamId(0), FactionType.Republic,
                PlayerController.Human, EnemyAiDifficulty.Medium, 0),
            new PlayerSlot(new PlayerId(1), new TeamId(1), FactionType.Separatist,
                PlayerController.Ai, EnemyAiDifficulty.Medium, 1)
        }, PlanetType.Coruscant, MapSize.Small, BattleVictoryCondition.DestroyEnemyFleet, 1000f).Build();
    }
}
