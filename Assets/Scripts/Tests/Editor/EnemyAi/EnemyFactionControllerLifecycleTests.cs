using System;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Controllers;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Units;
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
        public void Reserve_HoldsMixedWaveUntilSupportOrOpeningRelease(bool openingRelease)
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
            int minimumReady = openingRelease ? 0 : 2;
            queue.Advance(1f, arrived.Add, minimumReady);
            Assert.That(arrived.Count, Is.EqualTo(openingRelease ? 1 : 0));
            queue.Advance(4f, arrived.Add, minimumReady);
            Assert.That(arrived, Is.EqualTo(new UnitRequest[] { flagship, escort }));
            Assert.That(queue.Builds, Is.Empty);
        }

        [Test]
        public void Reserve_AtPopulationLimitCanReleaseLoneReadyShipBeforeSlowBatchFinishes()
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

        // W18: builds of one type run one at a time; the second is waiting, a held finished ship is not.
        [Test]
        public void CountUnfinished_CountsBuildsInProductionAndWaitingButNotFinishedOnes()
        {
            var queue = new EnemyBuildQueue();
            var data = new FactionData();
            var first = new ShipUnitRequest(data, ShipType.Tector);
            var second = new ShipUnitRequest(data, ShipType.Tector);
            queue.Add(first, 5f);
            queue.Add(second, 5f);

            Assert.That(queue.CountUnfinished(first.Id), Is.EqualTo(2));
            queue.Advance(5f, _ => { }, minimumReadyCombatUnits: int.MaxValue);
            Assert.That(queue.CountUnfinished(first.Id), Is.EqualTo(1), "The finished ship is held, not waiting.");
            Assert.That(queue.Builds, Has.Count.EqualTo(2));
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
        public void Reserve_DefenseRetryWaitsForAnotherReadyUnit()
        {
            var queue = new EnemyBuildQueue();
            var data = new FactionData();
            var blocked = new ShipUnitRequest(data, ShipType.Venator);
            var escort = new ShipUnitRequest(data, ShipType.Recusant);
            var arrived = new List<UnitRequest>();
            queue.RetryShipDeployment(blocked, 1f);
            queue.Add(escort, 5f);
            queue.Advance(1f, arrived.Add, minimumReadyCombatUnits: 2, batchDeploymentRetries: true);
            Assert.That(arrived, Is.Empty);

            queue.Advance(4f, arrived.Add, minimumReadyCombatUnits: 2, batchDeploymentRetries: true);
            Assert.That(arrived, Is.EqualTo(new UnitRequest[] { blocked, escort }));
        }

        // Round 5, E1f: ships finished together land smallest hull first; other requests keep their place.
        [Test]
        public void Advance_ShipDeployOrder_DeploysSmallestHullFirst()
        {
            var queue = new EnemyBuildQueue();
            var capital = new ShipUnitRequest(new FactionData(), ShipType.Venator);
            var corvette = new ShipUnitRequest(new FactionData(), ShipType.CorellianCorvette);
            var frigate = new ShipUnitRequest(new FactionData(), ShipType.Recusant);
            var hull = new Dictionary<ShipType, float>
            {
                [ShipType.Venator] = 600f, [ShipType.CorellianCorvette] = 80f, [ShipType.Recusant] = 250f
            };
            queue.Add(capital, 1f);
            queue.Add(corvette, 1f);
            queue.Add(frigate, 1f);
            var arrived = new List<UnitRequest>();

            queue.Advance(1f, arrived.Add, shipDeployOrder: (left, right) =>
                hull[((ShipUnitRequest)left).Key].CompareTo(hull[((ShipUnitRequest)right).Key]));

            Assert.That(arrived, Is.EqualTo(new UnitRequest[] { corvette, frigate, capital }));
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
                    playerRegistry: new PlayerRegistry(), stationFactory: null, profiles: null,
                    secondStation: new SecondStationLevelModel(), forwardAnchors: NoForwardSpawnAnchors.Instance, economyModel: null);
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
        public void SurroundedStation_CompletedShipDeploysWithinHoldCap()
        {
            var data = ScriptableObject.CreateInstance<ReinforcementData>();
            var shipObject = new GameObject("Emergency ship");
            try
            {
                SetBackingField(data, nameof(ReinforcementData.MaxUnitCapacity), 10);
                var faction = new FactionData();
                SetBackingField(faction, nameof(FactionData.MaxCount), 2);
                SetBackingField(faction, nameof(FactionData.BuildTime), 1);
                SetBackingField(faction, nameof(FactionData.UnitCapacity), 1);
                Vector3 rally = new Vector3(100f, 0f, -500f);
                var ai = new RallyAiState { Rally = rally };
                var spawnPoints = new RallyShipSpawnPoints { IsAvailable = false };
                var factory = new TrackingShipFactory(shipObject.AddComponent<ShipEntity>());
                var model = new EnemyFactionModel(null, null);
                var controller = new EnemyFactionController(aiState: ai, model: model,
                    shipFactory: factory, miningFacilityFactory: null, defendPlatformFactory: null,
                    research: null, economyProvider: new TrackingEconomyProvider(), wallet: new TrackingWallet(),
                    shipSpawnPoints: spawnPoints, unitLimitModel: new EnemyUnitLimitModel(),
                    reinforcementData: data, enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    productionAccess: new OperationalProductionAccess(), squadronLauncher: null,
                    squadronCommander: null, owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry(), stationFactory: null, profiles: null,
                    secondStation: new SecondStationLevelModel(), forwardAnchors: NoForwardSpawnAnchors.Instance, economyModel: null);

                controller.Purchase(new ShipUnitRequest(faction, ShipType.Venator));
                controller.Purchase(new ShipUnitRequest(faction, ShipType.Recusant));
                controller.Advance(1f);
                Assert.That(factory.Created, Is.Empty);
                Assert.That(model.HeldShips, Is.EqualTo(2));

                for (int second = 0; second < 15; second++)
                    controller.Advance(1f);

                Assert.That(factory.Created, Is.EquivalentTo(new[] { ShipType.Venator, ShipType.Recusant }));
                Assert.That(controller.PendingBuilds, Is.Empty);
                Assert.That(model.HeldShips, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shipObject);
                UnityEngine.Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void DefenseWave_OneBlockedShip_DeploysOpenShipAfterHoldLimit()
        {
            var data = ScriptableObject.CreateInstance<ReinforcementData>();
            var shipObject = new GameObject("Partial wave ship");
            try
            {
                SetBackingField(data, nameof(ReinforcementData.MaxUnitCapacity), 10);
                var faction = new FactionData();
                SetBackingField(faction, nameof(FactionData.MaxCount), 2);
                SetBackingField(faction, nameof(FactionData.BuildTime), 1);
                SetBackingField(faction, nameof(FactionData.UnitCapacity), 1);
                var ai = new RallyAiState { Rally = new Vector3(40f, 0f, -900f) };
                var spawnPoints = new BlockedShipSpawnPoints { IsBlocked = true };
                var factory = new TrackingShipFactory(shipObject.AddComponent<ShipEntity>());
                var controller = new EnemyFactionController(aiState: ai, model: new EnemyFactionModel(null, null),
                    shipFactory: factory, miningFacilityFactory: null, defendPlatformFactory: null,
                    research: null, economyProvider: new TrackingEconomyProvider(), wallet: new TrackingWallet(),
                    shipSpawnPoints: spawnPoints, unitLimitModel: new EnemyUnitLimitModel(),
                    reinforcementData: data, enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    productionAccess: new OperationalProductionAccess(), squadronLauncher: null,
                    squadronCommander: null, owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry(), stationFactory: null, profiles: null,
                    secondStation: new SecondStationLevelModel(), forwardAnchors: NoForwardSpawnAnchors.Instance, economyModel: null);

                controller.Purchase(new ShipUnitRequest(faction, ShipType.Venator));
                controller.Purchase(new ShipUnitRequest(faction, ShipType.Recusant));
                controller.Advance(1f);
                Assert.That(factory.Created, Is.Empty);

                for (int second = 0; second < 8; second++)
                    controller.Advance(1f);

                Assert.That(factory.Created, Does.Contain(ShipType.Recusant));
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
                    playerRegistry: new PlayerRegistry(), stationFactory: null, profiles: null,
                    secondStation: new SecondStationLevelModel(), forwardAnchors: NoForwardSpawnAnchors.Instance, economyModel: null);

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

        // W69/D6: a new type waits for a free pipeline; the same type joins its pipeline. A ship type has no
        // purchase limit beyond the human queue limit, whatever its MaxCount.
        [Test]
        public void Purchase_FullPipelinesRejectANewTypeBeforeSpending_TheSameTypeStillQueues()
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
                    playerRegistry: new PlayerRegistry(), stationFactory: null, profiles: null,
                    secondStation: new SecondStationLevelModel(), forwardAnchors: NoForwardSpawnAnchors.Instance, economyModel: null);
                ShipType[] types = (ShipType[])Enum.GetValues(typeof(ShipType));
                for (int i = 0; i < PlayerFactionModel.MAX_ACTIVE_PIPELINES; i++)
                    controller.Purchase(new ShipUnitRequest(data, types[i]));

                controller.Purchase(new ShipUnitRequest(data, types[PlayerFactionModel.MAX_ACTIVE_PIPELINES]));
                Assert.That(wallet.SpendCount, Is.EqualTo(7));
                Assert.That(limits.CurrentUnitCapacity, Is.EqualTo(7));
                Assert.That(limits.ShipOrdersCount, Is.EqualTo(7));

                int sameType = data.MaxCount;
                Assert.That(PlayerFactionModel.GetQueueLimit(new ShipUnitRequest(data, types[0])),
                    Is.GreaterThan(sameType), "MaxCount no longer limits a ship type.");
                for (int i = 0; i < sameType; i++)
                    controller.Purchase(new ShipUnitRequest(data, types[0]));
                int bought = PlayerFactionModel.MAX_ACTIVE_PIPELINES + sameType;
                Assert.That(wallet.SpendCount, Is.EqualTo(bought));
                Assert.That(limits.CurrentUnitCapacity, Is.EqualTo(bought * data.UnitCapacity));
                Assert.That(limits.ShipOrdersCount, Is.EqualTo(bought));
                Assert.That(controller.PendingBuilds.Count, Is.EqualTo(bought));
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
                    playerRegistry: new PlayerRegistry(), stationFactory: null, profiles: null,
                    secondStation: new SecondStationLevelModel(), forwardAnchors: NoForwardSpawnAnchors.Instance, economyModel: null);
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

        // Round 5, E1a/E1d: no station pressure → forward beside own units first; none open → relay or station
        // search in the same tick; a defended station → rear mode, no forward search.
        [TestCase(true, false, "forward")]
        [TestCase(false, false, "near")]
        [TestCase(true, true, "rear")]
        public void ShipDeployment_ChoosesForwardOrFallbackOrRearMode(bool forwardOpen, bool defendHome,
            string expected)
        {
            var data = ScriptableObject.CreateInstance<ReinforcementData>();
            var shipObject = new GameObject("Forward deployment ship");
            try
            {
                SetBackingField(data, nameof(ReinforcementData.MaxUnitCapacity), 10);
                var faction = new FactionData();
                SetBackingField(faction, nameof(FactionData.MaxCount), 1);
                SetBackingField(faction, nameof(FactionData.BuildTime), 1);
                SetBackingField(faction, nameof(FactionData.UnitCapacity), 1);
                Vector3 task = new Vector3(0f, 0f, 1500f);
                Vector3 wing = new Vector3(200f, 0f, 1200f);
                var spawnPoints = new ForwardShipSpawnPoints { ForwardOpen = forwardOpen };
                var anchors = new FixedForwardAnchors(new ForwardSpawnAnchor(wing, "wing"));
                var factory = new TrackingShipFactory(shipObject.AddComponent<ShipEntity>());
                var controller = new EnemyFactionController(
                    aiState: new TaskAiState { Rally = task, DefendHome = defendHome },
                    model: new EnemyFactionModel(null, null),
                    shipFactory: factory, miningFacilityFactory: null, defendPlatformFactory: null, research: null,
                    economyProvider: new TrackingEconomyProvider(), wallet: new TrackingWallet(),
                    shipSpawnPoints: spawnPoints, unitLimitModel: new EnemyUnitLimitModel(), reinforcementData: data,
                    enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    productionAccess: new OperationalProductionAccess(), squadronLauncher: null,
                    squadronCommander: null, owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry(), stationFactory: null, profiles: null,
                    secondStation: new SecondStationLevelModel(), forwardAnchors: anchors, economyModel: null);

                controller.Purchase(new ShipUnitRequest(faction, ShipType.Venator));
                controller.Advance(1f);

                if (expected == "rear")
                {
                    Assert.That(anchors.Calls, Is.Zero, "A defended station never looks for forward points.");
                    Assert.That(spawnPoints.Calls, Does.Not.Contain("forward"));
                    return;
                }
                Assert.That(factory.Created, Is.EqualTo(new[] { ShipType.Venator }), "The ship lands in this tick.");
                Assert.That(anchors.LastTask, Is.EqualTo(task), "Units nearest the ship's task come first.");
                Assert.That(spawnPoints.ForwardUnits, Is.EqualTo(new[] { wing }));
                Assert.That(spawnPoints.Calls[0], Is.EqualTo("forward"));
                if (expected == "forward")
                {
                    Assert.That(spawnPoints.Calls, Is.EqualTo(new[] { "forward" }));
                    Assert.That(factory.Positions[0], Is.EqualTo(ForwardShipSpawnPoints.FORWARD_POINT));
                }
                else
                {
                    Assert.That(spawnPoints.Calls, Is.EqualTo(new[] { "forward", "near" }));
                    Assert.That(factory.Positions[0], Is.EqualTo(ForwardShipSpawnPoints.NEAR_POINT));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shipObject);
                UnityEngine.Object.DestroyImmediate(data);
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
                    playerRegistry: new PlayerRegistry(), stationFactory: null, profiles: null,
                    secondStation: new SecondStationLevelModel(), forwardAnchors: NoForwardSpawnAnchors.Instance, economyModel: null);
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

                Assert.That(limits.GetReservedCount(UnitLimitKey.From(blocked)), Is.EqualTo(1),
                    "The blocked ship retains its unit limit reservation.");
                Assert.That(controller.CanQueue(blocked.Id, PlayerFactionModel.GetQueueLimit(blocked)), Is.True,
                    "A held ship is the reserve pool, not the queue: its type queues again, as for a human (D6).");
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
                    owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy), playerRegistry: new PlayerRegistry(), stationFactory: null, profiles: null,
                    secondStation: new SecondStationLevelModel(), forwardAnchors: NoForwardSpawnAnchors.Instance, economyModel: null);
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
                    playerRegistry: new PlayerRegistry(), stationFactory: null, profiles: null,
                    secondStation: new SecondStationLevelModel(), forwardAnchors: NoForwardSpawnAnchors.Instance, economyModel: null);
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
                playerRegistry: new PlayerRegistry(), stationFactory: null, profiles: null,
                    secondStation: new SecondStationLevelModel(), forwardAnchors: NoForwardSpawnAnchors.Instance, economyModel: null);

            controller.Initialize();
            controller.Initialize();

            Assert.That(structurePlacement.ResetCount, Is.EqualTo(1));

            controller.LateDispose();
        }

        // W69: platforms and miners wait their queue build time like a human's before placement; a paid structure with no
        // point stays queued and retries instead of a refund-and-rebuy loop.
        [TestCase(false)]
        [TestCase(true)]
        public void StructurePurchase_WaitsItsBuildTime_ThenRetriesPlacementWhilePaid(bool platform)
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
                var placement = new UnavailableStructurePlacement();
                var controller = new EnemyFactionController(aiState: new IdleAiState(), model: new EnemyFactionModel(null, null),
                    shipFactory: null, miningFacilityFactory: null, defendPlatformFactory: null, research: null,
                    economyProvider: new TrackingEconomyProvider(), wallet: wallet, shipSpawnPoints: null,
                    unitLimitModel: limits, reinforcementData: data,
                    enemyStructurePlacementService: placement,
                    productionAccess: new OperationalProductionAccess(), squadronLauncher: null,
                    squadronCommander: null, owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry(), stationFactory: null, profiles: null,
                    secondStation: new SecondStationLevelModel(), forwardAnchors: NoForwardSpawnAnchors.Instance, economyModel: null);
                UnitRequest request = platform
                    ? new DefendPlatformUnitRequest(faction, DefendPlatformType.Xq6)
                    : new MiningFacilityUnitRequest(faction, MiningFacilityType.CommonMiner);

                controller.Purchase(request);

                Assert.That(wallet.SpendCount, Is.EqualTo(1));
                Assert.That(controller.PendingBuilds, Has.Count.EqualTo(1));
                Assert.That(controller.HasPlatformInProgress, Is.EqualTo(platform),
                    "A queued platform counts as the platform in progress; a miner does not.");
                controller.Advance(faction.BuildTime - 1f);
                Assert.That(placement.DeferredCount, Is.Zero, "No placement before the queue build time.");

                controller.Advance(1f);
                Assert.That(placement.DeferredCount, Is.EqualTo(1), "Placement is tried once the build completes.");
                Assert.That(wallet.RefundCount, Is.Zero);
                Assert.That(controller.PendingBuilds, Has.Count.EqualTo(1), "The paid structure waits for a point.");
                Assert.That(limits.GetReservedCount(UnitLimitKey.From(request)), Is.EqualTo(1),
                    "The paid structure keeps its purchase slot.");
                Assert.That(limits.CurrentUnitCapacity, Is.Zero, "Structures never use population, as for a human.");
                Assert.That(controller.HasPlatformInProgress, Is.EqualTo(platform));
            }
            finally { UnityEngine.Object.DestroyImmediate(data); }
        }

        // W69: with no team base left, a completed structure cannot be placed; it is refunded so it never keeps a
        // defeated faction alive.
        [Test]
        public void StructureBuild_WithoutTeamBase_IsRefunded()
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
                var access = new OperationalProductionAccess();
                var controller = new EnemyFactionController(aiState: new IdleAiState(), model: new EnemyFactionModel(null, null),
                    shipFactory: null, miningFacilityFactory: null, defendPlatformFactory: null, research: null,
                    economyProvider: new TrackingEconomyProvider(), wallet: wallet, shipSpawnPoints: null,
                    unitLimitModel: limits, reinforcementData: data,
                    enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    productionAccess: access, squadronLauncher: null,
                    squadronCommander: null, owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry(), stationFactory: null, profiles: null,
                    secondStation: new SecondStationLevelModel(), forwardAnchors: NoForwardSpawnAnchors.Instance, economyModel: null);
                var platform = new DefendPlatformUnitRequest(faction, DefendPlatformType.Xq6);
                controller.Purchase(platform);

                access.CanProduce = false;
                controller.Advance(faction.BuildTime);

                Assert.That(wallet.RefundCount, Is.EqualTo(1));
                Assert.That(controller.HasPendingReinforcement, Is.False);
                Assert.That(limits.GetReservedCount(UnitLimitKey.From(platform)), Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(data); }
        }

        // W69: the per-type queue limit of ships is the human one, not the unit's MaxCount.
        [Test]
        public void Purchase_PerTypeQueueLimit_IsTheHumanLimit()
        {
            var data = ScriptableObject.CreateInstance<ReinforcementData>();
            try
            {
                SetBackingField(data, nameof(ReinforcementData.MaxUnitCapacity), 1000);
                var faction = new FactionData();
                SetBackingField(faction, nameof(FactionData.BuildTime), 10);
                SetBackingField(faction, nameof(FactionData.UnitCapacity), 1);
                // MaxCount above the human queue limit: only the queue limit stops the purchases.
                SetBackingField(faction, nameof(FactionData.MaxCount), 100);
                int humanLimit = PlayerFactionModel.GetQueueLimit(new ShipUnitRequest(faction, ShipType.Venator));
                Assert.That(humanLimit, Is.LessThan(faction.MaxCount));
                var controller = new EnemyFactionController(aiState: new IdleAiState(), model: new EnemyFactionModel(null, null),
                    shipFactory: null, miningFacilityFactory: null, defendPlatformFactory: null, research: null,
                    economyProvider: new TrackingEconomyProvider(), wallet: new TrackingWallet(), shipSpawnPoints: null,
                    unitLimitModel: new EnemyUnitLimitModel(), reinforcementData: data,
                    enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    productionAccess: new OperationalProductionAccess(), squadronLauncher: null,
                    squadronCommander: null, owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry(), stationFactory: null, profiles: null,
                    secondStation: new SecondStationLevelModel(), forwardAnchors: NoForwardSpawnAnchors.Instance, economyModel: null);

                for (int i = 0; i < faction.MaxCount; i++)
                    controller.Purchase(new ShipUnitRequest(faction, ShipType.Venator));

                Assert.That(controller.PendingBuilds, Has.Count.EqualTo(humanLimit));
                controller.LateDispose();
            }
            finally { UnityEngine.Object.DestroyImmediate(data); }
        }

        // W56: with no living own station, an assault capital lands beside the escorted group body, never at a relay or
        // an allied base; with no body to join it is held, then refunded.
        [TestCase(true)]
        [TestCase(false)]
        public void AssaultCapital_WithoutOwnStation_LandsOnlyBesideTheBody(bool bodyAnchor)
        {
            var data = ScriptableObject.CreateInstance<ReinforcementData>();
            var shipObject = new GameObject("Capital deployment ship");
            try
            {
                SetBackingField(data, nameof(ReinforcementData.MaxUnitCapacity), 1000);
                var roster = new FactionRoster(LoadSharedData<FactionCatalog>().Get(FactionType.Empire),
                    LoadSharedData<MiningFacilityCatalog>(), LoadSharedData<DefendPlatformCatalog>(),
                    LoadSharedData<SuperWeaponCatalog>());
                KeyValuePair<ShipType, FactionData> capital =
                    roster.Ships.FirstOrDefault(ship => ship.Value.AvailableLevel >= 4);
                Assert.That(capital.Value, Is.Not.Null, "The Empire roster unlocks a ship at level 4 or later.");
                var spawnPoints = new ForwardShipSpawnPoints { ForwardOpen = true };
                var factory = new TrackingShipFactory(shipObject.AddComponent<ShipEntity>());
                var wallet = new TrackingWallet();
                var limits = new EnemyUnitLimitModel();
                IEnemyForwardSpawnAnchors anchors = bodyAnchor
                    ? new FixedForwardAnchors(new ForwardSpawnAnchor(new Vector3(200f, 0f, 1200f), "ship"))
                    : NoForwardSpawnAnchors.Instance;
                var controller = new EnemyFactionController(
                    aiState: new TaskAiState { Rally = new Vector3(0f, 0f, 1500f) },
                    model: new EnemyFactionModel(null, roster),
                    shipFactory: factory, miningFacilityFactory: null, defendPlatformFactory: null, research: null,
                    economyProvider: new TrackingEconomyProvider(), wallet: wallet,
                    shipSpawnPoints: spawnPoints, unitLimitModel: limits, reinforcementData: data,
                    enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    // An allied base still produces; the own station is gone.
                    productionAccess: new OperationalProductionAccess { HasHostBaseOverride = false },
                    squadronLauncher: null, squadronCommander: null,
                    owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry(), stationFactory: null,
                    profiles: CapitalCatalog(capital.Key),
                    secondStation: new SecondStationLevelModel(), forwardAnchors: anchors, economyModel: null);

                controller.Purchase(new ShipUnitRequest(capital.Value, capital.Key));
                controller.Advance(capital.Value.BuildTime);

                if (bodyAnchor)
                {
                    Assert.That(factory.Created, Is.EqualTo(new[] { capital.Key }));
                    Assert.That(factory.Positions[0], Is.EqualTo(ForwardShipSpawnPoints.FORWARD_POINT));
                    return;
                }
                Assert.That(factory.Created, Is.Empty, "No relay or allied-base landing without a body.");
                Assert.That(spawnPoints.Calls, Does.Not.Contain("near"));
                Assert.That(wallet.RefundCount, Is.Zero, "Held while a body may still gather.");
                for (int i = 0; i < 30 && wallet.RefundCount == 0; i++) controller.Advance(1f);
                Assert.That(wallet.RefundCount, Is.EqualTo(1), "A capital with nowhere to land is not bought.");
                Assert.That(factory.Created, Is.Empty);
                Assert.That(controller.PendingBuilds, Is.Empty);
                Assert.That(limits.CurrentUnitCapacity, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shipObject);
                UnityEngine.Object.DestroyImmediate(data);
            }
        }

        // W68: with a planned attack wave the finished ship lands beside the nearest own unit at the wave target through the
        // planned-wave anchors (relaxed escort filters); without one the normal forward anchors are used.
        [TestCase(true)]
        [TestCase(false)]
        public void PlannedWave_LandsThroughThePlannedWaveAnchors(bool planned)
        {
            var data = ScriptableObject.CreateInstance<ReinforcementData>();
            var shipObject = new GameObject("Planned wave ship");
            try
            {
                SetBackingField(data, nameof(ReinforcementData.MaxUnitCapacity), 1000);
                var roster = new FactionRoster(LoadSharedData<FactionCatalog>().Get(FactionType.Empire),
                    LoadSharedData<MiningFacilityCatalog>(), LoadSharedData<DefendPlatformCatalog>(),
                    LoadSharedData<SuperWeaponCatalog>());
                KeyValuePair<ShipType, FactionData> ship =
                    roster.Ships.FirstOrDefault(entry => entry.Value.AvailableLevel <= 1);
                Assert.That(ship.Value, Is.Not.Null, "The Empire roster has a ship at level 1.");
                var spawnPoints = new ForwardShipSpawnPoints { ForwardOpen = true };
                var factory = new TrackingShipFactory(shipObject.AddComponent<ShipEntity>());
                var anchors = new FixedForwardAnchors(new ForwardSpawnAnchor(new Vector3(200f, 0f, 1200f), "ship"));
                Vector3 target = new Vector3(0f, 0f, 1500f);
                var controller = new EnemyFactionController(
                    aiState: new TaskAiState { Rally = target, PlannedWave = planned ? target : (Vector3?)null },
                    model: new EnemyFactionModel(null, roster),
                    shipFactory: factory, miningFacilityFactory: null, defendPlatformFactory: null, research: null,
                    economyProvider: new TrackingEconomyProvider(), wallet: new TrackingWallet(),
                    shipSpawnPoints: spawnPoints, unitLimitModel: new EnemyUnitLimitModel(), reinforcementData: data,
                    enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    productionAccess: new OperationalProductionAccess(),
                    squadronLauncher: null, squadronCommander: null,
                    owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry(), stationFactory: null, profiles: null,
                    secondStation: new SecondStationLevelModel(), forwardAnchors: anchors, economyModel: null);

                controller.Purchase(new ShipUnitRequest(ship.Value, ship.Key));
                controller.Advance(ship.Value.BuildTime);

                Assert.That(factory.Created, Is.EqualTo(new[] { ship.Key }));
                Assert.That(factory.Positions[0], Is.EqualTo(ForwardShipSpawnPoints.FORWARD_POINT));
                Assert.That(anchors.LastTask, Is.EqualTo(target));
                Assert.That(anchors.PlannedWaveCalls, Is.EqualTo(planned ? 1 : 0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shipObject);
                UnityEngine.Object.DestroyImmediate(data);
            }
        }

        /// <summary>Every ship of the given type rates as a capital; other types are unknown to the catalog.</summary>
        private static UnitCombatProfileCatalog CapitalCatalog(ShipType capital)
        {
            var catalog = new UnitCombatProfileCatalog(null, null, null, null);
            var profiles = (Dictionary<UnitTypeId, UnitCombatProfile>)typeof(UnitCombatProfileCatalog)
                .GetField("_profiles", PRIVATE_INSTANCE).GetValue(catalog);
            profiles.Add(UnitTypeId.Ship(capital), new UnitCombatProfile(ShipClass.Capital, 1f, 0f,
                new float[ForceComposition.ClassSlots], new float[ForceComposition.ClassSlots],
                new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>()));
            return catalog;
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
                    owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy), playerRegistry: new PlayerRegistry(), stationFactory: null, profiles: null,
                    secondStation: new SecondStationLevelModel(), forwardAnchors: NoForwardSpawnAnchors.Instance, economyModel: null);
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
        public void SecondStation_RefundsAnUnsupportedUpgrade_AndKeepsAnUnplaceableStationQueued(bool upgrade)
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
                    owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy), playerRegistry: new PlayerRegistry(), stationFactory: null, profiles: null,
                    secondStation: new SecondStationLevelModel(), forwardAnchors: NoForwardSpawnAnchors.Instance, economyModel: null);
                UnitRequest request = upgrade ? new SecondStationLevelUnitRequest(faction, 1) : new SecondStationUnitRequest(faction);

                controller.Purchase(request);
                Assert.That(wallet.SpendCount, Is.Zero);
                Assert.That(controller.PendingBuilds, Is.Empty);

                controller.RestorePendingBuild(request, 30f);
                Assert.That(limits.GetReservedCount(UnitLimitKey.From(request)), Is.EqualTo(1));
                // A restored upgrade keeps its build time; with no second station it is refunded when it completes.
                controller.Advance(30f);
                if (upgrade)
                {
                    Assert.That(wallet.RefundCount, Is.EqualTo(1));
                    Assert.That(controller.PendingBuilds, Is.Empty);
                    Assert.That(limits.GetReservedCount(UnitLimitKey.From(request)), Is.Zero);
                    controller.Advance(1f);
                    Assert.That(wallet.RefundCount, Is.EqualTo(1));
                    return;
                }

                // Round 6 (G6d): a paid station with no placement point stays queued and retries; no refund-and-rebuy.
                Assert.That(wallet.RefundCount, Is.Zero);
                Assert.That(controller.PendingBuilds, Has.Count.EqualTo(1));
                Assert.That(limits.GetReservedCount(UnitLimitKey.From(request)), Is.EqualTo(1));
                controller.Advance(30f);
                Assert.That(wallet.RefundCount, Is.Zero);
                Assert.That(controller.PendingBuilds, Has.Count.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(data); }
        }

        // W1: the AI upgrades its second station with the real build time, never past the main station's level, one
        // upgrade at a time, and gets the money back when the station dies first.
        [TestCase(false)]
        [TestCase(true)]
        public void SecondStationUpgrade_BuildsBelowTheMainLevel_AndIsRefundedWhenTheStationFalls(bool stationFalls)
        {
            var data = ScriptableObject.CreateInstance<ReinforcementData>();
            try
            {
                var upgrade = new FactionData();
                SetBackingField(upgrade, nameof(FactionData.MaxCount), 1);
                SetBackingField(upgrade, nameof(FactionData.BuildTime), 10);
                var wallet = new TrackingWallet();
                var model = new EnemyFactionModel(null, null) { CurrentLevel = 2 };
                var secondStation = new SecondStationLevelModel { IsBuilt = true };
                var controller = new EnemyFactionController(aiState: new IdleAiState(), model: model, shipFactory: null,
                    miningFacilityFactory: null, defendPlatformFactory: null, research: null,
                    economyProvider: new TrackingEconomyProvider(), wallet: wallet, shipSpawnPoints: null,
                    unitLimitModel: new EnemyUnitLimitModel(), reinforcementData: data,
                    enemyStructurePlacementService: new UnavailableStructurePlacement(),
                    productionAccess: new OperationalProductionAccess(), squadronLauncher: null,
                    squadronCommander: null, owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                    playerRegistry: new PlayerRegistry(), stationFactory: null, profiles: null,
                    secondStation: secondStation, forwardAnchors: NoForwardSpawnAnchors.Instance, economyModel: null);
                controller.Initialize();

                controller.Purchase(new SecondStationLevelUnitRequest(upgrade, 1));
                controller.Purchase(new SecondStationLevelUnitRequest(upgrade, 1));
                Assert.That(wallet.SpendCount, Is.EqualTo(1), "One second-station upgrade at a time.");
                Assert.That(controller.IsUpgradingSecondStation, Is.True);

                if (stationFalls)
                {
                    secondStation.HandleLost();
                    Assert.That(wallet.RefundCount, Is.EqualTo(1));
                    Assert.That(controller.PendingBuilds, Is.Empty);
                    controller.LateDispose();
                    return;
                }

                controller.Advance(upgrade.BuildTime - 1f);
                Assert.That(secondStation.CurrentLevel, Is.EqualTo(1), "The upgrade takes its build time.");
                controller.Advance(1f);
                Assert.That(secondStation.CurrentLevel, Is.EqualTo(2));
                Assert.That(controller.IsUpgradingSecondStation, Is.False);

                controller.Purchase(new SecondStationLevelUnitRequest(upgrade, 2));
                Assert.That(wallet.SpendCount, Is.EqualTo(1), "Never past the main station's level.");
                controller.LateDispose();
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
                    playerRegistry: new PlayerRegistry(), stationFactory: null, profiles: null,
                    secondStation: new SecondStationLevelModel(), forwardAnchors: NoForwardSpawnAnchors.Instance, economyModel: null);

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

        private sealed class TaskAiState : IEnemyAiStateProvider
        {
            public EnemyStrategicState CurrentState => EnemyStrategicState.CaptureZone;
            public int ActiveShipCount => 0;
            public Vector3 Rally { get; set; }
            public bool DefendHome { get; set; }
            public Vector3? PlannedWave { get; set; }

            public bool TryGetReinforcementRally(out Vector3 rally, out bool defendHome)
            {
                rally = Rally;
                defendHome = DefendHome;
                return true;
            }

            public bool TryGetPlannedWave(out Vector3 target)
            {
                target = PlannedWave ?? default;
                return PlannedWave.HasValue;
            }

            public void FillDefenseDeploymentThreats(PlacementThreatSnapshot threats) => threats.Clear();
        }

        private sealed class FixedForwardAnchors : IEnemyForwardSpawnAnchors
        {
            private readonly ForwardSpawnAnchor _anchor;

            public int Calls { get; private set; }
            public int PlannedWaveCalls { get; private set; }
            public Vector3 LastTask { get; private set; }

            public FixedForwardAnchors(ForwardSpawnAnchor anchor) => _anchor = anchor;

            public bool Collect(Vector3 task, IReadOnlyList<Vector3> landedShips, bool capital,
            List<ForwardSpawnAnchor> anchors,
                out Vector3 awayFrom)
            {
                Calls++;
                LastTask = task;
                anchors.Clear();
                anchors.Add(_anchor);
                awayFrom = Vector3.zero;
                return true;
            }

            public bool CollectPlannedWave(Vector3 task, IReadOnlyList<Vector3> landedShips,
                List<ForwardSpawnAnchor> anchors, out Vector3 awayFrom)
            {
                PlannedWaveCalls++;
                return Collect(task, landedShips, false, anchors, out awayFrom);
            }
        }

        private sealed class ForwardShipSpawnPoints : IShipSpawnPoints
        {
            public static readonly Vector3 FORWARD_POINT = new Vector3(300f, 0f, 1400f);
            public static readonly Vector3 NEAR_POINT = new Vector3(0f, 0f, -800f);

            public bool ForwardOpen { get; set; }
            public List<string> Calls { get; } = new List<string>();
            public List<Vector3> ForwardUnits { get; } = new List<Vector3>();

            public bool TryGetSpawnPositionNearUnits(PlayerId owner, ShipType shipType, IReadOnlyList<Vector3> units,
                Vector3 awayFrom, out Vector3 position, out ForwardSpawnAttempt attempt)
            {
                Calls.Add("forward");
                ForwardUnits.AddRange(units);
                attempt = new ForwardSpawnAttempt { UnitIndex = ForwardOpen ? 0 : -1, Points = 1 };
                position = ForwardOpen ? FORWARD_POINT : default;
                return ForwardOpen;
            }

            public float GetHullRadius(ShipType shipType) => 0f;

            public bool TryGetSpawnPositionNear(PlayerId owner, ShipType shipType, Vector3 target, out Vector3 position)
            {
                Calls.Add("near");
                position = NEAR_POINT;
                return true;
            }

            public bool TryGetRearSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
                PlacementThreatSnapshot threats, bool requireThreatSafety, out Vector3 position)
            {
                Calls.Add("rear");
                position = anchor + Vector3.back * 500f;
                return true;
            }

            public bool TryGetRandomSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position) =>
                Fail("random", out position);

            public bool TryGetStationSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position) =>
                Fail("station", out position);

            public bool TryGetDefaultZoneSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position) =>
                Fail("default", out position);

            public bool TryGetDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
                PlacementThreatSnapshot threats, out Vector3 position) => Fail("defense", out position);

            public void ReserveDefenseLanding(object token, PlayerId owner, ShipType shipType, Vector3 position) { }

            public void ReleaseDefenseLanding(object token) { }

            public bool TryGetBestDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
                PlacementThreatSnapshot threats, out Vector3 position) => Fail("best", out position);

            public bool TryGetWideDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
                PlacementThreatSnapshot threats, out Vector3 position) => Fail("wide", out position);

            private bool Fail(string call, out Vector3 position)
            {
                Calls.Add(call);
                position = default;
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
            public bool TryGetSpawnPositionNearUnits(PlayerId owner, ShipType shipType, IReadOnlyList<Vector3> units,
                Vector3 awayFrom, out Vector3 position, out ForwardSpawnAttempt attempt)
            {
                attempt = new ForwardSpawnAttempt { UnitIndex = -1 };
                position = default;
                return false;
            }

            public float GetHullRadius(ShipType shipType) => 0f;

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

            public void ReserveDefenseLanding(object token, PlayerId owner, ShipType shipType, Vector3 position) { }
            public void ReleaseDefenseLanding(object token) { }

            public bool TryGetBestDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
                PlacementThreatSnapshot threats, out Vector3 position)
            {
                Calls.Add("best");
                position = default;
                return false;
            }

            // Rear rings behave like the station-perimeter searches this fake already records.
            public bool TryGetRearSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
                PlacementThreatSnapshot threats, bool requireThreatSafety, out Vector3 position) =>
                requireThreatSafety
                    ? TryGetDefenseSpawnPosition(owner, shipType, anchor, threats, out position)
                    : TryGetBestDefenseSpawnPosition(owner, shipType, anchor, threats, out position);

            public bool TryGetWideDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
                PlacementThreatSnapshot threats, out Vector3 position) =>
                TryGetBestDefenseSpawnPosition(owner, shipType, anchor, threats, out position);
        }

        private sealed class BlockedShipSpawnPoints : IShipSpawnPoints
        {
            public bool TryGetSpawnPositionNearUnits(PlayerId owner, ShipType shipType, IReadOnlyList<Vector3> units,
                Vector3 awayFrom, out Vector3 position, out ForwardSpawnAttempt attempt)
            {
                attempt = new ForwardSpawnAttempt { UnitIndex = -1 };
                position = default;
                return false;
            }

            public float GetHullRadius(ShipType shipType) => 0f;

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

            public void ReserveDefenseLanding(object token, PlayerId owner, ShipType shipType, Vector3 position) { }
            public void ReleaseDefenseLanding(object token) { }

            public bool TryGetBestDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
                PlacementThreatSnapshot threats, out Vector3 position) =>
                TryGetRandomSpawnPosition(owner, shipType, out position);

            public bool TryGetRearSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
                PlacementThreatSnapshot threats, bool requireThreatSafety, out Vector3 position) =>
                TryGetRandomSpawnPosition(owner, shipType, out position);

            public bool TryGetWideDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
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
            /// <summary>Placement attempts that found no point.</summary>
            public int DeferredCount { get; private set; }
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
            public void RecordDeferred(string request, float refund) => DeferredCount++;
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

            // No station entity in these fixtures: ship deployment skips the rear-of-station search.
            public bool TryGetProductionBase(out IEntity station)
            {
                station = null;
                return false;
            }
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
