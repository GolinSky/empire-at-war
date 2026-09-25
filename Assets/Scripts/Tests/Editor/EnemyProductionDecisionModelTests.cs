using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Services.Enemy;
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

        [Test]
        public void CaptureFleet_BalancesQuickShipsWithSlowerReinforcements()
        {
            EnemyProductionDecisionModel model = new EnemyProductionDecisionModel();
            float firstLight = model.CalculateShipPriority(
                EnemyStrategicState.CaptureZone, 3, 0, 5, 2);
            float firstMedium = model.CalculateShipPriority(
                EnemyStrategicState.CaptureZone, 3, 0, 10, 4);
            float firstHeavy = model.CalculateShipPriority(
                EnemyStrategicState.CaptureZone, 3, 0, 25, 6);
            float sixthLight = model.CalculateShipPriority(
                EnemyStrategicState.CaptureZone, 7, 5, 5, 2);
            float thirdMedium = model.CalculateShipPriority(
                EnemyStrategicState.CaptureZone, 7, 2, 10, 4);

            Assert.That(firstLight, Is.LessThan(firstMedium));
            Assert.That(firstMedium, Is.LessThan(firstHeavy));
            Assert.That(firstHeavy, Is.LessThan(sixthLight));
            Assert.That(firstHeavy, Is.LessThan(thirdMedium));
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
        private const string FACTIONS_MODEL_PATH =
            "Assets/Settings/Data/Models/Factions/FactionsData.asset";
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
            FactionsData source =
                AssetDatabase.LoadAssetAtPath<FactionsData>(FACTIONS_MODEL_PATH);
            Assert.That(source, Is.Not.Null);

            FactionsData factionsModel = UnityEngine.Object.Instantiate(source);
            EnemyFactionData factionModel =
                ScriptableObject.CreateInstance<EnemyFactionData>();
            GameData gameModel = ScriptableObject.CreateInstance<GameData>();
            ReinforcementData reinforcementData =
                ScriptableObject.CreateInstance<ReinforcementData>();

            try
            {
                SetBackingField(
                    factionModel,
                    "FactionsModel",
                    factionsModel);
                SetBackingField(
                    factionModel,
                    nameof(EnemyFactionData.FactionType),
                    FactionType.Republic);
                SetBackingField(
                    reinforcementData,
                    nameof(ReinforcementData.MaxUnitCapacity),
                    MAX_UNIT_CAPACITY);
                gameModel.EnemyDifficulty = difficulty;

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
                    factionModel,
                    purchaseProcessor,
                    new UnitRequestFactory(),
                    new EconomyModelStub(10000f),
                    new StateProviderStub(),
                    gameModel,
                    new EnemyProductionDecisionModel(),
                    unitLimitModel,
                    reinforcementData,
                    new StructurePlacementServiceStub(),
                    new OperationalEntityLocator());

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
                UnityEngine.Object.DestroyImmediate(factionModel);
                UnityEngine.Object.DestroyImmediate(factionsModel);
                UnityEngine.Object.DestroyImmediate(gameModel);
                UnityEngine.Object.DestroyImmediate(reinforcementData);
            }
        }

        [TestCase(5000f, ShipType.Recusant)]
        [TestCase(1500f, null)]
        [TestCase(0f, null)]
        public void UltraHardRebuild_SavesForPriorityShipAndRechecksLosses(
            float money,
            ShipType? expectedShip)
        {
            FactionsData source =
                AssetDatabase.LoadAssetAtPath<FactionsData>(FACTIONS_MODEL_PATH);
            Assert.That(source, Is.Not.Null);

            FactionsData factionsModel = UnityEngine.Object.Instantiate(source);
            EnemyFactionData factionModel =
                ScriptableObject.CreateInstance<EnemyFactionData>();
            GameData gameModel = ScriptableObject.CreateInstance<GameData>();
            ReinforcementData reinforcementData =
                ScriptableObject.CreateInstance<ReinforcementData>();

            try
            {
                SetBackingField(factionModel, "FactionsModel", factionsModel);
                SetBackingField(
                    factionModel,
                    nameof(EnemyFactionData.FactionType),
                    FactionType.Separatist);
                factionModel.CurrentLevel = 5;
                SetBackingField(
                    reinforcementData,
                    nameof(ReinforcementData.MaxUnitCapacity),
                    MAX_UNIT_CAPACITY);
                gameModel.EnemyDifficulty = EnemyAiDifficulty.UltraHard;

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
                    factionModel,
                    purchaseProcessor,
                    new UnitRequestFactory(),
                    new EconomyModelStub(money),
                    new StateProviderStub(),
                    gameModel,
                    new EnemyProductionDecisionModel(),
                    unitLimitModel,
                    reinforcementData,
                    new StructurePlacementServiceStub(),
                    new OperationalEntityLocator());

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
                UnityEngine.Object.DestroyImmediate(factionModel);
                UnityEngine.Object.DestroyImmediate(factionsModel);
                UnityEngine.Object.DestroyImmediate(gameModel);
                UnityEngine.Object.DestroyImmediate(reinforcementData);
            }
        }

        private static void ReserveEconomicFloor(
            EnemyFactionData factionModel,
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
            EnemyFactionData factionModel,
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

        private static string GetUnitId<TRequest>(string requestId)
        {
            return $"{typeof(TRequest).FullName}:{requestId}";
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
            public EconomyModelStub(float money)
            {
                Money = money;
            }

            public event Action<float> OnMoneyChanged
            {
                add { }
                remove { }
            }

            public float Money { get; }
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
        }

        private sealed class OperationalEntityLocator : IEntityLocator
        {
            public string Id => nameof(OperationalEntityLocator);
            public IReadOnlyCollection<IEntity> Entities => Array.Empty<IEntity>();
            public event Action<IEntity> EntityAdded { add { } remove { } }
            public event Action<IEntity> EntityRemoved { add { } remove { } }
            public bool IsStationOperational(PlayerType playerType) => true;
            public void AddEntity(IEntity entity) { }
            public void RemoveEntity(IEntity entity) { }
            public IEntity GetEntity(long entityId) => throw new NotImplementedException();
            public bool TryGetEntity(long entityId, out IEntity entity) =>
                throw new NotImplementedException();
            public bool TryGetEntity(RaycastHit raycastHit, out IEntity entity)
            {
                entity = null;
                return false;
            }
            public bool TryGetEntity(Collider collider, out IEntity entity)
            {
                entity = null;
                return false;
            }
        }

        private sealed class RecordingPurchaseProcessor : IEnemyPurchaseProcessor
        {
            public UnitRequest LastRequest { get; private set; }
            public int RequestCount { get; private set; }

            public IChainHandler<UnitRequest> SetNext(
                IChainHandler<UnitRequest> chainHandler)
            {
                return chainHandler;
            }

            public void Handle(UnitRequest request)
            {
                LastRequest = request;
                RequestCount++;
            }
        }
    }
}
