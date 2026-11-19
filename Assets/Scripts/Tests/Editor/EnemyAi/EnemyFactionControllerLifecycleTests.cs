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
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Services.ShipSpawning;
using EmpireAtWar.Ship;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using ShipEntity = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyFactionControllerLifecycleTests
    {
        private const BindingFlags PRIVATE_INSTANCE =
            BindingFlags.Instance | BindingFlags.NonPublic;

        [TestCase(false)]
        [TestCase(true)]
        public void Reserve_HoldsMixedWaveUntilSupportOrImmediateDefense(bool immediateDefense)
        {
            EnemyBuildQueue queue = new EnemyBuildQueue();
            FactionData data = new FactionData();
            var flagship = new ShipUnitRequest(data, ShipType.Tector);
            var escort = new SquadronUnitRequest(data, SquadronType.V19Torrent);
            List<UnitRequest> arrived = new List<UnitRequest>();
            queue.Add(flagship, 2f);
            queue.Advance(2f, arrived.Add, minimumReadyCombatUnits: 2);
            Assert.That(arrived, Is.Empty);
            Assert.That(queue.Builds[0].TimeLeft, Is.Zero);

            queue.Add(escort, 5f);
            int minimumReady = immediateDefense ? 0 : 2;
            queue.Advance(1f, arrived.Add, minimumReady);
            Assert.That(arrived.Count, Is.EqualTo(immediateDefense ? 1 : 0));
            queue.Advance(4f, arrived.Add, minimumReady);
            Assert.That(arrived, Is.EqualTo(new UnitRequest[] { flagship, escort }));
            Assert.That(queue.Builds, Is.Empty);
        }

        [Test]
        public void Reserve_HomeDefenseCanReleaseLoneReadyShipBeforeSlowBatchFinishes()
        {
            EnemyBuildQueue queue = new EnemyBuildQueue();
            FactionData data = new FactionData();
            var flagship = new ShipUnitRequest(data, ShipType.Tector);
            var escort = new ShipUnitRequest(data, ShipType.Venator);
            List<UnitRequest> arrived = new List<UnitRequest>();
            queue.Add(flagship, 2f);
            queue.Add(escort, 30f);

            queue.Advance(7f, arrived.Add, minimumReadyCombatUnits: 2, deploymentWaitLimit: 6f);
            Assert.That(arrived, Is.Empty);
            queue.Advance(1f, arrived.Add, minimumReadyCombatUnits: 2, deploymentWaitLimit: 6f);
            Assert.That(arrived, Is.EqualTo(new UnitRequest[] { flagship }));
            Assert.That(queue.Builds[0].TimeLeft, Is.EqualTo(22f));
        }

        [Test]
        public void Reserve_WaitsWithoutTaskAndReleasesAllReadyUnitsForDefense()
        {
            var queue = new EnemyBuildQueue();
            var data = new FactionData();
            UnitRequest[] reserve =
            {
                new ShipUnitRequest(data, ShipType.Tector),
                new SquadronUnitRequest(data, SquadronType.V19Torrent),
                new ShipUnitRequest(data, ShipType.Venator),
                new SquadronUnitRequest(data, SquadronType.YWing)
            };
            foreach (UnitRequest request in reserve)
                queue.Add(request, 5f);

            var arrived = new List<UnitRequest>();
            queue.Advance(60f, arrived.Add, minimumReadyCombatUnits: int.MaxValue);
            Assert.That(arrived, Is.Empty);
            Assert.That(queue.GetRemainingTime(reserve[0].Id), Is.Zero);
            Assert.That(queue.GetRemainingTime(reserve[1].Id), Is.Zero);
            Assert.That(queue.Builds, Has.Count.EqualTo(reserve.Length));

            queue.Advance(0f, arrived.Add, minimumReadyCombatUnits: 2);
            Assert.That(arrived, Is.EqualTo(reserve));
            Assert.That(queue.Builds, Is.Empty);
        }

        [Test]
        public void Advance_ReleasesReadyShipWhenProductionPipelinesAreOccupied()
        {
            EnemyBuildQueue queue = new EnemyBuildQueue();
            var ship = new ShipUnitRequest(new FactionData(), ShipType.Venator);
            List<UnitRequest> arrived = new List<UnitRequest>();
            queue.Add(ship, 1f);
            queue.Advance(1f, arrived.Add, minimumReadyCombatUnits: 2);
            for (int i = 0; i < PlayerFactionModel.MAX_ACTIVE_PIPELINES; i++)
                queue.ReserveExternalPipeline(i.ToString());

            queue.Advance(1f, arrived.Add);
            Assert.That(arrived, Is.EqualTo(new UnitRequest[] { ship }));
            Assert.That(queue.Builds, Is.Empty);
        }

        [Test]
        public void RetryDeployment_DoesNotOccupyAnotherShipBuildOfTheSameType()
        {
            EnemyBuildQueue queue = new EnemyBuildQueue();
            FactionData data = new FactionData();
            var queued = new ShipUnitRequest(data, ShipType.Venator);
            var blocked = new ShipUnitRequest(data, ShipType.Venator);
            List<UnitRequest> arrived = new List<UnitRequest>();
            queue.Add(queued, 15f);
            queue.RetryShipDeployment(blocked, 1f);

            queue.Advance(1f, arrived.Add);

            Assert.That(arrived, Is.EqualTo(new[] { blocked }));
            Assert.That(queue.Builds.Count, Is.EqualTo(1));
            Assert.That(queue.Builds[0].TimeLeft, Is.EqualTo(14f));
        }

        [Test]
        public void RetryDeployment_AccumulatesBlockedSecondsUntilCleared()
        {
            EnemyBuildQueue queue = new EnemyBuildQueue();
            var ship = new ShipUnitRequest(new FactionData(), ShipType.Venator);

            queue.RetryShipDeployment(ship, 1f);
            queue.RetryShipDeployment(ship, 1f);
            Assert.That(queue.GetDeploymentBlockedSeconds(ship), Is.EqualTo(2f));

            queue.ClearDeploymentBlock(ship);
            Assert.That(queue.GetDeploymentBlockedSeconds(ship), Is.Zero);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void HomeDefense_DeploysCompletedWaveTogetherOrRetriesWithoutUsingDefaultZone(bool defenseAvailable)
        {
            var data = ScriptableObject.CreateInstance<ReinforcementData>();
            var shipObject = new GameObject("Withdrawal reinforcement");
            try
            {
                SetBackingField(data, nameof(ReinforcementData.MaxUnitCapacity), 10);
                var faction = new FactionData();
                SetBackingField(faction, nameof(FactionData.MaxCount), 1);
                SetBackingField(faction, nameof(FactionData.BuildTime), 2);
                SetBackingField(faction, nameof(FactionData.UnitCapacity), 1);
                Vector3 fallback = new Vector3(40f, 0f, -900f);
                var ai = new RallyAiState { Rally = fallback };
                var wallet = new TrackingWallet();
                var limits = new EnemyUnitLimitModel();
                var spawnPoints = new RallyShipSpawnPoints { IsAvailable = defenseAvailable };
                var factory = new TrackingShipFactory(shipObject.AddComponent<ShipEntity>());
                var controller = new EnemyFactionController(aiState: ai, model: new EnemyFactionModel(null, null),
                    shipFactory: factory, miningFacilityFactory: null, defendPlatformFactory: null,
                    research: null,
                    economyProvider: new TrackingEconomyProvider(), wallet: wallet, shipSpawnPoints: spawnPoints,
                    unitLimitModel: limits, reinforcementData: data,
                    enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    productionAccess: new OperationalProductionAccess(), squadronLauncher: null,
                    squadronCommander: null, owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry());
                var request = new ShipUnitRequest(faction, ShipType.Venator);
                var escort = new ShipUnitRequest(faction, ShipType.Recusant);
                controller.Purchase(request);
                controller.Advance(1f);
                controller.Purchase(escort);
                controller.Advance(1f);
                Assert.That(controller.PendingBuilds[0].TimeLeft, Is.Zero);
                Assert.That(factory.Created, Is.Empty);
                Assert.That(spawnPoints.Calls, Is.Empty);

                controller.Advance(1f);

                Assert.That(spawnPoints.Calls, Is.EqualTo(new[] { "defense", "defense" }));
                Assert.That(spawnPoints.Anchor, Is.EqualTo(fallback));
                Assert.That(spawnPoints.Owner, Is.EqualTo(TestPlayers.Enemy));
                Assert.That(wallet.SpendCount, Is.EqualTo(2));
                Assert.That(wallet.RefundCount, Is.Zero);
                Assert.That(limits.CurrentUnitCapacity, Is.EqualTo(2));
                if (!defenseAvailable)
                {
                    Assert.That(factory.Created, Is.Empty);
                    Assert.That(controller.PendingBuilds.Count, Is.EqualTo(2));
                    Assert.That(controller.PendingBuilds[0].Request, Is.SameAs(request));
                    Assert.That(controller.PendingBuilds[0].TimeLeft, Is.Zero);
                    Assert.That(controller.PendingBuilds[1].Request, Is.SameAs(escort));
                    Assert.That(controller.PendingBuilds[1].TimeLeft, Is.Zero);

                    spawnPoints.IsAvailable = true;
                    controller.Advance(0.5f);
                    Assert.That(spawnPoints.Calls.Count, Is.EqualTo(2));
                    controller.Advance(0.5f);
                    Assert.That(spawnPoints.Calls, Is.EqualTo(new[] { "defense", "defense", "defense", "defense" }));
                }

                Assert.That(factory.Created, Is.EqualTo(new[] { ShipType.Venator, ShipType.Recusant }));
                Assert.That(factory.Positions, Is.EqualTo(new[] { fallback + Vector3.right, fallback + Vector3.right }));
                Assert.That(controller.PendingBuilds, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shipObject);
                UnityEngine.Object.DestroyImmediate(data);
            }
        }

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
                EnemyFactionController controller = new EnemyFactionController(aiState: new IdleAiState(),
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
        public void Purchase_FullPipelinesAndSameIdLimitRejectBeforeSpending()
        {
            ReinforcementData reinforcementData = ScriptableObject.CreateInstance<ReinforcementData>();
            try
            {
                SetBackingField(reinforcementData, nameof(ReinforcementData.MaxUnitCapacity), 100);
                FactionData data = new FactionData();
                SetBackingField(data, nameof(FactionData.MaxCount), 2);
                SetBackingField(data, nameof(FactionData.BuildTime), 10);
                SetBackingField(data, nameof(FactionData.UnitCapacity), 1);
                TrackingWallet wallet = new TrackingWallet();
                EnemyUnitLimitModel limits = new EnemyUnitLimitModel();
                EnemyFactionController controller = new EnemyFactionController(aiState: new IdleAiState(),
                    model: new EnemyFactionModel(null, null),
                    shipFactory: null,
                    miningFacilityFactory: null,
                    defendPlatformFactory: null,
                    research: null,
                    economyProvider: new TrackingEconomyProvider(),
                    wallet: wallet,
                    shipSpawnPoints: null,
                    unitLimitModel: limits,
                    reinforcementData: reinforcementData,
                    enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    productionAccess: new OperationalProductionAccess(),
                    squadronLauncher: null,
                    squadronCommander: null,
                    owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry());
                ShipType[] types = (ShipType[])Enum.GetValues(typeof(ShipType));
                for (int i = 0; i < PlayerFactionModel.MAX_ACTIVE_PIPELINES; i++)
                    controller.Purchase(new ShipUnitRequest(data, types[i]));

                controller.Purchase(new ShipUnitRequest(data, types[PlayerFactionModel.MAX_ACTIVE_PIPELINES]));
                Assert.That(wallet.SpendCount, Is.EqualTo(7));
                Assert.That(limits.CurrentUnitCapacity, Is.EqualTo(7));
                Assert.That(limits.ShipOrdersCount, Is.EqualTo(7));

                controller.Purchase(new ShipUnitRequest(data, types[0]));
                controller.Purchase(new ShipUnitRequest(data, types[0]));
                Assert.That(wallet.SpendCount, Is.EqualTo(8));
                Assert.That(limits.CurrentUnitCapacity, Is.EqualTo(8));
                Assert.That(limits.ShipOrdersCount, Is.EqualTo(8));
                Assert.That(controller.PendingBuilds.Count, Is.EqualTo(8));
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
                EnemyFactionController controller = new EnemyFactionController(aiState: new IdleAiState(),
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
        public void BlockedShip_RetainsPurchaseAndRetriesWithoutDelayingOtherBuilds()
        {
            var data = ScriptableObject.CreateInstance<ReinforcementData>();
            var shipObject = new GameObject("Deployment test ship");
            try
            {
                SetBackingField(data, nameof(ReinforcementData.MaxUnitCapacity), 10);
                var faction = new FactionData();
                SetBackingField(faction, nameof(FactionData.MaxCount), 1);
                SetBackingField(faction, nameof(FactionData.BuildTime), 10);
                SetBackingField(faction, nameof(FactionData.UnitCapacity), 1);
                var wallet = new TrackingWallet();
                var limits = new EnemyUnitLimitModel();
                var spawnPoints = new BlockedShipSpawnPoints();
                var factory = new TrackingShipFactory(shipObject.AddComponent<ShipEntity>());
                var controller = new EnemyFactionController(aiState: new IdleAiState(), model: new EnemyFactionModel(null, null),
                    shipFactory: factory, miningFacilityFactory: null, defendPlatformFactory: null, research: null,
                    economyProvider: new TrackingEconomyProvider(), wallet: wallet, shipSpawnPoints: spawnPoints,
                    unitLimitModel: limits, reinforcementData: data,
                    enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    productionAccess: new OperationalProductionAccess(), squadronLauncher: null,
                    squadronCommander: null, owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry());
                var blocked = new ShipUnitRequest(faction, ShipType.Venator);
                controller.Purchase(blocked);
                controller.Purchase(new ShipUnitRequest(faction, ShipType.Recusant));

                controller.Advance(10f);

                Assert.That(factory.Created, Is.EqualTo(new[] { ShipType.Recusant }));
                Assert.That(controller.PendingBuilds.Count, Is.EqualTo(1));
                Assert.That(controller.PendingBuilds[0].Request, Is.SameAs(blocked));
                Assert.That(controller.PendingBuilds[0].TimeLeft, Is.Zero);
                Assert.That(wallet.RefundCount, Is.Zero);
                Assert.That(limits.CurrentUnitCapacity, Is.EqualTo(2));
                Assert.That(limits.ShipOrdersCount, Is.EqualTo(2));
                Assert.That(limits.ReleaseVersion, Is.Zero);

                controller.Purchase(blocked);
                Assert.That(wallet.SpendCount, Is.EqualTo(2), "The blocked ship retains its unit limit reservation.");
                // Each retry queries the living stations, the default zone, then falls back to a random spawn point.
                controller.Advance(0.5f);
                Assert.That(spawnPoints.BlockedShipAttempts, Is.EqualTo(3));
                controller.Advance(0.5f);
                Assert.That(spawnPoints.BlockedShipAttempts, Is.EqualTo(6));
                Assert.That(wallet.RefundCount, Is.Zero);

                spawnPoints.IsBlocked = false;
                controller.Advance(1f);

                Assert.That(factory.Created, Is.EqualTo(new[] { ShipType.Recusant, ShipType.Venator }));
                Assert.That(controller.PendingBuilds, Is.Empty);
                Assert.That(wallet.SpendCount, Is.EqualTo(2));
                Assert.That(limits.CurrentUnitCapacity, Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shipObject);
                UnityEngine.Object.DestroyImmediate(data);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BlockedShip_ProductionLostDuringRetry_ReleasesPendingReinforcement(bool hosted)
        {
            var data = ScriptableObject.CreateInstance<ReinforcementData>();
            try
            {
                SetBackingField(data, nameof(ReinforcementData.MaxUnitCapacity), 10);
                var faction = new FactionData();
                SetBackingField(faction, nameof(FactionData.MaxCount), 1);
                SetBackingField(faction, nameof(FactionData.BuildTime), 10);
                SetBackingField(faction, nameof(FactionData.UnitCapacity), 1);
                var wallet = new TrackingWallet();
                var limits = new EnemyUnitLimitModel();
                var access = new OperationalProductionAccess
                {
                    ProductionHost = hosted ? TestPlayers.Human : TestPlayers.Enemy
                };
                var controller = new EnemyFactionController(aiState: new IdleAiState(), model: new EnemyFactionModel(null, null),
                    shipFactory: null, miningFacilityFactory: null, defendPlatformFactory: null, research: null,
                    economyProvider: new TrackingEconomyProvider(), wallet: wallet,
                    shipSpawnPoints: new BlockedShipSpawnPoints(), unitLimitModel: limits, reinforcementData: data,
                    enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    productionAccess: access, squadronLauncher: null, squadronCommander: null,
                    owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy), playerRegistry: new PlayerRegistry());
                controller.Purchase(new ShipUnitRequest(faction, ShipType.Venator));
                controller.Advance(10f);
                Assert.That(controller.HasPendingReinforcement, Is.True);

                access.CanProduce = false;
                if (!hosted)
                    LogAssert.Expect(LogType.Error, new Regex(@"^\[EnemyAI:Production\] Build failed for ShipUnitRequest"));
                controller.Advance(1f);

                Assert.That(controller.HasPendingReinforcement, Is.False);
                Assert.That(wallet.RefundCount, Is.EqualTo(1));
                Assert.That(limits.CurrentUnitCapacity, Is.Zero);
                Assert.That(limits.ShipOrdersCount, Is.Zero);
                controller.Advance(10f);
                Assert.That(wallet.RefundCount, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(data); }
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
                EnemyFactionController controller = new EnemyFactionController(aiState: new IdleAiState(),
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
            EnemyFactionController controller = new EnemyFactionController(aiState: new IdleAiState(),
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

        [TestCase(false)]
        [TestCase(true)]
        public void StructurePurchase_AttemptsPlacementImmediatelyWithoutPendingBuild(bool platform)
        {
            var data = ScriptableObject.CreateInstance<ReinforcementData>();
            try
            {
                SetBackingField(data, nameof(ReinforcementData.MaxUnitCapacity), 10);
                var faction = new FactionData();
                SetBackingField(faction, nameof(FactionData.MaxCount), 1);
                SetBackingField(faction, nameof(FactionData.BuildTime), 30);
                SetBackingField(faction, nameof(FactionData.UnitCapacity), 1);
                var wallet = new TrackingWallet();
                var limits = new EnemyUnitLimitModel();
                var controller = new EnemyFactionController(aiState: new IdleAiState(), model: new EnemyFactionModel(null, null),
                    shipFactory: null, miningFacilityFactory: null, defendPlatformFactory: null, research: null,
                    economyProvider: new TrackingEconomyProvider(), wallet: wallet, shipSpawnPoints: null,
                    unitLimitModel: limits, reinforcementData: data,
                    enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    productionAccess: new OperationalProductionAccess(), squadronLauncher: null,
                    squadronCommander: null, owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry());
                UnitRequest request = platform
                    ? new DefendPlatformUnitRequest(faction, DefendPlatformType.Xq6)
                    : new MiningFacilityUnitRequest(faction, MiningFacilityType.CommonMiner);
                LogAssert.Expect(LogType.Error, new Regex(@"^\[EnemyAI:Production\] Build failed for "));

                controller.Purchase(request);

                Assert.That(wallet.SpendCount, Is.EqualTo(1));
                Assert.That(wallet.RefundCount, Is.EqualTo(1), "Placement is attempted during purchase, before any Advance call.");
                Assert.That(wallet.LastRefunded, Is.SameAs(request));
                Assert.That(controller.PendingBuilds, Is.Empty);
                Assert.That(limits.CurrentUnitCapacity, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(data); }
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
                var controller = new EnemyFactionController(aiState: new IdleAiState(), model: model, shipFactory: null,
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

        [TestCase(true)]
        [TestCase(false)]
        public void SecondStation_RejectsNewPurchaseAndRefundsLegacyQueueEntry(bool upgrade)
        {
            var data = ScriptableObject.CreateInstance<ReinforcementData>();
            try
            {
                var faction = new FactionData();
                SetBackingField(faction, nameof(FactionData.MaxCount), 1);
                SetBackingField(faction, nameof(FactionData.UnitCapacity), 1);
                var wallet = new TrackingWallet();
                var limits = new EnemyUnitLimitModel();
                var controller = new EnemyFactionController(aiState: new IdleAiState(),
                    model: new EnemyFactionModel(null, null), shipFactory: null,
                    miningFacilityFactory: null, defendPlatformFactory: null, research: null,
                    economyProvider: new TrackingEconomyProvider(), wallet: wallet, shipSpawnPoints: null,
                    unitLimitModel: limits, reinforcementData: data,
                    enemyStructurePlacementService: new UnavailableStructurePlacement { IsRecovering = true },
                    productionAccess: new OperationalProductionAccess(),
                    squadronLauncher: null, squadronCommander: null,
                    owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy), playerRegistry: new PlayerRegistry());
                UnitRequest request = upgrade ? new SecondStationLevelUnitRequest(faction, 1) : new SecondStationUnitRequest(faction);

                controller.Purchase(request);
                Assert.That(wallet.SpendCount, Is.Zero);
                Assert.That(controller.PendingBuilds, Is.Empty);

                controller.RestorePendingBuild(request, 30f);
                Assert.That(limits.CurrentUnitCapacity, Is.EqualTo(1));
                controller.Advance(0f);
                Assert.That(wallet.RefundCount, Is.EqualTo(1));
                Assert.That(controller.PendingBuilds, Is.Empty);
                Assert.That(limits.CurrentUnitCapacity, Is.Zero);
                controller.Advance(1f);
                Assert.That(wallet.RefundCount, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(data); }
        }

        [Test]
        public void StationUpgrade_WaitsForBuildTimeAndBlocksASecondPurchase()
        {
            var data = ScriptableObject.CreateInstance<ReinforcementData>();
            try
            {
                var upgrade = new FactionData();
                SetBackingField(upgrade, nameof(FactionData.MaxCount), 1);
                SetBackingField(upgrade, nameof(FactionData.BuildTime), 10);
                var wallet = new TrackingWallet();
                var model = new EnemyFactionModel(null, null);
                var controller = new EnemyFactionController(aiState: new IdleAiState(), model: model, shipFactory: null,
                    miningFacilityFactory: null, defendPlatformFactory: null, research: null,
                    economyProvider: new TrackingEconomyProvider(), wallet: wallet, shipSpawnPoints: null,
                    unitLimitModel: new EnemyUnitLimitModel(), reinforcementData: data,
                    enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    productionAccess: new OperationalProductionAccess(), squadronLauncher: null,
                    squadronCommander: null, owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry());

                controller.Purchase(new LevelUnitRequest(upgrade, 1));
                controller.Purchase(new LevelUnitRequest(upgrade, 1));

                Assert.That(wallet.SpendCount, Is.EqualTo(1));
                Assert.That(controller.IsUpgradingStation, Is.True);
                controller.Advance(9f);
                Assert.That(model.CurrentLevel, Is.EqualTo(1));
                controller.Advance(1f);
                Assert.That(model.CurrentLevel, Is.EqualTo(2));
                Assert.That(controller.IsUpgradingStation, Is.False);
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

        private sealed class IdleAiState : IEnemyAiStateProvider
        {
            public EnemyStrategicState CurrentState => EnemyStrategicState.Hold;
            public int ActiveShipCount => 0;
            public bool TryGetReinforcementRally(out Vector3 rally, out bool defendHome)
            {
                rally = default;
                defendHome = false;
                return false;
            }
        }

        private sealed class RallyAiState : IEnemyAiStateProvider
        {
            public EnemyStrategicState CurrentState => EnemyStrategicState.RetreatValue;
            public int ActiveShipCount => 0;
            public Vector3 Rally { get; set; }

            public bool TryGetReinforcementRally(out Vector3 rally, out bool defendHome)
            {
                rally = Rally;
                defendHome = true;
                return true;
            }

            public void FillDefenseDeploymentThreats(PlacementThreatSnapshot threats) => threats.Clear();
        }

        private sealed class RallyShipSpawnPoints : IShipSpawnPoints
        {
            public List<string> Calls { get; } = new List<string>();
            public bool IsAvailable { get; set; }
            public Vector3 Anchor { get; private set; }
            public PlayerId Owner { get; private set; }

            public bool TryGetDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
                PlacementThreatSnapshot threats, out Vector3 position)
            {
                Calls.Add("defense");
                Owner = owner;
                Anchor = anchor;
                position = anchor + Vector3.right;
                return IsAvailable;
            }

            public bool TryGetDefaultZoneSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position)
            {
                Calls.Add("default");
                position = new Vector3(20f, 0f, -700f);
                return true;
            }

            public bool TryGetSpawnPositionNear(PlayerId owner, ShipType shipType, Vector3 target, out Vector3 position)
            {
                Calls.Add("near");
                position = default;
                return false;
            }

            public bool TryGetRandomSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position)
            {
                Calls.Add("random");
                position = default;
                return false;
            }

            public bool TryGetStationSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position)
            {
                position = default;
                return false;
            }

            public bool TryGetBestDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
                PlacementThreatSnapshot threats, out Vector3 position)
            {
                Calls.Add("best");
                position = default;
                return false;
            }
        }

        private sealed class BlockedShipSpawnPoints : IShipSpawnPoints
        {
            public bool IsBlocked { get; set; } = true;
            public int BlockedShipAttempts { get; private set; }

            public bool TryGetRandomSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position)
            {
                position = Vector3.zero;
                if (shipType != ShipType.Venator) return true;
                BlockedShipAttempts++;
                return !IsBlocked;
            }

            public bool TryGetSpawnPositionNear(PlayerId owner, ShipType shipType, Vector3 target, out Vector3 position) =>
                TryGetRandomSpawnPosition(owner, shipType, out position);

            public bool TryGetDefaultZoneSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position) =>
                TryGetRandomSpawnPosition(owner, shipType, out position);

            public bool TryGetDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
                PlacementThreatSnapshot threats, out Vector3 position) =>
                TryGetRandomSpawnPosition(owner, shipType, out position);

            public bool TryGetStationSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position) =>
                TryGetRandomSpawnPosition(owner, shipType, out position);

            public bool TryGetBestDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
                PlacementThreatSnapshot threats, out Vector3 position) =>
                TryGetRandomSpawnPosition(owner, shipType, out position);
        }

        private sealed class TrackingShipFactory : ShipFactory
        {
            private readonly ShipEntity _ship;
            public List<ShipType> Created { get; } = new List<ShipType>();
            public List<Vector3> Positions { get; } = new List<Vector3>();

            public TrackingShipFactory(ShipEntity ship) : base(null, null, null, null, null, null)
            {
                _ship = ship;
            }

            public override ShipEntity Create(PlayerId owner, ShipType shipType, Vector3 position)
            {
                Created.Add(shipType);
                Positions.Add(position);
                return _ship;
            }
        }

        private sealed class UnavailableStructurePlacement : IEnemyStructurePlacementService
        {
            public bool IsRecovering { get; set; }
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

            public void RecordPlacement(string request, Vector3 position) { }
            public void RecordDeferred(string request, float refund) { }
            public void RecordStationBuilt(Vector3 position) { }

            public void RecordDestroyedPosition(Vector3 position)
            {
            }

            public void Reset()
            {
            }
        }

        private sealed class TrackingStructurePlacement : IEnemyStructurePlacementService
        {
            public bool IsRecovering => false;
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

            public void RecordPlacement(string request, Vector3 position) { }
            public void RecordDeferred(string request, float refund) { }
            public void RecordStationBuilt(Vector3 position) { }

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
            public bool? HasHostBaseOverride { get; set; }
            public bool HasHostBase => HasHostBaseOverride ?? CanProduce;

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
