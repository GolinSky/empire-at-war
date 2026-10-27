using System;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    /// <summary>
    /// Per-difficulty AI tuning. Advantage and ratio thresholds compare fleet strength derived from the damage matrix,
    /// where 1 is an even fight (see <see cref="Combat.CombatMatchup.Advantage"/>). Harder AIs decide faster and
    /// commit more, but never attack at a disadvantage: taking losing fights only throws the fleet away.
    /// </summary>
    public sealed class EnemyAiDifficultyProfile
    {
        private static readonly EnemyAiDifficultyProfile _easy =
            new EnemyAiDifficultyProfile(decisionInterval: 4f, requiredAttackRatio: 1.6f, huntAdvantage: 1.1f, retreatAdvantage: 0.8f, committedFleetRatio: 0.5f, retreatShieldThreshold: 0.35f, minimumMiningFacilities: 1, minimumControlledZones: 1, shipOrdersPerLevel: 3, defenseThreatRatio: 0.75f, abilityDecisionInterval: 3f, abilityUseChance: 0.35f, abilityTargetPrecision: 0.2f, superWeaponMinFireDelay: 25f, superWeaponMaxFireDelay: 45f, superWeaponTargetChoices: 4);
        private static readonly EnemyAiDifficultyProfile _medium =
            new EnemyAiDifficultyProfile(decisionInterval: 2.5f, requiredAttackRatio: 1.25f, huntAdvantage: 1f, retreatAdvantage: 0.7f, committedFleetRatio: 0.65f, retreatShieldThreshold: 0.25f, minimumMiningFacilities: 1, minimumControlledZones: 1, shipOrdersPerLevel: 2, defenseThreatRatio: 1f, abilityDecisionInterval: 2f, abilityUseChance: 0.6f, abilityTargetPrecision: 0.5f, superWeaponMinFireDelay: 15f, superWeaponMaxFireDelay: 30f, superWeaponTargetChoices: 3);
        private static readonly EnemyAiDifficultyProfile _hard =
            new EnemyAiDifficultyProfile(decisionInterval: 1.25f, requiredAttackRatio: 1.15f, huntAdvantage: 1f, retreatAdvantage: 0.7f, committedFleetRatio: 0.8f, retreatShieldThreshold: 0.18f, minimumMiningFacilities: 2, minimumControlledZones: 2, shipOrdersPerLevel: 2, defenseThreatRatio: 1.25f, abilityDecisionInterval: 1f, abilityUseChance: 0.85f, abilityTargetPrecision: 0.8f, superWeaponMinFireDelay: 8f, superWeaponMaxFireDelay: 18f, superWeaponTargetChoices: 2);
        private static readonly EnemyAiDifficultyProfile _ultraHard =
            new EnemyAiDifficultyProfile(decisionInterval: 0.5f, requiredAttackRatio: 1.1f, huntAdvantage: 1f, retreatAdvantage: 0.8f, committedFleetRatio: 1f, retreatShieldThreshold: 0.1f, minimumMiningFacilities: 3, minimumControlledZones: 2, shipOrdersPerLevel: 1, defenseThreatRatio: 1.5f, abilityDecisionInterval: 0.5f, abilityUseChance: 1f, abilityTargetPrecision: 1f, superWeaponMinFireDelay: 4f, superWeaponMaxFireDelay: 10f, superWeaponTargetChoices: 1);

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

        /// <summary>Seconds the AI waits after a superweapon becomes fireable, rolled between these bounds.</summary>
        public float SuperWeaponMinFireDelay { get; }
        public float SuperWeaponMaxFireDelay { get; }

        /// <summary>How many of the best-scoring superweapon targets the AI picks from at random; 1 always takes the best.</summary>
        public int SuperWeaponTargetChoices { get; }

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
            float superWeaponMinFireDelay,
            float superWeaponMaxFireDelay,
            int superWeaponTargetChoices,
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
            SuperWeaponMinFireDelay = superWeaponMinFireDelay;
            SuperWeaponMaxFireDelay = superWeaponMaxFireDelay;
            SuperWeaponTargetChoices = superWeaponTargetChoices;
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
