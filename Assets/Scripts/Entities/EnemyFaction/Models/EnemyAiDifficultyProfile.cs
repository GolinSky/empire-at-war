using System;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public sealed class EnemyAiDifficultyProfile
    {
        public EnemyAiDifficultyProfile(
            float decisionInterval,
            float requiredAttackRatio,
            float committedFleetRatio,
            float retreatShieldThreshold,
            int outnumberedRetreatCount)
        {
            DecisionInterval = decisionInterval;
            RequiredAttackRatio = requiredAttackRatio;
            CommittedFleetRatio = committedFleetRatio;
            RetreatShieldThreshold = retreatShieldThreshold;
            OutnumberedRetreatCount = outnumberedRetreatCount;
        }

        public float DecisionInterval { get; }
        public float RequiredAttackRatio { get; }
        public float CommittedFleetRatio { get; }
        public float RetreatShieldThreshold { get; }
        public int OutnumberedRetreatCount { get; }

        public static EnemyAiDifficultyProfile Get(EnemyAiDifficulty difficulty)
        {
            return difficulty switch
            {
                EnemyAiDifficulty.Easy => new EnemyAiDifficultyProfile(4f, 1.6f, 0.5f, 0.35f, 2),
                EnemyAiDifficulty.Medium => new EnemyAiDifficultyProfile(2.5f, 1.25f, 0.65f, 0.25f, 3),
                EnemyAiDifficulty.Hard => new EnemyAiDifficultyProfile(1.25f, 1f, 0.8f, 0.18f, 4),
                EnemyAiDifficulty.UltraHard => new EnemyAiDifficultyProfile(0.5f, 0.75f, 1f, 0.1f, 6),
                _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
            };
        }
    }
}
