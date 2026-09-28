using System.Collections.Generic;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.Game
{
    /// <summary>Battle summary from the local player's side: "player" means the local team, "enemy" every hostile team.</summary>
    public sealed class BattleResult
    {
        public BattleResult(
            BattleOutcome outcome,
            BattleVictoryCondition victoryCondition,
            PlanetType planet,
            FactionType playerFaction,
            IReadOnlyList<FactionType> enemyFactions,
            int playerShipCount,
            int enemyShipCount,
            bool isPlayerBaseAlive,
            bool isEnemyBaseAlive)
        {
            Outcome = outcome;
            VictoryCondition = victoryCondition;
            Planet = planet;
            PlayerFaction = playerFaction;
            EnemyFactions = enemyFactions;
            PlayerShipCount = playerShipCount;
            EnemyShipCount = enemyShipCount;
            IsPlayerBaseAlive = isPlayerBaseAlive;
            IsEnemyBaseAlive = isEnemyBaseAlive;
        }

        public BattleOutcome Outcome { get; }
        public BattleVictoryCondition VictoryCondition { get; }
        public PlanetType Planet { get; }
        public FactionType PlayerFaction { get; }
        /// <summary>Distinct factions of all hostile players.</summary>
        public IReadOnlyList<FactionType> EnemyFactions { get; }
        public int PlayerShipCount { get; }
        public int EnemyShipCount { get; }
        public bool IsPlayerBaseAlive { get; }
        public bool IsEnemyBaseAlive { get; }
    }
}
