using System;
using EmpireAtWar.Entities.Game;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public enum EnemyStrategicState
    {
        RebuildFleet = 0,
        CaptureZone = 1,
        HuntFleet = 2,
        AssaultBase = 3,
        DefendBase = 4,
        Hold = 5
    }

    public readonly struct EnemyStrategicSnapshot
    {
        public EnemyStrategicSnapshot(
            BattleVictoryCondition victoryCondition,
            EnemyAiDifficulty difficulty,
            int ownShipCount,
            int enemyShipCount,
            bool hasCaptureTarget,
            bool hasEnemyBaseTarget,
            bool hasOwnBase,
            int ownedCapturableZoneCount,
            int enemyShipsNearOwnBase)
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
        }

        public BattleVictoryCondition VictoryCondition { get; }
        public EnemyAiDifficulty Difficulty { get; }
        public int OwnShipCount { get; }
        public int EnemyShipCount { get; }
        public bool HasCaptureTarget { get; }
        public bool HasEnemyBaseTarget { get; }
        public bool HasOwnBase { get; }
        public int OwnedCapturableZoneCount { get; }
        public int EnemyShipsNearOwnBase { get; }
    }

    public readonly struct EnemyStrategicDecision
    {
        public EnemyStrategicDecision(EnemyStrategicState state, int committedShipCount, string reason)
        {
            State = state;
            CommittedShipCount = committedShipCount;
            Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        }

        public EnemyStrategicState State { get; }
        public int CommittedShipCount { get; }
        public string Reason { get; }
    }

    public sealed class EnemyStrategicDecisionModel
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
                    EnemyStrategicState.RebuildFleet,
                    0,
                    "No combat ships are available.");
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
                    EnemyStrategicState.DefendBase,
                    committedShipCount,
                    "A nearby enemy task force threatens the home base.");
            }

            if (snapshot.HasCaptureTarget &&
                snapshot.OwnedCapturableZoneCount < profile.MinimumControlledZones)
            {
                return new EnemyStrategicDecision(
                    EnemyStrategicState.CaptureZone,
                    committedShipCount,
                    "The configured map-control floor has not been established.");
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
                        EnemyStrategicState.AssaultBase,
                        committedShipCount,
                        "The selected victory condition is base destruction and the attack threshold is met.");
                }

                if (snapshot.HasCaptureTarget)
                {
                    return new EnemyStrategicDecision(
                        EnemyStrategicState.CaptureZone,
                        committedShipCount,
                        "More map control is needed before assaulting the base.");
                }

                if (snapshot.EnemyShipCount > 0)
                {
                    return new EnemyStrategicDecision(
                        EnemyStrategicState.HuntFleet,
                        committedShipCount,
                        "Enemy ships block the route to the base objective.");
                }

                if (snapshot.HasEnemyBaseTarget)
                {
                    return new EnemyStrategicDecision(
                        EnemyStrategicState.AssaultBase,
                        committedShipCount,
                        "No other target remains before the base objective.");
                }
            }
            else
            {
                if (snapshot.EnemyShipCount > 0)
                {
                    return new EnemyStrategicDecision(
                        EnemyStrategicState.HuntFleet,
                        committedShipCount,
                        "The selected victory condition prioritizes eliminating the enemy fleet.");
                }

                if (snapshot.HasCaptureTarget)
                {
                    return new EnemyStrategicDecision(
                        EnemyStrategicState.CaptureZone,
                        committedShipCount,
                        "No visible fleet target exists, so the AI expands map control.");
                }
            }

            return new EnemyStrategicDecision(
                EnemyStrategicState.Hold,
                committedShipCount,
                "No valid strategic target is currently available.");
        }

        private static int CalculateThreshold(int unitCount, float ratio)
        {
            return (int)Math.Ceiling(unitCount * (decimal)ratio);
        }
    }
}
