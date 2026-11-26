using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Services.Ships;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Ship;
using EmpireAtWar.Utils;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyDecisionSchedulingTests
    {
        private readonly List<GameObject> _entityObjects = new List<GameObject>();

        [TearDown]
        public void DestroyEntityObjects()
        {
            foreach (GameObject entityObject in _entityObjects)
            {
                UnityEngine.Object.DestroyImmediate(entityObject);
            }

            _entityObjects.Clear();
        }

        [Test]
        public void SquadronPlayers_StartAtDifferentOffsets_AndPausePreservesCountdown()
        {
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            var battleState = new ReplayNotifier<BattleState>(BattleState.Paused);
            var scheduler = new EnemyAiScheduler(battleState);
            scheduler.Initialize();
            var entities = new EntityLocator();
            var snapshot = new AiWorldSnapshot(entities);
            var first = CreateSquadronCommander(roster.Get(TestPlayers.Enemy), battleState, snapshot, scheduler);
            var second = CreateSquadronCommander(roster.Get(TestPlayers.SecondEnemy), battleState, snapshot, scheduler);
            first.Initialize();
            second.Initialize();
            try
            {
                float firstDelay = SquadronDelay(first, scheduler);
                float secondDelay = SquadronDelay(second, scheduler);
                Assert.That(firstDelay, Is.InRange(0f, 3f));
                Assert.That(secondDelay, Is.InRange(0f, 3f));
                Assert.That(firstDelay, Is.Not.EqualTo(secondDelay));
                scheduler.Step(1f);
                Assert.That(SquadronDelay(first, scheduler), Is.EqualTo(firstDelay));
                Assert.That(SquadronDelay(second, scheduler), Is.EqualTo(secondDelay));
                battleState.Set(BattleState.Running);
                battleState.Set(BattleState.Paused);
                Assert.That(SquadronDelay(first, scheduler), Is.EqualTo(firstDelay), "Resume must not synchronize players again.");
            }
            finally
            {
                first.LateDispose();
                second.LateDispose();
                scheduler.LateDispose();
            }
        }

        [Test]
        public void DiagnosticRates_AreIndependentAndExpireAfterAnIdleWindow()
        {
            var periodic = new EnemyDecisionDiagnostics("Test.Enemy.Periodic");
            var launches = new EnemyDecisionDiagnostics("Test.Enemy.Launch");
            periodic.Record();
            launches.Record();
            launches.Record();
            periodic.Tick(1f);
            launches.Tick(1f);
            Assert.That(periodic.PerSecond, Is.EqualTo(1f));
            Assert.That(launches.PerSecond, Is.EqualTo(2f));
            periodic.Tick(1f);
            launches.Tick(1f);
            Assert.That(periodic.PerSecond, Is.Zero);
            Assert.That(launches.PerSecond, Is.Zero);
            Assert.That(launches.Count, Is.EqualTo(2));
        }

        [TestCase(0f, 0.5f)]
        [TestCase(0.1f, 0.4f)]
        [TestCase(0.25f, 0.25f)]
        public void ShipWorldChanges_RespectRemainingMinimumDecisionGap(float elapsed, float expectedDelay)
        {
            var battleState = new ReplayNotifier<BattleState>(BattleState.Running);
            var scheduler = new EnemyAiScheduler(battleState);
            scheduler.Initialize();
            EnemyUnitCommander commander = CreateShipCommander(scheduler, out EnemyAiScheduler.WorkHandle handle);
            SetShipClock(scheduler, handle, elapsed, 4f);

            NotifyWorldChange(commander);

            Assert.That(scheduler.GetRemainingDelay(handle), Is.EqualTo(expectedDelay).Within(0.0001f));
        }

        [Test]
        public void ShipWorldChanges_BurstsDoNotPostponePendingDecisionOrEarlierNormalTimer()
        {
            var battleState = new ReplayNotifier<BattleState>(BattleState.Running);
            var scheduler = new EnemyAiScheduler(battleState);
            scheduler.Initialize();
            EnemyUnitCommander commander = CreateShipCommander(scheduler, out EnemyAiScheduler.WorkHandle handle);
            SetShipClock(scheduler, handle, 0f, 4f);
            NotifyWorldChange(commander);
            for (int i = 1; i <= 4; i++)
            {
                float elapsed = i * 0.1f;
                float remaining = 0.5f - elapsed;
                SetShipClock(scheduler, handle, elapsed, remaining);
                NotifyWorldChange(commander);
                NotifyWorldChange(commander); // Ship and entity notifications can describe the same spawn.
                Assert.That(scheduler.GetRemainingDelay(handle), Is.EqualTo(remaining).Within(0.0001f),
                    "Repeated events must not turn the decision deadline into a sliding debounce.");
            }

            SetShipClock(scheduler, handle, 0.25f, 0.05f);
            NotifyWorldChange(commander);
            Assert.That(scheduler.GetRemainingDelay(handle), Is.EqualTo(0.05f),
                "An earlier normal decision or initial stagger must never be delayed by an event.");
        }

        [Test]
        public void ShipWorldChanges_PausePreservesElapsedGapAndCountdown()
        {
            var battleState = new ReplayNotifier<BattleState>(BattleState.Running);
            var scheduler = new EnemyAiScheduler(battleState);
            scheduler.Initialize();
            EnemyUnitCommander commander = CreateShipCommander(scheduler, out EnemyAiScheduler.WorkHandle handle);
            SetShipClock(scheduler, handle, 0.1f, 0.4f);
            battleState.Set(BattleState.Paused);
            scheduler.Step(1f);
            Assert.That(scheduler.GetRemainingDelay(handle), Is.EqualTo(0.4f));
            battleState.Set(BattleState.Running);
            NotifyWorldChange(commander);
            Assert.That(scheduler.GetRemainingDelay(handle), Is.EqualTo(0.4f));
        }

        [Test]
        public void Scheduler_RunsDueUnitsOldestFirst_AndRespectsBudgetUntilCompletion()
        {
            var battleState = new ReplayNotifier<BattleState>(BattleState.Running);
            double clockMs = 0.0;
            var scheduler = new EnemyAiScheduler(battleState)
            {
                FrameBudgetMs = 1f
            };
            scheduler.SetElapsedMsProviderForTests(() => clockMs);
            scheduler.Initialize();
            var executed = new List<string>();

            scheduler.Register(TestPlayers.Enemy, 2f, 0.5f, () =>
            {
                executed.Add("first");
                clockMs += 1.5;
            });
            scheduler.Register(TestPlayers.SecondEnemy, 2f, 0.2f, () =>
            {
                executed.Add("second");
                clockMs += 1.5;
            });

            scheduler.Step(1f);
            Assert.That(executed, Is.EqualTo(new[] { "second" }), "Oldest due unit must run first and exhaust budget.");

            scheduler.Step(0.1f);
            Assert.That(executed, Is.EqualTo(new[] { "second", "first" }), "Deferred unit must complete on the next frame.");

            scheduler.LateDispose();
        }

        [Test]
        public void Scheduler_StationCriticalJumpsQueue_AndPauseFreezesDueTimes()
        {
            var battleState = new ReplayNotifier<BattleState>(BattleState.Running);
            double clockMs = 0.0;
            var scheduler = new EnemyAiScheduler(battleState)
            {
                FrameBudgetMs = 0.5f
            };
            scheduler.SetElapsedMsProviderForTests(() => clockMs);
            scheduler.Initialize();
            var executed = new List<string>();

            var normal = scheduler.Register(TestPlayers.Enemy, 2f, 0.1f, () =>
            {
                executed.Add("normal");
                clockMs += 1.0;
            });
            var critical = scheduler.Register(TestPlayers.SecondEnemy, 2f, 0.5f, () =>
            {
                executed.Add("critical");
                clockMs += 1.0;
            }, isCritical: () => true);

            scheduler.Step(1f);
            Assert.That(executed, Is.EqualTo(new[] { "critical" }), "StationCritical unit must jump ahead of older normal unit.");

            battleState.Set(BattleState.Paused);
            float remainingBeforePause = scheduler.GetRemainingDelay(critical);
            scheduler.Step(5f);
            Assert.That(scheduler.GetRemainingDelay(critical), Is.EqualTo(remainingBeforePause));
            Assert.That(executed.Count, Is.EqualTo(1));
            Assert.That(scheduler.GetRemainingDelay(normal), Is.Zero);

            scheduler.LateDispose();
        }

        [Test]
        public void Scheduler_StarvedUnitRunsRegardlessOfBudget_AndIncrementsCount()
        {
            var battleState = new ReplayNotifier<BattleState>(BattleState.Running);
            double clockMs = 0.0;
            var scheduler = new EnemyAiScheduler(battleState)
            {
                FrameBudgetMs = 0.5f
            };
            scheduler.SetElapsedMsProviderForTests(() => clockMs);
            scheduler.Initialize();
            int starvedRuns = 0;

            scheduler.Register(TestPlayers.Enemy, 1f, 0f, () => clockMs += 1.0, isCritical: () => true);
            scheduler.Register(TestPlayers.SecondEnemy, 1f, 0f, () =>
            {
                starvedRuns++;
                clockMs += 0.1;
            });

            // Advance past 2 * interval so the second unit is starved even after the critical unit spends the budget.
            scheduler.Step(2.5f);
            Assert.That(starvedRuns, Is.EqualTo(1));
            Assert.That(scheduler.StarvationRunCount, Is.EqualTo(1));

            scheduler.LateDispose();
        }

        [Test]
        public void AiWorldSnapshot_UpdatesOnEntityChange_AndSplitsByOwner()
        {
            var entities = new EntityLocator();
            var snapshot = new AiWorldSnapshot(entities);
            snapshot.Initialize();
            try
            {
                Assert.That(snapshot.LivingUnits.Count, Is.Zero);
                Assert.That(snapshot.GetOwnedUnits(TestPlayers.Enemy).Count, Is.Zero);

                var ownShip = new TestEntity(1, TestPlayers.Enemy, new TestHealthModel { ShipClass = ShipClass.Capital },
                    CreateTransform())
                {
                    IsUnit = true,
                    UnitTypeId = UnitTypeId.Ship(ShipType.Venator)
                };
                var hostileBase = new TestEntity(2, TestPlayers.Human, new TestHealthModel { ShipClass = ShipClass.Structure },
                    CreateTransform())
                {
                    IsUnit = false,
                    IsPlayerBase = true
                };
                var hostileMine = new TestEntity(3, TestPlayers.Human, new TestHealthModel { ShipClass = ShipClass.Structure },
                    CreateTransform())
                {
                    IsUnit = false,
                    IsMiningFacility = true
                };
                var destroyedShip = new TestEntity(4, TestPlayers.Human, new TestHealthModel { IsDestroyed = true },
                    CreateTransform())
                {
                    IsUnit = true,
                    UnitTypeId = UnitTypeId.Ship(ShipType.Venator)
                };

                entities.AddEntity(ownShip);
                entities.AddEntity(hostileBase);
                entities.AddEntity(hostileMine);
                entities.AddEntity(destroyedShip);

                Assert.That(snapshot.LivingUnits.Count, Is.EqualTo(1));
                Assert.That(snapshot.GetOwnedUnits(TestPlayers.Enemy).Count, Is.EqualTo(1));
                Assert.That(snapshot.GetOwnedUnits(TestPlayers.Human).Count, Is.Zero);
                Assert.That(snapshot.Bases.Count, Is.EqualTo(1));
                Assert.That(snapshot.Structures.Count, Is.EqualTo(2));
                Assert.That(snapshot.RaidStructures.Count, Is.EqualTo(1));
                Assert.That(snapshot.RaidStructures[0].IsMiningFacility, Is.True);

                entities.RemoveEntity(ownShip);
                Assert.That(snapshot.LivingUnits.Count, Is.Zero);
                Assert.That(snapshot.GetOwnedUnits(TestPlayers.Enemy).Count, Is.Zero);
            }
            finally
            {
                snapshot.LateDispose();
            }
        }

        [Test]
        public void TeamAiPicture_FiltersConfidentSightings_AndCachesStableZoneIds()
        {
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            var intelRegistry = new TeamIntelRegistry();
            var entities = new EntityLocator();
            var snapshot = new AiWorldSnapshot(entities);
            var catalog = new UnitCombatProfileCatalog(null, null, null, null);
            var profiles = (Dictionary<UnitTypeId, UnitCombatProfile>)typeof(UnitCombatProfileCatalog)
                .GetField("_profiles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(catalog);
            UnitTypeId vulture = UnitTypeId.Squadron(SquadronType.Vulture);
            profiles.Add(vulture, new UnitCombatProfile(ShipClass.Fighter, 100f, 50f,
                new float[ForceComposition.ClassSlots], new float[ForceComposition.ClassSlots],
                new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>()));
            var registry = new TeamAiPictureRegistry(intelRegistry, roster, new TestZoneSource(), catalog, snapshot);
            PlayerSlot enemy = roster.Get(TestPlayers.Enemy);
            HostileIntelModel intel = intelRegistry.Get(enemy.Team);
            intel.Report(new HostileSighting(1, vulture,
                TestPlayers.Human, new FormationPoint(0f, 0f), 100f, 50f, 1f, 0f));

            TeamAiPicture picture = registry.GetOrCreate(enemy);
            picture.Refresh(1f);
            Assert.That(picture.ConfidentSightings.Count, Is.EqualTo(1));
            Assert.That(picture.GetZoneId(0), Is.EqualTo("zone:0"));
            Assert.That(picture.GetZoneId(0), Is.SameAs(picture.GetZoneId(0)));
            ForceComposition threat = picture.GetOrBuildObjectiveThreat("site:0", Vector3.zero, 10f);
            Assert.That(threat.IsEmpty, Is.False);

            picture.Refresh(IntelConfidence.FORGET_AGE + 1f);
            Assert.That(picture.ConfidentSightings.Count, Is.Zero);
        }

        [Test]
        public void TeamHostileIntelService_TracksTeamMembershipAcrossJoinAndLeave()
        {
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            var intelRegistry = new TeamIntelRegistry();
            var entities = new EntityLocator();
            var snapshot = new AiWorldSnapshot(entities);
            var vision = new VisionService(roster, fogEnabled: false);
            var service = new TeamHostileIntelService(entities, vision, roster, intelRegistry, snapshot);
            snapshot.Initialize();
            service.Initialize();
            try
            {
                var hostile = new TestEntity(10, TestPlayers.Human, new TestHealthModel { ShipClass = ShipClass.Capital },
                    CreateTransform())
                {
                    IsUnit = true,
                    UnitTypeId = UnitTypeId.Ship(ShipType.Venator)
                };
                entities.AddEntity(hostile);

                PlayerSlot first = roster.Get(TestPlayers.Enemy);
                PlayerSlot second = roster.Get(TestPlayers.SecondEnemy);
                HostileIntelModel teamIntel = intelRegistry.Get(first.Team);

                service.RegisterPlayer(first);
                service.RegisterPlayer(second);
                service.GatherAll(1f);
                Assert.That(teamIntel.Sightings.Count, Is.EqualTo(1));
                Assert.That(teamIntel.Sightings.Single().SeenAt, Is.EqualTo(1f));

                service.UnregisterPlayer(first);
                service.GatherAll(2f);
                Assert.That(teamIntel.Sightings.Count, Is.EqualTo(1));
                Assert.That(teamIntel.Sightings.Single().SeenAt, Is.EqualTo(2f));

                service.UnregisterPlayer(second);
                service.GatherAll(3f);
                Assert.That(teamIntel.Sightings.Single().SeenAt, Is.EqualTo(2f));
            }
            finally
            {
                service.LateDispose();
                snapshot.LateDispose();
            }
        }

        [Test]
        public void Commander_ShipRemovedMidMultiFrameCycle_DoesNotThrow()
        {
            PlayerRoster roster = TestPlayers.CreateDuel(EnemyAiDifficulty.Easy);
            PlayerSlot owner = roster.Get(TestPlayers.Enemy);
            var battleState = new ReplayNotifier<BattleState>(BattleState.Running);
            var scheduler = new EnemyAiScheduler(battleState);
            var entities = new EntityLocator();
            var snapshot = new AiWorldSnapshot(entities);
            var shipService = new ShipService();
            var intelRegistry = new TeamIntelRegistry();
            var catalog = new UnitCombatProfileCatalog(null, null, null, null);
            var profiles = (Dictionary<UnitTypeId, UnitCombatProfile>)typeof(UnitCombatProfileCatalog)
                .GetField("_profiles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(catalog);
            UnitTypeId venator = UnitTypeId.Ship(ShipType.Venator);
            profiles.Add(venator, new UnitCombatProfile(ShipClass.Capital, 100f, 0f,
                new float[ForceComposition.ClassSlots], new float[ForceComposition.ClassSlots],
                new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>()));
            var zones = new TestZoneSource();
            var zoneSystem = new TestZonesSystem();
            var navigation = new ShipNavigationService(new TestObstacles(), new TestHazards(), new OfflineSession(PlayerId.None));
            var patrol = new EnemyPatrolService(intelRegistry, owner, new MapModel(), zones,
                Array.Empty<ICaptureSiteDirectory>(), new VisionService(roster, fogEnabled: false),
                new BattleAreaService(roster), entities, roster, snapshot);
            patrol.Memory.NextSampleTime = float.PositiveInfinity;
            var orders = new TestOrderService();
            var patrolShips = new EnemyPatrolShipController(patrol, null, null, entities, orders, owner);
            var pressure = new EnemyStationPressureTracker();
            var production = new TestProductionAccess(owner.Id);
            var recovery = new EnemyRecoveryAnchor(zoneSystem, zones, roster, entities, new BattleAreaService(roster),
                new MapModel(), intelRegistry, owner, production, pressure, snapshot);
            var game = new TestGameModel();
            var builder = new EnemyStrategicContextBuilder(shipService, zoneSystem,
                Array.Empty<IEnemyCaptureObjectiveSource>(), entities, game, roster,
                new ForceCompositionBuilder(entities, catalog, snapshot), intelRegistry,
                new VisionService(roster, fogEnabled: false), owner, production, new TestHazards(), zones,
                navigation, pressure, recovery, snapshot, new TeamAiPictureRegistry(intelRegistry, roster, zones, catalog, snapshot), new EnemyFactionModel(null, null),
                new PlayerRegistry());
            var commander = new EnemyUnitCommander(shipService, zoneSystem, entities, game, orders,
                new EnemyStrategicDecisionModel(), builder, new EnemyTaskForceExecutor(orders, UnlimitedFocusFireSlots.Instance),
                owner, patrol, patrolShips, battleState, scheduler, UnlimitedFocusFireSlots.Instance,
                new TestHitAndRunSettings(TestHitAndRunSettings.OFF));

            var entity1 = new TestEntity(1, owner.Id, new TestHealthModel(), CreateTransform())
                { IsUnit = true, UnitTypeId = venator };
            var entity2 = new TestEntity(2, owner.Id, new TestHealthModel(), CreateTransform(new Vector3(5000f, 0f, 0f)))
                { IsUnit = true, UnitTypeId = venator };
            var entity3 = new TestEntity(3, owner.Id, new TestHealthModel(), CreateTransform(new Vector3(5010f, 0f, 0f)))
                { IsUnit = true, UnitTypeId = venator };
            var entity4 = new TestEntity(4, owner.Id, new TestHealthModel(), CreateTransform(new Vector3(5020f, 0f, 0f)))
                { IsUnit = true, UnitTypeId = venator };
            var ship1 = new TestShip(entity1.Id, owner.Id, entity1.Transform.position);
            var ship2 = new TestShip(entity2.Id, owner.Id, entity2.Transform.position);
            var ship3 = new TestShip(entity3.Id, owner.Id, entity3.Transform.position);
            var ship4 = new TestShip(entity4.Id, owner.Id, entity4.Transform.position);
            entities.AddEntity(entity1);
            entities.AddEntity(entity2);
            entities.AddEntity(entity3);
            entities.AddEntity(entity4);
            shipService.Add(ship1);
            shipService.Add(ship2);
            shipService.Add(ship3);
            shipService.Add(ship4);
            // A force already sent to a zone gives the cycle a second group, so it spans more than one frame.
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var planner = (EnemyShipTaskGroupPlanner)typeof(EnemyUnitCommander).GetField("_taskGroups", flags)
                .GetValue(commander);
            var force = (EnemyShipTaskGroup)typeof(EnemyShipTaskGroupPlanner)
                .GetMethod("GetOrCreateZoneGroup", flags).Invoke(planner, new object[] { "zone:test" });
            force.Ships.AddRange(new IShipEntity[] { ship2, ship3, ship4 });

            scheduler.Initialize();
            snapshot.Initialize();
            commander.Initialize();
            try
            {
                scheduler.Step(10f);
                var cycleField = typeof(EnemyUnitCommander).GetField("_cycleInProgress", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That((bool)cycleField.GetValue(commander), Is.True);

                entities.RemoveEntity(entity2);
                shipService.Remove(ship2);
                ((TestHealthModel)entity4.HealthModel).IsDestroyed = true;
                entities.RemoveEntity(entity4);
                int attackMovesBefore = orders.AttackMoveCalls;

                Assert.DoesNotThrow(() => scheduler.Step(0.1f));
                Assert.That((bool)cycleField.GetValue(commander), Is.False);
                Assert.That(commander.HasDecision, Is.True);
                // X2: the zone force holds at its gather point (attack-move) instead of a spawn-zone exit move.
                Assert.That(orders.AttackMoveCalls, Is.GreaterThan(attackMovesBefore));
            }
            finally
            {
                commander.LateDispose();
                snapshot.LateDispose();
                scheduler.LateDispose();
                navigation.Dispose();
            }
        }

        private Transform CreateTransform(Vector3 position = default)
        {
            var entityObject = new GameObject(nameof(TestEntity));
            _entityObjects.Add(entityObject);
            Transform transform = entityObject.transform;
            transform.position = position;
            return transform;
        }

        private static EnemyUnitCommander CreateShipCommander(
            EnemyAiScheduler scheduler,
            out EnemyAiScheduler.WorkHandle handle)
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            PlayerSlot owner = roster.Get(TestPlayers.Enemy);
            var entities = new EntityLocator();
            var snapshot = new AiWorldSnapshot(entities);
            var intelRegistry = new TeamIntelRegistry();
            var catalog = new UnitCombatProfileCatalog(null, null, null, null);
            var pictures = new TeamAiPictureRegistry(intelRegistry, roster, new TestZoneSource(), catalog, snapshot);
            var builder = new EnemyStrategicContextBuilder(null, null, Array.Empty<IEnemyCaptureObjectiveSource>(),
                entities, null, roster, null, intelRegistry, null, owner, null, null, null, null, null, null,
                snapshot, pictures, new EnemyFactionModel(null, null), new PlayerRegistry());
            var commander = new EnemyUnitCommander(null, null, entities, null, null, null, builder, null,
                owner, null, null, null, scheduler, UnlimitedFocusFireSlots.Instance,
                new TestHitAndRunSettings(TestHitAndRunSettings.OFF));
            handle = scheduler.Register(owner.Id, 4f, 4f, () => { });
            typeof(EnemyUnitCommander).GetField("_workHandle", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(commander, handle);
            return commander;
        }

        private static void SetShipClock(EnemyAiScheduler scheduler, EnemyAiScheduler.WorkHandle handle, float elapsed, float remaining)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            float clock = scheduler.BattleClock;
            typeof(EnemyAiScheduler.WorkHandle).GetField("LastRunTime", flags).SetValue(handle, clock - elapsed);
            typeof(EnemyAiScheduler.WorkHandle).GetField("DueTime", flags).SetValue(handle, clock + remaining);
        }

        private static void NotifyWorldChange(EnemyUnitCommander commander) =>
            typeof(EnemyUnitCommander).GetMethod("HandleWorldChanged", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(commander, null);

        private static float SquadronDelay(EnemySquadronCommander commander, EnemyAiScheduler scheduler)
        {
            var handle = (EnemyAiScheduler.WorkHandle)typeof(EnemySquadronCommander)
                .GetField("_workHandle", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(commander);
            return scheduler.GetRemainingDelay(handle);
        }

        private static EnemySquadronCommander CreateSquadronCommander(
            PlayerSlot owner,
            ReplayNotifier<BattleState> state,
            AiWorldSnapshot snapshot,
            EnemyAiScheduler scheduler) =>
            new EnemySquadronCommander(Array.Empty<IEnemyCaptureObjectiveSource>(), null, null, null,
                null, null, null, new TeamIntelRegistry(), owner, null, null, state, snapshot, scheduler, null, UnlimitedFocusFireSlots.Instance);

        private sealed class TestEntity : IEntity, IEntityTransformFacade, ICombatModifiersFacade, IUnitTypeFacade,
            IMiningFacilityFacade, IPlayerBaseFacade, IUnitOrderObserverFacade
        {
            public UnitOrderType CurrentOrder => UnitOrderType.None;
            public IEntity AttackTarget => null;
            public bool IsEngagingEnRoute => false;
            public bool IsUnit { get; set; }
            public bool IsMiningFacility { get; set; }
            public float GrossIncome { get; set; }
            public bool IsPlayerBase { get; set; }
            public UnitTypeId UnitTypeId { get; set; }
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public long Id { get; }
            public IHealthModelObserver HealthModel { get; }
            public PlayerId Owner { get; }
            public Transform Transform { get; }

            public TestEntity(long id, PlayerId owner, IHealthModelObserver healthModel, Transform transform)
            {
                Id = id;
                Owner = owner;
                HealthModel = healthModel;
                Transform = transform;
            }

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade
            {
                TryGetFacade(out TFacade facade);
                return facade;
            }

            public bool TryGetFacade<TFacade>(out TFacade entityFacade) where TFacade : IEntityFacade
            {
                if (!IsUnit && typeof(TFacade) == typeof(IUnitTypeFacade) ||
                    !IsMiningFacility && typeof(TFacade) == typeof(IMiningFacilityFacade) ||
                    !IsPlayerBase && typeof(TFacade) == typeof(IPlayerBaseFacade))
                {
                    entityFacade = default;
                    return false;
                }

                if (this is TFacade self)
                {
                    entityFacade = self;
                    return true;
                }

                entityFacade = default;
                return false;
            }
        }

        private sealed class TestHealthModel : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull { get; set; } = 100f;
            public ShipClass ShipClass { get; set; } = ShipClass.Capital;
            public bool HasLiveHardPoints => !IsDestroyed;
            public float HullPercentage => 1f;
            public float Shields { get; set; }
            public float ShieldPercentage => 0f;
            public bool IsDestroyed { get; set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits { get; set; } = true;
            public PlayerId Owner => PlayerId.None;
            public bool HasShields => false;
            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
        }

        private sealed class TestShip : IShipEntity
        {
            public PlayerId Owner { get; }
            public Vector3 WorldPosition { get; set; }
            public float NavigationRadius => 5f;
            public float NavigationSpeed => 10f;
            public long EntityId { get; }
            public UnitOrderType CurrentOrder { get; set; }

            public TestShip(long entityId, PlayerId owner, Vector3 position)
            {
                EntityId = entityId;
                Owner = owner;
                WorldPosition = position;
            }
        }

        private sealed class TestZoneSource : IReinforcementZoneSource
        {
            public IReadOnlyList<ReinforcementZone> Zones => Array.Empty<ReinforcementZone>();
            public float SquadronCaptureWeight => 0.5f;
            public (float Friendly, float Hostile) GetTeamStrengths(ReinforcementZone zone, PlayerId owner) => default;
        }

        private sealed class TestZonesSystem : IReinforcementZonesSystem
        {
            public event Action OwnershipChanged { add { } remove { } }
            public void CopyOwnedCapturableZoneBounds(PlayerId owner, List<Bounds> destination) => destination.Clear();
            public int GetOwnedCapturableZoneCount(PlayerId owner) => 0;
            public bool TryGetDefaultZoneCenter(PlayerId owner, out Vector3 position) { position = default; return false; }
            public bool TryGetCaptureTarget(PlayerId owner, Vector3 origin, out Vector3 position) { position = default; return false; }
            public bool TryGetDefaultZoneExitPosition(PlayerId owner, Vector3 shipPosition, float shipRadius, out Vector3 position)
            {
                if (shipPosition.x > 4000f)
                {
                    position = new Vector3(5200f, 0f, 0f);
                    return true;
                }

                position = default;
                return false;
            }
        }

        private sealed class TestObstacles : IMapObstacleContactProvider
        {
            public string Id => nameof(TestObstacles);
            public void CopyContacts(List<RadarContact> destination) => destination.Clear();
        }

        private sealed class TestProductionAccess : IProductionAccess
        {
            public bool CanProduce => false;
            public bool HasHostBase => false;
            public PlayerId ProductionHost { get; }
            public TestProductionAccess(PlayerId host) { ProductionHost = host; }
            public bool TryGetProductionBase(out IEntity productionBase) { productionBase = null; return false; }
        }

        private sealed class TestGameModel : IGameModelObserver
        {
            public GameMode GameMode => GameMode.Skirmish;
            public PlanetType PlanetType => default;
            public MapSize MapSize => default;
            public IReadOnlyList<PlayerSlot> Players => Array.Empty<PlayerSlot>();
            public BattleVictoryCondition VictoryCondition => BattleVictoryCondition.DestroyEnemyFleet;
            public float StartingMoney => 0f;
        }

        private sealed class TestOrderService : IUnitOrderService
        {
            public event Action<UnitOrder> OrderIssued { add { } remove { } }
            public int AttackMoveCalls { get; private set; }
            public void IssueMove(IReadOnlyList<IEntity> receivers, Vector3 point, MovePace pace = MovePace.Formation) { }
            public void IssueMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> destinations) { }
            public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target, MovePace pace = MovePace.Formation) { }
            public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target, IReadOnlyList<Vector3> offsets) { }
            public void IssueHardPointAttack(IReadOnlyList<IEntity> receivers, IEntity target, int hardPointId, MovePace pace = MovePace.Formation) { }
            public void IssueAttackMove(IReadOnlyList<IEntity> receivers, Vector3 point, MovePace pace = MovePace.Formation) => AttackMoveCalls++;
            public void IssueAttackMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> destinations) => AttackMoveCalls++;
            public void IssueStop(IReadOnlyList<IEntity> receivers) { }
            public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly) { }
            public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly, IReadOnlyList<Vector3> offsets) { }
            public void IssueWaypointMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> waypoints) { }
            public void IssueHunt(IReadOnlyList<IEntity> receivers) { }
            public void IssueRetreat(IReadOnlyList<IEntity> receivers) { }
            public void IssueRetreat(IReadOnlyList<IEntity> receivers, Vector3 point) { }
        }
    }
}
