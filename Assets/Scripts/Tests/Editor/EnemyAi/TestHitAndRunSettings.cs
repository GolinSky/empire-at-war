using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Hit-and-run thresholds a test chooses; the same for every difficulty.</summary>
    internal sealed class TestHitAndRunSettings : IEnemyHitAndRunSettings
    {
        /// <summary>No fight counts as a bad straight fight and no fleet loss as an emergency: the tactic never starts.</summary>
        public static readonly EnemyHitAndRunTuning OFF = Create(straightFightAdvantage: 0f, fleetEmergencySeconds: 0f);

        private readonly EnemyHitAndRunTuning _tuning;

        public TestHitAndRunSettings(EnemyHitAndRunTuning tuning)
        {
            _tuning = tuning;
        }

        public EnemyHitAndRunTuning Get(EnemyAiDifficulty difficulty) => _tuning;

        public static EnemyHitAndRunTuning Create(float straightFightAdvantage = 1.2f,
            float pullBackShieldFraction = 0.25f, float resumeShieldFraction = 0.85f, float resumeHullFraction = 0.8f,
            float minPhaseSeconds = 5f, float maxRecoverSeconds = 60f, float reactionDelaySeconds = 0f,
            float fleetEmergencySeconds = 60f) =>
            new EnemyHitAndRunTuning(straightFightAdvantage, pullBackShieldFraction, resumeShieldFraction,
                resumeHullFraction, minPhaseSeconds, maxRecoverSeconds, reactionDelaySeconds, fleetEmergencySeconds);
    }
}
