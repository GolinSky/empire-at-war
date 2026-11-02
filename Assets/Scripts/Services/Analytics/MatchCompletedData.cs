using System.Collections.Generic;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Analytics
{
    public sealed class MatchCompletedData
    {
        public string MatchId { get; }
        public GameMode Mode { get; }
        public BattleOutcome Outcome { get; }
        public BattleVictoryCondition VictoryCondition { get; }
        public FactionType PlayerFaction { get; }
        public IReadOnlyList<FactionType> OpponentFactions { get; }
        public float DurationSeconds { get; }
        public int PlayerShipsLeft { get; }
        public int EnemyShipsLeft { get; }
        public bool IsPlayerBaseAlive { get; }
        public bool IsEnemyBaseAlive { get; }
        public int UnitsDestroyed { get; }
        public int UnitsLost { get; }

        public MatchCompletedData(string matchId, GameMode mode, BattleOutcome outcome,
            BattleVictoryCondition victoryCondition, FactionType playerFaction,
            IReadOnlyList<FactionType> opponentFactions, float durationSeconds, int playerShipsLeft,
            int enemyShipsLeft, bool isPlayerBaseAlive, bool isEnemyBaseAlive, int unitsDestroyed, int unitsLost)
        {
            MatchId = matchId;
            Mode = mode;
            Outcome = outcome;
            VictoryCondition = victoryCondition;
            PlayerFaction = playerFaction;
            OpponentFactions = opponentFactions;
            DurationSeconds = durationSeconds;
            PlayerShipsLeft = playerShipsLeft;
            EnemyShipsLeft = enemyShipsLeft;
            IsPlayerBaseAlive = isPlayerBaseAlive;
            IsEnemyBaseAlive = isEnemyBaseAlive;
            UnitsDestroyed = unitsDestroyed;
            UnitsLost = unitsLost;
        }
    }
}
