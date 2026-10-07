using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.Stations;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class EnemyProductionDecisionModelTests
    {
        [TestCase(EnemyAiDifficulty.Easy, 0)]
        [TestCase(EnemyAiDifficulty.Medium, 1)]
        [TestCase(EnemyAiDifficulty.Hard, 2)]
        [TestCase(EnemyAiDifficulty.UltraHard, 1)]
        public void DepletedFleet_RebuildsBeforeInfrastructureAndTechnology(
            EnemyAiDifficulty difficulty,
            int shipCount)
        {
            EnemyProductionCategory result = new EnemyProductionDecisionModel().Evaluate(
                new EnemyProductionSnapshot(
                    EnemyStrategicState.DefendBase, difficulty,
                    0, shipCount, 100, 0, 5, 5, 5,
                    true, true, true, true, true, true, true, true));

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Ship));
        }

        // The strategy restricts counters to ships below this size; squadrons cannot end the rebuild.
        [TestCase(0, true)]
        [TestCase(2, true)]
        [TestCase(3, false)]
        public void NeedsMinimumFleet_UntilThreeShips(int shipCount, bool expected)
        {
            Assert.That(new EnemyProductionDecisionModel().NeedsMinimumFleet(shipCount), Is.EqualTo(expected));
        }

        [TestCase(EnemyAiDifficulty.Easy)]
        [TestCase(EnemyAiDifficulty.Medium)]
        [TestCase(EnemyAiDifficulty.Hard)]
        public void AtEconomicFloor_ResearchesIncomeBeforeShips(EnemyAiDifficulty difficulty)
        {
            EnemyProductionCategory result = EvaluateResearch(
                EnemyStrategicState.CaptureZone, difficulty, canResearchIncome: true, canResearchCombat: true);

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Research));
        }

        [Test]
        public void BelowMinimumFleet_BuildsShipBeforeIncomeResearch()
        {
            EnemyProductionCategory result = new EnemyProductionDecisionModel().Evaluate(
                new EnemyProductionSnapshot(
                    EnemyStrategicState.CaptureZone, EnemyAiDifficulty.Medium,
                    1, 0, 0, 1, 1, 1, 1,
                    true, true, true, true, true, true, true, false, true, true));

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Ship));
        }

        [TestCase(EnemyAiDifficulty.Easy, EnemyProductionCategory.Ship)]
        [TestCase(EnemyAiDifficulty.Medium, EnemyProductionCategory.Research)]
        public void HoldState_ResearchesCombatFromMediumDifficulty(
            EnemyAiDifficulty difficulty,
            EnemyProductionCategory expected)
        {
            EnemyProductionCategory result = EvaluateResearch(
                EnemyStrategicState.Hold, difficulty, canResearchIncome: false, canResearchCombat: true);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void NothingElseAffordable_ResearchesCombat()
        {
            EnemyProductionCategory result = new EnemyProductionDecisionModel().Evaluate(
                new EnemyProductionSnapshot(
                    EnemyStrategicState.CaptureZone, EnemyAiDifficulty.Easy,
                    1, 3, 2, 1, 1, 1, 1,
                    true, true, false, false, true, false, true, false, false, true));

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Research));
        }

        // An unaffordable due level is saved for; spending on research would keep the AI at its level.
        [TestCase(false, EnemyProductionCategory.None)]
        [TestCase(true, EnemyProductionCategory.Level)]
        public void UltraHardDueTechnology_LevelsBeforeCombatResearch(
            bool canLevelUp,
            EnemyProductionCategory expected)
        {
            EnemyProductionCategory result = new EnemyProductionDecisionModel().Evaluate(
                new EnemyProductionSnapshot(
                    EnemyStrategicState.HuntFleet, EnemyAiDifficulty.UltraHard,
                    3, 3, 5, 1, 2, 3, 1,
                    true, true, true, true, true, true, true, canLevelUp, false, true));

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(EnemyAiDifficulty.Easy, 3)]
        [TestCase(EnemyAiDifficulty.Medium, 2)]
        [TestCase(EnemyAiDifficulty.Hard, 2)]
        public void DueLevel_IsBoughtOrSavedForInsteadOfCheapUnits(EnemyAiDifficulty difficulty, int shipsOrdered)
        {
            EnemyProductionDecisionModel model = new EnemyProductionDecisionModel();
            int miningFloor = EnemyAiDifficultyProfile.Get(difficulty).MinimumMiningFacilities;

            EnemyProductionCategory affordable = model.Evaluate(new EnemyProductionSnapshot(
                EnemyStrategicState.HuntFleet, difficulty,
                miningFloor, 3, shipsOrdered, 1, 1, miningFloor, 1,
                true, true, true, true, true, true, true, true));
            EnemyProductionCategory unaffordable = model.Evaluate(new EnemyProductionSnapshot(
                EnemyStrategicState.HuntFleet, difficulty,
                miningFloor, 3, shipsOrdered, 1, 1, miningFloor, 1,
                true, true, true, true, true, true, true, false));
            EnemyProductionCategory notDue = model.Evaluate(new EnemyProductionSnapshot(
                EnemyStrategicState.HuntFleet, difficulty,
                miningFloor, 3, shipsOrdered - 1, 1, 1, miningFloor, 1,
                true, true, true, true, true, true, true, false));

            Assert.That(affordable, Is.EqualTo(EnemyProductionCategory.Level));
            Assert.That(unaffordable, Is.EqualTo(EnemyProductionCategory.None));
            Assert.That(notDue, Is.EqualTo(EnemyProductionCategory.Ship));
        }

        // Empire has no level-1 ship; with relays blocking structures the AI used to wait forever at level 1.
        [TestCase(EnemyAiDifficulty.Medium)]
        [TestCase(EnemyAiDifficulty.UltraHard)]
        public void NoShipAtCurrentLevel_LevelsUpInsteadOfWaiting(EnemyAiDifficulty difficulty)
        {
            EnemyProductionCategory result = new EnemyProductionDecisionModel().Evaluate(
                new EnemyProductionSnapshot(
                    EnemyStrategicState.RebuildFleet, difficulty,
                    0, 0, 0, 0, 1, 3, 1,
                    false, false, false, false, false, false, true, true, canOrderShips: false));

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Level));
        }

        // Ship category here is the squadron the strategy picked because no ship type is unlocked.
        [TestCase(EnemyAiDifficulty.Medium)]
        [TestCase(EnemyAiDifficulty.UltraHard)]
        public void NoShipAtCurrentLevel_BuysSquadronsWhileLevelIsUnaffordable(EnemyAiDifficulty difficulty)
        {
            EnemyProductionCategory result = new EnemyProductionDecisionModel().Evaluate(
                new EnemyProductionSnapshot(
                    EnemyStrategicState.RebuildFleet, difficulty,
                    0, 0, 0, 0, 1, 3, 1,
                    false, true, true, false, false, false, true, false, canOrderShips: false));

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Ship));
        }

        [Test]
        public void BaseUnderAttack_KeepsBuyingShipsWhileLevelIsDue()
        {
            EnemyProductionCategory result = new EnemyProductionDecisionModel().Evaluate(
                new EnemyProductionSnapshot(
                    EnemyStrategicState.DefendBase, EnemyAiDifficulty.Medium,
                    1, 3, 10, 1, 1, 1, 1,
                    true, true, true, true, true, true, true, false));

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Ship));
        }

        [Test]
        public void UltraHardAtMiningTarget_ResearchesIncomeBeforeLevel()
        {
            EnemyProductionCategory result = new EnemyProductionDecisionModel().Evaluate(
                new EnemyProductionSnapshot(
                    EnemyStrategicState.HuntFleet, EnemyAiDifficulty.UltraHard,
                    3, 3, 5, 1, 2, 3, 1,
                    true, true, true, true, true, true, true, true, true, true));

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Research));
        }

        [Test]
        public void CaptureFleet_BalancesQuickShipsWithSlowerReinforcements()
        {
            EnemyProductionDecisionModel model = new EnemyProductionDecisionModel();
            float firstLight = model.CalculateShipPriority(
                EnemyStrategicState.CaptureZone, 3, 0, 5, 2, 20);
            float firstMedium = model.CalculateShipPriority(
                EnemyStrategicState.CaptureZone, 3, 0, 10, 4, 20);
            float firstHeavy = model.CalculateShipPriority(
                EnemyStrategicState.CaptureZone, 3, 0, 25, 6, 20);
            float sixthLight = model.CalculateShipPriority(
                EnemyStrategicState.CaptureZone, 7, 5, 5, 2, 20);
            float thirdMedium = model.CalculateShipPriority(
                EnemyStrategicState.CaptureZone, 7, 2, 10, 4, 20);

            Assert.That(firstLight, Is.LessThan(firstMedium));
            Assert.That(firstMedium, Is.LessThan(firstHeavy));
            Assert.That(firstHeavy, Is.LessThan(sixthLight));
            Assert.That(firstHeavy, Is.LessThan(thirdMedium));
        }

        [Test]
        public void EstablishedFleet_BuildsMissingFlagshipFirst()
        {
            EnemyProductionDecisionModel model = new EnemyProductionDecisionModel();
            float firstLight = model.CalculateShipPriority(
                EnemyStrategicState.CaptureZone, 3, 0, 5, 1, 20);
            float flagship = model.CalculateShipPriority(
                EnemyStrategicState.CaptureZone, 3, 0, 90, 5, 1);
            float flagshipBeforeFleet = model.CalculateShipPriority(
                EnemyStrategicState.CaptureZone, 2, 0, 90, 5, 1);
            float secondFlagship = model.CalculateShipPriority(
                EnemyStrategicState.CaptureZone, 4, 1, 35, 8, 3);

            Assert.That(flagship, Is.LessThan(firstLight));
            Assert.That(flagshipBeforeFleet, Is.GreaterThan(firstLight));
            Assert.That(secondFlagship, Is.GreaterThan(firstLight));
        }

        [TestCase(EnemyAiDifficulty.Easy, 1)]
        [TestCase(EnemyAiDifficulty.Medium, 1)]
        [TestCase(EnemyAiDifficulty.Hard, 2)]
        [TestCase(EnemyAiDifficulty.UltraHard, 3)]
        public void BelowEconomicFloor_BuildsMiningBeforeMoreShips(
            EnemyAiDifficulty difficulty,
            int minimumMiningFacilities)
        {
            EnemyProductionCategory result = Evaluate(
                EnemyStrategicState.HuntFleet,
                difficulty,
                minimumMiningFacilities - 1,
                true,
                true,
                true,
                true,
                true);

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Mining));
        }

        [TestCase(EnemyAiDifficulty.Easy, 1)]
        [TestCase(EnemyAiDifficulty.Medium, 1)]
        [TestCase(EnemyAiDifficulty.Hard, 2)]
        [TestCase(EnemyAiDifficulty.UltraHard, 3)]
        public void BelowEconomicFloor_SavesForMiningInsteadOfBuyingAffordableShip(
            EnemyAiDifficulty difficulty,
            int minimumMiningFacilities)
        {
            EnemyProductionCategory result = Evaluate(
                EnemyStrategicState.HuntFleet,
                difficulty,
                minimumMiningFacilities - 1,
                true,
                true,
                false,
                true,
                false);

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.None));
        }

        [TestCase(EnemyAiDifficulty.Easy, 1)]
        [TestCase(EnemyAiDifficulty.Medium, 1)]
        [TestCase(EnemyAiDifficulty.Hard, 2)]
        [TestCase(EnemyAiDifficulty.UltraHard, 3)]
        public void AtEconomicFloor_DoesNotBuildAdditionalMiningFacility(
            EnemyAiDifficulty difficulty,
            int minimumMiningFacilities)
        {
            EnemyProductionCategory result = Evaluate(
                EnemyStrategicState.Hold,
                difficulty,
                minimumMiningFacilities,
                true,
                false,
                true,
                false,
                false);

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.None));
        }

        [Test]
        public void HardDefenseState_PrioritizesDefenseAfterEconomicFloor()
        {
            EnemyProductionCategory result = Evaluate(
                EnemyStrategicState.DefendBase,
                EnemyAiDifficulty.Hard,
                2,
                true,
                true,
                true,
                true,
                true);

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Defense));
        }

        [Test]
        public void EasyDefenseState_ReplacesMissingPlatformAfterEconomicFloor()
        {
            EnemyProductionCategory result = Evaluate(
                EnemyStrategicState.DefendBase,
                EnemyAiDifficulty.Easy,
                1,
                true,
                true,
                true,
                true,
                true);

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Defense));
        }

        [Test]
        public void HardHoldState_PrioritizesTechnologyAfterEconomicFloor()
        {
            EnemyProductionCategory result = Evaluate(
                EnemyStrategicState.Hold,
                EnemyAiDifficulty.UltraHard,
                3,
                true,
                true,
                true,
                false,
                true);

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Level));
        }

        [Test]
        public void UltraHardZeroFleet_BuildsAffordableRecoveryShip()
        {
            EnemyProductionCategory result = EvaluateUltraHard(
                shipCount: 0,
                shipsOrdered: 0,
                miningFacilityCount: 0,
                miningFacilityTarget: 3,
                hasShipOption: true,
                canBuildShip: true,
                hasMiningOption: true,
                canBuildMining: true,
                hasLevelUpOption: true,
                canLevelUp: true,
                defensePlatformCount: 0,
                defensePlatformTarget: 1,
                hasDefenseOption: true,
                canBuildDefense: true);

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Ship));
        }

        [Test]
        public void UltraHardZeroFleet_SeedsMiningWhenRecoveryShipIsUnaffordable()
        {
            EnemyProductionCategory result = EvaluateUltraHard(
                shipCount: 0,
                shipsOrdered: 0,
                miningFacilityCount: 0,
                miningFacilityTarget: 3,
                hasShipOption: true,
                canBuildShip: false,
                hasMiningOption: true,
                canBuildMining: true,
                hasLevelUpOption: true,
                canLevelUp: true,
                defensePlatformCount: 0,
                defensePlatformTarget: 1,
                hasDefenseOption: true,
                canBuildDefense: true);

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Mining));
        }

        [Test]
        public void UltraHardExpandsMiningBeforeDueTechnology()
        {
            EnemyProductionCategory result = EvaluateUltraHard(
                shipCount: 3,
                shipsOrdered: 2,
                miningFacilityCount: 3,
                miningFacilityTarget: 4,
                hasShipOption: true,
                canBuildShip: true,
                hasMiningOption: true,
                canBuildMining: true,
                hasLevelUpOption: true,
                canLevelUp: true,
                defensePlatformCount: 0,
                defensePlatformTarget: 1,
                hasDefenseOption: true,
                canBuildDefense: true);

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Mining));
        }

        [Test]
        public void UltraHardDueTechnology_ReplacesDefenseWhenUpgradeIsUnaffordable()
        {
            EnemyProductionCategory result = EvaluateUltraHard(
                shipCount: 3,
                shipsOrdered: 2,
                miningFacilityCount: 3,
                miningFacilityTarget: 3,
                hasShipOption: true,
                canBuildShip: true,
                hasMiningOption: true,
                canBuildMining: true,
                hasLevelUpOption: true,
                canLevelUp: false,
                defensePlatformCount: 0,
                defensePlatformTarget: 1,
                hasDefenseOption: true,
                canBuildDefense: true);

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Defense));
        }

        [Test]
        public void UltraHardPreferredShipUnaffordable_DoesNotBuyFallbackCategory()
        {
            EnemyProductionCategory result = EvaluateUltraHard(
                shipCount: 1,
                shipsOrdered: 0,
                miningFacilityCount: 3,
                miningFacilityTarget: 3,
                hasShipOption: true,
                canBuildShip: false,
                hasMiningOption: true,
                canBuildMining: true,
                hasLevelUpOption: true,
                canLevelUp: true,
                defensePlatformCount: 1,
                defensePlatformTarget: 1,
                hasDefenseOption: true,
                canBuildDefense: true);

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.None));
        }

        private static EnemyProductionCategory Evaluate(
            EnemyStrategicState state,
            EnemyAiDifficulty difficulty,
            int miningFacilityCount,
            bool hasMiningOption,
            bool canBuildShip,
            bool canBuildMining,
            bool canBuildDefense,
            bool canLevelUp)
        {
            return new EnemyProductionDecisionModel().Evaluate(
                new EnemyProductionSnapshot(
                    state,
                    difficulty,
                    miningFacilityCount,
                    3,
                    1,
                    0,
                    1,
                    EnemyAiDifficultyProfile.Get(difficulty).MinimumMiningFacilities,
                    1,
                    hasMiningOption,
                    canBuildShip,
                    canBuildShip,
                    canBuildMining,
                    canBuildDefense,
                    canBuildDefense,
                    canLevelUp,
                    canLevelUp));
        }

        /// <summary>Established fleet at the economic floor and defense target; ships and defense are affordable, level is not.</summary>
        private static EnemyProductionCategory EvaluateResearch(
            EnemyStrategicState state,
            EnemyAiDifficulty difficulty,
            bool canResearchIncome,
            bool canResearchCombat)
        {
            int miningFloor = EnemyAiDifficultyProfile.Get(difficulty).MinimumMiningFacilities;
            return new EnemyProductionDecisionModel().Evaluate(
                new EnemyProductionSnapshot(
                    state, difficulty,
                    miningFloor, 3, 3, 1, 1, miningFloor, 1,
                    true, true, true, true, true, true, false, false,
                    canResearchIncome, canResearchCombat));
        }

        private static EnemyProductionCategory EvaluateUltraHard(
            int shipCount,
            int shipsOrdered,
            int miningFacilityCount,
            int miningFacilityTarget,
            bool hasShipOption,
            bool canBuildShip,
            bool hasMiningOption,
            bool canBuildMining,
            bool hasLevelUpOption,
            bool canLevelUp,
            int defensePlatformCount,
            int defensePlatformTarget,
            bool hasDefenseOption,
            bool canBuildDefense)
        {
            return new EnemyProductionDecisionModel().Evaluate(
                new EnemyProductionSnapshot(
                    EnemyStrategicState.HuntFleet,
                    EnemyAiDifficulty.UltraHard,
                    miningFacilityCount,
                    shipCount,
                    shipsOrdered,
                    defensePlatformCount,
                    2,
                    miningFacilityTarget,
                    defensePlatformTarget,
                    hasMiningOption,
                    hasShipOption,
                    canBuildShip,
                    canBuildMining,
                    hasDefenseOption,
                    canBuildDefense,
                    hasLevelUpOption,
                    canLevelUp));
        }
    }

    public sealed class EnemyProductionStrategyTests
    {
        private const string SHARED_FACTION_DATA_PATH = "Assets/Settings/Data/Factions/Shared/";

        private const BindingFlags PRIVATE_INSTANCE =
            BindingFlags.Instance | BindingFlags.NonPublic;

        private const int MAX_UNIT_CAPACITY = 60;

        [TestCase(
            EnemyAiDifficulty.Easy,
            ShipType.Arquitens,
            ShipType.Venator)]
        [TestCase(
            EnemyAiDifficulty.Medium,
            ShipType.Arquitens,
            ShipType.Venator)]
        [TestCase(
            EnemyAiDifficulty.Hard,
            ShipType.Venator,
            ShipType.Arquitens)]
        [TestCase(
            EnemyAiDifficulty.UltraHard,
            ShipType.Venator,
            ShipType.Arquitens)]
        public void PreferredShipAtLimit_SelectsAnotherAvailableShip(
            EnemyAiDifficulty difficulty,
            ShipType preferredShip,
            ShipType expectedShip)
        {
            ReinforcementData reinforcementData =
                ScriptableObject.CreateInstance<ReinforcementData>();
            FactionDefinition definition =
                CopyDefinition(FactionType.Republic, ShipType.Arquitens, ShipType.Venator);

            try
            {
                EnemyFactionModel factionModel = CreateFactionModel(definition);
                factionModel.CurrentLevel = 5;
                SetBackingField(
                    reinforcementData,
                    nameof(ReinforcementData.MaxUnitCapacity),
                    MAX_UNIT_CAPACITY);
                PlayerSlot owner = TestPlayers.CreateDuel(difficulty).Get(TestPlayers.Enemy);

                EnemyUnitLimitModel unitLimitModel = new EnemyUnitLimitModel();
                ReserveEconomicFloor(
                    factionModel,
                    unitLimitModel,
                    EnemyAiDifficultyProfile.Get(difficulty)
                        .MinimumMiningFacilities);
                if (difficulty == EnemyAiDifficulty.UltraHard)
                {
                    ReserveDefensePlatform(factionModel, unitLimitModel);
                }

                FactionData preferredData =
                    factionModel.ShipFactionData[preferredShip];
                SetBackingField(
                    preferredData,
                    nameof(FactionData.MaxCount),
                    1);
                Assert.That(
                    unitLimitModel.TryReserve(
                        GetUnitId<ShipUnitRequest>(preferredShip.ToString()),
                        preferredData.MaxCount,
                        preferredData.UnitCapacity,
                        MAX_UNIT_CAPACITY),
                    Is.True);

                RecordingPurchaseProcessor purchaseProcessor =
                    new RecordingPurchaseProcessor();
                EnemyProductionStrategy strategy = new EnemyProductionStrategy(
                    factionModel: factionModel,
                    research: new FactionResearchModel(CreateRoster(definition)),
                    purchaseProcessor: purchaseProcessor,
                    requestFactory: new UnitRequestFactory(),
                    economyModel: new EconomyModelStub(10000f),
                    stateProvider: new StateProviderStub(),
                    decisionModel: new EnemyProductionDecisionModel(),
                    unitLimitModel: unitLimitModel,
                    reinforcementData: reinforcementData,
                    enemyStructurePlacementService: new StructurePlacementServiceStub(),
                    stationRegistry: new OperationalStationRegistry(),
                    playerRoster: TestPlayers.CreateDuel(difficulty),
                    // No hostile units exist, so the size-based selection runs and no profile is ever rated.
                    forceBuilder: new ForceCompositionBuilder(new EntityLocator(),
                        new UnitCombatProfileCatalog(null, null, null, null)),
                    profileCatalog: new UnitCombatProfileCatalog(null, null, null, null),
                    counterModel: new EnemyCounterProductionModel(new NeutralShipClassMatchups()),
                    intelRegistry: new TeamIntelRegistry(),
                    owner: owner);

                strategy.Start();
                strategy.Tick(0f);

                Assert.That(
                    purchaseProcessor.LastRequest,
                    Is.TypeOf<ShipUnitRequest>());
                Assert.That(
                    ((ShipUnitRequest)purchaseProcessor.LastRequest).Key,
                    Is.EqualTo(expectedShip));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(reinforcementData);
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [TestCase(5000f, ShipType.Recusant)]
        [TestCase(1500f, null)]
        [TestCase(0f, null)]
        public void UltraHardRebuild_SavesForPriorityShipAndRechecksLosses(
            float money,
            ShipType? expectedShip)
        {
            ReinforcementData reinforcementData =
                ScriptableObject.CreateInstance<ReinforcementData>();
            FactionDefinition definition =
                CopyDefinition(FactionType.Separatist, ShipType.Munificent, ShipType.Recusant);

            try
            {
                EnemyFactionModel factionModel = CreateFactionModel(definition);
                factionModel.CurrentLevel = 5;
                SetBackingField(
                    reinforcementData,
                    nameof(ReinforcementData.MaxUnitCapacity),
                    MAX_UNIT_CAPACITY);
                PlayerSlot owner = TestPlayers.CreateDuel(EnemyAiDifficulty.UltraHard).Get(TestPlayers.Enemy);

                EnemyUnitLimitModel unitLimitModel = new EnemyUnitLimitModel();
                ReserveEconomicFloor(factionModel, unitLimitModel, 3);
                ReserveDefensePlatform(factionModel, unitLimitModel);
                FactionData existingShipData =
                    factionModel.ShipFactionData[ShipType.Munificent];
                Assert.That(
                    unitLimitModel.TryReserve(
                        GetUnitId<ShipUnitRequest>(ShipType.Munificent.ToString()),
                        existingShipData.MaxCount,
                        existingShipData.UnitCapacity,
                        MAX_UNIT_CAPACITY),
                    Is.True);

                RecordingPurchaseProcessor purchaseProcessor =
                    new RecordingPurchaseProcessor();
                EnemyProductionStrategy strategy = new EnemyProductionStrategy(
                    factionModel: factionModel,
                    // All research is done so only the ship rebuild competes for money.
                    research: CreateCompletedResearch(definition),
                    purchaseProcessor: purchaseProcessor,
                    requestFactory: new UnitRequestFactory(),
                    economyModel: new EconomyModelStub(money),
                    stateProvider: new StateProviderStub(),
                    decisionModel: new EnemyProductionDecisionModel(),
                    unitLimitModel: unitLimitModel,
                    reinforcementData: reinforcementData,
                    enemyStructurePlacementService: new StructurePlacementServiceStub(),
                    stationRegistry: new OperationalStationRegistry(),
                    playerRoster: TestPlayers.CreateDuel(EnemyAiDifficulty.UltraHard),
                    // No hostile units exist, so the size-based selection runs and no profile is ever rated.
                    forceBuilder: new ForceCompositionBuilder(new EntityLocator(),
                        new UnitCombatProfileCatalog(null, null, null, null)),
                    profileCatalog: new UnitCombatProfileCatalog(null, null, null, null),
                    counterModel: new EnemyCounterProductionModel(new NeutralShipClassMatchups()),
                    intelRegistry: new TeamIntelRegistry(),
                    owner: owner);

                strategy.Start();
                strategy.Tick(0f);

                if (expectedShip.HasValue)
                {
                    Assert.That(
                        purchaseProcessor.LastRequest,
                        Is.TypeOf<ShipUnitRequest>());
                    Assert.That(
                        ((ShipUnitRequest)purchaseProcessor.LastRequest).Key,
                        Is.EqualTo(expectedShip.Value));

                    strategy.Tick(0f);
                    Assert.That(purchaseProcessor.RequestCount, Is.EqualTo(1));

                    unitLimitModel.Release(
                        GetUnitId<ShipUnitRequest>(ShipType.Munificent.ToString()),
                        existingShipData.UnitCapacity);
                    strategy.Tick(0f);

                    Assert.That(purchaseProcessor.RequestCount, Is.EqualTo(2));
                    Assert.That(
                        ((ShipUnitRequest)purchaseProcessor.LastRequest).Key,
                        Is.EqualTo(ShipType.Munificent));
                }
                else
                {
                    Assert.That(purchaseProcessor.LastRequest, Is.Null);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(reinforcementData);
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void EconomicFloorReached_BuysAffordableIncomeResearchOncePerLine()
        {
            ReinforcementData reinforcementData = ScriptableObject.CreateInstance<ReinforcementData>();
            FactionDefinition definition = CopyDefinition(FactionType.Empire);

            try
            {
                EnemyFactionModel factionModel = CreateFactionModel(definition);
                FactionResearchModel research = new FactionResearchModel(CreateRoster(definition));
                SetBackingField(reinforcementData, nameof(ReinforcementData.MaxUnitCapacity), MAX_UNIT_CAPACITY);
                PlayerSlot owner = TestPlayers.CreateDuel(EnemyAiDifficulty.Medium).Get(TestPlayers.Enemy);
                EnemyUnitLimitModel unitLimitModel = new EnemyUnitLimitModel();
                ReserveEconomicFloor(factionModel, unitLimitModel,
                    EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.Medium).MinimumMiningFacilities);
                ReserveDefensePlatform(factionModel, unitLimitModel);
                Assert.That(research.TryGetNextTier(ResearchType.IncreasedProduction, out ResearchTierData tier),
                    Is.True);

                // No ships are listed, so the minimum-fleet rule cannot claim the purchase.
                RecordingPurchaseProcessor purchaseProcessor = new RecordingPurchaseProcessor();
                EnemyProductionStrategy strategy = new EnemyProductionStrategy(
                    factionModel: factionModel,
                    research: research,
                    purchaseProcessor: purchaseProcessor,
                    requestFactory: new UnitRequestFactory(),
                    economyModel: new EconomyModelStub(tier.FactionData.Price),
                    stateProvider: new StateProviderStub(),
                    decisionModel: new EnemyProductionDecisionModel(),
                    unitLimitModel: unitLimitModel,
                    reinforcementData: reinforcementData,
                    enemyStructurePlacementService: new StructurePlacementServiceStub(),
                    stationRegistry: new OperationalStationRegistry(),
                    playerRoster: TestPlayers.CreateDuel(EnemyAiDifficulty.Medium),
                    forceBuilder: new ForceCompositionBuilder(new EntityLocator(),
                        new UnitCombatProfileCatalog(null, null, null, null)),
                    profileCatalog: new UnitCombatProfileCatalog(null, null, null, null),
                    counterModel: new EnemyCounterProductionModel(new NeutralShipClassMatchups()),
                    intelRegistry: new TeamIntelRegistry(),
                    owner: owner);

                strategy.Start();
                strategy.Tick(0f);

                Assert.That(purchaseProcessor.LastRequest, Is.TypeOf<ResearchUnitRequest>());
                ResearchUnitRequest request = (ResearchUnitRequest)purchaseProcessor.LastRequest;
                Assert.That(request.Key, Is.EqualTo(ResearchType.IncreasedProduction));
                Assert.That(request.Tier, Is.EqualTo(1));

                // While the tier is in progress its reservation keeps the AI from buying the line again.
                Assert.That(unitLimitModel.TryReserve(UnitLimitKey.From(request),
                    request.FactionData.MaxCount, request.FactionData.UnitCapacity, MAX_UNIT_CAPACITY), Is.True);
                strategy.Tick(10f);

                Assert.That(purchaseProcessor.RequestCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(reinforcementData);
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        private static void ReserveEconomicFloor(
            EnemyFactionModel factionModel,
            EnemyUnitLimitModel unitLimitModel,
            int minimumMiningFacilities)
        {
            FactionData miningData =
                factionModel.MiningFactions[MiningFacilityType.CommonMiner];
            for (int i = 0; i < minimumMiningFacilities; i++)
            {
                Assert.That(
                    unitLimitModel.TryReserve(
                        GetUnitId<MiningFacilityUnitRequest>(
                            MiningFacilityType.CommonMiner.ToString()),
                        miningData.MaxCount,
                        miningData.UnitCapacity,
                        MAX_UNIT_CAPACITY),
                    Is.True);
            }
        }

        private static void ReserveDefensePlatform(
            EnemyFactionModel factionModel,
            EnemyUnitLimitModel unitLimitModel)
        {
            foreach (KeyValuePair<DefendPlatformType, FactionData> option
                     in factionModel.DefendPlatforms)
            {
                Assert.That(
                    unitLimitModel.TryReserve(
                        GetUnitId<DefendPlatformUnitRequest>(option.Key.ToString()),
                        option.Value.MaxCount,
                        option.Value.UnitCapacity,
                        MAX_UNIT_CAPACITY),
                    Is.True);
                return;
            }

            Assert.Fail("Expected at least one defense platform option.");
        }

        private static UnitLimitKey GetUnitId<TRequest>(string requestId)
        {
            return UnitLimitKey.For<TRequest>(requestId);
        }

        /// <summary>
        /// Copies the faction so tests may edit its data, keeping only the given ships so new
        /// units or rebalanced stats in the live catalog do not change which ship wins.
        /// </summary>
        private static FactionDefinition CopyDefinition(FactionType factionType, params ShipType[] ships)
        {
            FactionDefinition definition =
                UnityEngine.Object.Instantiate(LoadSharedData<FactionCatalog>().Get(factionType));
            foreach (ShipType shipType in new List<ShipType>(definition.Ships.Keys))
            {
                if (Array.IndexOf(ships, shipType) < 0)
                {
                    definition.Ships.Remove(shipType);
                }
            }

            Assert.That(definition.Ships.Count, Is.EqualTo(ships.Length));
            return definition;
        }

        private static EnemyFactionModel CreateFactionModel(FactionDefinition definition)
        {
            return new EnemyFactionModel(LoadSharedData<StationLevelData>(), CreateRoster(definition));
        }

        private static FactionResearchModel CreateCompletedResearch(FactionDefinition definition)
        {
            FactionResearchModel research = new FactionResearchModel(CreateRoster(definition));
            foreach (ResearchType researchType in research.ResearchTypes)
            {
                while (research.TryGetNextTier(researchType, out _))
                {
                    research.Complete(researchType);
                }
            }

            return research;
        }

        private static FactionRoster CreateRoster(FactionDefinition definition)
        {
            return new FactionRoster(
                definition,
                LoadSharedData<MiningFacilityCatalog>(),
                LoadSharedData<DefendPlatformCatalog>(),
                LoadSharedData<SuperWeaponCatalog>());
        }

        private static T LoadSharedData<T>() where T : ScriptableObject
        {
            T data = AssetDatabase.LoadAssetAtPath<T>(SHARED_FACTION_DATA_PATH + typeof(T).Name + ".asset");
            Assert.That(data, Is.Not.Null, typeof(T).Name);
            return data;
        }

        private static void SetBackingField<T>(
            object target,
            string propertyName,
            T value)
        {
            FieldInfo field = target.GetType().GetField(
                $"<{propertyName}>k__BackingField",
                PRIVATE_INSTANCE);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private sealed class EconomyModelStub : IEconomyModelObserver
        {
            public event Action<float> OnMoneyChanged
            {
                add { }
                remove { }
            }

            public float Money { get; }

            public EconomyModelStub(float money)
            {
                Money = money;
            }
        }

        private sealed class StateProviderStub : IEnemyAiStateProvider
        {
            public int ActiveShipCount => 1;
            public EnemyStrategicState CurrentState =>
                EnemyStrategicState.RebuildFleet;
        }

        private sealed class StructurePlacementServiceStub :
            IEnemyStructurePlacementService
        {
            public void RecordDestroyedPosition(Vector3 position)
            {
            }

            public void Reset()
            {
            }

            public bool TryGetPosition(out Vector3 position)
            {
                position = Vector3.zero;
                return true;
            }

            public bool TryGetScoutTarget(out Vector3 position)
            {
                position = default;
                return false;
            }
        }

        private sealed class OperationalStationRegistry : IStationRegistry
        {
            public string Id => nameof(OperationalStationRegistry);

            public bool IsStationOperational(PlayerId owner) => true;

            public bool TryGetLivingStation(PlayerId owner, out IEntity station) =>
                throw new NotImplementedException();
        }

        private sealed class RecordingPurchaseProcessor : IEnemyPurchaseProcessor
        {
            public UnitRequest LastRequest { get; private set; }
            public int RequestCount { get; private set; }

            public void Purchase(UnitRequest request)
            {
                LastRequest = request;
                RequestCount++;
            }
        }
    }
}
