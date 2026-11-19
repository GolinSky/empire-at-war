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
        Level = 4,
        Research = 5
    }

    public readonly struct EnemyProductionSnapshot
    {
        public EnemyStrategicState StrategicState { get; }
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
        public bool CanBuildDefense { get; }
        public bool HasLevelUpOption { get; }
        public bool CanLevelUp { get; }

        /// <summary>An unlocked, not yet queued research tier that raises income is affordable.</summary>
        public bool CanResearchIncome { get; }

        /// <summary>An unlocked, not yet queued combat research tier is affordable.</summary>
        public bool CanResearchCombat { get; }

        /// <summary>At least one ship type (not squadron) is unlocked and within its limits.</summary>
        public bool CanOrderShips { get; }
        public bool IsStationCritical { get; }

        public bool CanResearchMining { get; }

        public EnemyProductionSnapshot(
            EnemyStrategicState strategicState,
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
            bool canBuildDefense,
            bool hasLevelUpOption,
            bool canLevelUp,
            bool canResearchIncome = false,
            bool canResearchCombat = false,
            bool canOrderShips = true,
            bool isStationCritical = false,
            bool canResearchMining = false)
        {
            StrategicState = strategicState;
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
            CanBuildDefense = canBuildDefense;
            HasLevelUpOption = hasLevelUpOption;
            CanLevelUp = canLevelUp;
            CanResearchIncome = canResearchIncome;
            CanResearchCombat = canResearchCombat;
            CanOrderShips = canOrderShips;
            IsStationCritical = isStationCritical;
            CanResearchMining = canResearchMining;
        }
    }

    public sealed class EnemyProductionDecisionModel : Model
    {
        private const int MINIMUM_FLEET_SIZE = 3;
        private const int SHIP_ORDERS_PER_LEVEL = 1;
        private const int FLAGSHIP_MAX_COUNT = 3;
        private const int FLAGSHIP_MIN_CAPACITY = 5;
        private const float FLAGSHIP_PRIORITY = 0f;

        /// <summary>Squadrons do not count toward the fleet, so a fleet below the minimum must buy ships.</summary>
        public bool NeedsMinimumFleet(int shipCount) => shipCount < MINIMUM_FLEET_SIZE;

        public float CalculateShipPriority(
            EnemyStrategicState state,
            int shipCount,
            int reservedCount,
            int buildTime,
            int unitCapacity,
            int maxCount,
            float combatValuePerCredit = 1f)
        {
            // Scarce flagship types (e.g. Rothana, Malevolence) never win the balance below,
            // so an established fleet without one builds it first. Scarce small ships (IPV-2C) are not flagships.
            if (shipCount >= MINIMUM_FLEET_SIZE && reservedCount == 0 &&
                maxCount <= FLAGSHIP_MAX_COUNT && unitCapacity >= FLAGSHIP_MIN_CAPACITY)
            {
                return FLAGSHIP_PRIORITY;
            }

            bool needsQuickShips = shipCount < MINIMUM_FLEET_SIZE ||
                state == EnemyStrategicState.CaptureZone ||
                state == EnemyStrategicState.RebuildFleet;
            int weight = needsQuickShips
                ? Math.Max(1, buildTime)
                : Math.Max(1, unitCapacity);

            // Balance the active and queued fleet; lower priority values build first. Value per credit scales the
            // balance, so strong ships are built more often and a quick but weak unit never wins on build time alone.
            return (reservedCount + 1f) * weight / combatValuePerCredit;
        }

        public EnemyProductionCategory Evaluate(EnemyProductionSnapshot snapshot)
        {
            if (snapshot.MiningFacilityCount < 0)
                throw new ArgumentOutOfRangeException(nameof(snapshot.MiningFacilityCount));

            bool needsMining = snapshot.MiningFacilityCount < snapshot.MiningFacilityTarget && snapshot.HasMiningOption;
            if (snapshot.CanResearchMining && !snapshot.IsStationCritical)
                return EnemyProductionCategory.Research;

            if (needsMining && (snapshot.CanBuildMining || !snapshot.IsStationCritical))
                return snapshot.CanBuildMining ? EnemyProductionCategory.Mining : EnemyProductionCategory.None;

            if (snapshot.IsStationCritical)
                return snapshot.CanBuildShip ? EnemyProductionCategory.Ship : EnemyProductionCategory.None;

            if (IsLevelDue(snapshot))
                return snapshot.CanLevelUp ? EnemyProductionCategory.Level : EnemyProductionCategory.None;

            if (NeedsMinimumFleet(snapshot.ShipCount) && snapshot.CanOrderShips && snapshot.HasShipOption)
                return snapshot.CanBuildShip ? EnemyProductionCategory.Ship : EnemyProductionCategory.None;

            if (snapshot.CanResearchIncome)
                return EnemyProductionCategory.Research;

            if (snapshot.DefensePlatformCount < snapshot.DefensePlatformTarget && snapshot.CanBuildDefense)
                return EnemyProductionCategory.Defense;

            if (snapshot.CanResearchCombat)
                return EnemyProductionCategory.Research;

            if (snapshot.HasShipOption)
                return snapshot.CanBuildShip ? EnemyProductionCategory.Ship : EnemyProductionCategory.None;

            if (snapshot.CanBuildDefense)
                return EnemyProductionCategory.Defense;

            if (snapshot.CanLevelUp)
                return EnemyProductionCategory.Level;

            return snapshot.CanResearchCombat ? EnemyProductionCategory.Research : EnemyProductionCategory.None;
        }

        public bool IsLevelDue(EnemyProductionSnapshot snapshot) =>
            snapshot.HasLevelUpOption &&
            (snapshot.CurrentFactionLevel == 1 ||
             snapshot.StrategicState != EnemyStrategicState.DefendBase &&
             (!snapshot.CanOrderShips ||
              snapshot.ShipsOrdered >= snapshot.CurrentFactionLevel * SHIP_ORDERS_PER_LEVEL));
    }
}
