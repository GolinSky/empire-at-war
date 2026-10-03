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
        public int EnemyShipsNearOwnBase { get; }
        public bool HasThreatenedSite { get; }

        public EnemyStrategicSnapshot(
            BattleVictoryCondition victoryCondition,
            EnemyAiDifficulty difficulty,
            int ownShipCount,
            int enemyShipCount,
            int ownedCapturableZoneCount,
            int enemyShipsNearOwnBase,
            bool hasCaptureTarget,
            bool hasEnemyBaseTarget,
            bool hasOwnBase,
            bool hasThreatenedSite = false)
        {
            VictoryCondition = victoryCondition;
            Difficulty = difficulty;
            OwnShipCount = ownShipCount;
            EnemyShipCount = enemyShipCount;
            HasCaptureTarget = hasCaptureTarget;
            HasEnemyBaseTarget = hasEnemyBaseTarget;
            HasOwnBase = hasOwnBase;
            OwnedCapturableZoneCount = ownedCapturableZoneCount;
            EnemyShipsNearOwnBase = enemyShipsNearOwnBase;
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

    public sealed class EnemyStrategicDecisionModel : PureModel
    {
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

            if (snapshot.EnemyShipsNearOwnBase < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(snapshot.EnemyShipsNearOwnBase));
            }

            if (snapshot.OwnShipCount == 0)
            {
                return new EnemyStrategicDecision(
                    state: EnemyStrategicState.RebuildFleet,
                    committedShipCount: 0,
                    reason: "No combat ships are available.");
            }

            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(snapshot.Difficulty);
            int committedShipCount = Math.Max(
                1,
                Math.Min(
                    snapshot.OwnShipCount,
                    CalculateThreshold(
                        snapshot.OwnShipCount,
                        profile.CommittedFleetRatio)));

            int defenseThreshold = Math.Max(
                1,
                CalculateThreshold(
                    snapshot.OwnShipCount,
                    profile.DefenseThreatRatio));
            if (snapshot.HasOwnBase &&
                snapshot.EnemyShipsNearOwnBase >= defenseThreshold)
            {
                return new EnemyStrategicDecision(
                    state: EnemyStrategicState.DefendBase,
                    committedShipCount: committedShipCount,
                    reason: "A nearby enemy task force threatens the home base.");
            }

            if (snapshot.HasOwnBase && snapshot.EnemyShipCount - snapshot.OwnShipCount >=
                profile.OutnumberedRetreatCount)
            {
                return new EnemyStrategicDecision(
                    state: EnemyStrategicState.RetreatValue,
                    committedShipCount: snapshot.OwnShipCount,
                    reason: "The fleet is outnumbered and is withdrawing to its base.");
            }

            if (snapshot.HasThreatenedSite)
            {
                return new EnemyStrategicDecision(
                    state: EnemyStrategicState.CaptureZone,
                    committedShipCount: committedShipCount,
                    reason: "Enemy ships are contesting an owned capture site.");
            }

            if (snapshot.HasCaptureTarget &&
                snapshot.OwnedCapturableZoneCount < profile.MinimumControlledZones)
            {
                return new EnemyStrategicDecision(
                    state: EnemyStrategicState.CaptureZone,
                    committedShipCount: committedShipCount,
                    reason: "The configured map-control floor has not been established.");
            }

            if (snapshot.VictoryCondition == BattleVictoryCondition.DestroyOpponentBase)
            {
                int requiredShips = Math.Max(
                    1,
                    CalculateThreshold(
                        Math.Max(1, snapshot.EnemyShipCount),
                        profile.RequiredAttackRatio));
                if (snapshot.HasEnemyBaseTarget && snapshot.OwnShipCount >= requiredShips)
                {
                    return new EnemyStrategicDecision(
                        state: EnemyStrategicState.AssaultBase,
                        committedShipCount: committedShipCount,
                        reason: "The selected victory condition is base destruction and the attack threshold is met.");
                }

                if (snapshot.HasCaptureTarget)
                {
                    return new EnemyStrategicDecision(
                        state: EnemyStrategicState.CaptureZone,
                        committedShipCount: committedShipCount,
                        reason: "More map control is needed before assaulting the base.");
                }

                if (snapshot.EnemyShipCount > 0)
                {
                    return new EnemyStrategicDecision(
                        state: EnemyStrategicState.HuntFleet,
                        committedShipCount: committedShipCount,
                        reason: "Enemy ships block the route to the base objective.");
                }

                if (snapshot.HasEnemyBaseTarget)
                {
                    return new EnemyStrategicDecision(
                        state: EnemyStrategicState.AssaultBase,
                        committedShipCount: committedShipCount,
                        reason: "No other target remains before the base objective.");
                }
            }
            else
            {
                if (snapshot.EnemyShipCount > 0)
                {
                    return new EnemyStrategicDecision(
                        state: EnemyStrategicState.HuntFleet,
                        committedShipCount: committedShipCount,
                        reason: "The selected victory condition prioritizes eliminating the enemy fleet.");
                }

                if (snapshot.HasCaptureTarget)
                {
                    return new EnemyStrategicDecision(
                        state: EnemyStrategicState.CaptureZone,
                        committedShipCount: committedShipCount,
                        reason: "No visible fleet target exists, so the AI expands map control.");
                }
            }

            return new EnemyStrategicDecision(
                state: EnemyStrategicState.Hold,
                committedShipCount: committedShipCount,
                reason: "No valid strategic target is currently available.");
        }

        private static int CalculateThreshold(int unitCount, float ratio)
        {
            return (int)Math.Ceiling(unitCount * (decimal)ratio);
        }
    }
}
