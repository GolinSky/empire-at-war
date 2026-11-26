using System;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Hit-and-run thresholds of the enemy AI, one entry per difficulty.</summary>
    [CreateAssetMenu(fileName = nameof(EnemyHitAndRunSettings), menuName = "Data/EnemyHitAndRunSettings")]
    public sealed class EnemyHitAndRunSettings : Data, IEnemyHitAndRunSettings
    {
        [SerializeField] private Entry easy = new Entry(1f, 0.1f, 0.7f, 0.6f, 8f, 75f, 4f, 30f);
        [SerializeField] private Entry medium = new Entry(1.15f, 0.2f, 0.8f, 0.7f, 6f, 60f, 2f, 45f);
        [SerializeField] private Entry hard = new Entry(1.25f, 0.25f, 0.85f, 0.8f, 6f, 60f, 1f, 60f);
        [SerializeField] private Entry ultraHard = new Entry(1.3f, 0.3f, 0.9f, 0.9f, 6f, 60f, 0f, 75f);

        public EnemyHitAndRunTuning Get(EnemyAiDifficulty difficulty) => difficulty switch
        {
            EnemyAiDifficulty.Easy => easy.ToTuning(),
            EnemyAiDifficulty.Medium => medium.ToTuning(),
            EnemyAiDifficulty.Hard => hard.ToTuning(),
            EnemyAiDifficulty.UltraHard => ultraHard.ToTuning(),
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
        };

        [Serializable]
        private sealed class Entry
        {
            [Tooltip("Group strength against the target and its support below which the AI uses hit-and-run; 1 is an even fight.")]
            [SerializeField, Min(0f)] private float straightFightAdvantage;
            [Tooltip("Average striker shield share below which the strike group pulls back.")]
            [SerializeField, Range(0f, 1f)] private float pullBackShieldFraction;
            [Tooltip("Average striker shield share at which the strike group returns. Keep above the pull-back share.")]
            [SerializeField, Range(0f, 1f)] private float resumeShieldFraction;
            [Tooltip("Average striker hull share waited for when Field Repairs or a repairer covers the safe point.")]
            [SerializeField, Range(0f, 1f)] private float resumeHullFraction;
            [Tooltip("Shortest strike or recovery in seconds.")]
            [SerializeField, Min(0f)] private float minPhaseSeconds;
            [Tooltip("Longest pull-back or recovery in seconds.")]
            [SerializeField, Min(0f)] private float maxRecoverSeconds;
            [Tooltip("Seconds the shields must stay low before the strike group pulls back.")]
            [SerializeField, Min(0f)] private float reactionDelaySeconds;
            [Tooltip("The ships at a fight die faster than this many seconds: production buys what arrives in time.")]
            [SerializeField, Min(0f)] private float fleetEmergencySeconds;

            public Entry(float straightFightAdvantage, float pullBackShieldFraction, float resumeShieldFraction,
                float resumeHullFraction, float minPhaseSeconds, float maxRecoverSeconds, float reactionDelaySeconds,
                float fleetEmergencySeconds)
            {
                this.straightFightAdvantage = straightFightAdvantage;
                this.pullBackShieldFraction = pullBackShieldFraction;
                this.resumeShieldFraction = resumeShieldFraction;
                this.resumeHullFraction = resumeHullFraction;
                this.minPhaseSeconds = minPhaseSeconds;
                this.maxRecoverSeconds = maxRecoverSeconds;
                this.reactionDelaySeconds = reactionDelaySeconds;
                this.fleetEmergencySeconds = fleetEmergencySeconds;
            }

            public EnemyHitAndRunTuning ToTuning() => new EnemyHitAndRunTuning(straightFightAdvantage,
                pullBackShieldFraction, resumeShieldFraction, resumeHullFraction, minPhaseSeconds, maxRecoverSeconds,
                reactionDelaySeconds, fleetEmergencySeconds);
        }
    }
}
