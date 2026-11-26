namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>Hit-and-run thresholds of one difficulty (<see cref="IEnemyHitAndRunSettings"/>).</summary>
    public sealed class EnemyHitAndRunTuning
    {
        /// <summary>Group advantage against the target and its support below which a straight fight is bad.</summary>
        public float StraightFightAdvantage { get; }

        /// <summary>Average striker shield fraction below which the strike group pulls back.</summary>
        public float PullBackShieldFraction { get; }

        /// <summary>Average striker shield fraction at which the strike group returns.</summary>
        public float ResumeShieldFraction { get; }

        /// <summary>Average striker hull fraction waited for when hull repair covers the safe point.</summary>
        public float ResumeHullFraction { get; }

        /// <summary>A strike or a recovery lasts at least this long, so the cycle does not flip-flop.</summary>
        public float MinPhaseSeconds { get; }

        /// <summary>A pull-back or a recovery ends after this long, whatever the shields read.</summary>
        public float MaxRecoverSeconds { get; }

        /// <summary>The pull-back condition must hold this long before the strike group reacts.</summary>
        public float ReactionDelaySeconds { get; }

        /// <summary>
        /// Hostiles destroy the ships at a fight faster than this with no station pressure: production buys what
        /// arrives in time, as for a critical station.
        /// </summary>
        public float FleetEmergencySeconds { get; }

        public EnemyHitAndRunTuning(
            float straightFightAdvantage,
            float pullBackShieldFraction,
            float resumeShieldFraction,
            float resumeHullFraction,
            float minPhaseSeconds,
            float maxRecoverSeconds,
            float reactionDelaySeconds,
            float fleetEmergencySeconds)
        {
            StraightFightAdvantage = straightFightAdvantage;
            PullBackShieldFraction = pullBackShieldFraction;
            ResumeShieldFraction = resumeShieldFraction;
            ResumeHullFraction = resumeHullFraction;
            MinPhaseSeconds = minPhaseSeconds;
            MaxRecoverSeconds = maxRecoverSeconds;
            ReactionDelaySeconds = reactionDelaySeconds;
            FleetEmergencySeconds = fleetEmergencySeconds;
        }
    }
}
