using System;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    /// <summary>
    /// Per-difficulty AI tuning. Advantage and ratio thresholds compare fleet strength derived from the damage matrix,
    /// where 1 is an even fight (see <see cref="Combat.CombatMatchup.Advantage"/>).
    /// </summary>
    public sealed class EnemyAiDifficultyProfile
    {
        private static readonly EnemyAiDifficultyProfile _easy =
            new EnemyAiDifficultyProfile(decisionInterval: 4f, requiredAttackRatio: 1.6f, huntAdvantage: 1.1f, retreatAdvantage: 0.8f, committedFleetRatio: 0.5f, retreatShieldThreshold: 0.35f, minimumMiningFacilities: 1, minimumControlledZones: 1, shipOrdersPerLevel: 3, defenseThreatRatio: 0.75f, abilityDecisionInterval: 3f, abilityUseChance: 0.35f, abilityTargetPrecision: 0.2f);
        private static readonly EnemyAiDifficultyProfile _medium =
            new EnemyAiDifficultyProfile(decisionInterval: 2.5f, requiredAttackRatio: 1.25f, huntAdvantage: 1f, retreatAdvantage: 0.7f, committedFleetRatio: 0.65f, retreatShieldThreshold: 0.25f, minimumMiningFacilities: 1, minimumControlledZones: 1, shipOrdersPerLevel: 2, defenseThreatRatio: 1f, abilityDecisionInterval: 2f, abilityUseChance: 0.6f, abilityTargetPrecision: 0.5f);
        private static readonly EnemyAiDifficultyProfile _hard =
            new EnemyAiDifficultyProfile(decisionInterval: 1.25f, requiredAttackRatio: 1f, huntAdvantage: 0.9f, retreatAdvantage: 0.6f, committedFleetRatio: 0.8f, retreatShieldThreshold: 0.18f, minimumMiningFacilities: 2, minimumControlledZones: 2, shipOrdersPerLevel: 2, defenseThreatRatio: 1.25f, abilityDecisionInterval: 1f, abilityUseChance: 0.85f, abilityTargetPrecision: 0.8f);
        private static readonly EnemyAiDifficultyProfile _ultraHard =
            new EnemyAiDifficultyProfile(decisionInterval: 0.5f, requiredAttackRatio: 0.75f, huntAdvantage: 0.8f, retreatAdvantage: 0.5f, committedFleetRatio: 1f, retreatShieldThreshold: 0.1f, minimumMiningFacilities: 3, minimumControlledZones: 2, shipOrdersPerLevel: 1, defenseThreatRatio: 1.5f, abilityDecisionInterval: 0.5f, abilityUseChance: 1f, abilityTargetPrecision: 1f);

        public float DecisionInterval { get; }

        /// <summary>Fleet advantage needed to assault the enemy base.</summary>
        public float RequiredAttackRatio { get; }

        /// <summary>Fleet advantage needed to seek out the enemy fleet.</summary>
        public float HuntAdvantage { get; }

        /// <summary>Fleet advantage at or below which the fleet withdraws to its base.</summary>
        public float RetreatAdvantage { get; }
        public float CommittedFleetRatio { get; }
        public float RetreatShieldThreshold { get; }
        public int MinimumMiningFacilities { get; }
        public int MinimumControlledZones { get; }

        /// <summary>Ships ordered per station level before the next level-up is due; the AI saves for a due level.</summary>
        public int ShipOrdersPerLevel { get; }

        /// <summary>Strength of hostiles near the base, relative to the own fleet, that triggers a defense.</summary>
        public float DefenseThreatRatio { get; }
        public float AbilityDecisionInterval { get; }
        public float AbilityUseChance { get; }
        public float AbilityTargetPrecision { get; }

        public EnemyAiDifficultyProfile(
            float decisionInterval,
            float requiredAttackRatio,
            float huntAdvantage,
            float retreatAdvantage,
            float committedFleetRatio,
            float retreatShieldThreshold,
            float defenseThreatRatio,
            float abilityDecisionInterval,
            float abilityUseChance,
            float abilityTargetPrecision,
            int minimumMiningFacilities,
            int minimumControlledZones,
            int shipOrdersPerLevel)
        {
            ShipOrdersPerLevel = shipOrdersPerLevel;
            DecisionInterval = decisionInterval;
            RequiredAttackRatio = requiredAttackRatio;
            HuntAdvantage = huntAdvantage;
            RetreatAdvantage = retreatAdvantage;
            CommittedFleetRatio = committedFleetRatio;
            RetreatShieldThreshold = retreatShieldThreshold;
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
