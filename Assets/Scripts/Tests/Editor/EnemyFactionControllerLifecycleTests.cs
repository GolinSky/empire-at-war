using System;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Models.Players;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Controllers;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.Stations;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Utilities.ScriptUtils.Time;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class EnemyFactionControllerLifecycleTests
    {
        private const BindingFlags PRIVATE_INSTANCE =
            BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void LateDispose_CancelsPendingBuildAndIsIdempotent()
        {
            EnemyFactionModel model =
                new EnemyFactionModel(null, null);
            ReinforcementData reinforcementData =
                ScriptableObject.CreateInstance<ReinforcementData>();

            try
            {
                FactionData factionData = new FactionData();
                SetBackingField(factionData, nameof(FactionData.MaxCount), 1);
                SetBackingField(factionData, nameof(FactionData.BuildTime), 10);
                SetBackingField(factionData, nameof(FactionData.UnitCapacity), 1);
                SetBackingField(
                    reinforcementData,
                    nameof(ReinforcementData.MaxUnitCapacity),
                    10);

                TimerPoolService timerPool =
                    new TimerPoolService();
                EnemyUnitLimitModel unitLimitModel = new EnemyUnitLimitModel();
                TrackingEconomyProvider economyProvider =
                    new TrackingEconomyProvider();
                EnemyFactionController controller = new EnemyFactionController(
                    model: model,
                    shipFactory: null,
                    miningFacilityFactory: null,
                    defendPlatformFactory: null,
                    timerPoolService: timerPool,
                    research: null,
                    economyProvider: economyProvider,
                    wallet: new TrackingWallet(),
                    shipSpawnPoints: null,
                    unitLimitModel: unitLimitModel,
                    reinforcementData: reinforcementData,
                    enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    stationRegistry: new OperationalStationRegistry(),
                    squadronLauncher: null,
                    squadronCommander: null,
                    owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry());

                controller.Initialize();
                controller.Purchase(
                    new ShipUnitRequest(factionData, ShipType.Venator));

                Assert.That(GetActiveTimerCount(timerPool), Is.EqualTo(1));
                Assert.That(unitLimitModel.CurrentUnitCapacity, Is.EqualTo(1));
                Assert.That(unitLimitModel.ShipOrdersCount, Is.EqualTo(1));

                controller.LateDispose();
                controller.LateDispose();

                Assert.That(GetActiveTimerCount(timerPool), Is.Zero);
                Assert.That(unitLimitModel.CurrentUnitCapacity, Is.Zero);
                Assert.That(unitLimitModel.ShipOrdersCount, Is.Zero);
                Assert.That(economyProvider.RemoveCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(reinforcementData);
            }
        }

        [Test]
        public void ScheduledBuildFailure_RefundsOnceAndDoesNotBlockLaterBuilds()
        {
            EnemyFactionModel model =
                new EnemyFactionModel(null, null);
            ReinforcementData reinforcementData =
                ScriptableObject.CreateInstance<ReinforcementData>();

            try
            {
                FactionData factionData = new FactionData();
                SetBackingField(factionData, nameof(FactionData.MaxCount), 1);
                SetBackingField(factionData, nameof(FactionData.BuildTime), 10);
                SetBackingField(factionData, nameof(FactionData.UnitCapacity), 1);
                SetBackingField(
                    reinforcementData,
                    nameof(ReinforcementData.MaxUnitCapacity),
                    10);

                TimerPoolService timerPool =
                    new TimerPoolService();
                EnemyUnitLimitModel unitLimitModel = new EnemyUnitLimitModel();
                TrackingWallet wallet = new TrackingWallet();
                EnemyFactionController controller = new EnemyFactionController(
                    model: model,
                    shipFactory: null,
                    miningFacilityFactory: null,
                    defendPlatformFactory: null,
                    timerPoolService: timerPool,
                    research: null,
                    economyProvider: new TrackingEconomyProvider(),
                    wallet: wallet,
                    shipSpawnPoints: null,
                    unitLimitModel: unitLimitModel,
                    reinforcementData: reinforcementData,
                    enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    stationRegistry: new OperationalStationRegistry(),
                    squadronLauncher: null,
                    squadronCommander: null,
                    owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry());
                ShipUnitRequest request =
                    new ShipUnitRequest(factionData, ShipType.Venator);
                UnitLimitKey unitId = UnitLimitKey.From(request);
                Assert.That(
                    unitLimitModel.TryReserve(unitId, 1, 1, 10),
                    Is.True);
                unitLimitModel.RecordShipOrder();

                LogAssert.Expect(
                    LogType.Error,
                    new Regex(
                        @"^\[EnemyAI:Production\] Build failed for " +
                        @"ShipUnitRequest \(Venator\)\. Purchase refunded\."));
                ScheduleBuild(
                    controller,
                    request,
                    () => throw new InvalidOperationException("build failure"));
                GetOnlyActiveTimer(timerPool).Release(true);

                Assert.That(wallet.RefundCount, Is.EqualTo(1));
                Assert.That(unitLimitModel.ShipOrdersCount, Is.Zero);
                Assert.That(wallet.LastRefunded, Is.SameAs(request));
                Assert.That(unitLimitModel.CurrentUnitCapacity, Is.Zero);
                Assert.That(GetActiveTimerCount(timerPool), Is.Zero);
                Assert.That(GetPendingBuildCount(controller), Is.Zero);

                bool laterBuildExecuted = false;
                ScheduleBuild(controller, request, () => laterBuildExecuted = true);
                GetOnlyActiveTimer(timerPool).Release(true);

                Assert.That(laterBuildExecuted, Is.True);
                Assert.That(GetActiveTimerCount(timerPool), Is.Zero);
                Assert.That(GetPendingBuildCount(controller), Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(reinforcementData);
            }
        }

        [Test]
        public void ResearchPurchase_CompletesTierOnBuildAndFreesTheLine()
        {
            FactionResearchModel research = new FactionResearchModel(new FactionRoster(
                LoadSharedData<FactionCatalog>().Get(FactionType.Empire),
                LoadSharedData<MiningFacilityCatalog>(),
                LoadSharedData<DefendPlatformCatalog>(),
                LoadSharedData<SuperWeaponCatalog>()));
            Assert.That(research.TryGetNextTier(ResearchType.IncreasedProduction, out ResearchTierData tier), Is.True);
            ReinforcementData reinforcementData = ScriptableObject.CreateInstance<ReinforcementData>();

            try
            {
                SetBackingField(reinforcementData, nameof(ReinforcementData.MaxUnitCapacity), 10);
                TimerPoolService timerPool = new TimerPoolService();
                EnemyUnitLimitModel unitLimitModel = new EnemyUnitLimitModel();
                EnemyFactionController controller = new EnemyFactionController(
                    model: new EnemyFactionModel(null, null),
                    shipFactory: null,
                    miningFacilityFactory: null,
                    defendPlatformFactory: null,
                    timerPoolService: timerPool,
                    research: research,
                    economyProvider: new TrackingEconomyProvider(),
                    wallet: new TrackingWallet(),
                    shipSpawnPoints: null,
                    unitLimitModel: unitLimitModel,
                    reinforcementData: reinforcementData,
                    enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    stationRegistry: new OperationalStationRegistry(),
                    squadronLauncher: null,
                    squadronCommander: null,
                    owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry());
                ResearchUnitRequest request =
                    new ResearchUnitRequest(tier.FactionData, ResearchType.IncreasedProduction, 1);

                controller.Purchase(request);

                Assert.That(unitLimitModel.GetReservedCount(UnitLimitKey.From(request)), Is.EqualTo(1));
                Assert.That(research.GetCompletedTiers(ResearchType.IncreasedProduction), Is.Zero);

                GetOnlyActiveTimer(timerPool).Release(true);

                Assert.That(research.GetCompletedTiers(ResearchType.IncreasedProduction), Is.EqualTo(1));
                Assert.That(research.IncomeMultiplier, Is.GreaterThan(1f));
                Assert.That(unitLimitModel.GetReservedCount(UnitLimitKey.From(request)), Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(reinforcementData);
            }
        }

        [Test]
        public void Initialize_ResetsStructurePlacementStateOncePerBattle()
        {
            EnemyFactionModel model =
                new EnemyFactionModel(null, null);

            TrackingStructurePlacement structurePlacement =
                new TrackingStructurePlacement();
            EnemyFactionController controller = new EnemyFactionController(
                model: model,
                shipFactory: null,
                miningFacilityFactory: null,
                defendPlatformFactory: null,
                timerPoolService: new TimerPoolService(),
                research: null,
                economyProvider: new TrackingEconomyProvider(),
                wallet: null,
                shipSpawnPoints: null,
                unitLimitModel: new EnemyUnitLimitModel(),
                reinforcementData: null,
                enemyStructurePlacementService: structurePlacement,
                stationRegistry: new OperationalStationRegistry(),
                squadronLauncher: null,
                squadronCommander: null,
                owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                playerRegistry: new PlayerRegistry());

            controller.Initialize();
            controller.Initialize();

            Assert.That(structurePlacement.ResetCount, Is.EqualTo(1));

            controller.LateDispose();
        }

        private static T LoadSharedData<T>() where T : ScriptableObject
        {
            T data = AssetDatabase.LoadAssetAtPath<T>(
                "Assets/Settings/Data/Factions/Shared/" + typeof(T).Name + ".asset");
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

        private static int GetActiveTimerCount(
            TimerPoolService timerPoolService)
        {
            return GetActiveTimers(timerPoolService).Count;
        }

        private static ICollection GetActiveTimers(
            TimerPoolService timerPoolService)
        {
            FieldInfo activeTimersField = typeof(TimerPoolService).GetField(
                "customCoroutines",
                PRIVATE_INSTANCE);
            Assert.That(activeTimersField, Is.Not.Null);
            ICollection activeTimers =
                activeTimersField.GetValue(timerPoolService) as ICollection;
            Assert.That(activeTimers, Is.Not.Null);
            return activeTimers;
        }

        private static CustomCoroutine GetOnlyActiveTimer(
            TimerPoolService timerPoolService)
        {
            ICollection activeTimers = GetActiveTimers(timerPoolService);
            Assert.That(activeTimers.Count, Is.EqualTo(1));
            foreach (object activeTimer in activeTimers)
            {
                return (CustomCoroutine)activeTimer;
            }

            throw new InvalidOperationException("Expected one active timer.");
        }

        private static void ScheduleBuild(
            EnemyFactionController controller,
            UnitRequest request,
            Action buildAction)
        {
            MethodInfo method = typeof(EnemyFactionController).GetMethod(
                "ScheduleBuild",
                PRIVATE_INSTANCE);
            Assert.That(method, Is.Not.Null);
            method.Invoke(controller, new object[] { request, buildAction });
        }

        private static int GetPendingBuildCount(
            EnemyFactionController controller)
        {
            FieldInfo field = typeof(EnemyFactionController).GetField(
                "_pendingBuilds",
                PRIVATE_INSTANCE);
            Assert.That(field, Is.Not.Null);
            ICollection pendingBuilds = field.GetValue(controller) as ICollection;
            Assert.That(pendingBuilds, Is.Not.Null);
            return pendingBuilds.Count;
        }

        private sealed class UnavailableStructurePlacement : IEnemyStructurePlacementService
        {
            public bool TryGetPosition(out Vector3 position)
            {
                position = default;
                return false;
            }

            public void RecordDestroyedPosition(Vector3 position)
            {
            }

            public void Reset()
            {
            }
        }

        private sealed class TrackingStructurePlacement : IEnemyStructurePlacementService
        {
            public int ResetCount { get; private set; }

            public bool TryGetPosition(out Vector3 position)
            {
                position = default;
                return false;
            }

            public void RecordDestroyedPosition(Vector3 position)
            {
            }

            public void Reset()
            {
                ResetCount++;
            }
        }

        private sealed class OperationalStationRegistry : IStationRegistry
        {
            public string Id => nameof(OperationalStationRegistry);

            public bool IsStationOperational(PlayerId owner) => true;

            public bool TryGetLivingStation(PlayerId owner, out IEntity station) =>
                throw new NotImplementedException();
        }

        private sealed class TrackingEconomyProvider : IEconomyProvider
        {
            public int RemoveCount { get; private set; }

            public void AddProvider(IIncomeProvider incomeProvider)
            {
            }

            public void RemoveProvider(IIncomeProvider incomeProvider)
            {
                RemoveCount++;
            }

            public void RecalculateIncome(IIncomeProvider incomeProvider)
            {
            }
        }

        private sealed class TrackingWallet : IWallet
        {
            public int RefundCount { get; private set; }
            public UnitRequest LastRefunded { get; private set; }

            public bool TrySpend(UnitRequest unitRequest)
            {
                return true;
            }

            public void Refund(UnitRequest unitRequest)
            {
                RefundCount++;
                LastRefunded = unitRequest;
            }
        }
    }
}
