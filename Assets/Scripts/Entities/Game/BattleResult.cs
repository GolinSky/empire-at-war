using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.Game
{
    public sealed class BattleResult
    {
        public BattleResult(
            BattleOutcome outcome,
            BattleVictoryCondition victoryCondition,
            PlanetType planet,
            FactionType playerFaction,
            FactionType enemyFaction,
            int playerShipCount,
            int enemyShipCount,
            bool isPlayerBaseAlive,
            bool isEnemyBaseAlive)
        {
            Outcome = outcome;
            VictoryCondition = victoryCondition;
            Planet = planet;
            PlayerFaction = playerFaction;
            EnemyFaction = enemyFaction;
            PlayerShipCount = playerShipCount;
            EnemyShipCount = enemyShipCount;
            IsPlayerBaseAlive = isPlayerBaseAlive;
            IsEnemyBaseAlive = isEnemyBaseAlive;
        }

        public BattleOutcome Outcome { get; }
        public BattleVictoryCondition VictoryCondition { get; }
        public PlanetType Planet { get; }
        public FactionType PlayerFaction { get; }
        public FactionType EnemyFaction { get; }
        public int PlayerShipCount { get; }
        public int EnemyShipCount { get; }
        public bool IsPlayerBaseAlive { get; }
        public bool IsEnemyBaseAlive { get; }
    }
}
