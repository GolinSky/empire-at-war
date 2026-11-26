using System;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>
    /// Hit-and-run cycle of one task group against a hostile big ship: strike → the strike group pulls back together
    /// → recover → strike again. The cycle runs beside the strategic state of the group, never instead of it: only
    /// the strike ships follow it. It is used when a straight fight is bad, one strike pays, the strikers can break
    /// contact to a safe point and the ships left at the fight survive the time the strikers are away.
    /// </summary>
    public sealed class EnemyStrikeCycleModel : Model
    {
        /// <summary>Field Repairs restores this share of the maximum hull per second.</summary>
        public const float FIELD_REPAIR_FRACTION = 0.01f;
        /// <summary>Field Repairs starts after this long with no weapon target and no damage.</summary>
        public const float FIELD_REPAIR_CALM_SECONDS = 10f;

        private readonly EnemyHitAndRunTuning _tuning;
        private float _phaseStartedAt;
        private float _awayStartedAt;
        private float _lowShieldsSince = float.NaN;

        public EnemyStrikePhase Phase { get; private set; } = EnemyStrikePhase.Idle;

        /// <summary>Why the cycle is in its phase, or why it is not used (telemetry).</summary>
        public string Reason { get; private set; } = "none";

        public bool IsActive => Phase != EnemyStrikePhase.Idle;

        public EnemyStrikeCycleModel(EnemyHitAndRunTuning tuning)
        {
            _tuning = tuning;
        }

        public EnemyStrikePhase Evaluate(in EnemyStrikeCycleInput input)
        {
            string failure = GetFailure(input);
            if (failure != null) return Set(EnemyStrikePhase.Idle, failure, input.Now);
            bool straightFightBad = input.GroupAdvantage < _tuning.StraightFightAdvantage;
            bool leftBehindHolds = LeftBehindHolds(input);
            float held = input.Now - _phaseStartedAt;
            switch (Phase)
            {
                case EnemyStrikePhase.Idle:
                    if (!straightFightBad) Reason = "groupWins";
                    else if (!leftBehindHolds) Reason = "leftBehind";
                    else Set(EnemyStrikePhase.Strike, "start", input.Now);
                    break;
                case EnemyStrikePhase.Strike:
                    if (input.AverageShieldFraction >= _tuning.PullBackShieldFraction)
                    {
                        _lowShieldsSince = float.NaN;
                        break;
                    }
                    if (float.IsNaN(_lowShieldsSince)) _lowShieldsSince = input.Now;
                    if (input.Now - _lowShieldsSince < _tuning.ReactionDelaySeconds || held < _tuning.MinPhaseSeconds) break;
                    // The strikers stay when the ships left behind would not survive their absence.
                    if (!leftBehindHolds) Set(EnemyStrikePhase.Idle, "leftBehind", input.Now);
                    else Set(EnemyStrikePhase.PullBack, "lowShields", input.Now);
                    break;
                case EnemyStrikePhase.PullBack:
                    if (!leftBehindHolds) Set(EnemyStrikePhase.Idle, "leftBehind", input.Now);
                    else if (input.IsChased) Set(EnemyStrikePhase.Strike, "chased", input.Now);
                    else if (input.HaveStrikersArrived) Set(EnemyStrikePhase.Recover, "arrived", input.Now);
                    else if (held >= _tuning.MaxRecoverSeconds) Set(EnemyStrikePhase.Strike, "pullBackTimeout", input.Now);
                    break;
                case EnemyStrikePhase.Recover:
                    if (!leftBehindHolds) Set(EnemyStrikePhase.Idle, "leftBehind", input.Now);
                    else if (input.IsChased) Set(EnemyStrikePhase.Strike, "chased", input.Now);
                    else if (held >= _tuning.MaxRecoverSeconds || held >= _tuning.MinPhaseSeconds && IsRecovered(input))
                    {
                        if (straightFightBad)
                            Set(EnemyStrikePhase.Strike,
                                held >= _tuning.MaxRecoverSeconds ? "recoverTimeout" : "recovered", input.Now);
                        else Set(EnemyStrikePhase.Idle, "groupWins", input.Now);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Phase));
            }
            return Phase;
        }

        /// <summary>Ends the cycle from outside: the group left its fight, answers a request or gathers.</summary>
        public void Stop(string reason, float now) => Set(EnemyStrikePhase.Idle, reason, now);

        /// <summary>
        /// One strike removes more hull than the target regains while the strikers are away. The strike is rated in
        /// its steady state: strikers start at the resume line, the target with the shields it regained meanwhile.
        /// </summary>
        public bool Pays(in EnemyStrikeCycleInput input)
        {
            float usableShields = (_tuning.ResumeShieldFraction - _tuning.PullBackShieldFraction) * input.StrikerMaxShields;
            float netDamageTaken = input.HostileShieldDps - input.StrikerShieldRegen;
            float strikeSeconds = netDamageTaken <= 0f
                ? CombatMatchup.MAX_TIME : Math.Min(CombatMatchup.MAX_TIME, usableShields / netDamageTaken);
            float awaySeconds = GetAwaySeconds(input);
            float targetShields = Math.Min(input.TargetMaxShields,
                Math.Max(input.TargetShields, input.TargetShieldRegen * awaySeconds));
            float netBreak = input.StrikerShieldDps - input.TargetShieldRegen;
            float breakSeconds = targetShields <= 0f ? 0f : netBreak <= 0f ? CombatMatchup.MAX_TIME : targetShields / netBreak;
            if (strikeSeconds <= breakSeconds) return false;
            float hullDamage = input.StrikerPiercingDps * breakSeconds +
                (input.StrikerHullDps + input.StrikerPiercingDps) * (strikeSeconds - breakSeconds);
            float healed = input.HostileRepairPerSecond * awaySeconds;
            // Ships left at the fight keep the target under fire, so its Field Repairs never starts.
            if (input.TargetSelfRepairs && input.NonStrikerCount == 0)
                healed += FIELD_REPAIR_FRACTION * input.TargetMaxHull *
                    Math.Max(0f, awaySeconds - FIELD_REPAIR_CALM_SECONDS);
            return hullDamage > healed;
        }

        /// <summary>Travel out, recovery from the pull-back line to the resume line, travel back.</summary>
        public float GetAwaySeconds(in EnemyStrikeCycleInput input)
        {
            float recover = input.StrikerShieldRegen > 0f
                ? Math.Min(_tuning.MaxRecoverSeconds, (_tuning.ResumeShieldFraction - _tuning.PullBackShieldFraction) *
                    input.StrikerMaxShields / input.StrikerShieldRegen)
                : _tuning.MaxRecoverSeconds;
            return 2f * input.TravelSeconds + recover;
        }

        /// <summary>The strikers are faster than the target, or the station cover is closer than the target shoots.</summary>
        public static bool CanBreakContact(in EnemyStrikeCycleInput input) =>
            input.HasSafePoint && (input.SlowestStrikerSpeed > input.TargetSpeed ||
                input.SafePointIsStationCover && input.SafePointDistance < input.TargetWeaponRange);

        // Null when the tactic can run; else the first condition that fails.
        private string GetFailure(in EnemyStrikeCycleInput input)
        {
            if (!input.IsInFight) return "noFight";
            if (!input.HasTarget) return "noTarget";
            if (input.StrikerCount < EnemyStrikeGroupRules.MINIMUM_STRIKERS) return "fewStrikers";
            if (input.RegeneratingStrikerCount == 0) return "noShieldRegen";
            if (!input.HasSafePoint) return "noSafePoint";
            if (!CanBreakContact(input)) return "cannotBreakContact";
            if (input.TargetUnderUnbeatableDefenses) return "targetCovered";
            return Pays(input) ? null : "strikeDoesNotPay";
        }

        private bool LeftBehindHolds(in EnemyStrikeCycleInput input)
        {
            if (input.NonStrikerCount == 0) return true;
            float away = GetAwaySeconds(input);
            // While the strikers are away only the rest of their absence counts, never less than the way back.
            if (Phase is EnemyStrikePhase.PullBack or EnemyStrikePhase.Recover)
                away = Math.Max(input.TravelSeconds, away - (input.Now - _awayStartedAt));
            return input.LeftBehindSurvivalSeconds > away;
        }

        // Station under attack, or no hull repair at the safe point: shields only.
        private bool IsRecovered(in EnemyStrikeCycleInput input)
        {
            if (input.AverageShieldFraction < _tuning.ResumeShieldFraction) return false;
            bool waitsForHull = !input.HasStationPressure && input.IsHullRepairAvailable;
            return !waitsForHull || input.AverageHullFraction >= _tuning.ResumeHullFraction;
        }

        private EnemyStrikePhase Set(EnemyStrikePhase phase, string reason, float now)
        {
            Reason = reason;
            if (phase == Phase) return Phase;
            if (phase == EnemyStrikePhase.PullBack) _awayStartedAt = now;
            Phase = phase;
            _phaseStartedAt = now;
            _lowShieldsSince = float.NaN;
            return Phase;
        }
    }
}
