using System;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public sealed class EnemyAiDifficultyProfile
    {
        private static readonly EnemyAiDifficultyProfile _easy =
            new EnemyAiDifficultyProfile(decisionInterval: 4f, requiredAttackRatio: 1.6f, committedFleetRatio: 0.5f, retreatShieldThreshold: 0.35f, outnumberedRetreatCount: 2, minimumMiningFacilities: 1, minimumControlledZones: 1, defenseThreatRatio: 0.75f, abilityDecisionInterval: 3f, abilityUseChance: 0.35f, abilityTargetPrecision: 0.2f);
        private static readonly EnemyAiDifficultyProfile _medium =
            new EnemyAiDifficultyProfile(decisionInterval: 2.5f, requiredAttackRatio: 1.25f, committedFleetRatio: 0.65f, retreatShieldThreshold: 0.25f, outnumberedRetreatCount: 3, minimumMiningFacilities: 1, minimumControlledZones: 1, defenseThreatRatio: 1f, abilityDecisionInterval: 2f, abilityUseChance: 0.6f, abilityTargetPrecision: 0.5f);
        private static readonly EnemyAiDifficultyProfile _hard =
            new EnemyAiDifficultyProfile(decisionInterval: 1.25f, requiredAttackRatio: 1f, committedFleetRatio: 0.8f, retreatShieldThreshold: 0.18f, outnumberedRetreatCount: 4, minimumMiningFacilities: 2, minimumControlledZones: 2, defenseThreatRatio: 1.25f, abilityDecisionInterval: 1f, abilityUseChance: 0.85f, abilityTargetPrecision: 0.8f);
        private static readonly EnemyAiDifficultyProfile _ultraHard =
            new EnemyAiDifficultyProfile(decisionInterval: 0.5f, requiredAttackRatio: 0.75f, committedFleetRatio: 1f, retreatShieldThreshold: 0.1f, outnumberedRetreatCount: 6, minimumMiningFacilities: 3, minimumControlledZones: 2, defenseThreatRatio: 1.5f, abilityDecisionInterval: 0.5f, abilityUseChance: 1f, abilityTargetPrecision: 1f);

        public float DecisionInterval { get; }
        public float RequiredAttackRatio { get; }
        public float CommittedFleetRatio { get; }
        public float RetreatShieldThreshold { get; }
        public int OutnumberedRetreatCount { get; }
        public int MinimumMiningFacilities { get; }
        public int MinimumControlledZones { get; }
        public float DefenseThreatRatio { get; }
        public float AbilityDecisionInterval { get; }
        public float AbilityUseChance { get; }
        public float AbilityTargetPrecision { get; }

        public EnemyAiDifficultyProfile(
            float decisionInterval,
            float requiredAttackRatio,
            float committedFleetRatio,
            float retreatShieldThreshold,
            float defenseThreatRatio,
            float abilityDecisionInterval,
            float abilityUseChance,
            float abilityTargetPrecision,
            int outnumberedRetreatCount,
            int minimumMiningFacilities,
            int minimumControlledZones)
        {
            DecisionInterval = decisionInterval;
            RequiredAttackRatio = requiredAttackRatio;
            CommittedFleetRatio = committedFleetRatio;
            RetreatShieldThreshold = retreatShieldThreshold;
            OutnumberedRetreatCount = outnumberedRetreatCount;
            MinimumMiningFacilities = minimumMiningFacilities;
            MinimumControlledZones = minimumControlledZones;
            DefenseThreatRatio = defenseThreatRatio;
            AbilityDecisionInterval = abilityDecisionInterval;
            AbilityUseChance = abilityUseChance;
            AbilityTargetPrecision = abilityTargetPrecision;
        }

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
