using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Utils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyProductionDecisionModelTests
    {
        [TestCase(true, EnemyProductionCategory.Ship)]
        [TestCase(false, EnemyProductionCategory.None)]
        public void CriticalStation_OnlyBuysCombatUnitsThatCanArrive(bool canBuildShip, EnemyProductionCategory expected)
        {
            EnemyProductionSnapshot snapshot = Snapshot(
                mines: 0, ships: 10, shipsOrdered: 100, level: 5, critical: true, canBuildShip: canBuildShip);

            Assert.That(new EnemyProductionDecisionModel().Evaluate(snapshot), Is.EqualTo(expected));
        }

        [TestCase(0, true, false, EnemyProductionCategory.Mining)]
        [TestCase(4, true, true, EnemyProductionCategory.Mining)]
        [TestCase(4, false, false, EnemyProductionCategory.None)]
        public void BelowMiningTarget_ReservesCreditsForMinersBeforeFleetAndTechnology(int mines, bool affordable,
            bool critical, EnemyProductionCategory expected)
        {
            EnemyProductionSnapshot snapshot = Snapshot(mines: mines, level: 1, ships: 0, shipsOrdered: 0,
                canBuildMining: affordable, openingShip: true, critical: critical, opening: true);

            Assert.That(new EnemyProductionDecisionModel().Evaluate(snapshot), Is.EqualTo(expected));
        }

        [TestCase(true, true, EnemyProductionCategory.Ship)]
        [TestCase(false, true, EnemyProductionCategory.Level)]
        [TestCase(false, false, EnemyProductionCategory.None)]
        public void OpeningLevel_OneAvailableCorvetteThenEarliestLevelUp(bool openingShip, bool upgradeAffordable,
            EnemyProductionCategory expected)
        {
            EnemyProductionSnapshot snapshot = Snapshot(level: 1, ships: 0, shipsOrdered: openingShip ? 0 : 1,
                canLevelUp: upgradeAffordable, openingShip: openingShip);

            Assert.That(new EnemyProductionDecisionModel().Evaluate(snapshot), Is.EqualTo(expected));
        }

        [Test]
        public void OpeningWithoutCorvette_TakesLevelTwoAsSoonAsAffordable()
        {
            EnemyProductionSnapshot snapshot = Snapshot(level: 1, ships: 0, shipsOrdered: 0, canOrderShips: false);

            Assert.That(new EnemyProductionDecisionModel().Evaluate(snapshot), Is.EqualTo(EnemyProductionCategory.Level));
        }

        [Test]
        public void OpeningUpgradeInProgress_BuysSquadrons()
        {
            EnemyProductionSnapshot snapshot = Snapshot(level: 1, ships: 1, shipsOrdered: 1,
                hasLevelOption: false, canOrderShips: false);

            Assert.That(new EnemyProductionDecisionModel().Evaluate(snapshot), Is.EqualTo(EnemyProductionCategory.Ship));
        }

        [TestCase(0, true)]
        [TestCase(2, true)]
        [TestCase(3, false)]
        public void NeedsMinimumFleet_UntilThreeShips(int shipCount, bool expected)
        {
            Assert.That(new EnemyProductionDecisionModel().NeedsMinimumFleet(shipCount), Is.EqualTo(expected));
        }

        [Test]
        public void DepletedFleet_RebuildsWhenEconomicFloorIsMetAndNoUpgradeDue()
        {
            EnemyProductionSnapshot snapshot = Snapshot(ships: 2, shipsOrdered: 2,
                state: EnemyStrategicState.DefendBase, incomeResearch: true, platforms: 0);

            Assert.That(new EnemyProductionDecisionModel().Evaluate(snapshot), Is.EqualTo(EnemyProductionCategory.Ship));
        }

        [Test]
        public void DepletedFleet_SavesForSelectedShip()
        {
            EnemyProductionSnapshot snapshot = Snapshot(ships: 1, canBuildShip: false, platforms: 0,
                incomeResearch: true);

            Assert.That(new EnemyProductionDecisionModel().Evaluate(snapshot), Is.EqualTo(EnemyProductionCategory.None));
        }

        [TestCase(true, EnemyProductionCategory.Level)]
        [TestCase(false, EnemyProductionCategory.None)]
        public void DueLevel_IsBoughtOrSavedForBeforeOptionalUnits(bool canLevelUp, EnemyProductionCategory expected)
        {
            EnemyProductionSnapshot snapshot = Snapshot(shipsOrdered: 2, canLevelUp: canLevelUp,
                incomeResearch: true, combatResearch: true, platforms: 0);

            Assert.That(new EnemyProductionDecisionModel().Evaluate(snapshot), Is.EqualTo(expected));
        }

        [TestCase(true, false, false, EnemyProductionCategory.Research)]
        [TestCase(false, true, false, EnemyProductionCategory.Defense)]
        [TestCase(false, false, true, EnemyProductionCategory.Research)]
        [TestCase(false, false, false, EnemyProductionCategory.Ship)]
        public void StableFleet_UsesSharedTechnologyAndDefensePriorities(bool incomeResearch, bool missingDefense,
            bool combatResearch, EnemyProductionCategory expected)
        {
            EnemyProductionSnapshot snapshot = Snapshot(state: EnemyStrategicState.Hold, incomeResearch: incomeResearch,
                combatResearch: combatResearch, platforms: missingDefense ? 0 : 1);

            Assert.That(new EnemyProductionDecisionModel().Evaluate(snapshot), Is.EqualTo(expected));
        }

        [Test]
        public void NoShipOptionAndNothingElseAffordable_ResearchesCombat()
        {
            EnemyProductionSnapshot snapshot = Snapshot(hasShipOption: false, canBuildShip: false,
                hasLevelOption: false, canLevelUp: false, canBuildDefense: false, combatResearch: true);

            Assert.That(new EnemyProductionDecisionModel().Evaluate(snapshot), Is.EqualTo(EnemyProductionCategory.Research));
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
        public void CaptureFleet_DoesNotPreferAQuickShipThatFightsPoorlyForItsPrice()
        {
            EnemyProductionDecisionModel model = new EnemyProductionDecisionModel();
            // A transport builds faster than a frigate but delivers a third of its value per credit.
            float transport = model.CalculateShipPriority(
                EnemyStrategicState.CaptureZone, 3, 0, 20, 1, 20, combatValuePerCredit: 0.1f);
            float frigate = model.CalculateShipPriority(
                EnemyStrategicState.CaptureZone, 3, 0, 30, 3, 40, combatValuePerCredit: 0.3f);

            Assert.That(frigate, Is.LessThan(transport));
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

        [Test]
        public void ScarceSmallShip_IsNotTreatedAsFlagship()
        {
            EnemyProductionDecisionModel model = new EnemyProductionDecisionModel();
            // IPV-2C: limit 1 like a hero ship, but a 2-capacity corvette.
            float corvette = model.CalculateShipPriority(
                EnemyStrategicState.CaptureZone, 3, 0, 18, 2, 1);

            Assert.That(corvette, Is.GreaterThan(0f));
        }

        private static EnemyProductionSnapshot Snapshot(
            EnemyStrategicState state = EnemyStrategicState.CaptureZone,
            int mines = 5, int ships = 3, int shipsOrdered = 1, int platforms = 1, int level = 2,
            bool hasShipOption = true, bool canBuildShip = true, bool canBuildMining = true,
            bool hasLevelOption = true, bool canLevelUp = true, bool canBuildDefense = true,
            bool incomeResearch = false, bool combatResearch = false, bool canOrderShips = true,
            bool critical = false, bool openingShip = false, bool opening = false) =>
            new EnemyProductionSnapshot(state, mines, ships, shipsOrdered, platforms, level, 5, 1,
                hasMiningOption: true, hasShipOption: hasShipOption, canBuildShip: canBuildShip,
                canBuildMining: canBuildMining, canBuildDefense: canBuildDefense, hasLevelUpOption: hasLevelOption,
                canLevelUp: canLevelUp, canResearchIncome: incomeResearch, canResearchCombat: combatResearch,
                canOrderShips: canOrderShips, isStationCritical: critical, canBuyOpeningShip: openingShip,
                isOpening: opening);
    }

    [Category("EnemyAi")]
    public sealed class EnemyProductionStrategyTests
    {
        private const string SHARED_FACTION_DATA_PATH = "Assets/Settings/Data/Factions/Shared/";

        private const BindingFlags PRIVATE_INSTANCE =
            BindingFlags.Instance | BindingFlags.NonPublic;

        private const int MAX_UNIT_CAPACITY = 60;

        [TestCase(EnemyAiDifficulty.Easy)]
        [TestCase(EnemyAiDifficulty.Medium)]
        [TestCase(EnemyAiDifficulty.Hard)]
        [TestCase(EnemyAiDifficulty.UltraHard)]
        public void Opening_BuysAllMinersThenAtMostOneCorvetteAndStartsLevelTwo(EnemyAiDifficulty difficulty)
        {
            var reinforcementData = ScriptableObject.CreateInstance<ReinforcementData>();
            FactionDefinition definition = CopyDefinition(FactionType.Separatist, ShipType.PatrolFrigate, ShipType.C9979);
            try
            {
                EnemyFactionModel factionModel = CreateFactionModel(definition);
                var unitLimits = new EnemyUnitLimitModel();
                var purchases = new RecordingPurchaseProcessor();
                SetBackingField(reinforcementData, nameof(ReinforcementData.MaxUnitCapacity), MAX_UNIT_CAPACITY);
                var scheduler = new EnemyAiScheduler(new ReplayNotifier<BattleState>(BattleState.Running));
                scheduler.Initialize();
                var strategy = new EnemyProductionStrategy(
                    factionModel: factionModel,
                    research: CreateCompletedResearch(definition),
                    purchaseProcessor: purchases,
                    requestFactory: new UnitRequestFactory(),
                    economyModel: new EconomyModelStub(10000f),
                    stateProvider: new StateProviderStub(),
                    decisionModel: new EnemyProductionDecisionModel(),
                    unitLimitModel: unitLimits,
                    reinforcementData: reinforcementData,
                    enemyStructurePlacementService: new StructurePlacementServiceStub(),
                    productionAccess: new OperationalProductionAccess(),
                    forceBuilder: null,
                    profileCatalog: UniformValueCatalog(definition, ShipClass.Corvette),
                    counterModel: null,
                    intelRegistry: new TeamIntelRegistry(),
                    owner: TestPlayers.CreateDuel(difficulty).Get(TestPlayers.Enemy),
                    stationPressure: new EnemyStationPressureTracker(),
                    scheduler: scheduler);

                strategy.Start();
                Assert.That(strategy.OpeningMiningPending, Is.True);
                int miningLimit = factionModel.MiningFactions.First().Value.MaxCount;
                for (int i = 0; i < miningLimit; i++)
                {
                    scheduler.Step(10f);
                    Assert.That(purchases.LastRequest, Is.TypeOf<MiningFacilityUnitRequest>());
                    UnitRequest request = purchases.LastRequest;
                    unitLimits.Adopt(UnitLimitKey.From(request), request.FactionData.UnitCapacity);
                }
                Assert.That(strategy.OpeningMiningPending, Is.False);

                scheduler.Step(10f);
                Assert.That(purchases.LastRequest, Is.TypeOf<ShipUnitRequest>());
                // A destroyed first corvette still counts as bought for the level-one cap.
                unitLimits.RecordShipOrder();
                scheduler.Step(10f);
                Assert.That(purchases.LastRequest, Is.TypeOf<LevelUnitRequest>());
                Assert.That(((LevelUnitRequest)purchases.LastRequest).Key, Is.EqualTo(1));

                purchases.IsUpgradingStation = true;
                strategy.PriorityConstructionPending = true;
                int purchaseCount = purchases.RequestCount;
                scheduler.Step(10f);
                Assert.That(purchases.RequestCount, Is.EqualTo(purchaseCount));

                strategy.PriorityConstructionPending = false;
                scheduler.Step(10f);
                Assert.That(purchases.LastRequest, Is.TypeOf<SquadronUnitRequest>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(reinforcementData);
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

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
                ReserveEconomicFloor(factionModel, unitLimitModel, factionModel.MiningFactions.First().Value.MaxCount);
                ReserveDefensePlatform(factionModel, unitLimitModel);

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
                var entities = new EntityLocator();
                var scheduler = new EnemyAiScheduler(new ReplayNotifier<BattleState>(BattleState.Running));
                scheduler.Initialize();
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
                    productionAccess: new OperationalProductionAccess(),
                    // No hostile units exist, so the size-based selection runs; every ship rates the same per credit.
                    forceBuilder: new ForceCompositionBuilder(entities,
                        new UnitCombatProfileCatalog(null, null, null, null), new AiWorldSnapshot(entities)),
                    profileCatalog: UniformValueCatalog(definition),
                    counterModel: new EnemyCounterProductionModel(new NeutralShipClassMatchups()),
                    intelRegistry: new TeamIntelRegistry(),
                    owner: owner, stationPressure: new EnemyStationPressureTracker(),
                    scheduler: scheduler);

                strategy.Start();
                strategy.Tick(0f);
                scheduler.Step(0f);

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

        [TestCase(5000f, ShipType.Recusant, false)]
        [TestCase(1500f, null, false)]
        [TestCase(0f, null, false)]
        [TestCase(5000f, null, true)]
        public void UltraHardRebuild_SavesForPriorityShipAndRechecksLosses(
            float money,
            ShipType? expectedShip,
            bool capacityBlocked)
        {
            ReinforcementData reinforcementData =
                ScriptableObject.CreateInstance<ReinforcementData>();
            FactionDefinition definition =
                CopyDefinition(FactionType.Separatist, ShipType.Munificent, ShipType.Recusant);
            SetBackingField(definition.Ships[ShipType.Munificent], nameof(FactionData.BuildTime), 20);
            SetBackingField(definition.Ships[ShipType.Recusant], nameof(FactionData.BuildTime), 30);

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
                ReserveEconomicFloor(factionModel, unitLimitModel, factionModel.MiningFactions.First().Value.MaxCount);
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
                var entities = new EntityLocator();
                var scheduler = new EnemyAiScheduler(new ReplayNotifier<BattleState>(BattleState.Running));
                scheduler.Initialize();
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
                    productionAccess: new OperationalProductionAccess(),
                    // No hostile units exist, so the size-based selection runs; every ship rates the same per credit.
                    forceBuilder: new ForceCompositionBuilder(entities,
                        new UnitCombatProfileCatalog(null, null, null, null), new AiWorldSnapshot(entities)),
                    profileCatalog: UniformValueCatalog(definition),
                    counterModel: new EnemyCounterProductionModel(new NeutralShipClassMatchups()),
                    intelRegistry: new TeamIntelRegistry(),
                    owner: owner, stationPressure: new EnemyStationPressureTracker(),
                    scheduler: scheduler);

                UnitLimitKey capacityHolder = GetUnitId<ShipUnitRequest>("capacity-holder");
                int heldCapacity = MAX_UNIT_CAPACITY - unitLimitModel.CurrentUnitCapacity -
                    existingShipData.UnitCapacity;
                if (capacityBlocked)
                {
                    Assert.That(factionModel.ShipFactionData[ShipType.Recusant].UnitCapacity,
                        Is.GreaterThan(existingShipData.UnitCapacity));
                    unitLimitModel.Adopt(capacityHolder, heldCapacity);
                }

                strategy.Start();
                strategy.Tick(0f);
                scheduler.Step(0f);

                if (expectedShip.HasValue)
                {
                    Assert.That(
                        purchaseProcessor.LastRequest,
                        Is.TypeOf<ShipUnitRequest>());
                    Assert.That(
                        ((ShipUnitRequest)purchaseProcessor.LastRequest).Key,
                        Is.EqualTo(expectedShip.Value));

                    strategy.Tick(0f);
                    scheduler.Step(0f);
                    Assert.That(purchaseProcessor.RequestCount, Is.EqualTo(1));

                    unitLimitModel.Release(
                        GetUnitId<ShipUnitRequest>(ShipType.Munificent.ToString()),
                        existingShipData.UnitCapacity);
                    strategy.Tick(0f);
                    scheduler.Step(0f);

                    Assert.That(purchaseProcessor.RequestCount, Is.EqualTo(2));
                    Assert.That(
                        ((ShipUnitRequest)purchaseProcessor.LastRequest).Key,
                        Is.EqualTo(ShipType.Munificent));
                }
                else
                {
                    Assert.That(purchaseProcessor.LastRequest, Is.Null);
                    if (capacityBlocked)
                    {
                        unitLimitModel.Release(capacityHolder, heldCapacity);
                        strategy.Tick(0f);
                        scheduler.Step(0f);
                        Assert.That(purchaseProcessor.LastRequest, Is.TypeOf<ShipUnitRequest>());
                        Assert.That(((ShipUnitRequest)purchaseProcessor.LastRequest).Key,
                            Is.EqualTo(ShipType.Recusant));
                    }
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
                factionModel.CurrentLevel = 5;
                FactionResearchModel research = new FactionResearchModel(CreateRoster(definition));
                SetBackingField(reinforcementData, nameof(ReinforcementData.MaxUnitCapacity), MAX_UNIT_CAPACITY);
                PlayerSlot owner = TestPlayers.CreateDuel(EnemyAiDifficulty.Medium).Get(TestPlayers.Enemy);
                EnemyUnitLimitModel unitLimitModel = new EnemyUnitLimitModel();
                ReserveEconomicFloor(factionModel, unitLimitModel, factionModel.MiningFactions.First().Value.MaxCount);
                ReserveDefensePlatform(factionModel, unitLimitModel);
                Assert.That(research.TryGetNextTier(ResearchType.IncreasedProduction, out ResearchTierData tier),
                    Is.True);

                // No ships are listed, so the minimum-fleet rule cannot claim the purchase.
                RecordingPurchaseProcessor purchaseProcessor = new RecordingPurchaseProcessor();
                var entities = new EntityLocator();
                var scheduler = new EnemyAiScheduler(new ReplayNotifier<BattleState>(BattleState.Running));
                scheduler.Initialize();
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
                    productionAccess: new OperationalProductionAccess(),
                    forceBuilder: new ForceCompositionBuilder(entities,
                        new UnitCombatProfileCatalog(null, null, null, null), new AiWorldSnapshot(entities)),
                    profileCatalog: new UnitCombatProfileCatalog(null, null, null, null),
                    counterModel: new EnemyCounterProductionModel(new NeutralShipClassMatchups()),
                    intelRegistry: new TeamIntelRegistry(),
                    owner: owner, stationPressure: new EnemyStationPressureTracker(),
                    scheduler: scheduler);

                strategy.Start();
                strategy.Tick(0f);
                scheduler.Step(0f);

                Assert.That(purchaseProcessor.LastRequest, Is.TypeOf<ResearchUnitRequest>());
                ResearchUnitRequest request = (ResearchUnitRequest)purchaseProcessor.LastRequest;
                Assert.That(request.Key, Is.EqualTo(ResearchType.IncreasedProduction));
                Assert.That(request.Tier, Is.EqualTo(1));

                // While the tier is in progress its reservation keeps the AI from buying the line again.
                Assert.That(unitLimitModel.TryReserve(UnitLimitKey.From(request),
                    request.FactionData.MaxCount, request.FactionData.UnitCapacity, MAX_UNIT_CAPACITY), Is.True);
                strategy.Tick(10f);
                scheduler.Step(10f);

                Assert.That(purchaseProcessor.Purchases.OfType<ResearchUnitRequest>().Count(bought => bought.Key == request.Key),
                    Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(reinforcementData);
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [TestCase(false, true)]
        [TestCase(true, true)]
        [TestCase(true, false)]
        public void CriticalStation_BuysSquadronOnlyWhenItCanArriveInTime(bool hasUnlockedShip, bool timely)
        {
            ReinforcementData reinforcementData = ScriptableObject.CreateInstance<ReinforcementData>();
            FactionDefinition definition = CopyDefinition(FactionType.Republic,
                hasUnlockedShip ? new[] { ShipType.Arquitens } : Array.Empty<ShipType>());
            try
            {
                EnemyFactionModel factionModel = CreateFactionModel(definition);
                factionModel.CurrentLevel = 5;
                SetBackingField(reinforcementData, nameof(ReinforcementData.MaxUnitCapacity), MAX_UNIT_CAPACITY);
                var catalog = new UnitCombatProfileCatalog(null, null, null, null);
                var profiles = (Dictionary<UnitTypeId, UnitCombatProfile>)typeof(UnitCombatProfileCatalog)
                    .GetField("_profiles", PRIVATE_INSTANCE).GetValue(catalog);
                float[] rates = new float[ForceComposition.ClassSlots];
                rates[(int)ShipClass.Capital] = 100f;
                var profile = new UnitCombatProfile(ShipClass.Bomber, 100f, 0f, rates,
                    new float[ForceComposition.ClassSlots], new float[ForceComposition.ClassSlots],
                    Array.Empty<UnitCombatProfile>());
                foreach (var ship in factionModel.ShipFactionData)
                {
                    SetBackingField(ship.Value, nameof(FactionData.BuildTime), 1000);
                    profiles.Add(UnitTypeId.Ship(ship.Key), profile);
                }
                foreach (var squadron in factionModel.SquadronFactionData)
                {
                    SetBackingField(squadron.Value, nameof(FactionData.BuildTime), timely ? 2 : 5);
                    profiles.Add(UnitTypeId.Squadron(squadron.Key), profile);
                }
                Assert.That(factionModel.SquadronFactionData.Count, Is.GreaterThan(0));
                var pressure = new EnemyStationPressureTracker();
                pressure.Update(1, 1000f, 1000f, 0f);
                pressure.Update(1, 499f, 1000f, 1f);
                pressure.Update(1, 490f, 1000f, 4f);
                Assert.That(pressure.IsStationCritical, Is.True);
                var purchases = new RecordingPurchaseProcessor();
                var entities = new EntityLocator();
                var scheduler = new EnemyAiScheduler(new ReplayNotifier<BattleState>(BattleState.Running));
                scheduler.Initialize();
                var strategy = new EnemyProductionStrategy(
                    factionModel: factionModel,
                    research: CreateCompletedResearch(definition),
                    purchaseProcessor: purchases,
                    requestFactory: new UnitRequestFactory(),
                    economyModel: new EconomyModelStub(10000f),
                    stateProvider: new StateProviderStub(),
                    decisionModel: new EnemyProductionDecisionModel(),
                    unitLimitModel: new EnemyUnitLimitModel(),
                    reinforcementData: reinforcementData,
                    enemyStructurePlacementService: new StructurePlacementServiceStub(),
                    productionAccess: new OperationalProductionAccess(),
                    forceBuilder: new ForceCompositionBuilder(entities, catalog, new AiWorldSnapshot(entities)),
                    profileCatalog: catalog,
                    counterModel: new EnemyCounterProductionModel(new NeutralShipClassMatchups()),
                    intelRegistry: new TeamIntelRegistry(),
                    owner: TestPlayers.CreateDuel(EnemyAiDifficulty.UltraHard).Get(TestPlayers.Enemy),
                    stationPressure: pressure,
                    scheduler: scheduler);

                strategy.Start();
                strategy.Tick(0f);
                scheduler.Step(0f);

                if (timely)
                    Assert.That(purchases.LastRequest, Is.TypeOf<SquadronUnitRequest>());
                else
                    Assert.That(purchases.RequestCount, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(reinforcementData);
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void StationlessRecovery_DoesNotOrderAReplacementStation()
        {
            var reinforcementData = ScriptableObject.CreateInstance<ReinforcementData>();
            FactionDefinition definition = CopyDefinition(FactionType.Republic, Array.Empty<ShipType>());
            try
            {
                var purchases = new RecordingPurchaseProcessor();
                var scheduler = new EnemyAiScheduler(new ReplayNotifier<BattleState>(BattleState.Running));
                scheduler.Initialize();
                var strategy = new EnemyProductionStrategy(
                    factionModel: CreateFactionModel(definition), research: null,
                    purchaseProcessor: purchases, requestFactory: new UnitRequestFactory(),
                    economyModel: new EconomyModelStub(10000f), stateProvider: new StateProviderStub(),
                    decisionModel: new EnemyProductionDecisionModel(), unitLimitModel: new EnemyUnitLimitModel(),
                    reinforcementData: reinforcementData, enemyStructurePlacementService: new StructurePlacementServiceStub(),
                    productionAccess: new OperationalProductionAccess { CanProduce = false },
                    forceBuilder: null, profileCatalog: null, counterModel: null, intelRegistry: new TeamIntelRegistry(),
                    owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy), stationPressure: new EnemyStationPressureTracker(),
                    scheduler: scheduler);

                strategy.Start();
                strategy.Tick(0f);
                scheduler.Step(0f);

                Assert.That(purchases.RequestCount, Is.Zero);
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
            KeyValuePair<MiningFacilityType, FactionData> mining = factionModel.MiningFactions.First();
            FactionData miningData = mining.Value;
            for (int i = 0; i < minimumMiningFacilities; i++)
            {
                Assert.That(
                    unitLimitModel.TryReserve(
                        GetUnitId<MiningFacilityUnitRequest>(
                            mining.Key.ToString()),
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

        /// <summary>A catalog where every ship of <paramref name="definition"/> is worth exactly its price.</summary>
        private static UnitCombatProfileCatalog UniformValueCatalog(FactionDefinition definition,
            ShipClass shipClass = ShipClass.Frigate)
        {
            UnitCombatProfileCatalog catalog = new UnitCombatProfileCatalog(null, null, null, null);
            var profiles = (Dictionary<UnitTypeId, UnitCombatProfile>)typeof(UnitCombatProfileCatalog)
                .GetField("_profiles", PRIVATE_INSTANCE).GetValue(catalog);
            foreach (KeyValuePair<ShipType, FactionData> ship in definition.Ships)
            {
                // Combat value is sqrt(durability x average warship damage): price x price gives value = price.
                float[] rates = new float[ForceComposition.ClassSlots];
                foreach (ShipClass target in new[] { ShipClass.Corvette, ShipClass.Frigate, ShipClass.Cruiser,
                             ShipClass.HeavyCruiser, ShipClass.Capital, ShipClass.BattleCruiser, ShipClass.Dreadnought })
                    rates[(int)target] = ship.Value.Price;
                profiles.Add(UnitTypeId.Ship(ship.Key), new UnitCombatProfile(shipClass, ship.Value.Price, 0f,
                    rates, new float[ForceComposition.ClassSlots], new float[ForceComposition.ClassSlots],
                    Array.Empty<UnitCombatProfile>()));
            }

            return catalog;
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

            public bool TryGetReinforcementRally(out Vector3 rally, out bool defendHome)
            {
                rally = default;
                defendHome = false;
                return false;
            }
        }

        private sealed class StructurePlacementServiceStub :
            IEnemyStructurePlacementService
        {
            public bool IsRecovering => false;
            public void RecordPlacement(string request, Vector3 position) { }
            public void RecordDeferred(string request, float refund) { }
            public void RecordStationBuilt(Vector3 position) { }

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

        private sealed class OperationalProductionAccess : IProductionAccess
        {
            public PlayerId ProductionHost => TestPlayers.Enemy;

            public bool CanProduce { get; set; } = true;
            public bool HasHostBase => CanProduce;

            public bool TryGetProductionBase(out IEntity station) =>
                throw new NotImplementedException();
        }

        private sealed class RecordingPurchaseProcessor : IEnemyPurchaseProcessor
        {
            public List<UnitRequest> Purchases { get; } = new List<UnitRequest>();
            public UnitRequest LastRequest => Purchases.LastOrDefault();
            public int RequestCount => Purchases.Count;
            public bool IsUpgradingStation { get; set; }
            public bool PriorityConstructionPending { get; set; }

            public bool CanQueue(string requestId, int maxCount) => true;
            public float GetQueueRemainingTime(string requestId) => 0f;

            public void ReserveExternalPipeline(string requestId) { }
            public void ReleaseExternalPipeline(string requestId) { }

            public void Purchase(UnitRequest request) => Purchases.Add(request);
        }
    }
}
