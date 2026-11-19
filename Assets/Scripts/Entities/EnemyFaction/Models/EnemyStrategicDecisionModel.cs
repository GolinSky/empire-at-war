using System;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public enum EnemyStrategicState
    {
        RebuildFleet = 0,
        CaptureZone = 1,
        HuntFleet = 2,
        AssaultBase = 3,
        DefendBase = 4,
        Hold = 5,
        RetreatValue = 6,
        /// <summary>Attacks hostiles near the fleet.</summary>
        EngageLocal = 7,
        /// <summary>Attacks a known, undefended hostile structure when nothing better is available.</summary>
        RaidStructures = 8,
        GuardSite = 9
    }

    public readonly struct EnemyStrategicSnapshot
    {
        public BattleVictoryCondition VictoryCondition { get; }
        public EnemyAiDifficulty Difficulty { get; }
        public int OwnShipCount { get; }
        public int EnemyShipCount { get; }
        public bool HasCaptureTarget { get; }
        public bool HasEnemyBaseTarget { get; }
        public bool HasOwnBase { get; }
        public int OwnedCapturableZoneCount { get; }
        public bool HasThreatenedSite { get; }
        public bool HasStationPressure { get; }
        public bool HasSafeRetreat { get; }

        /// <summary>A known hostile ship or squadron is within engagement range of the fleet.</summary>
        public bool HasLocalThreat { get; }

        /// <summary>A visible hostile structure (not a base) has defenders this group can beat.</summary>
        public bool HasRaidTarget { get; }
        public bool HasEconomyRaidTarget { get; }
        /// <summary>Every own ship is within <see cref="FleetCohesion.RADIUS"/> of the fleet's anchor ship.</summary>
        public bool IsRegrouped { get; }

        /// <summary>Own fleet strength over the focused enemy team's fleet; 1 is an even fight.</summary>
        public float FleetAdvantage { get; }

        /// <summary>Strength of hostiles near the own base over the own fleet; 0 when none are near.</summary>
        public float BaseThreatRatio { get; }

        /// <summary>Own fleet strength over hostiles near the fleet; the clamp maximum when none are near.</summary>
        public float LocalAdvantage { get; }
        public float LocalRaw { get; }
        public float FleetRaw { get; }
        public bool RecoveryMode { get; }
        public bool HasGuardSite { get; }
        public bool IsCentreGuardGoal { get; }

        /// <summary>The station is losing hull fast enough that the whole fleet must defend it.</summary>
        public bool IsStationCritical { get; }

        public EnemyStrategicSnapshot(
            BattleVictoryCondition victoryCondition,
            EnemyAiDifficulty difficulty,
            int ownShipCount,
            int enemyShipCount,
            int ownedCapturableZoneCount,
            float fleetAdvantage,
            float baseThreatRatio,
            bool hasCaptureTarget,
            bool hasEnemyBaseTarget,
            bool hasOwnBase,
            bool hasThreatenedSite = false,
            float localAdvantage = Combat.CombatMatchup.MAX_ADVANTAGE,
            bool hasLocalThreat = false,
            bool hasRaidTarget = false,
            bool isRegrouped = true,
            bool hasStationPressure = false,
            bool hasSafeRetreat = true,
            bool hasEconomyRaidTarget = false,
            float localRaw = float.NaN, float fleetRaw = float.NaN, bool recoveryMode = false,
            bool isStationCritical = false, bool hasGuardSite = false, bool isCentreGuardGoal = false)
        {
            IsStationCritical = isStationCritical;
            VictoryCondition = victoryCondition;
            Difficulty = difficulty;
            OwnShipCount = ownShipCount;
            EnemyShipCount = enemyShipCount;
            HasCaptureTarget = hasCaptureTarget;
            HasEnemyBaseTarget = hasEnemyBaseTarget;
            HasOwnBase = hasOwnBase;
            OwnedCapturableZoneCount = ownedCapturableZoneCount;
            FleetAdvantage = fleetAdvantage;
            BaseThreatRatio = baseThreatRatio;
            LocalAdvantage = localAdvantage;
            LocalRaw = float.IsNaN(localRaw) ? localAdvantage : localRaw;
            FleetRaw = float.IsNaN(fleetRaw) ? fleetAdvantage : fleetRaw;
            RecoveryMode = recoveryMode;
            HasGuardSite = hasGuardSite;
            IsCentreGuardGoal = isCentreGuardGoal;
            HasThreatenedSite = hasThreatenedSite;
            HasLocalThreat = hasLocalThreat;
            HasRaidTarget = hasRaidTarget;
            HasEconomyRaidTarget = hasEconomyRaidTarget;
            IsRegrouped = isRegrouped;
            HasStationPressure = hasStationPressure;
            HasSafeRetreat = hasSafeRetreat;
        }
    }

    public readonly struct EnemyStrategicDecision
    {
        public EnemyStrategicState State { get; }
        public int CommittedShipCount { get; }
        public string Reason { get; }

        public EnemyStrategicDecision(string reason, EnemyStrategicState state, int committedShipCount)
        {
            State = state;
            CommittedShipCount = committedShipCount;
            Reason = reason;
        }
    }

    /// <summary>
    /// Scores every <see cref="EnemyStrategicRule"/> and picks the highest. Strength checks use
    /// <see cref="EnemyStrategicSnapshot.FleetAdvantage"/>, which comes from the damage matrix, not ship counts.
    /// </summary>
    public sealed class EnemyStrategicDecisionModel : Model
    {
        /// <summary>Bonus for the current state so near-equal options do not flip every decision.</summary>
        private const float HYSTERESIS = 0.05f;
        public const float MIN_STATE_SECONDS = 8f;
        private const float URGENT_LOCAL_DISADVANTAGE_SCALE = 0.8f;
        /// <summary>Fleet advantage, as a multiple of the attack ratio, at which attacking outranks capturing.</summary>
        public const float PRESS_ADVANTAGE_SCALE = 2f;
        /// <summary>Smallest fleet share sent to defend a base that is not yet critical.</summary>
        public const float MIN_DEFENSE_SHARE = 0.34f;
        /// <summary>Defenders sent per unit of attacker strength, so the detachment outmatches the raid.</summary>
        public const float DEFENSE_MARGIN = 1.5f;

        private static readonly EnemyStrategicRule[] _rules =
        {
            new EnemyStrategicRule(EnemyStrategicState.RebuildFleet, 1f,
                "No combat ships are available.",
                (s, p) => ResponseCurve.When(s.OwnShipCount == 0)),
            new EnemyStrategicRule(EnemyStrategicState.DefendBase, 0.9f,
                "Known attackers threaten the station; intercept the visible threat.",
                (s, p) => ResponseCurve.When(s.HasOwnBase) *
                          (s.HasStationPressure ? 1f :
                              ResponseCurve.AtLeast(s.BaseThreatRatio, p.DefenseThreatRatio))),
            // A clearly stronger fleet ends the game instead of trading ships over capture sites.
            new EnemyStrategicRule(EnemyStrategicState.AssaultBase, 0.75f,
                "The fleet clearly outmatches the enemy, so it presses the advantage against the base.",
                (s, p) => ResponseCurve.When(s.VictoryCondition == BattleVictoryCondition.DestroyOpponentBase &&
                                             s.HasEnemyBaseTarget) *
                          ResponseCurve.When(!s.IsCentreGuardGoal ||
                              s.FleetAdvantage >= p.RequiredAttackRatio * PRESS_ADVANTAGE_SCALE) *
                          ResponseCurve.AtLeast(s.FleetAdvantage, p.RequiredAttackRatio * PRESS_ADVANTAGE_SCALE)),
            new EnemyStrategicRule(EnemyStrategicState.HuntFleet, 0.75f,
                "The fleet clearly outmatches the enemy fleet, so it presses the advantage and hunts it.",
                (s, p) => ResponseCurve.When(s.VictoryCondition != BattleVictoryCondition.DestroyOpponentBase &&
                                             s.EnemyShipCount > 0) *
                          ResponseCurve.When(!s.IsCentreGuardGoal ||
                              s.FleetAdvantage >= p.RequiredAttackRatio * PRESS_ADVANTAGE_SCALE) *
                          ResponseCurve.AtLeast(s.FleetAdvantage, p.RequiredAttackRatio * PRESS_ADVANTAGE_SCALE)),
            new EnemyStrategicRule(EnemyStrategicState.CaptureZone, 0.7f,
                "Enemy ships are contesting an owned capture site.",
                (s, p) => ResponseCurve.When(s.HasThreatenedSite)),
            new EnemyStrategicRule(EnemyStrategicState.RaidStructures, 0.66f,
                "Destroy exposed hostile income before expanding to another objective.",
                (s, p) => ResponseCurve.When(s.HasRaidTarget && s.HasEconomyRaidTarget && !s.HasThreatenedSite && !s.IsCentreGuardGoal)),
            new EnemyStrategicRule(EnemyStrategicState.CaptureZone, 0.6f,
                "Expanding map control to the next closest relay or site.",
                (s, p) => ResponseCurve.When(s.HasCaptureTarget)),
            // Outmatched overall but not where the fleet is: fight what is in front of it instead of freezing.
            new EnemyStrategicRule(EnemyStrategicState.EngageLocal, 0.55f,
                "Hostiles near the fleet do not outmatch it, so the fleet engages them.",
                (s, p) => ResponseCurve.When(s.HasLocalThreat) *
                          ResponseCurve.AtLeast(s.LocalAdvantage, p.RetreatAdvantage)),
            new EnemyStrategicRule(EnemyStrategicState.AssaultBase, 0.5f,
                "The objective is base destruction and the fleet outmatches the defenders.",
                (s, p) => ResponseCurve.When(!s.IsCentreGuardGoal && s.VictoryCondition == BattleVictoryCondition.DestroyOpponentBase &&
                                             s.HasEnemyBaseTarget) *
                          ResponseCurve.AtLeast(s.FleetAdvantage, p.RequiredAttackRatio)),
            new EnemyStrategicRule(EnemyStrategicState.HuntFleet, 0.5f,
                "The objective is the enemy fleet and our team clearly outmatches it.",
                (s, p) => ResponseCurve.When(!s.IsCentreGuardGoal && s.VictoryCondition != BattleVictoryCondition.DestroyOpponentBase &&
                                             s.EnemyShipCount > 0) *
                          ResponseCurve.AtLeast(s.FleetAdvantage, p.RequiredAttackRatio)),
            new EnemyStrategicRule(EnemyStrategicState.HuntFleet, 0.3f,
                "Nothing is left to capture, so the fleet hunts the enemy.",
                (s, p) => ResponseCurve.When(!s.IsCentreGuardGoal && s.EnemyShipCount > 0) *
                          ResponseCurve.AtLeast(s.FleetAdvantage, p.HuntAdvantage)),
            // Both objectives need the base destroyed: destroy-all also waits for every station and building.
            new EnemyStrategicRule(EnemyStrategicState.AssaultBase, 0.2f,
                "No other target remains before the base objective.",
                (s, p) => ResponseCurve.When(!s.IsCentreGuardGoal && s.HasEnemyBaseTarget && s.EnemyShipCount == 0)),
            // Too weak to hunt and nothing to capture: hit structures the enemy left undefended.
            new EnemyStrategicRule(EnemyStrategicState.RaidStructures, 0.1f,
                "The enemy fleet is too strong, so the fleet raids an undefended structure.",
                (s, p) => ResponseCurve.When(s.HasRaidTarget && !s.IsCentreGuardGoal)),
            new EnemyStrategicRule(EnemyStrategicState.GuardSite, 0.08f,
                "Guard the centre or nearby allied mining site.",
                (s, p) => ResponseCurve.When(s.HasGuardSite)),
            new EnemyStrategicRule(EnemyStrategicState.Hold, 0.05f,
                "No valid strategic target is currently available.",
                (s, p) => 1f)
        };

        private EnemyStrategicState _lastState;

        private bool _hasLastState;
        private bool _hadBase;
        private float _stateStartedAt;
        private bool _holdEpisode;
        public float StateHeldSeconds { get; private set; }
        public bool SwitchHeld { get; private set; }
        public string Bypass { get; private set; } = "none";
        public EnemyStrategicState BlockedState { get; private set; }
        public float BestScore { get; private set; }
        public float RunnerUpScore { get; private set; }

        public EnemyStrategicDecision Evaluate(EnemyStrategicSnapshot snapshot, bool captureDetachment = false,
            bool allowOffense = true, float now = float.NaN, bool commit = true)
        {
            if (snapshot.OwnShipCount < 0)
                throw new ArgumentOutOfRangeException(nameof(snapshot.OwnShipCount));
            if (snapshot.EnemyShipCount < 0)
                throw new ArgumentOutOfRangeException(nameof(snapshot.EnemyShipCount));
            if (snapshot.OwnedCapturableZoneCount < 0)
                throw new ArgumentOutOfRangeException(nameof(snapshot.OwnedCapturableZoneCount));

            EnemyStrategicState previousState = _lastState;
            bool hadState = _hasLastState;
            bool hadBase = _hadBase;
            bool holdEpisode = _holdEpisode;
            float startedAt = _stateStartedAt;
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(snapshot.Difficulty);
            EnemyStrategicRule best = _rules[_rules.Length - 1];
            float bestScore = 0f;
            float runnerUp = 0f;
            SwitchHeld = false;
            StateHeldSeconds = _hasLastState && !float.IsNaN(now) ? Math.Max(0f, now - _stateStartedAt) : 0f;
            Bypass = snapshot.OwnShipCount == 0 ? "noShips" :
                _hasLastState && _hadBase && !snapshot.HasOwnBase ? "stationLost" : "none";
            foreach (EnemyStrategicRule rule in _rules)
            {
                if (!allowOffense && (rule.State == EnemyStrategicState.HuntFleet ||
                    rule.State == EnemyStrategicState.AssaultBase || rule.State == EnemyStrategicState.RaidStructures)) continue;
                if (captureDetachment &&
                    rule.State != EnemyStrategicState.CaptureZone &&
                    rule.State != EnemyStrategicState.EngageLocal &&
                    rule.State != EnemyStrategicState.GuardSite &&
                    rule.State != EnemyStrategicState.RebuildFleet &&
                    rule.State != EnemyStrategicState.Hold) continue;
                float score = rule.Score(snapshot, profile);
                if (score > 0f && _hasLastState && rule.State == _lastState)
                    score += HYSTERESIS;
                if (score > bestScore)
                {
                    runnerUp = bestScore;
                    best = rule;
                    bestScore = score;
                }
                else runnerUp = Math.Max(runnerUp, score);
            }

            EnemyStrategicState state = best.State;
            string reason = best.Reason;
            bool standAndFight = snapshot.OwnShipCount > 0 && snapshot.HasLocalThreat &&
                snapshot.LocalAdvantage <= profile.RetreatAdvantage;
            if (standAndFight)
            {
                state = EnemyStrategicState.EngageLocal;
                reason = "Hostiles near the fleet outmatch it; focus fire and fight in place.";
                if (snapshot.LocalAdvantage <= profile.RetreatAdvantage * URGENT_LOCAL_DISADVANTAGE_SCALE)
                    Bypass = "standAndFight";
            }
            if (Bypass == "none" && state == EnemyStrategicState.DefendBase &&
                snapshot.HasStationPressure && (!_hasLastState || _lastState != EnemyStrategicState.DefendBase))
                Bypass = "defend";
            BestScore = bestScore;
            RunnerUpScore = runnerUp;
            bool held = _hasLastState && state != _lastState && Bypass == "none" &&
                !float.IsNaN(now) && StateHeldSeconds < MIN_STATE_SECONDS &&
                (_lastState == EnemyStrategicState.EngageLocal && snapshot.HasLocalThreat ||
                 _lastState == EnemyStrategicState.CaptureZone && snapshot.HasCaptureTarget &&
                 state == EnemyStrategicState.EngageLocal);
            if (held)
            {
                BlockedState = state;
                SwitchHeld = !_holdEpisode;
                state = _lastState;
                reason = "Minimum strategic state commitment.";
            }
            _holdEpisode = held;
            if (!_hasLastState || _lastState != state)
                _stateStartedAt = float.IsNaN(now) ? 0f : now;
            _hadBase = snapshot.HasOwnBase;
            _lastState = state;
            _hasLastState = true;
            if (!commit)
            {
                _lastState = previousState;
                _hasLastState = hadState;
                _hadBase = hadBase;
                _holdEpisode = holdEpisode;
                _stateStartedAt = startedAt;
            }
            return new EnemyStrategicDecision(
                state: state,
                committedShipCount: standAndFight && state == EnemyStrategicState.EngageLocal
                    ? snapshot.OwnShipCount : CalculateCommittedShipCount(state, snapshot, profile),
                reason: reason);
        }

        private static int CalculateCommittedShipCount(
            EnemyStrategicState state,
            EnemyStrategicSnapshot snapshot,
            EnemyAiDifficultyProfile profile)
        {
            int ownShipCount = snapshot.OwnShipCount;
            if (state == EnemyStrategicState.DefendBase)
            {
                if (snapshot.IsStationCritical) return ownShipCount;
                // A raid is answered with a detachment sized to it; the rest keeps its fight.
                float share = Math.Min(1f, Math.Max(MIN_DEFENSE_SHARE, snapshot.BaseThreatRatio * DEFENSE_MARGIN));
                return Math.Max(1, Math.Min(ownShipCount, (int)Math.Ceiling(ownShipCount * (decimal)share)));
            }

            if (state == EnemyStrategicState.RebuildFleet)
            {
                return 0;
            }

            if (state == EnemyStrategicState.GuardSite) return ownShipCount;

            int maxCommitment = ownShipCount;
            if (ownShipCount >= 3 && state is EnemyStrategicState.AssaultBase or EnemyStrategicState.HuntFleet)
                maxCommitment--;
            return Math.Max(1, Math.Min(maxCommitment,
                (int)Math.Ceiling(ownShipCount * (decimal)profile.CommittedFleetRatio)));
        }
    }
}
