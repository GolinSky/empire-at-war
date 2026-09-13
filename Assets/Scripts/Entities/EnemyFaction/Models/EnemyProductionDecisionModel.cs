using System;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public enum EnemyProductionCategory
    {
        None = 0,
        Ship = 1,
        Mining = 2,
        Defense = 3,
        Level = 4
    }

    public readonly struct EnemyProductionSnapshot
    {
        public EnemyProductionSnapshot(
            EnemyStrategicState strategicState,
            EnemyAiDifficulty difficulty,
            int miningFacilityCount,
            int shipCount,
            int shipsOrdered,
            int defensePlatformCount,
            int currentFactionLevel,
            int miningFacilityTarget,
            int defensePlatformTarget,
            bool hasMiningOption,
            bool hasShipOption,
            bool canBuildShip,
            bool canBuildMining,
            bool hasDefenseOption,
            bool canBuildDefense,
            bool hasLevelUpOption,
            bool canLevelUp)
        {
            StrategicState = strategicState;
            Difficulty = difficulty;
            MiningFacilityCount = miningFacilityCount;
            ShipCount = shipCount;
            ShipsOrdered = shipsOrdered;
            DefensePlatformCount = defensePlatformCount;
            CurrentFactionLevel = currentFactionLevel;
            MiningFacilityTarget = miningFacilityTarget;
            DefensePlatformTarget = defensePlatformTarget;
            HasMiningOption = hasMiningOption;
            HasShipOption = hasShipOption;
            CanBuildShip = canBuildShip;
            CanBuildMining = canBuildMining;
            HasDefenseOption = hasDefenseOption;
            CanBuildDefense = canBuildDefense;
            HasLevelUpOption = hasLevelUpOption;
            CanLevelUp = canLevelUp;
        }

        public EnemyStrategicState StrategicState { get; }
        public EnemyAiDifficulty Difficulty { get; }
        public int MiningFacilityCount { get; }
        public int ShipCount { get; }
        public int ShipsOrdered { get; }
        public int DefensePlatformCount { get; }
        public int CurrentFactionLevel { get; }
        public int MiningFacilityTarget { get; }
        public int DefensePlatformTarget { get; }
        public bool HasMiningOption { get; }
        public bool HasShipOption { get; }
        public bool CanBuildShip { get; }
        public bool CanBuildMining { get; }
        public bool HasDefenseOption { get; }
        public bool CanBuildDefense { get; }
        public bool HasLevelUpOption { get; }
        public bool CanLevelUp { get; }
    }

    public sealed class EnemyProductionDecisionModel : PureModel
    {
        private const int MINIMUM_FLEET_SIZE = 3;

        public float CalculateShipPriority(
            EnemyStrategicState state,
            int shipCount,
            int reservedCount,
            int buildTime,
            int unitCapacity)
        {
            bool needsQuickShips = shipCount < MINIMUM_FLEET_SIZE ||
                state == EnemyStrategicState.CaptureZone ||
                state == EnemyStrategicState.RebuildFleet;
            int weight = needsQuickShips
                ? Math.Max(1, buildTime)
                : Math.Max(1, unitCapacity);

            // Balance the active and queued fleet; lower priority values build first.
            return (reservedCount + 1f) * weight;
        }

        public EnemyProductionCategory Evaluate(EnemyProductionSnapshot snapshot)
        {
            if (snapshot.MiningFacilityCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(snapshot.MiningFacilityCount));
            }

            if (snapshot.ShipCount < MINIMUM_FLEET_SIZE && snapshot.CanBuildShip)
            {
                return EnemyProductionCategory.Ship;
            }

            if (snapshot.Difficulty == EnemyAiDifficulty.UltraHard)
            {
                return EvaluateUltraHard(snapshot);
            }

            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(snapshot.Difficulty);
            bool needsEconomicFoundation =
                snapshot.MiningFacilityCount < profile.MinimumMiningFacilities &&
                snapshot.HasMiningOption;
            if (needsEconomicFoundation)
            {
                return snapshot.CanBuildMining
                    ? EnemyProductionCategory.Mining
                    : EnemyProductionCategory.None;
            }

            if (snapshot.DefensePlatformCount < snapshot.DefensePlatformTarget &&
                snapshot.CanBuildDefense)
            {
                return EnemyProductionCategory.Defense;
            }

            if (snapshot.StrategicState == EnemyStrategicState.Hold)
            {
                if (snapshot.Difficulty >= EnemyAiDifficulty.Hard && snapshot.CanLevelUp)
                {
                    return EnemyProductionCategory.Level;
                }
            }

            if (snapshot.CanBuildShip)
            {
                return EnemyProductionCategory.Ship;
            }

            if (snapshot.CanBuildDefense)
            {
                return EnemyProductionCategory.Defense;
            }

            return snapshot.CanLevelUp
                ? EnemyProductionCategory.Level
                : EnemyProductionCategory.None;
        }

        private static EnemyProductionCategory EvaluateUltraHard(
            EnemyProductionSnapshot snapshot)
        {
            if (snapshot.ShipCount == 0)
            {
                if (snapshot.CanBuildShip)
                {
                    return EnemyProductionCategory.Ship;
                }

                return snapshot.MiningFacilityCount == 0 && snapshot.CanBuildMining
                    ? EnemyProductionCategory.Mining
                    : EnemyProductionCategory.None;
            }

            if (snapshot.MiningFacilityCount < snapshot.MiningFacilityTarget &&
                snapshot.HasMiningOption)
            {
                return snapshot.CanBuildMining
                    ? EnemyProductionCategory.Mining
                    : EnemyProductionCategory.None;
            }

            if (snapshot.ShipsOrdered >= snapshot.CurrentFactionLevel &&
                snapshot.HasLevelUpOption && snapshot.CanLevelUp)
            {
                return EnemyProductionCategory.Level;
            }

            if (snapshot.DefensePlatformCount < snapshot.DefensePlatformTarget &&
                snapshot.HasDefenseOption)
            {
                return snapshot.CanBuildDefense
                    ? EnemyProductionCategory.Defense
                    : EnemyProductionCategory.None;
            }

            if (snapshot.HasShipOption)
            {
                return snapshot.CanBuildShip
                    ? EnemyProductionCategory.Ship
                    : EnemyProductionCategory.None;
            }

            return EnemyProductionCategory.None;
        }
    }
}
