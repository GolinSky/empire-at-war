using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Entities.Game
{
    public interface IGameModelObserver : IModelObserver
    {
        PlanetType PlanetType { get; }
        FactionType PlayerFactionType { get; }
        FactionType EnemyFactionType { get; }
        BattleVictoryCondition VictoryCondition { get; }
        EnemyAiDifficulty EnemyDifficulty { get; }
        float StartingMoney { get; }
    }

    [CreateAssetMenu(fileName = "GameModel", menuName = "Model/GameModel")]
    public class GameModel : Model, IGameModelObserver
    {
        public GameMode GameMode { get; set; }
        public PlanetType PlanetType { get; set; }
        public FactionType PlayerFactionType { get; set; }
        public FactionType EnemyFactionType { get; set; } = FactionType.Separatist;
        public BattleVictoryCondition VictoryCondition { get; set; } = BattleVictoryCondition.DestroyEnemyFleet;
        public EnemyAiDifficulty EnemyDifficulty { get; set; } = EnemyAiDifficulty.Medium;
        public float StartingMoney { get; set; } = 1000f;
    }
}
