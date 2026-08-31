using System;
using System.Reflection;
using EmpireAtWar.Controllers.Factions;
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
        public void EasyDefenseState_PrioritizesShipsAfterEconomicFloor()
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

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Ship));
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
                true,
                true);

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Level));
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
                    hasMiningOption,
                    canBuildShip,
                    canBuildMining,
                    canBuildDefense,
                    canLevelUp));
        }
    }

    public sealed class EnemyProductionStrategyTests
    {
        private const string FACTIONS_MODEL_PATH =
            "Assets/Settings/Data/Models/Factions/FactionsModel.asset";
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
            FactionsModel source =
                AssetDatabase.LoadAssetAtPath<FactionsModel>(FACTIONS_MODEL_PATH);
            Assert.That(source, Is.Not.Null);

            FactionsModel factionsModel = UnityEngine.Object.Instantiate(source);
            EnemyFactionModel factionModel =
                ScriptableObject.CreateInstance<EnemyFactionModel>();
            GameModel gameModel = ScriptableObject.CreateInstance<GameModel>();
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
                    nameof(EnemyFactionModel.FactionType),
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
                    reinforcementData);

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
            public EnemyStrategicState CurrentState =>
                EnemyStrategicState.RebuildFleet;
        }

        private sealed class RecordingPurchaseProcessor : IEnemyPurchaseProcessor
        {
            public UnitRequest LastRequest { get; private set; }

            public IChainHandler<UnitRequest> SetNext(
                IChainHandler<UnitRequest> chainHandler)
            {
                return chainHandler;
            }

            public void Handle(UnitRequest request)
            {
                LastRequest = request;
            }
        }
    }
}
