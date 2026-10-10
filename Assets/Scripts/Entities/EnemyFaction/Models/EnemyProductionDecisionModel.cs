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

        /// <summary>An unlocked, not yet queued research tier that raises income is affordable.</summary>
        public bool CanResearchIncome { get; }

        /// <summary>An unlocked, not yet queued combat research tier is affordable.</summary>
        public bool CanResearchCombat { get; }

        /// <summary>At least one ship type (not squadron) is unlocked and within its limits.</summary>
        public bool CanOrderShips { get; }

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
            bool canLevelUp,
            bool canResearchIncome = false,
            bool canResearchCombat = false,
            bool canOrderShips = true)
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
            CanResearchIncome = canResearchIncome;
            CanResearchCombat = canResearchCombat;
            CanOrderShips = canOrderShips;
        }
    }

    public sealed class EnemyProductionDecisionModel : Model
    {
        private const int MINIMUM_FLEET_SIZE = 3;
        private const int FLAGSHIP_MAX_COUNT = 3;
        private const float FLAGSHIP_PRIORITY = 0f;

        /// <summary>Squadrons do not count toward the fleet, so a fleet below the minimum must buy ships.</summary>
        public bool NeedsMinimumFleet(int shipCount) => shipCount < MINIMUM_FLEET_SIZE;

        public float CalculateShipPriority(
            EnemyStrategicState state,
            int shipCount,
            int reservedCount,
            int buildTime,
            int unitCapacity,
            int maxCount)
        {
            // Scarce flagship types (e.g. Rothana, Malevolence) never win the balance below,
            // so an established fleet without one builds it first.
            if (shipCount >= MINIMUM_FLEET_SIZE && reservedCount == 0 &&
                maxCount <= FLAGSHIP_MAX_COUNT)
            {
                return FLAGSHIP_PRIORITY;
            }

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

            // No ship type is unlocked (Empire at level 1): levelling up is the only way to a fleet.
            if (!snapshot.CanOrderShips && IsLevelDue(snapshot, EnemyAiDifficultyProfile.Get(snapshot.Difficulty)) &&
                snapshot.CanLevelUp)
            {
                return EnemyProductionCategory.Level;
            }

            if (NeedsMinimumFleet(snapshot.ShipCount) && snapshot.CanOrderShips && snapshot.CanBuildShip)
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

            // Income research is cheap and pays for itself, so it follows the economic floor.
            if (snapshot.CanResearchIncome)
            {
                return EnemyProductionCategory.Research;
            }

            if (snapshot.DefensePlatformCount < snapshot.DefensePlatformTarget &&
                snapshot.CanBuildDefense)
            {
                return EnemyProductionCategory.Defense;
            }

            // Cheap units are always affordable, so a due level must be saved for or it is never reached.
            // Without any ship type the AI buys squadrons while it saves instead of idling.
            if (IsLevelDue(snapshot, profile))
            {
                if (snapshot.CanLevelUp)
                {
                    return EnemyProductionCategory.Level;
                }

                if (snapshot.CanOrderShips)
                {
                    return EnemyProductionCategory.None;
                }
            }

            if (snapshot.StrategicState == EnemyStrategicState.Hold)
            {
                if (snapshot.Difficulty >= EnemyAiDifficulty.Hard && snapshot.CanLevelUp)
                {
                    return EnemyProductionCategory.Level;
                }

                if (snapshot.Difficulty >= EnemyAiDifficulty.Medium && snapshot.CanResearchCombat)
                {
                    return EnemyProductionCategory.Research;
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

            if (snapshot.CanLevelUp)
            {
                return EnemyProductionCategory.Level;
            }

            return snapshot.CanResearchCombat
                ? EnemyProductionCategory.Research
                : EnemyProductionCategory.None;
        }

        private static EnemyProductionCategory EvaluateUltraHard(
            EnemyProductionSnapshot snapshot)
        {
            // Without any ship type at this level the rebuild cannot succeed, so the AI moves on to economy,
            // levelling and squadrons instead of waiting forever.
            if (snapshot.ShipCount == 0 && snapshot.CanOrderShips)
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

            if (snapshot.CanResearchIncome)
            {
                return EnemyProductionCategory.Research;
            }

            if (IsLevelDue(snapshot, EnemyAiDifficultyProfile.Get(snapshot.Difficulty)) &&
                snapshot.CanLevelUp)
            {
                return EnemyProductionCategory.Level;
            }

            if (snapshot.ShipsOrdered >= snapshot.CurrentFactionLevel && snapshot.CanResearchCombat &&
                !ShouldSaveForLevel(snapshot, EnemyAiDifficultyProfile.Get(snapshot.Difficulty)))
            {
                return EnemyProductionCategory.Research;
            }

            if (snapshot.DefensePlatformCount < snapshot.DefensePlatformTarget &&
                snapshot.HasDefenseOption)
            {
                return snapshot.CanBuildDefense
                    ? EnemyProductionCategory.Defense
                    : EnemyProductionCategory.None;
            }

            if (ShouldSaveForLevel(snapshot, EnemyAiDifficultyProfile.Get(snapshot.Difficulty)))
            {
                return EnemyProductionCategory.None;
            }

            if (snapshot.HasShipOption)
            {
                return snapshot.CanBuildShip
                    ? EnemyProductionCategory.Ship
                    : EnemyProductionCategory.None;
            }

            return EnemyProductionCategory.None;
        }

        /// <summary>
        /// A level is due once <see cref="EnemyAiDifficultyProfile.ShipOrdersPerLevel"/> ships per current level
        /// were ordered, or at once when no ship type can be ordered at the current level; a base under attack
        /// keeps buying units instead.
        /// </summary>
        private static bool IsLevelDue(
            EnemyProductionSnapshot snapshot,
            EnemyAiDifficultyProfile profile)
        {
            return snapshot.HasLevelUpOption &&
                   snapshot.StrategicState != EnemyStrategicState.DefendBase &&
                   (!snapshot.CanOrderShips ||
                    snapshot.ShipsOrdered >= snapshot.CurrentFactionLevel * profile.ShipOrdersPerLevel);
        }

        /// <summary>A due level is saved for while ships can still be ordered; without ships, squadrons are bought.</summary>
        private static bool ShouldSaveForLevel(
            EnemyProductionSnapshot snapshot,
            EnemyAiDifficultyProfile profile) =>
            snapshot.CanOrderShips && IsLevelDue(snapshot, profile);
    }
}
