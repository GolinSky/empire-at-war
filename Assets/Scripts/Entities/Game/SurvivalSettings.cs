using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.Game
{
    public sealed class SurvivalSettings
    {
        public FactionType PlayerFaction { get; }
        public FactionType? AllyFaction { get; }
        public FactionType EnemyFaction { get; }
        public MapSize MapSize { get; }
        public EnemyAiDifficulty Difficulty { get; }
        public bool SuperWeapons { get; }
        /// <summary>Factions of the extra human co-op players (online); empty offline.</summary>
        public IReadOnlyList<FactionType> CoopHumanFactions { get; }

        public SurvivalSettings(FactionType playerFaction, FactionType? allyFaction,
            FactionType enemyFaction, MapSize mapSize, EnemyAiDifficulty difficulty, bool superWeapons)
            : this(playerFaction, allyFaction, enemyFaction, mapSize, difficulty, superWeapons, System.Array.Empty<FactionType>())
        {
        }

        public SurvivalSettings(FactionType playerFaction, FactionType? allyFaction,
            FactionType enemyFaction, MapSize mapSize, EnemyAiDifficulty difficulty, bool superWeapons,
            IReadOnlyList<FactionType> coopHumanFactions)
        {
            CoopHumanFactions = coopHumanFactions;
            PlayerFaction = playerFaction;
            AllyFaction = allyFaction;
            EnemyFaction = enemyFaction;
            MapSize = mapSize;
            Difficulty = difficulty;
            SuperWeapons = superWeapons;
        }
    }
}
