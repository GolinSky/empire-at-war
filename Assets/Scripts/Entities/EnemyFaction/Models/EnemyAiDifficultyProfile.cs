using System;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public sealed class EnemyAiDifficultyProfile
    {
        private static readonly EnemyAiDifficultyProfile _easy =
            new EnemyAiDifficultyProfile(4f, 1.6f, 0.5f, 0.35f, 2, 1, 1, 0.75f);
        private static readonly EnemyAiDifficultyProfile _medium =
            new EnemyAiDifficultyProfile(2.5f, 1.25f, 0.65f, 0.25f, 3, 1, 1, 1f);
        private static readonly EnemyAiDifficultyProfile _hard =
            new EnemyAiDifficultyProfile(1.25f, 1f, 0.8f, 0.18f, 4, 2, 2, 1.25f);
        private static readonly EnemyAiDifficultyProfile _ultraHard =
            new EnemyAiDifficultyProfile(0.5f, 0.75f, 1f, 0.1f, 6, 3, 2, 1.5f);

        public EnemyAiDifficultyProfile(
            float decisionInterval,
            float requiredAttackRatio,
            float committedFleetRatio,
            float retreatShieldThreshold,
            int outnumberedRetreatCount,
            int minimumMiningFacilities,
            int minimumControlledZones,
            float defenseThreatRatio)
        {
            DecisionInterval = decisionInterval;
            RequiredAttackRatio = requiredAttackRatio;
            CommittedFleetRatio = committedFleetRatio;
            RetreatShieldThreshold = retreatShieldThreshold;
            OutnumberedRetreatCount = outnumberedRetreatCount;
            MinimumMiningFacilities = minimumMiningFacilities;
            MinimumControlledZones = minimumControlledZones;
            DefenseThreatRatio = defenseThreatRatio;
        }

        public float DecisionInterval { get; }
        public float RequiredAttackRatio { get; }
        public float CommittedFleetRatio { get; }
        public float RetreatShieldThreshold { get; }
        public int OutnumberedRetreatCount { get; }
        public int MinimumMiningFacilities { get; }
        public int MinimumControlledZones { get; }
        public float DefenseThreatRatio { get; }

        public static EnemyAiDifficultyProfile Get(EnemyAiDifficulty difficulty)
        {
            return difficulty switch
            {
                EnemyAiDifficulty.Easy => _easy,
                EnemyAiDifficulty.Medium => _medium,
                EnemyAiDifficulty.Hard => _hard,
                EnemyAiDifficulty.UltraHard => _ultraHard,
                _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
            };
        }
    }
}
