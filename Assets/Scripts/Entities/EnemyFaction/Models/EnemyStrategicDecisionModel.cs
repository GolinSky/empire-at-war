using System;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
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
        GuardSite = 9,
        Consolidate = 10
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
        public bool CanAssaultBase { get; }
        public bool CanHuntFleet { get; }
        public bool HasSecuredCentre { get; }

        /// <summary>
        /// The group is away from the station and every held zone, a gathering point exists and nothing blocks the
        /// way back: an outmatched local fight is left instead of fought to the last ship.
        /// </summary>
        public bool CanDisengage { get; }

        /// <summary>
        /// The team holds fewer zones than an enemy and this force has no zone it can take: it sabotages known
        /// hostile income instead of waiting at its gathering point.
        /// </summary>
        public bool RaidWhenBehind { get; }

        /// <summary>The station is losing hull fast enough that the whole fleet must defend it.</summary>
        public bool IsStationCritical { get; }

        /// <summary>
        /// Outside the opening, by situation (W48): a team station is under pressure or a hostile armada threatens a
        /// team asset. Capture and raid then need fleet advantage at the retreat line.
        /// </summary>
        public bool IsLateGame { get; }

        /// <summary>
        /// False while the group retreats and has not reached its gathering point: the retreat stays latched (W49).
        /// </summary>
        public bool HasReachedRetreatPoint { get; }

        /// <summary>
        /// End game by situation, never the clock: station at the top level and the unit cap full or an assault surplus
        /// banked. The centre no longer holds the fleet back and the whole fleet assaults.
        /// </summary>
        public bool IsEndGame { get; }

        /// <summary>Allied human ships fight at the focused hostile station: the fleet joins their assault.</summary>
        public bool JoinsAllyAssault { get; }

        /// <summary>
        /// Plan-vs-actual loss exchange against the focused hostile team (W67), already multiplied into
        /// <see cref="LocalAdvantage"/>, <see cref="FleetAdvantage"/> and their raw values; 1 without fights.
        /// </summary>
        public float TradeCorrection { get; }

        /// <summary>
        /// Phase of the group's hit-and-run cycle. While it runs the strike ships pull back and return on their own,
        /// so an outmatched local fight does not make the whole group disengage.
        /// </summary>
        public EnemyStrikePhase StrikePhase { get; }

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
            float localAdvantage = CombatMatchup.MAX_ADVANTAGE,
            bool hasLocalThreat = false,
            bool hasRaidTarget = false,
            bool isRegrouped = true,
            bool hasStationPressure = false,
            bool hasSafeRetreat = true,
            bool hasEconomyRaidTarget = false,
            float localRaw = float.NaN, float fleetRaw = float.NaN, bool recoveryMode = false,
            bool isStationCritical = false, bool hasGuardSite = false, bool isCentreGuardGoal = false,
            bool canAssaultBase = false, bool canHuntFleet = false, bool hasSecuredCentre = false,
            bool canDisengage = false, bool raidWhenBehind = false, bool isLateGame = false,
            bool hasReachedRetreatPoint = true, float tradeCorrection = 1f, bool isEndGame = false,
            bool joinsAllyAssault = false, EnemyStrikePhase strikePhase = EnemyStrikePhase.Idle)
        {
            StrikePhase = strikePhase;
            IsEndGame = isEndGame;
            JoinsAllyAssault = joinsAllyAssault;
            IsLateGame = isLateGame;
            HasReachedRetreatPoint = hasReachedRetreatPoint;
            TradeCorrection = tradeCorrection;
            CanDisengage = canDisengage;
            RaidWhenBehind = raidWhenBehind;
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
            CanAssaultBase = canAssaultBase;
            CanHuntFleet = canHuntFleet;
            HasSecuredCentre = hasSecuredCentre;
            HasThreatenedSite = hasThreatenedSite;
            HasLocalThreat = hasLocalThreat;
            HasRaidTarget = hasRaidTarget;
            HasEconomyRaidTarget = hasEconomyRaidTarget;
            IsRegrouped = isRegrouped;
            HasStationPressure = hasStationPressure;
            HasSafeRetreat = hasSafeRetreat;
        }

        private EnemyStrategicSnapshot(EnemyStrategicSnapshot source, EnemyStrikePhase strikePhase)
        {
            this = source;
            StrikePhase = strikePhase;
        }

        /// <summary>The same snapshot with the hit-and-run phase the group is in this decision.</summary>
        public EnemyStrategicSnapshot WithStrikePhase(EnemyStrikePhase strikePhase) =>
            new EnemyStrategicSnapshot(this, strikePhase);
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
            // Stations die in every mode: a station keeps building ships, so no victory condition switches assault off.
            // End game and an allied assault skip the centre (user rule 2026-11-25).
            new EnemyStrategicRule(EnemyStrategicState.AssaultBase, 0.76f,
                "Capital strike group has zone control, fleet strength and reinforcement waves.",
                (s, p) => ResponseCurve.When((s.HasSecuredCentre || s.IsEndGame || s.JoinsAllyAssault) &&
                    s.HasEnemyBaseTarget) *
                    ResponseCurve.AtLeast(s.FleetAdvantage, p.RequiredAttackRatio)),
            // A clearly stronger fleet ends the game instead of trading ships over capture sites.
            new EnemyStrategicRule(EnemyStrategicState.AssaultBase, 0.75f,
                "The fleet clearly outmatches the enemy, so it presses the advantage against the base.",
                (s, p) => ResponseCurve.When(s.HasEnemyBaseTarget) *
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
                (s, p) => ResponseCurve.When(s.HasRaidTarget && s.HasEconomyRaidTarget && !s.HasThreatenedSite &&
                    (!s.IsCentreGuardGoal || s.RaidWhenBehind))),
            new EnemyStrategicRule(EnemyStrategicState.CaptureZone, 0.6f,
                "Expanding map control to the next closest relay or site.",
                (s, p) => ResponseCurve.When(s.HasCaptureTarget)),
            // Outmatched overall but not where the fleet is: fight what is in front of it instead of freezing.
            new EnemyStrategicRule(EnemyStrategicState.EngageLocal, 0.55f,
                "Hostiles near the fleet do not outmatch it, so the fleet engages them.",
                (s, p) => ResponseCurve.When(s.HasLocalThreat) *
                          ResponseCurve.AtLeast(s.LocalAdvantage, p.RetreatAdvantage)),
            new EnemyStrategicRule(EnemyStrategicState.AssaultBase, 0.5f,
                "The fleet outmatches the defenders of the hostile base.",
                (s, p) => ResponseCurve.When(!s.IsCentreGuardGoal && s.HasEnemyBaseTarget) *
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
                (s, p) => ResponseCurve.When(s.HasRaidTarget && (!s.IsCentreGuardGoal || s.RaidWhenBehind ||
                    s.HasSecuredCentre && s.HasEconomyRaidTarget && s.OwnShipCount >= 6))),
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
            float lastStateScore = 0f;
            EnemyStrategicRule bestHunt = null;
            float bestHuntScore = 0f;
            SwitchHeld = false;
            StateHeldSeconds = _hasLastState && !float.IsNaN(now) ? Math.Max(0f, now - _stateStartedAt) : 0f;
            Bypass = snapshot.OwnShipCount == 0 ? "noShips" :
                _hasLastState && _hadBase && !snapshot.HasOwnBase ? "stationLost" : "none";
            foreach (EnemyStrategicRule rule in _rules)
            {
                if (rule.State == EnemyStrategicState.AssaultBase && !snapshot.CanAssaultBase ||
                    rule.State == EnemyStrategicState.HuntFleet && !snapshot.CanHuntFleet) continue;
                if (snapshot.HasSecuredCentre && snapshot.HasCaptureTarget &&
                    rule.State == EnemyStrategicState.RaidStructures) continue;
                if (!allowOffense && (rule.State == EnemyStrategicState.HuntFleet ||
                    rule.State == EnemyStrategicState.AssaultBase || rule.State == EnemyStrategicState.RaidStructures)) continue;
                if (captureDetachment &&
                    rule.State != EnemyStrategicState.CaptureZone &&
                    rule.State != EnemyStrategicState.EngageLocal &&
                    rule.State != EnemyStrategicState.GuardSite &&
                    rule.State != EnemyStrategicState.RebuildFleet &&
                    rule.State != EnemyStrategicState.Hold) continue;
                // W48: outside the opening a fleet below the retreat line gathers instead of capturing or raiding.
                if (snapshot.IsLateGame && snapshot.FleetAdvantage < profile.RetreatAdvantage &&
                    rule.State is EnemyStrategicState.CaptureZone or EnemyStrategicState.RaidStructures) continue;
                float score = rule.Score(snapshot, profile);
                if (_hasLastState && (rule.State == _lastState || _lastState == EnemyStrategicState.Consolidate &&
                        rule.State == EnemyStrategicState.GuardSite))
                    lastStateScore = Math.Max(lastStateScore, score);
                if (score > 0f && _hasLastState && rule.State == _lastState)
                    score += HYSTERESIS;
                if (rule.State == EnemyStrategicState.HuntFleet && score > bestHuntScore)
                {
                    bestHunt = rule;
                    bestHuntScore = score;
                }
                if (score > bestScore)
                {
                    runnerUp = bestScore;
                    best = rule;
                    bestScore = score;
                }
                else runnerUp = Math.Max(runnerUp, score);
            }

            // S2: against the enemy fleet, a fleet away from its station is hunted first; the station is assaulted when
            // no hunt target is left (hunt rules are skipped while CanHuntFleet is false). Not in the end game and not
            // while joining an allied assault: there the next station is the target (L1, 20:43 log — the ally hunted
            // while the second station still stood).
            if (best.State == EnemyStrategicState.AssaultBase && bestHunt != null &&
                snapshot.VictoryCondition == BattleVictoryCondition.DestroyEnemyFleet &&
                !snapshot.IsEndGame && !snapshot.JoinsAllyAssault)
            {
                runnerUp = Math.Max(runnerUp, bestScore);
                best = bestHunt;
                bestScore = bestHuntScore;
            }
            EnemyStrategicState state = best.State;
            string reason = best.Reason;
            if (state == EnemyStrategicState.AssaultBase && snapshot.JoinsAllyAssault)
                reason = "Joining an allied assault.";
            if (state == EnemyStrategicState.GuardSite && snapshot.HasSecuredCentre)
            {
                state = EnemyStrategicState.Consolidate;
                reason = "Hold the centre while developing owned zones.";
            }
            bool standAndFight = snapshot.OwnShipCount > 0 && snapshot.HasLocalThreat &&
                snapshot.LocalAdvantage <= profile.RetreatAdvantage;
            // A running hit-and-run cycle already pulls its strike ships back: the group keeps its fight.
            bool disengage = standAndFight && snapshot.CanDisengage && snapshot.StrikePhase == EnemyStrikePhase.Idle;
            if (disengage)
            {
                // Pull back only as far as the gathering point; the state hold below stops a flip back.
                state = EnemyStrategicState.RetreatValue;
                reason = "Hostiles near the fleet outmatch it away from held ground; pull back to the gathering point.";
                Bypass = "disengage";
            }
            else if (standAndFight)
            {
                state = EnemyStrategicState.EngageLocal;
                reason = "Hostiles near the fleet outmatch it; focus fire and fight in place.";
                if (snapshot.LocalAdvantage <= profile.RetreatAdvantage * URGENT_LOCAL_DISADVANTAGE_SCALE)
                    Bypass = "standAndFight";
            }
            if (Bypass == "none" && state == EnemyStrategicState.DefendBase &&
                snapshot.HasStationPressure && (!_hasLastState || _lastState != EnemyStrategicState.DefendBase))
                Bypass = "defend";
            // W49: a retreat runs until the group reaches its gathering point; only urgent switches stop it.
            bool retreatLatched = Bypass == "none" && _hasLastState && _lastState == EnemyStrategicState.RetreatValue &&
                state != EnemyStrategicState.RetreatValue && !snapshot.HasReachedRetreatPoint && snapshot.OwnShipCount > 0;
            if (retreatLatched)
            {
                state = EnemyStrategicState.RetreatValue;
                reason = "The retreat continues until the gathering point is reached.";
            }
            BestScore = bestScore;
            RunnerUpScore = runnerUp;
            // W49: every non-urgent switch waits MIN_STATE_SECONDS while the current state still has its reason. Idle
            // states (Hold, RebuildFleet) have nothing to keep: a new task starts at once.
            bool lastStillValid = _lastState switch
            {
                EnemyStrategicState.EngageLocal or EnemyStrategicState.RetreatValue => snapshot.HasLocalThreat,
                EnemyStrategicState.Hold or EnemyStrategicState.RebuildFleet => false,
                _ => lastStateScore > 0f
            };
            bool held = _hasLastState && state != _lastState && Bypass == "none" &&
                !float.IsNaN(now) && StateHeldSeconds < MIN_STATE_SECONDS && lastStillValid;
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
                committedShipCount: (standAndFight || retreatLatched) &&
                    state is EnemyStrategicState.EngageLocal or EnemyStrategicState.RetreatValue
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

            if (state is EnemyStrategicState.GuardSite or EnemyStrategicState.Consolidate) return ownShipCount;
            // S4: the end game sends the whole fleet against the base.
            if (state == EnemyStrategicState.AssaultBase && snapshot.IsEndGame) return ownShipCount;
            if (state == EnemyStrategicState.RaidStructures && snapshot.HasSecuredCentre) return 2;
            if (state == EnemyStrategicState.AssaultBase && snapshot.HasSecuredCentre)
                return Math.Min(ownShipCount, Math.Max(EnemyFactionModel.MINIMUM_ASSAULT_CAPITALS, ownShipCount - 1));

            int maxCommitment = ownShipCount;
            if (ownShipCount >= 3 && state is EnemyStrategicState.AssaultBase or EnemyStrategicState.HuntFleet)
                maxCommitment--;
            return Math.Max(1, Math.Min(maxCommitment,
                (int)Math.Ceiling(ownShipCount * (decimal)profile.CommittedFleetRatio)));
        }
    }
}
