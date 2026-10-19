using System;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Models.Players;
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

                EnemyUnitLimitModel unitLimitModel = new EnemyUnitLimitModel();
                TrackingEconomyProvider economyProvider =
                    new TrackingEconomyProvider();
                EnemyFactionController controller = new EnemyFactionController(
                    model: model,
                    shipFactory: null,
                    miningFacilityFactory: null,
                    defendPlatformFactory: null,
                    research: null,
                    economyProvider: economyProvider,
                    wallet: new TrackingWallet(),
                    shipSpawnPoints: null,
                    unitLimitModel: unitLimitModel,
                    reinforcementData: reinforcementData,
                    enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    productionAccess: new OperationalProductionAccess(),
                    squadronLauncher: null,
                    squadronCommander: null,
                    owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry());

                controller.Initialize();
                controller.Purchase(
                    new ShipUnitRequest(factionData, ShipType.Venator));

                Assert.That(controller.PendingBuilds.Count, Is.EqualTo(1));
                Assert.That(controller.PendingBuilds[0].TimeLeft, Is.EqualTo(10f));
                Assert.That(unitLimitModel.CurrentUnitCapacity, Is.EqualTo(1));
                Assert.That(unitLimitModel.ShipOrdersCount, Is.EqualTo(1));

                controller.LateDispose();
                controller.LateDispose();

                Assert.That(controller.PendingBuilds, Is.Empty);
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

                EnemyUnitLimitModel unitLimitModel = new EnemyUnitLimitModel();
                TrackingWallet wallet = new TrackingWallet();
                EnemyFactionController controller = new EnemyFactionController(
                    model: model,
                    shipFactory: null,
                    miningFacilityFactory: null,
                    defendPlatformFactory: null,
                    research: null,
                    economyProvider: new TrackingEconomyProvider(),
                    wallet: wallet,
                    shipSpawnPoints: null,
                    unitLimitModel: unitLimitModel,
                    reinforcementData: reinforcementData,
                    enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    productionAccess: new OperationalProductionAccess(),
                    squadronLauncher: null,
                    squadronCommander: null,
                    owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry());
                ShipUnitRequest request =
                    new ShipUnitRequest(factionData, ShipType.Venator);
                Regex failure = new Regex(
                    @"^\[EnemyAI:Production\] Build failed for " +
                    @"ShipUnitRequest \(Venator\)\. Purchase refunded\.");

                controller.Purchase(request);
                LogAssert.Expect(LogType.Error, failure);
                controller.Advance(10f);

                Assert.That(wallet.RefundCount, Is.EqualTo(1));
                Assert.That(unitLimitModel.ShipOrdersCount, Is.Zero);
                Assert.That(wallet.LastRefunded, Is.SameAs(request));
                Assert.That(unitLimitModel.CurrentUnitCapacity, Is.Zero);
                Assert.That(controller.PendingBuilds, Is.Empty);

                controller.Purchase(request);
                Assert.That(controller.PendingBuilds.Count, Is.EqualTo(1), "A failed build must not block the next purchase.");
                LogAssert.Expect(LogType.Error, failure);
                controller.Advance(10f);

                Assert.That(wallet.RefundCount, Is.EqualTo(2));
                Assert.That(controller.PendingBuilds, Is.Empty);
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
                EnemyUnitLimitModel unitLimitModel = new EnemyUnitLimitModel();
                EnemyFactionController controller = new EnemyFactionController(
                    model: new EnemyFactionModel(null, null),
                    shipFactory: null,
                    miningFacilityFactory: null,
                    defendPlatformFactory: null,
                    research: research,
                    economyProvider: new TrackingEconomyProvider(),
                    wallet: new TrackingWallet(),
                    shipSpawnPoints: null,
                    unitLimitModel: unitLimitModel,
                    reinforcementData: reinforcementData,
                    enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    productionAccess: new OperationalProductionAccess(),
                    squadronLauncher: null,
                    squadronCommander: null,
                    owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry());
                ResearchUnitRequest request =
                    new ResearchUnitRequest(tier.FactionData, ResearchType.IncreasedProduction, 1);

                controller.Purchase(request);

                Assert.That(unitLimitModel.GetReservedCount(UnitLimitKey.From(request)), Is.EqualTo(1));
                Assert.That(research.GetCompletedTiers(ResearchType.IncreasedProduction), Is.Zero);

                controller.Advance(tier.FactionData.BuildTime);

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
                research: null,
                economyProvider: new TrackingEconomyProvider(),
                wallet: null,
                shipSpawnPoints: null,
                unitLimitModel: new EnemyUnitLimitModel(),
                reinforcementData: null,
                enemyStructurePlacementService: structurePlacement,
                productionAccess: new OperationalProductionAccess(),
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

        [Test]
        public void HostedPurchase_RejectsUpgradesAndRefundsQueuedShipAfterHostLoss()
        {
            var data = ScriptableObject.CreateInstance<ReinforcementData>();
            try
            {
                SetBackingField(data, nameof(ReinforcementData.MaxUnitCapacity), 10);
                var faction = new FactionData();
                SetBackingField(faction, nameof(FactionData.MaxCount), 1);
                SetBackingField(faction, nameof(FactionData.BuildTime), 10);
                SetBackingField(faction, nameof(FactionData.UnitCapacity), 1);
                var access = new OperationalProductionAccess { ProductionHost = TestPlayers.Human };
                var wallet = new TrackingWallet();
                var limits = new EnemyUnitLimitModel(); var model = new EnemyFactionModel(null, null);
                var controller = new EnemyFactionController(model: model, shipFactory: null,
                    miningFacilityFactory: null, defendPlatformFactory: null, research: null,
                    economyProvider: new TrackingEconomyProvider(), wallet: wallet, shipSpawnPoints: null,
                    unitLimitModel: limits, reinforcementData: data, enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    productionAccess: access, squadronLauncher: null, squadronCommander: null,
                    owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy), playerRegistry: new PlayerRegistry());
                controller.Purchase(new LevelUnitRequest(faction, 1));
                Assert.That(wallet.SpendCount, Is.Zero);
                Assert.That(controller.ForceUpgradeStation(), Is.False);
                controller.Purchase(new ShipUnitRequest(faction, ShipType.Venator));
                Assert.That(wallet.SpendCount, Is.EqualTo(1));
                access.CanProduce = false;
                controller.Advance(10f);
                Assert.That(wallet.RefundCount, Is.EqualTo(1));
                Assert.That(limits.CurrentUnitCapacity, Is.Zero);
                Assert.That(limits.ShipOrdersCount, Is.Zero);
                controller.LateDispose();
            }
            finally { UnityEngine.Object.DestroyImmediate(data); }
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

        private sealed class UnavailableStructurePlacement : IEnemyStructurePlacementService
        {
            public bool TryGetPosition(out Vector3 position)
            {
                position = default;
                return false;
            }

            public bool TryGetScoutTarget(out Vector3 position)
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

            public bool TryGetScoutTarget(out Vector3 position)
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

        private sealed class OperationalProductionAccess : IProductionAccess
        {
            public PlayerId ProductionHost { get; set; } = TestPlayers.Enemy;

            public bool CanProduce { get; set; } = true;

            public bool TryGetProductionBase(out IEntity station) =>
                throw new NotImplementedException();
        }

        private sealed class TrackingEconomyProvider : IEconomyProvider
        {
            public void AddMoney(float amount) { }

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
            public int SpendCount { get; private set; }
            public UnitRequest LastRefunded { get; private set; }

            public bool TrySpend(UnitRequest unitRequest)
            {
                SpendCount++;
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
