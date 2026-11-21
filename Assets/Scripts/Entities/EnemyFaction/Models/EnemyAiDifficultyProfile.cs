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
            new EnemyAiDifficultyProfile(decisionInterval: 4f, requiredAttackRatio: 1.6f, huntAdvantage: 1.1f, retreatAdvantage: 0.8f, committedFleetRatio: 0.5f, defenseThreatRatio: 1.5f, superWeaponMinFireDelay: 25f, superWeaponMaxFireDelay: 45f, superWeaponTargetChoices: 4);
        private static readonly EnemyAiDifficultyProfile _medium =
            new EnemyAiDifficultyProfile(decisionInterval: 2.5f, requiredAttackRatio: 1.25f, huntAdvantage: 1f, retreatAdvantage: 0.7f, committedFleetRatio: 0.65f, defenseThreatRatio: 1.25f, superWeaponMinFireDelay: 15f, superWeaponMaxFireDelay: 30f, superWeaponTargetChoices: 3);
        private static readonly EnemyAiDifficultyProfile _hard =
            new EnemyAiDifficultyProfile(decisionInterval: 1.25f, requiredAttackRatio: 1.15f, huntAdvantage: 1f, retreatAdvantage: 0.7f, committedFleetRatio: 0.8f, defenseThreatRatio: 1f, superWeaponMinFireDelay: 8f, superWeaponMaxFireDelay: 18f, superWeaponTargetChoices: 2);
        private static readonly EnemyAiDifficultyProfile _ultraHard =
            new EnemyAiDifficultyProfile(decisionInterval: 0.5f, requiredAttackRatio: 1.1f, huntAdvantage: 1f, retreatAdvantage: 0.8f, committedFleetRatio: 0.85f, defenseThreatRatio: 0.75f, superWeaponMinFireDelay: 4f, superWeaponMaxFireDelay: 10f, superWeaponTargetChoices: 1);

        public float DecisionInterval { get; }

        /// <summary>Fleet advantage needed to assault the enemy base.</summary>
        public float RequiredAttackRatio { get; }

        /// <summary>Fleet advantage needed to seek out the enemy fleet.</summary>
        public float HuntAdvantage { get; }

        /// <summary>Local strength threshold for standing and fighting, and a safety margin for route and recovery checks.</summary>
        public float RetreatAdvantage { get; }
        public float CommittedFleetRatio { get; }

        /// <summary>Strength of hostiles near the base, relative to the own fleet, that triggers a defense.</summary>
        public float DefenseThreatRatio { get; }

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
            float defenseThreatRatio,
            float superWeaponMinFireDelay,
            float superWeaponMaxFireDelay,
            int superWeaponTargetChoices)
        {
            DecisionInterval = decisionInterval;
            RequiredAttackRatio = requiredAttackRatio;
            HuntAdvantage = huntAdvantage;
            RetreatAdvantage = retreatAdvantage;
            CommittedFleetRatio = committedFleetRatio;
            DefenseThreatRatio = defenseThreatRatio;
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
