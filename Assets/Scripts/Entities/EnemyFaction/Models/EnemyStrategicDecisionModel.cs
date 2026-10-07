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
        RetreatValue = 6
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
            float localAdvantage = Combat.CombatMatchup.MAX_ADVANTAGE)
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
    public sealed class EnemyStrategicDecisionModel : PureModel
    {
        /// <summary>Bonus for the current state so near-equal options do not flip every decision.</summary>
        private const float HYSTERESIS = 0.05f;

        private static readonly EnemyStrategicRule[] _rules =
        {
            new EnemyStrategicRule(EnemyStrategicState.RebuildFleet, 1f,
                "No combat ships are available.",
                (s, p) => ResponseCurve.When(s.OwnShipCount == 0)),
            new EnemyStrategicRule(EnemyStrategicState.DefendBase, 0.9f,
                "A nearby enemy task force threatens the home base.",
                (s, p) => ResponseCurve.When(s.HasOwnBase) *
                          ResponseCurve.AtLeast(s.BaseThreatRatio, p.DefenseThreatRatio)),
            new EnemyStrategicRule(EnemyStrategicState.RetreatValue, 0.8f,
                "Hostiles near the fleet outmatch it, so the fleet withdraws to its base.",
                (s, p) => ResponseCurve.When(s.HasOwnBase) *
                          ResponseCurve.AtMost(s.LocalAdvantage, p.RetreatAdvantage)),
            new EnemyStrategicRule(EnemyStrategicState.CaptureZone, 0.7f,
                "Enemy ships are contesting an owned capture site.",
                (s, p) => ResponseCurve.When(s.HasThreatenedSite)),
            new EnemyStrategicRule(EnemyStrategicState.CaptureZone, 0.6f,
                "The configured map-control floor has not been established.",
                (s, p) => ResponseCurve.When(s.HasCaptureTarget &&
                                             s.OwnedCapturableZoneCount < p.MinimumControlledZones)),
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
            // Expansion is the default activity: the next closest relay or site, then the farther ones.
            new EnemyStrategicRule(EnemyStrategicState.CaptureZone, 0.45f,
                "Expanding map control to the next closest relay or site.",
                (s, p) => ResponseCurve.When(s.HasCaptureTarget)),
            new EnemyStrategicRule(EnemyStrategicState.HuntFleet, 0.3f,
                "Nothing is left to capture, so the fleet hunts the enemy.",
                (s, p) => ResponseCurve.When(s.EnemyShipCount > 0) *
                          ResponseCurve.AtLeast(s.FleetAdvantage, p.HuntAdvantage)),
            new EnemyStrategicRule(EnemyStrategicState.AssaultBase, 0.2f,
                "No other target remains before the base objective.",
                (s, p) => ResponseCurve.When(s.VictoryCondition == BattleVictoryCondition.DestroyOpponentBase &&
                                             s.HasEnemyBaseTarget && s.EnemyShipCount == 0)),
            new EnemyStrategicRule(EnemyStrategicState.Hold, 0.05f,
                "No valid strategic target is currently available.",
                (s, p) => 1f)
        };

        private EnemyStrategicState _lastState;

        private bool _hasLastState;

        public EnemyStrategicDecision Evaluate(EnemyStrategicSnapshot snapshot)
        {
            if (snapshot.OwnShipCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(snapshot.OwnShipCount));
            }

            if (snapshot.EnemyShipCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(snapshot.EnemyShipCount));
            }

            if (snapshot.OwnedCapturableZoneCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(snapshot.OwnedCapturableZoneCount));
            }

            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(snapshot.Difficulty);
            EnemyStrategicRule best = _rules[_rules.Length - 1];
            float bestScore = 0f;
            foreach (EnemyStrategicRule rule in _rules)
            {
                float score = rule.Score(snapshot, profile);
                if (score > 0f && _hasLastState && rule.State == _lastState)
                {
                    score += HYSTERESIS;
                }

                if (score > bestScore)
                {
                    best = rule;
                    bestScore = score;
                }
            }

            _lastState = best.State;
            _hasLastState = true;
            return new EnemyStrategicDecision(
                state: best.State,
                committedShipCount: CalculateCommittedShipCount(best.State, snapshot.OwnShipCount, profile),
                reason: best.Reason);
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
