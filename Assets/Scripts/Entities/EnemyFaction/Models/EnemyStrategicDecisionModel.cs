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
        /// <summary>Attacks hostiles near the fleet that it is not outmatched by.</summary>
        EngageLocal = 7,
        /// <summary>Attacks a known, undefended hostile structure when nothing better is available.</summary>
        RaidStructures = 8
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

        /// <summary>A known hostile structure (not a base) has no known hostile ships near it.</summary>
        public bool HasRaidTarget { get; }
        /// <summary>Every own ship is within <see cref="FleetCohesion.RADIUS"/> of the fleet's anchor ship.</summary>
        public bool IsRegrouped { get; }

        /// <summary>Own fleet strength over the focused enemy team's fleet; 1 is an even fight.</summary>
        public float FleetAdvantage { get; }

        /// <summary>Strength of hostiles near the own base over the own fleet; 0 when none are near.</summary>
        public float BaseThreatRatio { get; }

        /// <summary>Own fleet strength over hostiles near the fleet; the clamp maximum when none are near.</summary>
        public float LocalAdvantage { get; }

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
            bool hasSafeRetreat = true)
        {
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
            HasThreatenedSite = hasThreatenedSite;
            HasLocalThreat = hasLocalThreat;
            HasRaidTarget = hasRaidTarget;
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
        /// <summary>Fleet advantage, as a multiple of the attack ratio, at which attacking outranks capturing.</summary>
        public const float PRESS_ADVANTAGE_SCALE = 2f;
        private const string RECOVERING_REASON =
            "Regrouping at a safe anchor before selecting a viable local objective.";

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
            new EnemyStrategicRule(EnemyStrategicState.RetreatValue, 0.8f,
                "Hostiles near the fleet outmatch it, so the fleet withdraws to its base.",
                (s, p) => ResponseCurve.When(s.HasSafeRetreat) *
                          ResponseCurve.AtMost(s.LocalAdvantage, p.RetreatAdvantage)),
            // A clearly stronger fleet ends the game instead of trading ships over capture sites.
            new EnemyStrategicRule(EnemyStrategicState.AssaultBase, 0.75f,
                "The fleet clearly outmatches the enemy, so it presses the advantage against the base.",
                (s, p) => ResponseCurve.When(s.VictoryCondition == BattleVictoryCondition.DestroyOpponentBase &&
                                             s.HasEnemyBaseTarget) *
                          ResponseCurve.AtLeast(s.FleetAdvantage, p.RequiredAttackRatio * PRESS_ADVANTAGE_SCALE)),
            new EnemyStrategicRule(EnemyStrategicState.HuntFleet, 0.75f,
                "The fleet clearly outmatches the enemy fleet, so it presses the advantage and hunts it.",
                (s, p) => ResponseCurve.When(s.VictoryCondition != BattleVictoryCondition.DestroyOpponentBase &&
                                             s.EnemyShipCount > 0) *
                          ResponseCurve.AtLeast(s.FleetAdvantage, p.RequiredAttackRatio * PRESS_ADVANTAGE_SCALE)),
            new EnemyStrategicRule(EnemyStrategicState.CaptureZone, 0.7f,
                "Enemy ships are contesting an owned capture site.",
                (s, p) => ResponseCurve.When(s.HasThreatenedSite)),
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
                (s, p) => ResponseCurve.When(s.VictoryCondition == BattleVictoryCondition.DestroyOpponentBase &&
                                             s.HasEnemyBaseTarget) *
                          ResponseCurve.AtLeast(s.FleetAdvantage, p.RequiredAttackRatio)),
            new EnemyStrategicRule(EnemyStrategicState.HuntFleet, 0.5f,
                "The objective is the enemy fleet and our team clearly outmatches it.",
                (s, p) => ResponseCurve.When(s.VictoryCondition != BattleVictoryCondition.DestroyOpponentBase &&
                                             s.EnemyShipCount > 0) *
                          ResponseCurve.AtLeast(s.FleetAdvantage, p.RequiredAttackRatio)),
            new EnemyStrategicRule(EnemyStrategicState.HuntFleet, 0.3f,
                "Nothing is left to capture, so the fleet hunts the enemy.",
                (s, p) => ResponseCurve.When(s.EnemyShipCount > 0) *
                          ResponseCurve.AtLeast(s.FleetAdvantage, p.HuntAdvantage)),
            // Both objectives need the base destroyed: destroy-all also waits for every station and building.
            new EnemyStrategicRule(EnemyStrategicState.AssaultBase, 0.2f,
                "No other target remains before the base objective.",
                (s, p) => ResponseCurve.When(s.HasEnemyBaseTarget && s.EnemyShipCount == 0)),
            // Too weak to hunt and nothing to capture: hit structures the enemy left undefended.
            new EnemyStrategicRule(EnemyStrategicState.RaidStructures, 0.1f,
                "The enemy fleet is too strong, so the fleet raids an undefended structure.",
                (s, p) => ResponseCurve.When(s.HasRaidTarget)),
            new EnemyStrategicRule(EnemyStrategicState.Hold, 0.05f,
                "No valid strategic target is currently available.",
                (s, p) => 1f)
        };

        private EnemyStrategicState _lastState;

        private bool _hasLastState;

        /// <summary>
        /// Regrouping gates local action; the ordinary offensive rules still require global readiness.
        /// </summary>
        private bool _isRecovering;
        public bool IsRecovering => _isRecovering;

        public EnemyStrategicDecision Evaluate(EnemyStrategicSnapshot snapshot, bool captureDetachment = false)
        {
            if (snapshot.OwnShipCount < 0)
                throw new ArgumentOutOfRangeException(nameof(snapshot.OwnShipCount));
            if (snapshot.EnemyShipCount < 0)
                throw new ArgumentOutOfRangeException(nameof(snapshot.EnemyShipCount));
            if (snapshot.OwnedCapturableZoneCount < 0)
                throw new ArgumentOutOfRangeException(nameof(snapshot.OwnedCapturableZoneCount));

            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(snapshot.Difficulty);
            EnemyStrategicRule best = _rules[_rules.Length - 1];
            float bestScore = 0f;
            foreach (EnemyStrategicRule rule in _rules)
            {
                if (captureDetachment &&
                    rule.State != EnemyStrategicState.CaptureZone &&
                    rule.State != EnemyStrategicState.RetreatValue &&
                    rule.State != EnemyStrategicState.RebuildFleet &&
                    rule.State != EnemyStrategicState.Hold) continue;
                float score = rule.Score(snapshot, profile);
                if (score > 0f && _hasLastState && rule.State == _lastState)
                    score += HYSTERESIS;
                if (score > bestScore)
                {
                    best = rule;
                    bestScore = score;
                }
            }

            EnemyStrategicState state = best.State;
            string reason = best.Reason;
            if (state == EnemyStrategicState.RetreatValue)
                _isRecovering = true;
            else if (_isRecovering && state != EnemyStrategicState.DefendBase &&
                     state != EnemyStrategicState.RebuildFleet)
            {
                if (snapshot.IsRegrouped)
                    _isRecovering = false;
                else if (!snapshot.HasSafeRetreat &&
                         (state == EnemyStrategicState.CaptureZone || state == EnemyStrategicState.EngageLocal))
                    reason = "Home access is lost; rally through the viable local objective.";
                else
                {
                    state = snapshot.HasSafeRetreat ? EnemyStrategicState.RetreatValue : EnemyStrategicState.Hold;
                    reason = snapshot.HasSafeRetreat ? RECOVERING_REASON :
                        "No safe retreat anchor or cohesive local action is available; hold and rebuild.";
                }
            }

            _lastState = state;
            _hasLastState = true;
            return new EnemyStrategicDecision(
                state: state,
                committedShipCount: CalculateCommittedShipCount(state, snapshot.OwnShipCount, profile),
                reason: reason);
        }

        private static int CalculateCommittedShipCount(
            EnemyStrategicState state,
            int ownShipCount,
            EnemyAiDifficultyProfile profile)
        {
            if (state == EnemyStrategicState.RebuildFleet)
            {
                return 0;
            }

            if (state == EnemyStrategicState.RetreatValue)
            {
                return ownShipCount;
            }

            return Math.Max(1, Math.Min(ownShipCount,
                (int)Math.Ceiling(ownShipCount * (decimal)profile.CommittedFleetRatio)));
        }
    }
}
