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

        public SurvivalSettings(FactionType playerFaction, FactionType? allyFaction,
            FactionType enemyFaction, MapSize mapSize, EnemyAiDifficulty difficulty, bool superWeapons)
        {
            PlayerFaction = playerFaction;
            AllyFaction = allyFaction;
            EnemyFaction = enemyFaction;
            MapSize = mapSize;
            Difficulty = difficulty;
            SuperWeapons = superWeapons;
        }
    }
}
