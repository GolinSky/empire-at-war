using System;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Services.Ships;
using EmpireAtWar.Utils;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Hazards;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ship;
using NUnit.Framework;
using UnityEngine;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using System.Reflection;
using System.Linq;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Views.ReinforcementZones;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Entities.SpaceStation;
using UnityEditor;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Services.SpawnBlocking;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyUnitCommanderTests
    {
        [TestCase(BattleState.NotInitialized)]
        [TestCase(BattleState.Loading)]
        [TestCase(BattleState.Paused)]
        [TestCase(BattleState.Ended)]
        public void InitializeAndTick_BeforeBattleRuns_DoNotReadUnloadedMap(BattleState state)
        {
            GameObject view = new GameObject("Commander lifecycle zones");
            try
            {
                var zones = view.AddComponent<ReinforcementZonesSystem>();
                PlayerRoster roster = TestPlayers.CreateDuel();
                PlayerSlot owner = roster.Get(TestPlayers.Enemy);
                var battleState = new ReplayNotifier<BattleState>(state);
                var scheduler = new EnemyAiScheduler(battleState);
                var locator = new EntityLocator();
                var patrol = new EnemyPatrolService(new TeamIntelRegistry(), owner, new MapModel(),
                    null, Array.Empty<ICaptureSiteDirectory>(), null, null, locator, roster, new AiWorldSnapshot(locator));
                var orders = new FakeOrderService();
                EnemyStrategicContextBuilder context = CaptureBuilder(roster, new ZoneSource(roster),
                    new SiteSource(), new TestHazards());
                var commander = new EnemyUnitCommander(new ShipService(), zones, locator,
                    null, orders, new EnemyStrategicDecisionModel(), context, new EnemyTaskForceExecutor(orders),
                    owner, patrol, null, battleState, scheduler);

                scheduler.Initialize();
                Assert.DoesNotThrow(commander.Initialize);
                try
                {
                    Assert.DoesNotThrow(() => scheduler.Step(10f));
                    Assert.DoesNotThrow(commander.Tick);
                    Assert.That(commander.HasDecision, Is.False);
                }
                finally
                {
                    commander.LateDispose();
                }

                try
                {
                    battleState.Set(BattleState.Running);
                    Assert.DoesNotThrow(() => scheduler.Step(10f), "Disposed commanders must unregister their work.");
                    Assert.DoesNotThrow(commander.Tick, "Disposed commanders must unsubscribe from battle state.");
                    Assert.That(commander.HasDecision, Is.False);
                }
                finally
                {
                    scheduler.LateDispose();
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(view);
            }
        }

        [Test]
        public void PatrolShipSelectsCheapestCorvetteAndReleasesForDefense()
        {
            const string SHARED_DATA = "Assets/Settings/Data/Factions/Shared/";
            var catalog = AssetDatabase.LoadAssetAtPath<FactionCatalog>(SHARED_DATA + "FactionCatalog.asset");
            var definition = UnityEngine.Object.Instantiate(catalog.Get(FactionType.Empire));
            try
            {
                var roster = new FactionRoster(definition,
                    AssetDatabase.LoadAssetAtPath<MiningFacilityCatalog>(SHARED_DATA + "MiningFacilityCatalog.asset"),
                    AssetDatabase.LoadAssetAtPath<DefendPlatformCatalog>(SHARED_DATA + "DefendPlatformCatalog.asset"),
                    AssetDatabase.LoadAssetAtPath<SuperWeaponCatalog>(SHARED_DATA + "SuperWeaponCatalog.asset"));
                var faction = new EnemyFactionModel(null, roster);
                ShipType[] types = definition.Ships.Keys.Take(3).ToArray();
                int[] prices = { 500, 100, 1 };
                var locator = new EntityLocator();
                var ships = new List<IShipEntity>();
                for (int i = 0; i < types.Length; i++)
                {
                    typeof(FactionData).GetField("<Price>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(definition.Ships[types[i]], prices[i]);
                    locator.AddEntity(new FakeEntity(new FakeHealthModel(null)
                        { ShipClass = i == 2 ? ShipClass.Capital : ShipClass.Corvette }, TestPlayers.Enemy, i + 1)
                        { UnitTypeId = UnitTypeId.Ship(types[i]) });
                    ships.Add(new FakeShip(Vector3.zero, i + 1));
                }
                PlayerSlot owner = TestPlayers.CreateDuel().Get(TestPlayers.Enemy);
                var patrol = new EnemyPatrolService(new TeamIntelRegistry(), owner, null, null,
                    Array.Empty<ICaptureSiteDirectory>(), null, null, locator, null, new AiWorldSnapshot(locator));
                var controller = new EnemyPatrolShipController(patrol, null, faction, locator, null, owner);
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var selected = (IShipEntity)typeof(EnemyPatrolShipController).GetMethod("FindCheapest", flags)
                    .Invoke(controller, new object[] { ships });
                Assert.That(selected, Is.SameAs(ships[1]), "The cheaper capital ship cannot displace a corvette patrol.");
                typeof(EnemyPatrolShipController).GetField("_ship", flags).SetValue(controller, selected);

                Assert.That(controller.Select(ships, true), Is.Null);
                Assert.That(typeof(EnemyPatrolShipController).GetField("_ship", flags).GetValue(controller), Is.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(definition); }
        }

        [TestCase(false, true, false)]
        [TestCase(true, true, false)]
        [TestCase(true, false, false)]
        [TestCase(false, true, true)]
        public void ReinforcementRally_CachedDecisionSkipsRemovedEntities(bool removeStation, bool keepFleet,
            bool recovery)
        {
            GameObject stationView = new GameObject("Reinforcement rally station");
            GameObject targetView = new GameObject("Local rally target");
            try
            {
                stationView.transform.position = Vector3.forward * 100f;
                PlayerRoster roster = TestPlayers.CreateDuel();
                EntityLocator locator = new EntityLocator();
                FakeEntity station = new FakeEntity(new FakeHealthModel(stationView.transform), TestPlayers.Enemy, 1);
                FakeEntity survivor = new FakeEntity(new FakeHealthModel(null), TestPlayers.Enemy, 2);
                locator.AddEntity(station);
                locator.AddEntity(survivor);
                EnemyStrategicContextBuilder builder = CaptureBuilder(roster, new ZoneSource(roster),
                    new SiteSource(), new TestHazards());
                FakeOrderService orders = new FakeOrderService();
                EnemyUnitCommander commander = new EnemyUnitCommander(null, null, locator, null, orders,
                    new EnemyStrategicDecisionModel(), builder, new EnemyTaskForceExecutor(orders),
                    roster.Get(TestPlayers.Enemy), null, null, null,
                    new EnemyAiScheduler(new ReplayNotifier<BattleState>(BattleState.Running)));
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var planner = (EnemyShipTaskGroupPlanner)typeof(EnemyUnitCommander)
                    .GetField("_taskGroups", flags).GetValue(commander);
                EnemyShipTaskGroup group = planner.Groups[0];
                group.Ships.Add(new FakeShip(Vector3.right * 20f, survivor.Id));
                group.Ships.Add(new FakeShip(Vector3.right * 1000f, 3));
                targetView.transform.position = Vector3.right * 200f;
                FakeEntity target = new FakeEntity(new FakeHealthModel(targetView.transform), TestPlayers.Human, 4);
                if (keepFleet) locator.AddEntity(target);
                var snapshot = new EnemyStrategicSnapshot(BattleVictoryCondition.DestroyEnemyFleet,
                    EnemyAiDifficulty.UltraHard, 2, 1, 0, 1f, 1f, false, false, true,
                    hasStationPressure: true, recoveryMode: recovery);
                typeof(EnemyShipTaskGroup).GetProperty(nameof(EnemyShipTaskGroup.Decision)).SetValue(group,
                    new EnemyStrategicDecision("test", EnemyStrategicState.EngageLocal, 2));
                var context = new EnemyStrategicContext(group.Ships, null, null, station, snapshot,
                    Vector3.zero, null, null, localThreatTarget: target);
                typeof(EnemyShipTaskGroup).GetProperty(nameof(EnemyShipTaskGroup.Context)).SetValue(group, context);
                typeof(EnemyUnitCommander).GetField("_hasDecision", flags).SetValue(commander, true);

                if (removeStation)
                {
                    locator.RemoveEntity(station);
                    UnityEngine.Object.DestroyImmediate(stationView);
                }
                if (!keepFleet) locator.RemoveEntity(survivor);

                bool hasRally = commander.TryGetReinforcementRally(out Vector3 rally, out bool defendHome);

                bool shouldDefend = !removeStation;
                Assert.That(hasRally, Is.EqualTo(shouldDefend || keepFleet));
                Assert.That(defendHome, Is.EqualTo(shouldDefend));
                Assert.That(rally, Is.EqualTo(shouldDefend ? Vector3.forward * 100f :
                    keepFleet ? Vector3.right * 200f : Vector3.zero));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetView);
                if (stationView != null) UnityEngine.Object.DestroyImmediate(stationView);
            }
        }

        [TestCase("FindClosestKnownShip")]
        [TestCase("FindLocalThreat")]
        public void TargetSelection_PrefersSafeShipOverCloserIonStormTarget(string selector)
        {
            GameObject view = new GameObject("Hazard target test");
            SpaceHazardData data = ScriptableObject.CreateInstance<SpaceHazardData>();
            try
            {
                SerializedObject serialized = new SerializedObject(data);
                serialized.FindProperty("<DisablesShields>k__BackingField").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PlayerRoster roster = TestPlayers.CreateDuel();
                PlayerSlot owner = roster.Get(TestPlayers.Enemy);
                EntityLocator locator = new EntityLocator();
                FakeEntity hazardous = new FakeEntity(new FakeHealthModel(view.transform), TestPlayers.Human, 1);
                FakeEntity safe = new FakeEntity(new FakeHealthModel(view.transform), TestPlayers.Human, 2);
                locator.AddEntity(hazardous);
                locator.AddEntity(safe);
                TeamIntelRegistry registry = new TeamIntelRegistry();
                HostileIntelModel intel = registry.Get(owner.Team);
                intel.Report(new HostileSighting(1, UnitTypeId.Ship(ShipType.Venator), TestPlayers.Human,
                    new FormationPoint(10f, 0f), 1f, 0f, 1f, 0f));
                intel.Report(new HostileSighting(2, UnitTypeId.Ship(ShipType.Venator), TestPlayers.Human,
                    new FormationPoint(15f, 0f), 1f, 0f, 1f, 0f));
                TestHazards hazards = new TestHazards
                {
                    Sources = new IHazardSource[]
                    {
                        TestHazards.CreateEllipse(data, new Vector3(10f, 0f, 0f), Vector3.right, 2f, 2f)
                    }
                };
                EnemyStrategicContextBuilder builder = CreateContextBuilder(
                    null, null, Array.Empty<IEnemyCaptureObjectiveSource>(), locator, null, roster, null, registry,
                    new VisionService(roster, fogEnabled: false), owner, null, hazards, null, null, new EnemyStationPressureTracker(), CreateRecovery(roster));
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(EnemyStrategicContextBuilder).GetField("_localThreat", flags).SetValue(builder, hazardous);
                MethodInfo find = typeof(EnemyStrategicContextBuilder).GetMethod(selector, flags);
                Assert.That(find.Invoke(builder, new object[] { Vector3.zero, 0f }), Is.SameAs(safe));
                hazards.Sources = Array.Empty<IHazardSource>();
                Assert.That(find.Invoke(builder, new object[] { Vector3.zero, 0f }), Is.SameAs(hazardous));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(data);
                UnityEngine.Object.DestroyImmediate(view);
            }
        }

        [TestCase(BattleState.NotInitialized)]
        [TestCase(BattleState.Loading)]
        [TestCase(BattleState.Paused)]
        [TestCase(BattleState.Ended)]
        public void SquadronCommander_OutsideRunning_TracksRestoredSquadronsWithoutReadingMap(BattleState state)
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            PlayerSlot owner = roster.Get(TestPlayers.Enemy);
            var battleState = new ReplayNotifier<BattleState>(state);
            var scheduler = new EnemyAiScheduler(battleState);
            var locator = new EntityLocator();
            var snapshot = new AiWorldSnapshot(locator);
            var intel = new TeamIntelRegistry();
            var patrol = new EnemyPatrolService(intel, owner, new MapModel(),
                null, Array.Empty<ICaptureSiteDirectory>(), null, null, locator, roster, snapshot);
            var catalog = new UnitCombatProfileCatalog(null, null, null, null);
            var profiles = (Dictionary<UnitTypeId, UnitCombatProfile>)typeof(UnitCombatProfileCatalog)
                .GetField("_profiles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(catalog);
            profiles.Add(UnitTypeId.Squadron(SquadronType.Vulture), new UnitCombatProfile(ShipClass.Fighter,
                1f, 0f, Array.Empty<float>(), Array.Empty<float>(), Array.Empty<float>(), Array.Empty<UnitCombatProfile>()));
            var squadron = new FakeSquadron(1, owner.Id);
            var commander = new EnemySquadronCommander(Array.Empty<IEnemyCaptureObjectiveSource>(),
                null, locator, roster, null, catalog, null, intel, owner, patrol, null, battleState, snapshot, scheduler, null);

            commander.Initialize();
            try
            {
                Assert.DoesNotThrow(commander.Tick);
                Assert.DoesNotThrow(() => commander.Command(squadron, SquadronType.Vulture));
                Assert.That(squadron.ReleaseListeners, Is.EqualTo(1), "Restored squadrons must remain tracked.");
                Assert.That(squadron.OrdersReceived, Is.Zero);
                Assert.That(patrol.Memory.Entries, Is.Empty);

                battleState.Set(BattleState.Running);
                battleState.Set(state);
                Assert.DoesNotThrow(commander.Tick, "Leaving Running must suspend decisions again.");
            }
            finally
            {
                commander.LateDispose();
            }

            Assert.That(squadron.ReleaseListeners, Is.Zero);
            battleState.Set(BattleState.Running);
            Assert.DoesNotThrow(commander.Tick, "Disposed commanders must unsubscribe from battle state.");
        }

        [Test]
        public void ReserveSquadrons_AreTrackedOnceAndRejoinAfterPoolReuse()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            EntityLocator locator = new EntityLocator();
            FakeSquadron reserve = new FakeSquadron(1, TestPlayers.Enemy);
            FakeSquadron friendly = new FakeSquadron(2, TestPlayers.Human);
            locator.AddEntity(reserve);
            locator.AddEntity(friendly);
            UnitCombatProfileCatalog catalog = new UnitCombatProfileCatalog(null, null, null, null);
            UnitCombatProfile profile = new UnitCombatProfile(ShipClass.Fighter, 1f, 0f,
                Array.Empty<float>(), Array.Empty<float>(), Array.Empty<float>(), Array.Empty<UnitCombatProfile>());
            var profiles = (Dictionary<UnitTypeId, UnitCombatProfile>)typeof(UnitCombatProfileCatalog)
                .GetField("_profiles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(catalog);
            profiles.Add(UnitTypeId.Squadron(SquadronType.Vulture), profile);
            var battleState = new ReplayNotifier<BattleState>(BattleState.Running);
            EnemySquadronCommander commander = new EnemySquadronCommander(Array.Empty<IEnemyCaptureObjectiveSource>(), null, locator, roster,
                null, catalog, null, new TeamIntelRegistry(), roster.Get(TestPlayers.Enemy), null, null,
                battleState, new AiWorldSnapshot(locator), new EnemyAiScheduler(battleState), null);
            MethodInfo discover = typeof(EnemySquadronCommander).GetMethod("TrackOwnedSquadrons",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo track = typeof(EnemySquadronCommander).GetMethod("Track",
                BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(ISquadron), typeof(SquadronType) }, null);
            var tracked = (Dictionary<ISquadron, CommandedSquadron>)typeof(EnemySquadronCommander)
                .GetField("_squadrons", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(commander);
            try
            {
                discover.Invoke(commander, null);
                discover.Invoke(commander, null);
                track.Invoke(commander, new object[] { reserve, SquadronType.Vulture });
                Assert.That(tracked.Keys, Is.EquivalentTo(new[] { reserve }));
                Assert.That(tracked[reserve].Profile, Is.SameAs(profile));
                Assert.That(reserve.ReleaseListeners, Is.EqualTo(1));

                locator.RemoveEntity(reserve);
                reserve.Release();
                Assert.That(tracked, Is.Empty);
                Assert.That(reserve.ReleaseListeners, Is.Zero);
                locator.AddEntity(reserve);
                discover.Invoke(commander, null);
                Assert.That(tracked.Keys, Is.EquivalentTo(new[] { reserve }));
                Assert.That(reserve.ReleaseListeners, Is.EqualTo(1));
            }
            finally { commander.LateDispose(); }
            Assert.That(reserve.ReleaseListeners, Is.Zero);
        }

        [Test]
        public void HangarSquadrons_KeepEscortingTheirCarrier_WhileStationPurchasesTakeOrders()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            EntityLocator locator = new EntityLocator();
            FakeSquadron hangar = new FakeSquadron(1, TestPlayers.Enemy) { CurrentOrder = UnitOrderType.Guard };
            locator.AddEntity(hangar);
            UnitCombatProfileCatalog catalog = new UnitCombatProfileCatalog(null, null, null, null);
            UnitCombatProfile profile = new UnitCombatProfile(ShipClass.Fighter, 1f, 0f,
                Array.Empty<float>(), Array.Empty<float>(), Array.Empty<float>(), Array.Empty<UnitCombatProfile>());
            var profiles = (Dictionary<UnitTypeId, UnitCombatProfile>)typeof(UnitCombatProfileCatalog)
                .GetField("_profiles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(catalog);
            profiles.Add(UnitTypeId.Squadron(SquadronType.Vulture), profile);
            var battleState = new ReplayNotifier<BattleState>(BattleState.Running);
            EnemySquadronCommander commander = new EnemySquadronCommander(Array.Empty<IEnemyCaptureObjectiveSource>(), null, locator, roster,
                null, catalog, null, new TeamIntelRegistry(), roster.Get(TestPlayers.Enemy), null, null,
                battleState, new AiWorldSnapshot(locator), new EnemyAiScheduler(battleState), null);
            var tracked = (Dictionary<ISquadron, CommandedSquadron>)typeof(EnemySquadronCommander)
                .GetField("_squadrons", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(commander);
            MethodInfo issueOrder = typeof(EnemySquadronCommander).GetMethod("IssueOrder",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FakeSquadron purchased = new FakeSquadron(2, TestPlayers.Enemy);
            try
            {
                typeof(EnemySquadronCommander).GetMethod("TrackOwnedSquadrons",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(commander, null);
                typeof(EnemySquadronCommander).GetMethod("Track", BindingFlags.Instance | BindingFlags.NonPublic,
                        null, new[] { typeof(ISquadron), typeof(SquadronType) }, null)
                    .Invoke(commander, new object[] { purchased, SquadronType.Vulture });

                Assert.That(tracked[hangar].IsHangarLaunched, Is.True);
                Assert.That(tracked[purchased].IsHangarLaunched, Is.False);

                issueOrder.Invoke(commander, new object[] { hangar });
                Assert.That(hangar.OrdersReceived, Is.Zero, "A guarding hangar squadron must stay with its carrier.");
            }
            finally { commander.LateDispose(); }
        }

[TestCase(1, true, false, false)]
        [TestCase(2, true, false, true)]
        [TestCase(1, false, false, true)]
        [TestCase(2, true, true, false)]
        public void SquadronScout_LeavesAvailableSquadronToCapture(
            int squadronCount, bool hasMine, bool secondGuardsCarrier, bool expectsScout)
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            Vector3 minePosition = Vector3.right * 20f;
            SiteSource sites = hasMine
                ? new SiteSource(new CaptureSiteStub(minePosition))
                : new SiteSource();
            var locator = new EntityLocator();
            var battleState = new ReplayNotifier<BattleState>(BattleState.Running);
            var commander = new EnemySquadronCommander(SiteObjectives(sites, roster), new ZoneSource(roster), locator, roster,
                null, null, new ScoutPlacement(), new TeamIntelRegistry(), roster.Get(TestPlayers.Enemy), null, null,
                battleState, new AiWorldSnapshot(locator), new EnemyAiScheduler(battleState), null);
            var tracked = (Dictionary<ISquadron, CommandedSquadron>)typeof(EnemySquadronCommander)
                .GetField("_squadrons", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(commander);
            for (int i = 0; i < squadronCount; i++)
            {
                var squadron = new FakeSquadron(i + 1, TestPlayers.Enemy) { CurrentOrder = UnitOrderType.Guard };
                tracked.Add(squadron, i == 1 && secondGuardsCarrier
                    ? new CommandedSquadron(null, null, squadron)
                    : new CommandedSquadron(null, null));
            }

            typeof(EnemySquadronCommander).GetMethod("AssignCaptureTargets",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(commander, null);

            var scout = (ISquadron)typeof(EnemySquadronCommander)
                .GetField("_scout", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(commander);
            var assignments = (EnemySquadronCaptureAssignments)typeof(EnemySquadronCommander)
                .GetField("_captureAssignments", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(commander);
            Assert.That(scout != null, Is.EqualTo(expectsScout));
            int capturers = 0;
            foreach (ISquadron squadron in tracked.Keys)
            {
                if (!assignments.TryGetTarget(squadron, out Vector3 target)) continue;
                Assert.That(squadron, Is.Not.SameAs(scout));
                Assert.That(target, Is.EqualTo(minePosition));
                capturers++;
            }
            Assert.That(capturers, Is.EqualTo(hasMine ? 1 : 0));
        }

        [TestCase(3, false)]
        [TestCase(4, true)]
        public void SquadronRaid_UsesASurplusWingAgainstAnOuterMine(int wingCount, bool expectRaid)
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            PlayerSlot owner = roster.Get(TestPlayers.Enemy);
            var intel = new TeamIntelRegistry();
            var locator = new EntityLocator();
            var battleState = new ReplayNotifier<BattleState>(BattleState.Running);
            var zones = new ZoneSource(roster, 1, Vector3.right * 100f, Vector3.forward * 200f, Vector3.left * 300f);
            var commander = new EnemySquadronCommander(Array.Empty<IEnemyCaptureObjectiveSource>(), zones, locator, roster,
                null, null, new ScoutPlacement(), intel, owner, null, null, battleState,
                new AiWorldSnapshot(locator), new EnemyAiScheduler(battleState), null);
            var centreMine = new FakeEntity(new FakeHealthModel(null), TestPlayers.Human, 20) { IsMine = true, IsUnit = false };
            var outerMine = new FakeEntity(new FakeHealthModel(null), TestPlayers.Human, 21) { IsMine = true, IsUnit = false };
            locator.AddEntity(centreMine);
            locator.AddEntity(outerMine);
            intel.Get(owner.Team).ReportMine(new HostileSighting(centreMine.Id, default, centreMine.Owner,
                new FormationPoint(0f, 200f), 100f, 0f, 1f, Time.time));
            intel.Get(owner.Team).ReportMine(new HostileSighting(outerMine.Id, default, outerMine.Owner,
                new FormationPoint(0f, 300f), 100f, 0f, 1f, Time.time));
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var tracked = (Dictionary<ISquadron, CommandedSquadron>)typeof(EnemySquadronCommander)
                .GetField("_squadrons", flags).GetValue(commander);
            var damage = new float[ForceComposition.ClassSlots];
            damage[(int)ShipClass.Structure] = 10f;
            var profile = new UnitCombatProfile(ShipClass.Bomber, 100f, 0f, damage, damage,
                new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>());
            for (int i = 0; i < wingCount; i++)
                tracked.Add(new FakeSquadron(i + 1, owner.Id), new CommandedSquadron(profile, null));

            typeof(EnemySquadronCommander).GetMethod("AssignCaptureTargets", flags).Invoke(commander, null);

            var target = (IEntity)typeof(EnemySquadronCommander).GetField("_raidTarget", flags).GetValue(commander);
            Assert.That(target, expectRaid ? Is.SameAs(outerMine) : Is.Null);
            var assignments = (EnemySquadronCaptureAssignments)typeof(EnemySquadronCommander)
                .GetField("_captureAssignments", flags).GetValue(commander);
            Assert.That(tracked.Keys.Count(squadron => assignments.TryGetTarget(squadron, out _)), Is.EqualTo(3));
        }

        private sealed class ScoutPlacement : IEnemyStructurePlacementService
        {
            public bool IsRecovering => false;
            public bool TryGetPosition(out Vector3 position) { position = default; return false; }
            public bool TryGetScoutTarget(out Vector3 position) { position = Vector3.left * 100f; return true; }
            public void RecordPlacement(string request, Vector3 position) { }
            public void RecordDeferred(string request, float refund) { }
            public void RecordStationBuilt(Vector3 position) { }
            public void RecordDestroyedPosition(Vector3 position) { }
            public void Reset() { }
        }

        private sealed class FakeSquadron : IEntity, ISquadron, IUnitTypeFacade, IUnitOrderObserverFacade
        {
            public IEntity AttackTarget => null;
            public event Action Released;
            public UnitOrderType CurrentOrder { get; set; }
            public int OrdersReceived { get; private set; }
            public int ReleaseListeners => Released == null ? 0 : Released.GetInvocationList().Length;
            public long Id { get; }
            public long EntityId => Id;
            public PlayerId Owner { get; }
            public IHealthModelObserver HealthModel { get; } = new FakeHealthModel(null);
            public UnitTypeId UnitTypeId => UnitTypeId.Squadron(SquadronType.Vulture);
            public Vector3 WorldPosition => Vector3.zero;
            public FakeSquadron(long id, PlayerId owner) { Id = id; Owner = owner; }
            public void Guard(IEntity friendly, Vector3 offset) => OrdersReceived++;
            public void AttackMoveTo(Vector3 position) => OrdersReceived++;
            public void Hunt() => OrdersReceived++;
            public void Release() => Released();
            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade => (TFacade)(object)this;
            public bool TryGetFacade<TFacade>(out TFacade facade) where TFacade : IEntityFacade
            {
                if (this is TFacade found) { facade = found; return true; }
                facade = default;
                return false;
            }
        }

        [Test]
        public void IntelGatherer_DoesNotReportCloakedShipInVisibleTerrain()
        {
            GameObject view = new GameObject("Cloaked target");
            try
            {
                PlayerRoster roster = TestPlayers.CreateDuel();
                PlayerSlot owner = roster.Get(TestPlayers.Enemy);
                EntityLocator locator = new EntityLocator();
                FakeEntity target = new FakeEntity(new FakeHealthModel(view.transform), TestPlayers.Human, 1);
                target.Modifiers.SetCloaked(true);
                locator.AddEntity(target);
                TeamIntelRegistry registry = new TeamIntelRegistry();
                VisionService vision = new VisionService(roster, fogEnabled: false);
                AiWorldSnapshot snapshot = new AiWorldSnapshot(locator);
                TeamHostileIntelService gatherer = new TeamHostileIntelService(
                    locator, vision, roster, registry, snapshot);
                snapshot.Initialize();
                gatherer.Initialize();
                try
                {
                    gatherer.RegisterPlayer(owner);
                    gatherer.GatherAll(Time.time);
                    Assert.That(registry.Get(owner.Team).Sightings, Is.Empty);

                    float lastSeen = Time.time - 5f;
                    registry.Get(owner.Team).Report(new HostileSighting(target.Id,
                        UnitTypeId.Ship(ShipType.Venator), TestPlayers.Human,
                        new FormationPoint(100f, 0f), 1f, 0f, 1f, lastSeen));
                    gatherer.GatherAll(Time.time);
                    Assert.That(registry.Get(owner.Team).Sightings.Single().SeenAt, Is.EqualTo(lastSeen));
                    Assert.That(registry.Get(owner.Team).Sightings.Single().Position.X, Is.EqualTo(100f));

                    target.Modifiers.SetCloaked(false);
                    gatherer.GatherAll(Time.time);
                    Assert.That(registry.Get(owner.Team).Sightings.Count, Is.EqualTo(1));
                    Assert.That(registry.Get(owner.Team).Sightings.Single().Position.X, Is.Zero);
                }
                finally
                {
                    gatherer.LateDispose();
                    snapshot.LateDispose();
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(view); }
        }

        [TestCase("FindLocalThreat")]
        [TestCase("FindClosestKnownShip")]
        public void StrategicTarget_CloakedContactDoesNotHideValidAlternative(string selector)
        {
            GameObject view = new GameObject("Targets");
            try
            {
                PlayerRoster roster = TestPlayers.CreateDuel();
                PlayerSlot owner = roster.Get(TestPlayers.Enemy);
                EntityLocator locator = new EntityLocator();
                FakeEntity first = new FakeEntity(new FakeHealthModel(view.transform), TestPlayers.Human, 1);
                FakeEntity second = new FakeEntity(new FakeHealthModel(view.transform), TestPlayers.Human, 2);
                locator.AddEntity(first);
                locator.AddEntity(second);
                TeamIntelRegistry registry = new TeamIntelRegistry();
                HostileIntelModel intel = registry.Get(owner.Team);
                intel.Report(new HostileSighting(1, UnitTypeId.Ship(ShipType.Venator), TestPlayers.Human,
                    new FormationPoint(10f, 0f), 1f, 0f, 1f, 0f));
                intel.Report(new HostileSighting(2, UnitTypeId.Ship(ShipType.Venator), TestPlayers.Human,
                    new FormationPoint(20f, 0f), 1f, 0f, 1f, 0f));
                EnemyStrategicContextBuilder builder = CreateContextBuilder(
                    null, null, Array.Empty<IEnemyCaptureObjectiveSource>(), locator, null, roster, null, registry,
                    new VisionService(roster, fogEnabled: false), owner, null, new TestHazards(), null, null, new EnemyStationPressureTracker(), CreateRecovery(roster));
                MethodInfo find = typeof(EnemyStrategicContextBuilder).GetMethod(selector,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                object[] args = { Vector3.zero, 0f };
                Assert.That(find.Invoke(builder, args), Is.SameAs(first));
                // Build stores this result between decisions. Exercise the same cached-target path.
                typeof(EnemyStrategicContextBuilder).GetField("_localThreat",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(builder, first);

                first.Modifiers.SetCloaked(true);
                Assert.That(find.Invoke(builder, args), Is.SameAs(second));
                second.Modifiers.SetCloaked(true);
                Assert.That(find.Invoke(builder, args), Is.Null);
                first.Modifiers.SetCloaked(false);
                Assert.That(find.Invoke(builder, args), Is.SameAs(first));
            }
            finally { UnityEngine.Object.DestroyImmediate(view); }
        }

        [TestCase("FindLocalThreat", "removed")]
        [TestCase("FindLocalThreat", "destroyed")]
        [TestCase("FindLocalThreat", "replaced")]
        [TestCase("FindClosestKnownShip", "removed")]
        [TestCase("FindClosestKnownShip", "destroyed")]
        [TestCase("FindClosestKnownShip", "replaced")]
        public void StrategicTarget_InvalidCachedEntityIsNotRetained(string selector, string change)
        {
            GameObject view = new GameObject("Targets");
            try
            {
                PlayerRoster roster = TestPlayers.CreateDuel();
                PlayerSlot owner = roster.Get(TestPlayers.Enemy);
                EntityLocator locator = new EntityLocator();
                FakeEntity first = new FakeEntity(new FakeHealthModel(view.transform), TestPlayers.Human, 1);
                FakeEntity second = new FakeEntity(new FakeHealthModel(view.transform), TestPlayers.Human, 2);
                locator.AddEntity(first);
                locator.AddEntity(second);
                TeamIntelRegistry registry = new TeamIntelRegistry();
                HostileIntelModel intel = registry.Get(owner.Team);
                intel.Report(new HostileSighting(1, UnitTypeId.Ship(ShipType.Venator), TestPlayers.Human,
                    new FormationPoint(10f, 0f), 1f, 0f, 1f, 0f));
                intel.Report(new HostileSighting(2, UnitTypeId.Ship(ShipType.Venator), TestPlayers.Human,
                    new FormationPoint(20f, 0f), 1f, 0f, 1f, 0f));
                EnemyStrategicContextBuilder builder = CreateContextBuilder(
                    null, null, Array.Empty<IEnemyCaptureObjectiveSource>(), locator, null, roster, null, registry,
                    new VisionService(roster, fogEnabled: false), owner, null, new TestHazards(), null, null, new EnemyStationPressureTracker(), CreateRecovery(roster));
                MethodInfo find = typeof(EnemyStrategicContextBuilder).GetMethod(selector,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                object[] args = { Vector3.zero, 0f };
                Assert.That(find.Invoke(builder, args), Is.SameAs(first));
                // Build stores this result between decisions. Exercise the same cached-target path.
                typeof(EnemyStrategicContextBuilder).GetField("_localThreat",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(builder, first);

                typeof(EnemyStrategicContextBuilder).GetField("_fleetTarget",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(builder, first);
                if (change == "destroyed")
                    ((FakeHealthModel)first.HealthModel).IsDestroyed = true;
                else locator.RemoveEntity(first);
                if (change == "replaced")
                {
                    FakeEntity replacement = new FakeEntity(new FakeHealthModel(view.transform), TestPlayers.Human, 1);
                    locator.AddEntity(replacement);
                    Assert.That(find.Invoke(builder, args), Is.SameAs(replacement));
                }
                else
                    Assert.That(find.Invoke(builder, args), Is.SameAs(second));
            }
            finally { UnityEngine.Object.DestroyImmediate(view); }
        }

[TestCase(0, 100f, true)]
        [TestCase(3, 100f, false)]
        [TestCase(0, 2000f, false)]
        public void RaidDefense_UsesLocalCombatStrengthInsteadOfAnyShipPresence(
            int ownShipCount, float defenderDistance, bool expectedDefended)
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            PlayerSlot owner = roster.Get(TestPlayers.Enemy);
            EntityLocator locator = new EntityLocator();
            var catalog = new UnitCombatProfileCatalog(null, null, null, null);
            float[] damage = Enumerable.Repeat(10f, ForceComposition.ClassSlots).ToArray();
            var profile = new UnitCombatProfile(ShipClass.Capital, 100f, 0f,
                damage, damage, new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>());
            var profiles = (Dictionary<UnitTypeId, UnitCombatProfile>)typeof(UnitCombatProfileCatalog)
                .GetField("_profiles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(catalog);
            profiles.Add(UnitTypeId.Ship(ShipType.Venator), profile);
            var registry = new TeamIntelRegistry();
            registry.Get(owner.Team).Report(new HostileSighting(1, UnitTypeId.Ship(ShipType.Venator),
                TestPlayers.Human, new FormationPoint(defenderDistance, 0f), 100f, 0f, 1f, 0f));
            var builder = CreateContextBuilder(null, null, Array.Empty<IEnemyCaptureObjectiveSource>(), locator, null, roster,
                catalog, registry, new VisionService(roster, fogEnabled: false),
                owner, null, new TestHazards(), null, null, new EnemyStationPressureTracker(), CreateRecovery(roster));
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var ownForce = (ForceComposition)typeof(EnemyStrategicContextBuilder)
                .GetField("_ownForce", flags).GetValue(builder);
            for (int i = 0; i < ownShipCount; i++) ownForce.AddNew(profile, false);
            MethodInfo defended = typeof(EnemyStrategicContextBuilder).GetMethod("IsDefended", flags, null,
                new[] { typeof(Vector3), typeof(float) }, null);

            Assert.That(defended.Invoke(builder, new object[] { Vector3.zero, 0f }), Is.EqualTo(expectedDefended));
        }

        [TestCase(0f, false)]
        [TestCase(3000f, true)]
        public void RaidSelection_RejectsUndefendedTargetBehindStrongerRouteForce(float hostileZ, bool expectedRaid)
        {
            GameObject mineView = new GameObject("Mine");
            try
            {
                mineView.transform.position = Vector3.right * 3000f;
                PlayerRoster roster = TestPlayers.CreateDuel();
                PlayerSlot owner = roster.Get(TestPlayers.Enemy);
                EntityLocator locator = new EntityLocator();
                var mine = new FakeEntity(new FakeHealthModel(mineView.transform), TestPlayers.Human, 2)
                    { IsUnit = false, IsMine = true };
                locator.AddEntity(mine);
                var catalog = new UnitCombatProfileCatalog(null, null, null, null);
                float[] damage = Enumerable.Repeat(10f, ForceComposition.ClassSlots).ToArray();
                var profile = new UnitCombatProfile(ShipClass.Capital, 100f, 0f,
                    damage, damage, new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>());
                var profiles = (Dictionary<UnitTypeId, UnitCombatProfile>)typeof(UnitCombatProfileCatalog)
                    .GetField("_profiles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(catalog);
                profiles.Add(UnitTypeId.Ship(ShipType.Venator), profile);
                var registry = new TeamIntelRegistry();
                // Three hostiles midway: beyond the target's defense radius, but on or off the raid route.
                for (int i = 0; i < 3; i++)
                    registry.Get(owner.Team).Report(new HostileSighting(10 + i, UnitTypeId.Ship(ShipType.Venator),
                        TestPlayers.Human, new FormationPoint(1500f, hostileZ), 100f, 0f, 1f, 0f));
                var builder = CreateContextBuilder(null, null, Array.Empty<IEnemyCaptureObjectiveSource>(), locator, null, roster,
                    catalog, registry, new VisionService(roster, fogEnabled: false),
                    owner, null, new TestHazards(), null, null, new EnemyStationPressureTracker(), CreateRecovery(roster));
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                ((ForceComposition)typeof(EnemyStrategicContextBuilder).GetField("_localOwnForce", flags)
                    .GetValue(builder)).AddNew(profile, false);
                MethodInfo find = typeof(EnemyStrategicContextBuilder).GetMethod("FindRaidTarget", flags);

                object raid = find.Invoke(builder, new object[] { Vector3.zero, 0f });

                Assert.That(raid, expectedRaid ? Is.SameAs(mine) : Is.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(mineView); }
        }

[TestCase(false)]
        [TestCase(true)]
        public void RaidSelection_ExposedMineOutranksCloserStructureAndCachedRaid(bool cacheStructure)
        {
            GameObject structureView = new GameObject("Structure");
            GameObject mineView = new GameObject("Mine");
            try
            {
                structureView.transform.position = Vector3.right * 5f;
                mineView.transform.position = Vector3.right * 20f;
                PlayerRoster roster = TestPlayers.CreateDuel();
                EntityLocator locator = new EntityLocator();
                var structure = new FakeEntity(new FakeHealthModel(structureView.transform), TestPlayers.Human, 1)
                    { IsUnit = false };
                var mine = new FakeEntity(new FakeHealthModel(mineView.transform), TestPlayers.Human, 2)
                    { IsUnit = false, IsMine = true };
                locator.AddEntity(structure);
                var builder = CreateContextBuilder(null, null, Array.Empty<IEnemyCaptureObjectiveSource>(), locator, null, roster,
                    null, new TeamIntelRegistry(), new VisionService(roster, fogEnabled: false),
                    roster.Get(TestPlayers.Enemy), null, new TestHazards(), null, null, new EnemyStationPressureTracker(), CreateRecovery(roster));
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                MethodInfo find = typeof(EnemyStrategicContextBuilder).GetMethod("FindRaidTarget", flags);
                object[] args = { Vector3.zero, 0f };
                Assert.That(find.Invoke(builder, args), Is.SameAs(structure));
                if (cacheStructure)
                    typeof(EnemyStrategicContextBuilder).GetField("_raidTarget", flags).SetValue(builder, structure);

                locator.AddEntity(mine);

                Assert.That(find.Invoke(builder, args), Is.SameAs(mine));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(structureView);
                UnityEngine.Object.DestroyImmediate(mineView);
            }
        }

        [TestCase(true, true, false)]
        [TestCase(false, false, false)]
        [TestCase(true, false, true)]
        [TestCase(false, false, true)]
        public void RaidSelection_HiddenMinesRequireHostileTeamIntel(bool allied, bool visible, bool remembered)
        {
            GameObject structureView = new GameObject("Structure");
            GameObject mineView = new GameObject("Mine");
            GameObject scoutView = new GameObject("Scout");
            try
            {
                structureView.transform.position = Vector3.right * 5f;
                mineView.transform.position = Vector3.right * 100f;
                PlayerRoster roster = TestPlayers.CreateDuel();
                EntityLocator locator = new EntityLocator();
                var structure = new FakeEntity(new FakeHealthModel(structureView.transform), TestPlayers.Human, 1)
                    { IsUnit = false };
                var mine = new FakeEntity(new FakeHealthModel(mineView.transform),
                    allied ? TestPlayers.Enemy : TestPlayers.Human, 2) { IsUnit = false, IsMine = true };
                locator.AddEntity(mine);
                locator.AddEntity(structure);
                var vision = new VisionService(roster);
                vision.Register(TestPlayers.Enemy, scoutView.transform, visible ? 200f : 20f);
                var intel = new TeamIntelRegistry();
                if (remembered)
                    intel.Get(roster.Get(TestPlayers.Enemy).Team).ReportMine(new HostileSighting(
                        mine.Id, default, mine.Owner, new FormationPoint(100f, 0f), 100f, 0f, 1f, 0f));
                var builder = CreateContextBuilder(null, null, Array.Empty<IEnemyCaptureObjectiveSource>(), locator, null, roster,
                    null, intel, vision, roster.Get(TestPlayers.Enemy),
                    null, new TestHazards(), null, null, new EnemyStationPressureTracker(), CreateRecovery(roster));
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(EnemyStrategicContextBuilder).GetField("_raidTarget", flags).SetValue(builder, mine);
                MethodInfo find = typeof(EnemyStrategicContextBuilder).GetMethod("FindRaidTarget", flags);

                Assert.That(find.Invoke(builder, new object[] { Vector3.zero, 0f }),
                    Is.SameAs(!allied && remembered ? mine : structure));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(structureView);
                UnityEngine.Object.DestroyImmediate(mineView);
                UnityEngine.Object.DestroyImmediate(scoutView);
            }
        }

        [TestCase("FindClosestKnownShip")]
        [TestCase("FindLocalThreat")]
        [TestCase("FindRaidTarget")]
        public void RememberedTargets_RequireCurrentVisibilityIncludingCachedPick(string selector)
        {
            GameObject targetView = new GameObject("Target");
            GameObject scoutView = new GameObject("Scout");
            try
            {
                targetView.transform.position = Vector3.right * 10f;
                PlayerRoster roster = TestPlayers.CreateDuel();
                PlayerSlot owner = roster.Get(TestPlayers.Enemy);
                EntityLocator locator = new EntityLocator();
                FakeEntity target = new FakeEntity(new FakeHealthModel(targetView.transform), TestPlayers.Human, 1)
                    { IsUnit = selector != "FindRaidTarget" };
                locator.AddEntity(target);
                TeamIntelRegistry registry = new TeamIntelRegistry();
                if (target.IsUnit)
                    registry.Get(owner.Team).Report(new HostileSighting(1, UnitTypeId.Ship(ShipType.Venator),
                        TestPlayers.Human, new FormationPoint(10f, 0f), 1f, 0f, 1f, 0f));
                VisionService vision = new VisionService(roster);
                vision.Register(owner.Id, scoutView.transform, 20f);
                EnemyStrategicContextBuilder builder = CreateContextBuilder(
                    null, null, Array.Empty<IEnemyCaptureObjectiveSource>(), locator, null, roster, null, registry, vision, owner, null, new TestHazards(),
                    null, null, new EnemyStationPressureTracker(), CreateRecovery(roster));
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                MethodInfo find = typeof(EnemyStrategicContextBuilder).GetMethod(selector, flags);
                object[] args = { Vector3.zero, 0f };
                Assert.That(find.Invoke(builder, args), Is.SameAs(target));
                typeof(EnemyStrategicContextBuilder).GetField("_localThreat", flags).SetValue(builder, target);
                typeof(EnemyStrategicContextBuilder).GetField("_raidTarget", flags).SetValue(builder, target);

                // Last-seen position remains visible, but the target has moved outside team vision.
                targetView.transform.position = Vector3.right * 100f;
                Assert.That(find.Invoke(builder, args), Is.Null);
                scoutView.transform.position = targetView.transform.position;
                // EditMode assertions share a frame; registration refreshes the per-frame vision snapshot.
                vision.Register(owner.Id, scoutView.transform, 20f);
                Assert.That(find.Invoke(builder, args), Is.SameAs(target));
                vision.Unregister(scoutView.transform);
                Assert.That(find.Invoke(builder, args), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetView);
                UnityEngine.Object.DestroyImmediate(scoutView);
            }
        }

        [Test]
        public void CaptureZone_CommitsFastestShipAndStopsOnlyBusyRemainder()
        {
            FakeShip slow = new FakeShip(id: 1, position: Vector3.zero) { NavigationSpeed = 1f,
                CurrentOrder = UnitOrderType.Attack };
            FakeShip fast = new FakeShip(id: 2, position: Vector3.right) { NavigationSpeed = 4f };
            FakeOrderService orders = new FakeOrderService();
            EnemyStrategicContext context = Context(new IShipEntity[] { slow, fast });

            new EnemyTaskForceExecutor(orders).Execute(new EnemyStrategicDecision(
                state: EnemyStrategicState.CaptureZone, committedShipCount: 1, reason: "test"), context);

            Assert.That(orders.Calls[0].Action, Is.EqualTo(UnitActionId.AttackMove));
            Assert.That(orders.Calls[0].Receivers[0].Id, Is.EqualTo(fast.EntityId));
            Assert.That(orders.Calls[1].Action, Is.EqualTo(UnitActionId.Stop));
            Assert.That(orders.Calls[1].Receivers[0].Id, Is.EqualTo(slow.EntityId));
        }

        [Test]
        public void CaptureZone_LargeForceAssemblesBeforeAdvancing()
        {
            var ships = new List<IShipEntity>();
            for (int i = 0; i < 6; i++)
                ships.Add(new FakeShip(i < 4 ? Vector3.zero : Vector3.right * FleetCohesion.RADIUS * 3f, i + 1));
            var orders = new FakeOrderService();
            var executor = new EnemyTaskForceExecutor(orders);
            var decision = new EnemyStrategicDecision(state: EnemyStrategicState.CaptureZone,
                committedShipCount: 6, reason: "test");

            executor.Execute(decision, Context(ships));

            Assert.That(orders.Calls.Any(call => call.Action == UnitActionId.AttackMove), Is.False);
            Assert.That(executor.BodyCount, Is.EqualTo(4));
            Assert.That(executor.RallyingCount, Is.EqualTo(2));
            foreach (FakeShip ship in ships) ship.WorldPosition = Vector3.zero;
            orders.Calls.Clear();
            executor.Execute(decision, Context(ships));
            Assert.That(orders.Calls.Where(call => call.Action == UnitActionId.AttackMove)
                .Sum(call => call.Receivers.Count),
                Is.EqualTo(6));
        }

        [Test]
        public void HuntFleet_UsesAllUsefulCommittedShipsAgainstUndefendedTarget()
        {
            var screen = new FakeShip(Vector3.zero, 1);
            var strike = new FakeShip(Vector3.zero, 2);
            GameObject view = new GameObject("Target");
            try
            {
                var target = new FakeEntity(new FakeHealthModel(view.transform) { Hull = 100f, Shields = 0f },
                    TestPlayers.Human, 3);
                var forces = new Dictionary<IShipEntity, ForceComposition>
                {
                    [screen] = CombatForce(100f, 0.1f, 20f),
                    [strike] = CombatForce(100f, 20f)
                };
                var orders = new FakeOrderService();
                var executor = new EnemyTaskForceExecutor(orders);
                executor.Execute(new EnemyStrategicDecision(state: EnemyStrategicState.HuntFleet, committedShipCount: 2, reason: "test"),
                    Context(new IShipEntity[] { screen, strike }, target, shipForces: forces));

                OrderCall attack = orders.Calls.Single(call => call.Action == UnitActionId.Attack);
                Assert.That(attack.Receivers.Select(entity => entity.Id),
                    Is.EquivalentTo(new long[] { strike.EntityId, screen.EntityId }));
                Assert.That(orders.Calls.Any(call => call.Action == UnitActionId.Guard), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(view); }
        }

        [Test]
        public void HuntFleet_SelectedAntiFighterCounterActivelyEngagesDefenders()
        {
            var screen = new FakeShip(Vector3.zero, 1);
            var strike = new FakeShip(Vector3.zero, 2);
            GameObject view = new GameObject("Target");
            try
            {
                var target = new FakeEntity(new FakeHealthModel(view.transform) { Hull = 100f, Shields = 0f },
                    TestPlayers.Human, 3);
                var forces = new Dictionary<IShipEntity, ForceComposition>
                {
                    [screen] = CombatForce(100f, 0f, 20f),
                    [strike] = CombatForce(100f, 20f)
                };
                var defenders = new Dictionary<IEntity, ForceComposition>
                {
                    [target] = CombatForce(100f, 10f, shipClass: ShipClass.Fighter)
                };
                var orders = new FakeOrderService();
                var executor = new EnemyTaskForceExecutor(orders);
                var decision = new EnemyStrategicDecision(state: EnemyStrategicState.HuntFleet,
                    committedShipCount: 2, reason: "test");
                EnemyStrategicContext context = Context(new IShipEntity[] { screen, strike }, target,
                    shipForces: forces, targetForces: defenders);

                Assert.That(executor.CanAssemble(decision, context), Is.True);
                executor.Execute(decision, context);

                Assert.That(executor.BodyCount, Is.EqualTo(2), "Both strike and counter are required for readiness.");
                OrderCall attack = orders.Calls.Single(call => call.Action == UnitActionId.Attack);
                Assert.That(attack.Receivers.Select(entity => entity.Id), Is.EqualTo(new long[] { strike.EntityId }));
                OrderCall screenOrder = orders.Calls.Single(call => call.Action == UnitActionId.AttackMove);
                Assert.That(screenOrder.Receivers.Select(entity => entity.Id), Is.EqualTo(new long[] { screen.EntityId }));
                Assert.That(orders.Calls.Any(call => call.Action == UnitActionId.Guard), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(view); }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void HuntFleet_WaitsForRequiredStrikeMembersBeforeLaunching(bool defended)
        {
            var first = new FakeShip(Vector3.zero, 1);
            var second = new FakeShip(Vector3.right * FleetCohesion.RADIUS * 3f, 2);
            GameObject view = new GameObject("Target");
            try
            {
                var target = new FakeEntity(new FakeHealthModel(view.transform) { Hull = 100f, Shields = 0f },
                    TestPlayers.Human, 3);
                var defenders = new Dictionary<IEntity, ForceComposition>
                {
                    [target] = defended ? CombatForce(150f, 15f) : new ForceComposition()
                };
                var orders = new FakeOrderService();
                var executor = new EnemyTaskForceExecutor(orders);
                var decision = new EnemyStrategicDecision(state: EnemyStrategicState.HuntFleet, committedShipCount: 2, reason: "test");
                executor.Execute(decision, Context(new IShipEntity[] { first, second }, target,
                    targetForces: defenders));

                Assert.That(orders.Calls.Any(call => call.Action == UnitActionId.Attack), Is.False);
                OrderCall rally = orders.Calls.Single(call => call.Action == UnitActionId.Guard);
                Assert.That(rally.Receivers.Select(entity => entity.Id), Is.EqualTo(new long[] { second.EntityId }));
                Assert.That(rally.Target.Id, Is.EqualTo(first.EntityId));

                second.WorldPosition = Vector3.right * 10f;
                orders.Calls.Clear();
                executor.Execute(decision, Context(new IShipEntity[] { first, second }, target,
                    targetForces: defenders));

                OrderCall attack = orders.Calls.Single(call => call.Action == UnitActionId.Attack);
                Assert.That(attack.Receivers.Select(entity => entity.Id),
                    Is.EquivalentTo(new long[] { first.EntityId, second.EntityId }));
            }
            finally { UnityEngine.Object.DestroyImmediate(view); }
        }

        [Test]
        public void HuntFleet_StopsAllocatingScreensOnceDefenderDemandIsCovered()
        {
            var strike = new FakeShip(Vector3.zero, 1);
            var first = new FakeShip(Vector3.zero, 2);
            var second = new FakeShip(Vector3.zero, 3);
            var third = new FakeShip(Vector3.zero, 4);
            GameObject view = new GameObject("Target");
            try
            {
                var target = new FakeEntity(new FakeHealthModel(view.transform) { Hull = 100f, Shields = 0f },
                    TestPlayers.Human, 5);
                var forces = new Dictionary<IShipEntity, ForceComposition>
                {
                    [strike] = CombatForce(100f, 20f),
                    [first] = CombatForce(100f, 1f, 100f),
                    [second] = CombatForce(100f, 1f, 100f),
                    [third] = CombatForce(100f, 1f, 100f)
                };
                var defenders = new Dictionary<IEntity, ForceComposition>
                {
                    [target] = CombatForce(100f, 10f, shipClass: ShipClass.Fighter)
                };
                var orders = new FakeOrderService();
                new EnemyTaskForceExecutor(orders).Execute(new EnemyStrategicDecision(
                    state: EnemyStrategicState.HuntFleet, committedShipCount: 4, reason: "test"),
                    Context(new IShipEntity[] { strike, first, second, third }, target,
                        shipForces: forces, targetForces: defenders));

                Assert.That(orders.Calls.Single(call => call.Action == UnitActionId.Attack).Receivers.Count,
                    Is.EqualTo(3));
                Assert.That(orders.Calls.Single(call => call.Action == UnitActionId.AttackMove).Receivers.Count,
                    Is.EqualTo(1));
                Assert.That(orders.Calls.Any(call => call.Action == UnitActionId.Guard), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(view); }
        }

        [Test]
        public void DefendBase_InterceptsSiegeAt1755ButKeepsLeashBounded()
        {
            var ship = new FakeShip(Vector3.zero, 1);
            var stationView = new GameObject("Station");
            var attackerView = new GameObject("Siege attacker");
            try
            {
                var station = new FakeEntity(new FakeHealthModel(stationView.transform), TestPlayers.Enemy, 2);
                var attacker = new FakeEntity(new FakeHealthModel(attackerView.transform), TestPlayers.Human, 3)
                    { WeaponRange = 1755f };
                attackerView.transform.position = Vector3.right * 1755f;
                var orders = new FakeOrderService();
                var executor = new EnemyTaskForceExecutor(orders);
                var decision = new EnemyStrategicDecision("test", EnemyStrategicState.DefendBase, 1);
                executor.Execute(decision, Context(new[] { ship }, ownBase: station, defenseTarget: attacker));
                Assert.That(orders.Calls.Single().Action, Is.EqualTo(UnitActionId.Attack));
                Assert.That(orders.Calls.Single().Target, Is.SameAs(attacker));

                orders.Calls.Clear();
                attackerView.transform.position = Vector3.right * 5000f;
                executor.Execute(decision, Context(new[] { ship }, ownBase: station, defenseTarget: attacker));
                Assert.That(orders.Calls.Any(call => call.Action == UnitActionId.Attack), Is.False);
                Assert.That(orders.Calls.Single().Point.magnitude,
                    Is.LessThanOrEqualTo(station.NavigationRadius + 1755f + 300f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(attackerView);
                UnityEngine.Object.DestroyImmediate(stationView);
            }
        }

        [Test]
        public void DefendBase_SameRangeShipsReceiveSeparateThreatFacingSlots()
        {
            var first = new FakeShip(Vector3.zero, 1);
            var second = new FakeShip(Vector3.zero, 2);
            var stationView = new GameObject("Station");
            try
            {
                var station = new FakeEntity(new FakeHealthModel(stationView.transform), TestPlayers.Enemy, 3);
                var pressure = new EnemyStationPressureTracker();
                pressure.Update(station.Id, 100f, 100f, Time.time, station.Id);
                pressure.ObserveThreatPosition(0f, 0f, 2000f);
                var orders = new FakeOrderService();
                var context = Context(new IShipEntity[] { first, second }, ownBase: station, stationPressure: pressure);
                foreach (FakeEntity receiver in context.Receivers.Values) receiver.WeaponRange = 500f;
                new EnemyTaskForceExecutor(orders).Execute(
                    new EnemyStrategicDecision("test", EnemyStrategicState.DefendBase, 2),
                    context);

                OrderCall call = orders.Calls.Single();
                Assert.That(call.Receivers.Count, Is.EqualTo(2));
                Assert.That(call.Destinations[0].x * call.Destinations[1].x, Is.LessThan(0f));
                Assert.That(call.Destinations.All(point => point.z > 0f), Is.True);
                Assert.That(Vector3.Distance(call.Destinations[0], call.Destinations[1]),
                    Is.GreaterThan(first.NavigationRadius + second.NavigationRadius));
            }
            finally { UnityEngine.Object.DestroyImmediate(stationView); }
        }


        [TestCase(true)]
        [TestCase(false)]
        public void DefendBase_FacesKnownThreatOrUsesRingWithoutRepullingCompletedOrders(bool hasBearing)
        {
            var ship = new FakeShip(Vector3.right * 4000f, 1);
            GameObject stationView = new GameObject("Station");
            try
            {
                stationView.transform.position = Vector3.left * 1000f;
                var station = new FakeEntity(new FakeHealthModel(stationView.transform), TestPlayers.Enemy, 2);
                var pressure = new EnemyStationPressureTracker();
                pressure.Update(station.Id, 100f, 100f, Time.time);
                pressure.Update(station.Id, 99f, 100f, Time.time, station.Id);
                if (hasBearing) pressure.ObserveThreatPosition(-1000f, 0f, 4000f);
                var orders = new FakeOrderService();
                var executor = new EnemyTaskForceExecutor(orders);
                var decision = new EnemyStrategicDecision(state: EnemyStrategicState.DefendBase,
                    committedShipCount: 1, reason: "test");
                EnemyStrategicContext context = Context(new[] { ship }, ownBase: station, stationPressure: pressure);
                ((FakeEntity)context.Receivers[ship]).WeaponRange = 5000f;

                executor.Execute(decision, context);

                Assert.That(orders.Calls.Single().Action, Is.EqualTo(UnitActionId.AttackMove));
                Vector3 offset = orders.Calls.Single().Point - stationView.transform.position;
                Assert.That(offset.magnitude, Is.GreaterThan(station.NavigationRadius));
                Assert.That(offset.magnitude, Is.LessThanOrEqualTo(station.NavigationRadius + 1500f));
                Assert.That(Vector3.Dot(offset.normalized, hasBearing ? Vector3.forward : Vector3.right),
                    Is.GreaterThan(0.99f));
                ship.CurrentOrder = UnitOrderType.None;
                orders.Calls.Clear();
                executor.Execute(decision, context);
                Assert.That(orders.Calls, Is.Empty, "Completed defense orders must not pull ships home again.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stationView);
            }
        }

        [Test]
        public void HuntFleet_WaitingBodyKeepsExistingGuardOrders()
        {
            var anchor = new FakeShip(Vector3.zero, 1);
            var escort = new FakeShip(Vector3.right * 10f, 2);
            var distant = new FakeShip(Vector3.right * FleetCohesion.RADIUS * 3f, 3);
            GameObject view = new GameObject("Target");
            try
            {
                var target = new FakeEntity(new FakeHealthModel(view.transform) { Hull = 100f, Shields = 0f },
                    TestPlayers.Human, 4);
                var defenders = new Dictionary<IEntity, ForceComposition> { [target] = CombatForce(250f, 25f) };
                var orders = new FakeOrderService();
                var executor = new EnemyTaskForceExecutor(orders);
                var decision = new EnemyStrategicDecision(state: EnemyStrategicState.HuntFleet,
                    committedShipCount: 3, reason: "test");
                EnemyStrategicContext context = Context(new IShipEntity[] { anchor, escort, distant }, target,
                    targetForces: defenders);
                executor.Execute(decision, context);
                Assert.That(orders.Calls.Any(call => call.Action == UnitActionId.Attack), Is.False);
                Assert.That(orders.Calls.Where(call => call.Action == UnitActionId.Guard)
                    .SelectMany(call => call.Receivers).Select(entity => entity.Id),
                    Is.EquivalentTo(new long[] { escort.EntityId, distant.EntityId }));
                escort.CurrentOrder = UnitOrderType.Guard;
                distant.CurrentOrder = UnitOrderType.Guard;
                orders.Calls.Clear();

                executor.Execute(decision, context);

                Assert.That(orders.Calls, Is.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(view); }
        }

        [TestCase(EnemyStrategicState.DefendBase)]
        [TestCase(EnemyStrategicState.EngageLocal)]
        public void EmergencyCombat_DoesNotWaitForOffensiveStrikeReadiness(EnemyStrategicState state)
        {
            var ship = new FakeShip(Vector3.zero, 1);
            GameObject view = new GameObject("Threat");
            try
            {
                var target = new FakeEntity(new FakeHealthModel(view.transform) { Hull = 1000f, Shields = 0f },
                    TestPlayers.Human, 2);
                var defenders = new Dictionary<IEntity, ForceComposition> { [target] = CombatForce(1000f, 100f) };
                var orders = new FakeOrderService();
                var executor = new EnemyTaskForceExecutor(orders);
                EnemyStrategicContext context = Context(new IShipEntity[] { ship }, fleetTarget: target,
                    defenseTarget: target, localThreatTarget: target, targetForces: defenders);
                executor.Execute(new EnemyStrategicDecision(state: EnemyStrategicState.HuntFleet,
                    committedShipCount: 1, reason: "test"), context);
                Assert.That(orders.Calls.Any(call => call.Action == UnitActionId.Attack), Is.False);
                orders.Calls.Clear();

                executor.Execute(new EnemyStrategicDecision(state: state, committedShipCount: 1, reason: "test"),
                    context);

                OrderCall attack = orders.Calls.Single(call => call.Action == UnitActionId.Attack);
                Assert.That(attack.Target, Is.SameAs(target));
                Assert.That(attack.Receivers.Single().Id, Is.EqualTo(ship.EntityId));
            }
            finally { UnityEngine.Object.DestroyImmediate(view); }
        }

        [Test]
        public void HuntFleet_PreservesSurvivorOffsetsAcrossDecisions()
        {
            FakeShip first = new FakeShip(id: 1, position: Vector3.zero);
            FakeShip second = new FakeShip(id: 2, position: Vector3.zero);
            FakeShip third = new FakeShip(id: 3, position: Vector3.zero);
            GameObject view = new GameObject("Target");
            FakeEntity target = new FakeEntity(id: 4, side: TestPlayers.Human,
                health: new FakeHealthModel(view.transform) { Hull = 900f, Shields = 0f });
            FakeOrderService orders = new FakeOrderService();
            EnemyTaskForceExecutor executor = new EnemyTaskForceExecutor(orders);
            try
            {
                executor.Execute(new EnemyStrategicDecision(
                    state: EnemyStrategicState.HuntFleet, committedShipCount: 3, reason: "test"),
                    Context(new IShipEntity[] { first, second, third }, target));
                OrderCall firstAttack = orders.Calls.Single(call => call.Action == UnitActionId.Attack);
                Vector3 secondOffset = firstAttack.Offsets[1];
                Vector3 thirdOffset = firstAttack.Offsets[2];
                orders.Calls.Clear();

                executor.Execute(new EnemyStrategicDecision(
                    state: EnemyStrategicState.HuntFleet, committedShipCount: 2, reason: "test"),
                    Context(new IShipEntity[] { second, third }, target));

                OrderCall secondAttack = orders.Calls.Single(call => call.Action == UnitActionId.Attack);
                Assert.That(secondAttack.Offsets[0], Is.EqualTo(secondOffset));
                Assert.That(secondAttack.Offsets[1], Is.EqualTo(thirdOffset));
            }
            finally { UnityEngine.Object.DestroyImmediate(view); }
        }

        [TestCase(1400f, UnitActionId.Attack)]
        [TestCase(2000f, UnitActionId.AttackMove)]
        public void DefendBase_InterceptsArrivedRetreaterOnlyWithinStationLeash(float distance, UnitActionId action)
        {
            FakeShip defender = new FakeShip(id: 1, position: Vector3.zero);
            FakeShip idle = new FakeShip(id: 2, position: Vector3.right);
            GameObject view = new GameObject("Station attacker");
            GameObject stationView = new GameObject("Station");
            FakeEntity station = new FakeEntity(id: 3, side: TestPlayers.Enemy,
                health: new FakeHealthModel(stationView.transform));
            FakeEntity attacker = new FakeEntity(id: 4, side: TestPlayers.Human,
                health: new FakeHealthModel(view.transform));
            FakeOrderService orders = new FakeOrderService();
            EnemyTaskForceExecutor executor = new EnemyTaskForceExecutor(orders);
            try
            {
                view.transform.position = Vector3.forward * distance;
                EnemyStrategicContext context = Context(new IShipEntity[] { defender, idle },
                    ownBase: station, defenseTarget: attacker);
                executor.Execute(new EnemyStrategicDecision(
                    state: EnemyStrategicState.RetreatValue, committedShipCount: 2, reason: "test"), context);
                orders.Calls.Clear();

                executor.Execute(new EnemyStrategicDecision(
                    state: EnemyStrategicState.DefendBase, committedShipCount: 1, reason: "test"), context);

                Assert.That(orders.Calls.Count, Is.EqualTo(1));
                Assert.That(orders.Calls[0].Action, Is.EqualTo(action));
                if (action == UnitActionId.Attack)
                    Assert.That(orders.Calls[0].Target, Is.SameAs(attacker));
                else
                    Assert.That(orders.Calls[0].Point.magnitude, Is.LessThanOrEqualTo(station.NavigationRadius + 1500f));
                Assert.That(orders.Calls[0].Receivers[0].Id, Is.EqualTo(defender.EntityId));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(view);
                UnityEngine.Object.DestroyImmediate(stationView);
            }
        }

        [Test]
        public void RetreatValue_IssuesRetreatForAssignedGroupWithoutOwnBase()
        {
            FakeOrderService orders = new FakeOrderService();
            new EnemyTaskForceExecutor(orders).Execute(new EnemyStrategicDecision(
                state: EnemyStrategicState.RetreatValue, committedShipCount: 1, reason: "test"),
                Context(new IShipEntity[] { new FakeShip(id: 1, position: Vector3.zero),
                    new FakeShip(id: 2, position: Vector3.right) }));
            Assert.That(orders.Calls.Count, Is.EqualTo(1));
            Assert.That(orders.Calls[0].Action, Is.EqualTo(UnitActionId.Retreat));
            Assert.That(orders.Calls[0].Receivers.Count, Is.EqualTo(2));
        }

        [Test]
        public void RetreatValue_DoesNotReissueRetreatToRetreatingShips()
        {
            FakeShip retreating = new FakeShip(id: 1, position: Vector3.zero)
                { CurrentOrder = UnitOrderType.Retreat };
            FakeShip fresh = new FakeShip(id: 2, position: Vector3.right);
            FakeOrderService orders = new FakeOrderService();

            new EnemyTaskForceExecutor(orders).Execute(new EnemyStrategicDecision(
                state: EnemyStrategicState.RetreatValue, committedShipCount: 2, reason: "test"),
                Context(new IShipEntity[] { retreating, fresh }));

            Assert.That(orders.Calls.Count, Is.EqualTo(1));
            Assert.That(orders.Calls[0].Receivers.Count, Is.EqualTo(1));
            Assert.That(orders.Calls[0].Receivers[0].Id, Is.EqualTo(fresh.EntityId));
        }

        [Test]
        public void RetreatValue_ArrivalDoesNotReissueUntilStrategyChanges()
        {
            FakeShip ship = new FakeShip(id: 1, position: Vector3.zero);
            FakeOrderService orders = new FakeOrderService();
            EnemyTaskForceExecutor executor = new EnemyTaskForceExecutor(orders);
            EnemyStrategicContext context = Context(new IShipEntity[] { ship });
            EnemyStrategicDecision retreat = new EnemyStrategicDecision(
                state: EnemyStrategicState.RetreatValue, committedShipCount: 1, reason: "test");
            executor.Execute(retreat, context);
            orders.Calls.Clear();
            executor.Execute(retreat, context);
            Assert.That(orders.Calls, Is.Empty);
            executor.Execute(new EnemyStrategicDecision(
                state: EnemyStrategicState.Hold, committedShipCount: 0, reason: "test"), context);
            executor.Execute(retreat, context);
            Assert.That(orders.Calls.Count, Is.EqualTo(1));
        }

        [Test]
        public void CaptureZone_ArrivalStaysAssignedAndReinforcementJoinsOneGroupOrder()
        {
            FakeShip ship = new FakeShip(id: 1, position: Vector3.zero);
            FakeShip reinforcement = new FakeShip(id: 2, position: Vector3.right);
            FakeOrderService orders = new FakeOrderService();
            EnemyTaskForceExecutor executor = new EnemyTaskForceExecutor(orders);
            EnemyStrategicDecision decision = new EnemyStrategicDecision(
                state: EnemyStrategicState.CaptureZone, committedShipCount: 2, reason: "test");
            executor.Execute(decision, Context(new IShipEntity[] { ship }));
            ship.WorldPosition = new Vector3(55f, 0f, -55f);
            orders.Calls.Clear();

            executor.Execute(decision, Context(new IShipEntity[] { ship }));
            Assert.That(orders.Calls, Is.Empty);

            executor.Execute(decision, Context(new IShipEntity[] { ship, reinforcement }));
            Assert.That(orders.Calls.Count, Is.EqualTo(1));
            Assert.That(orders.Calls[0].Receivers.Select(receiver => receiver.Id),
                Is.EquivalentTo(new[] { ship.EntityId, reinforcement.EntityId }));
        }

        [Test]
        public void CaptureZone_SameTargetSkipsShipsAlreadyAttackMoving()
        {
            FakeShip ship = new FakeShip(id: 1, position: Vector3.zero);
            FakeOrderService orders = new FakeOrderService();
            EnemyTaskForceExecutor executor = new EnemyTaskForceExecutor(orders);
            EnemyStrategicDecision decision = new EnemyStrategicDecision(
                state: EnemyStrategicState.CaptureZone, committedShipCount: 1, reason: "test");
            executor.Execute(decision, Context(new IShipEntity[] { ship }));
            ship.CurrentOrder = UnitOrderType.AttackMove;
            orders.Calls.Clear();

            executor.Execute(decision, Context(new IShipEntity[] { ship }));

            Assert.That(orders.Calls, Is.Empty);
        }

        [Test]
        public void CaptureZone_StoppedOutsideObjectiveReissuesOneSharedGroupOrder()
        {
            FakeShip stopped = new FakeShip(id: 1, position: Vector3.zero);
            FakeShip engaging = new FakeShip(id: 2, position: Vector3.right);
            FakeOrderService orders = new FakeOrderService();
            EnemyTaskForceExecutor executor = new EnemyTaskForceExecutor(orders);
            EnemyStrategicDecision decision = new EnemyStrategicDecision(
                state: EnemyStrategicState.CaptureZone, committedShipCount: 2, reason: "test");
            EnemyStrategicContext context = Context(new IShipEntity[] { stopped, engaging });
            executor.Execute(decision, context);
            engaging.CurrentOrder = UnitOrderType.AttackMove;
            orders.Calls.Clear();

            executor.Execute(decision, context);

            Assert.That(orders.Calls.Count, Is.EqualTo(1));
            Assert.That(orders.Calls[0].Action, Is.EqualTo(UnitActionId.AttackMove));
            Assert.That(orders.Calls[0].Receivers.Select(receiver => receiver.Id),
                Is.EquivalentTo(new[] { stopped.EntityId, engaging.EntityId }));
        }

        [TestCase("relay-2", EnemyCaptureObjectiveType.ReinforcementZone, false)]
        [TestCase("relay-1", EnemyCaptureObjectiveType.Facility, false)]
        [TestCase("relay-1", EnemyCaptureObjectiveType.ReinforcementZone, true)]
        public void CaptureZone_ObjectiveIdentityOrOwnerChangeReassignsActiveShip(
            string id, EnemyCaptureObjectiveType kind, bool hostileOwner)
        {
            FakeShip ship = new FakeShip(id: 1, position: Vector3.zero);
            FakeOrderService orders = new FakeOrderService();
            EnemyTaskForceExecutor executor = new EnemyTaskForceExecutor(orders);
            EnemyStrategicDecision decision = new EnemyStrategicDecision(
                state: EnemyStrategicState.CaptureZone, committedShipCount: 1, reason: "test");
            executor.Execute(decision, Context(new IShipEntity[] { ship }));
            ship.CurrentOrder = UnitOrderType.AttackMove;
            orders.Calls.Clear();
            EnemyCaptureObjective replacement = new EnemyCaptureObjective(id, kind,
                hostileOwner ? TestPlayers.Human : PlayerId.None, new Vector3(55f, 0f, -55f), 10f, 0f);

            executor.Execute(decision, Context(new IShipEntity[] { ship }, captureObjective: replacement));

            Assert.That(orders.Calls.Count, Is.EqualTo(1));
            Assert.That(orders.Calls[0].Action, Is.EqualTo(UnitActionId.AttackMove));
        }

        [Test]
        public void CaptureZone_DistantCombatDiversionReturnsToObjective()
        {
            FakeShip ship = new FakeShip(id: 1, position: Vector3.zero);
            FakeOrderService orders = new FakeOrderService();
            EnemyTaskForceExecutor executor = new EnemyTaskForceExecutor(orders);
            EnemyStrategicDecision decision = new EnemyStrategicDecision(
                state: EnemyStrategicState.CaptureZone, committedShipCount: 1, reason: "test");
            executor.Execute(decision, Context(new IShipEntity[] { ship }));
            ship.CurrentOrder = UnitOrderType.AttackMove;
            ship.WorldPosition = Vector3.right * (FleetCohesion.RADIUS * 4f);
            orders.Calls.Clear();

            executor.Execute(decision, Context(new IShipEntity[] { ship }));

            // A diverted ship returns in combat posture, to the circle edge on its side, never by a blind Move.
            Assert.That(orders.Calls.Count, Is.EqualTo(1));
            Assert.That(orders.Calls[0].Action, Is.EqualTo(UnitActionId.AttackMove));
            Assert.That(Vector3.Distance(orders.Calls[0].Point, new Vector3(55f, 0f, -55f)), Is.EqualTo(10f * 0.9f).Within(0.01f));
        }

        [Test]
        public void CaptureZone_StationaryAttackMoveIsOverriddenUntilInsideTheCircle()
        {
            var ship = new FakeShip(Vector3.zero, 1);
            var orders = new FakeOrderService();
            var executor = new EnemyTaskForceExecutor(orders);
            var decision = new EnemyStrategicDecision("test", EnemyStrategicState.CaptureZone, 1);
            var context = Context(new IShipEntity[] { ship });
            executor.Execute(decision, context, now: 0f);
            ship.CurrentOrder = UnitOrderType.AttackMove;
            orders.Calls.Clear();

            executor.Execute(decision, context, now: 4f);
            Assert.That(orders.Calls, Is.Empty);
            executor.Execute(decision, context, now: 5f);
            Assert.That(orders.Calls.Single().Action, Is.EqualTo(UnitActionId.AttackMove));

            // A ship left in a plain Move outside the circle is put back into combat posture.
            ship.CurrentOrder = UnitOrderType.Move;
            orders.Calls.Clear();
            executor.Execute(decision, context, now: 6f);
            Assert.That(orders.Calls.Single().Action, Is.EqualTo(UnitActionId.AttackMove));
        }

        [Test]
        public void CaptureZone_AttacksBlockingMineBeforeMovingIntoItsSite()
        {
            var facility = new FakeEntity(new FakeHealthModel(null), TestPlayers.Human, 2) { IsMine = true };
            var ships = new IShipEntity[] { new FakeShip(Vector3.zero, 1) };
            var orders = new FakeOrderService();
            var executor = new EnemyTaskForceExecutor(orders);
            var decision = new EnemyStrategicDecision("test", EnemyStrategicState.CaptureZone, 1);
            var blocked = new EnemyCaptureObjective("site:0", EnemyCaptureObjectiveType.Facility, TestPlayers.Human,
                Vector3.right * 50f, 10f, 100f, isMine: true, blockingFacilityId: facility.Id);

            executor.Execute(decision, Context(ships, captureObjective: blocked, captureBlockingFacility: facility));
            Assert.That(orders.Calls.Single().Action, Is.EqualTo(UnitActionId.Attack));
            Assert.That(orders.Calls.Single().Target, Is.SameAs(facility));

            orders.Calls.Clear();
            var clear = new EnemyCaptureObjective(blocked.Id, blocked.Kind, PlayerId.None,
                blocked.Position, blocked.Radius, 0f, isMine: true);
            executor.Execute(decision, Context(ships, captureObjective: clear));
            Assert.That(orders.Calls.Single().Action, Is.EqualTo(UnitActionId.AttackMove));
            Assert.That(orders.Calls.Single().Point, Is.EqualTo(clear.Position));
        }

        [Test]
        public void RetreatValue_ChangedAnchorRedirectsRetreatingShip()
        {
            FakeShip ship = new FakeShip(id: 1, position: Vector3.zero);
            FakeOrderService orders = new FakeOrderService();
            EnemyTaskForceExecutor executor = new EnemyTaskForceExecutor(orders);
            EnemyStrategicDecision decision = new EnemyStrategicDecision(
                state: EnemyStrategicState.RetreatValue, committedShipCount: 1, reason: "test");
            executor.Execute(decision, Context(new IShipEntity[] { ship }, retreatAnchor: Vector3.left * 100f));
            ship.CurrentOrder = UnitOrderType.Retreat;
            orders.Calls.Clear();
            Vector3 newAnchor = Vector3.forward * 200f;

            executor.Execute(decision, Context(new IShipEntity[] { ship }, retreatAnchor: newAnchor));

            Assert.That(orders.Calls.Count, Is.EqualTo(1));
            Assert.That(orders.Calls[0].Action, Is.EqualTo(UnitActionId.Retreat));
            Assert.That(orders.Calls[0].Point, Is.EqualTo(newAnchor));
        }

        [Test]
        public void RetreatValue_NoSafeAnchorStopsTravelToLostHome()
        {
            FakeShip ship = new FakeShip(id: 1, position: Vector3.zero)
                { CurrentOrder = UnitOrderType.Retreat };
            FakeOrderService orders = new FakeOrderService();
            EnemyTaskForceExecutor executor = new EnemyTaskForceExecutor(orders);

            executor.Execute(new EnemyStrategicDecision(
                state: EnemyStrategicState.RetreatValue, committedShipCount: 1, reason: "test"),
                Context(new IShipEntity[] { ship }, hasSafeRetreat: false));

            Assert.That(orders.Calls.Count, Is.EqualTo(1));
            Assert.That(orders.Calls[0].Action, Is.EqualTo(UnitActionId.Stop));
        }

[TestCase(1196f, 800f, 100f, 600f, true)]
        [TestCase(1394f, 800f, 100f, 600f, true)]
        [TestCase(1501f, 800f, 100f, 600f, false)]
        [TestCase(500f, 800f, 100f, 0f, false)]
        public void StationPressure_UsesFootprintsAndWeaponReach(
            float distance, float stationRadius, float hostileRadius, float weaponRange, bool expected)
        {
            Assert.That(EnemyStrategicContextBuilder.CanThreatenStation(
                distance, stationRadius, hostileRadius, weaponRange), Is.EqualTo(expected));
        }

        [Test]
        public void OutmatchedGroupAtLegacySecondStation_AttacksWeakestShipInsteadOfLeaving()
        {
            var views = new List<GameObject>();
            var hazards = new TestHazards();
            var navigation = new ShipNavigationService(new RetreatObstacleProvider(), hazards, new OfflineSession(PlayerId.None));
            var game = ScriptableObject.CreateInstance<GameData>();
            try
            {
                PlayerRoster roster = TestPlayers.CreateDuel(EnemyAiDifficulty.UltraHard);
                PlayerSlot owner = roster.Get(TestPlayers.Enemy);
                var locator = new EntityLocator();
                var intel = new TeamIntelRegistry();
                var zones = new WithdrawalZones();
                var pressure = new EnemyStationPressureTracker();
                var production = new WithdrawalProduction();
                var catalog = new UnitCombatProfileCatalog(null, null, null, null);
                var damage = new float[ForceComposition.ClassSlots];
                damage[(int)ShipClass.Capital] = 100f;
                var profiles = (Dictionary<UnitTypeId, UnitCombatProfile>)typeof(UnitCombatProfileCatalog)
                    .GetField("_profiles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(catalog);
                profiles.Add(UnitTypeId.Ship(ShipType.Venator), new UnitCombatProfile(ShipClass.Capital,
                    10000f, 0f, damage, damage, new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>()));

                FakeEntity primary = AddEntity("Primary", 1, Vector3.zero, owner.Id);
                primary.IsUnit = false;
                primary.IsBase = true;
                FakeEntity front = AddEntity("Legacy second station", 2, Vector3.right * 3000f, owner.Id);
                front.IsUnit = false;
                front.IsBase = true;
                front.IsSecondStation = true;
                FakeEntity receiver = AddEntity("Survivor", 3, Vector3.right * 2900f, owner.Id);
                ((FakeHealthModel)receiver.HealthModel).Hull = 100f;
                var ships = new IShipEntity[] { new FakeShip(Vector3.right * 2900f, receiver.Id) };
                FakeEntity attacker = AddEntity("Closest attacker", 4, Vector3.right * 2950f, TestPlayers.Human);
                attacker.Target = front;
                attacker.WeaponRange = 200f;
                Report(attacker);
                FakeEntity weak = AddEntity("Weakest attacker", 5, Vector3.right * 3100f, TestPlayers.Human);
                ((FakeHealthModel)weak.HealthModel).Hull = 500f;
                Report(weak);

                var snapshot = new AiWorldSnapshot(locator);
                var heldZones = new ZoneSource(roster);
                var recovery = new EnemyRecoveryAnchor(zones, heldZones, roster, locator,
                    null, null, intel, owner, production, pressure, snapshot);
                var builder = CreateContextBuilder(null, zones, Array.Empty<IEnemyCaptureObjectiveSource>(),
                    locator, game, roster, catalog, intel,
                    new VisionService(roster, fogEnabled: false), owner, production, hazards,
                    heldZones, navigation, pressure, recovery, snapshot);
                Assert.That(builder.AssessStation(ships), Is.False);
                EnemyStrategicContext context = builder.CreateGroupBuilder().Build(ships);
                Assert.That(context.OwnBase, Is.SameAs(primary));
                Assert.That(context.RetreatAnchor, Is.Null);
                Assert.That(context.LocalThreatTarget, Is.SameAs(weak));
                EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(context.Snapshot);
                Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.EngageLocal));

                var orders = new FakeOrderService();
                new EnemyTaskForceExecutor(orders).Execute(decision, context);
                OrderCall attack = orders.Calls.Single();
                Assert.That(attack.Action, Is.EqualTo(UnitActionId.Attack));
                Assert.That(attack.Target, Is.SameAs(weak));
                Assert.That(attack.Receivers, Is.EqualTo(new IEntity[] { receiver }));

                FakeEntity AddEntity(string name, long id, Vector3 position, PlayerId player)
                {
                    var view = new GameObject(name);
                    views.Add(view);
                    view.transform.position = position;
                    var entity = new FakeEntity(new FakeHealthModel(view.transform) { Hull = 10000f, Shields = 0f }, player, id);
                    locator.AddEntity(entity);
                    return entity;
                }

                void Report(FakeEntity entity)
                {
                    Vector3 position = entity.GetFacade<IEntityTransformFacade>().Transform.position;
                    intel.Get(owner.Team).Report(new HostileSighting(entity.Id, entity.UnitTypeId, entity.Owner,
                        new FormationPoint(position.x, position.z), entity.HealthModel.Hull, 0f, 1f, Time.time));
                }
            }
            finally
            {
                navigation.Dispose();
                UnityEngine.Object.DestroyImmediate(game);
                foreach (GameObject view in views) UnityEngine.Object.DestroyImmediate(view);
            }
        }

        [Test]
        public void CriticalStation_RecoveryKeepsLocalPressureAnchorInsteadOfMovingToARelay()
        {
            var view = new GameObject("Pressured primary station");
            try
            {
                PlayerRoster roster = TestPlayers.CreateDuel();
                PlayerSlot owner = roster.Get(TestPlayers.Enemy);
                var locator = new EntityLocator();
                view.transform.position = Vector3.right * 500f;
                var station = new FakeEntity(new FakeHealthModel(view.transform) { Hull = 40f, Shields = 0f }, owner.Id, 10)
                    { IsBase = true, IsUnit = false, MaximumDurability = 100f };
                locator.AddEntity(station);
                var zones = new ZoneSource(roster, Vector3.right * 300f, Vector3.right * 2600f);
                foreach (ReinforcementZonePresenter zone in zones.Zones)
                    zone.Restore(owner.Id, PlayerId.None, 0f, false);
                var pressure = new EnemyStationPressureTracker();
                pressure.Update(station.Id, 40f, 100f, 1f, visibleThreatId: 99);
                var range = new Vector2Range();
                Type rangeType = range.GetType().BaseType;
                rangeType.GetField("<Min>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(range, Vector2.one * -3000f);
                rangeType.GetField("<Max>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(range, Vector2.one * 3000f);
                var map = new MapLayout(new Dictionary<PlayerId, Vector3> { { owner.Id, view.transform.position } },
                    Array.Empty<ZoneSpot>(), Array.Empty<SiteSpot>(), Array.Empty<MapLane>(), Array.Empty<AsteroidField>(),
                    range, Vector3.zero);
                var recovery = new EnemyRecoveryAnchor(new WithdrawalZones(), zones, roster, locator,
                    new BattleAreaService(roster), map, new TeamIntelRegistry(), owner, new WithdrawalProduction(),
                    pressure, new AiWorldSnapshot(locator));

                recovery.Update(Vector3.zero, 1f, false, true, 1f, (point, radius) => true);

                Assert.That(recovery.IsActive, Is.True);
                Assert.That(recovery.TryGetBuildAnchor(out Vector3 anchor), Is.True);
                Assert.That(anchor, Is.EqualTo(view.transform.position));

                recovery.Update(Vector3.left * 1600f, 1f, false, true, 2f, (point, radius) => true);
                Assert.That(recovery.TryGetBuildAnchor(out _), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(view);
            }
        }

        [Test]
        public void GuardSite_RepairsAnIdleShipsOrderOutsideTheGuardArea()
        {
            var ship = new FakeShip(Vector3.zero, 1);
            Vector3 guard = Vector3.right * (FleetCohesion.RADIUS + 100f);
            EnemyStrategicContext context = Context(new[] { ship }, guardPosition: guard);
            var orders = new FakeOrderService();
            var executor = new EnemyTaskForceExecutor(orders);
            var decision = new EnemyStrategicDecision("Guard a held zone", EnemyStrategicState.GuardSite, 1);

            executor.Execute(decision, context);
            ship.CurrentOrder = UnitOrderType.AttackMove;
            executor.Execute(decision, context);
            Assert.That(orders.Calls.Single().Point, Is.EqualTo(guard));

            ship.CurrentOrder = UnitOrderType.None;
            executor.Execute(decision, context);
            Assert.That(orders.Calls.Count, Is.EqualTo(2));
            Assert.That(orders.Calls[1].Action, Is.EqualTo(UnitActionId.AttackMove));
            Assert.That(orders.Calls[1].Point, Is.EqualTo(guard));

            ship.WorldPosition = guard;
            executor.Execute(decision, context);
            Assert.That(orders.Calls.Count, Is.EqualTo(2));
        }

        [Test]
        public void StationPressure_DepletedShieldsStillUseFullMaximumDurability()
        {
            GameObject view = new GameObject("Damaged restored station");
            try
            {
                PlayerRoster roster = TestPlayers.CreateDuel();
                PlayerSlot owner = roster.Get(TestPlayers.Enemy);
                var station = new FakeEntity(new FakeHealthModel(view.transform) { Hull = 400f, Shields = 0f },
                    owner.Id, 10) { MaximumDurability = 1000f };
                var locator = new EntityLocator();
                var tracker = new EnemyStationPressureTracker();
                var builder = CreateContextBuilder(null, null, Array.Empty<IEnemyCaptureObjectiveSource>(), locator, null, roster,
                    null,
                    new TeamIntelRegistry(), new VisionService(roster, fogEnabled: false), owner,
                    null, new TestHazards(), null, null, tracker, CreateRecovery(roster));

                typeof(EnemyStrategicContextBuilder).GetMethod("BuildBaseThreat",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(builder, new object[] { station });

                Assert.That(tracker.DurabilityFraction, Is.EqualTo(0.4f));
            }
            finally { UnityEngine.Object.DestroyImmediate(view); }
        }

[TestCase("attacking", true)]
        [TestCase("weapon-tracked", true)]
        [TestCase("unrelated", false)]
        [TestCase("hidden", false)]
        [TestCase("hidden-after-damage", true)]
        [TestCase("cloaked", false)]
        [TestCase("destroyed", false)]
        [TestCase("lost", false)]
        [TestCase("out-of-range", false)]
        public void StationPressure_ExtendedFootprintRequiresVisibleLiveAttacker(string condition, bool expected)
        {
            GameObject stationView = new GameObject("Threatened station");
            GameObject attackerView = new GameObject("Bombardment attacker");
            try
            {
                attackerView.transform.position = Vector3.right * (condition == "out-of-range" ? 1600f : 1300f);
                PlayerRoster roster = TestPlayers.CreateDuel();
                PlayerSlot owner = roster.Get(TestPlayers.Enemy);
                var stationHealth = new FakeHealthModel(stationView.transform);
                FakeEntity station = new FakeEntity(stationHealth, owner.Id, 10)
                    { NavigationRadius = 800f };
                FakeHealthModel attackerHealth = new FakeHealthModel(attackerView.transform)
                    { IsDestroyed = condition == "destroyed" };
                FakeEntity attacker = new FakeEntity(attackerHealth, TestPlayers.Human, 11)
                {
                    NavigationRadius = 100f,
                    WeaponRange = 600f,
                    Target = condition == "unrelated" || condition == "weapon-tracked" ? null : station,
                    TrackedWeaponTarget = condition == "weapon-tracked" ? station : null
                };
                attacker.Modifiers.SetCloaked(condition == "cloaked");
                EntityLocator locator = new EntityLocator();
                if (condition != "lost") locator.AddEntity(attacker);
                TeamIntelRegistry registry = new TeamIntelRegistry();
                bool damagedRecently = condition == "hidden-after-damage";
                if (!damagedRecently)
                    registry.Get(owner.Team).Report(new HostileSighting(attacker.Id,
                        UnitTypeId.Ship(ShipType.Venator), attacker.Owner,
                        new FormationPoint(attackerView.transform.position.x, 0f), 1f, 1f, 1f, Time.time));
                UnitCombatProfileCatalog catalog = new UnitCombatProfileCatalog(null, null, null, null);
                Dictionary<UnitTypeId, UnitCombatProfile> profiles =
                    (Dictionary<UnitTypeId, UnitCombatProfile>)typeof(UnitCombatProfileCatalog)
                        .GetField("_profiles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(catalog);
                profiles.Add(attacker.UnitTypeId, new UnitCombatProfile(ShipClass.Capital, 1f, 1f,
                    new float[ForceComposition.ClassSlots], new float[ForceComposition.ClassSlots],
                    new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>()));
                var pressure = new EnemyStationPressureTracker();
                if (damagedRecently)
                {
                    pressure.Update(station.Id, 2f, 2f, Time.time - 1f);
                    stationHealth.Shields = 0f;
                    pressure.Update(station.Id, 1f, 2f, Time.time);
                }
                EnemyStrategicContextBuilder builder = CreateContextBuilder(
                    null, null, Array.Empty<IEnemyCaptureObjectiveSource>(), locator, null, roster, catalog,
                    registry, new VisionService(roster, fogEnabled: condition == "hidden" || damagedRecently), owner,
                    null, new TestHazards(), null, null, pressure, CreateRecovery(roster));

                object target = typeof(EnemyStrategicContextBuilder)
                    .GetMethod("BuildBaseThreat", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(builder, new object[] { station });

                if (expected)
                {
                    Assert.That(target, Is.SameAs(attacker));
                    attacker.Modifiers.SetCloaked(true);
                    EnemyStrategicContextBuilder newGroup = builder.CreateGroupBuilder();
                    object hiddenTarget = typeof(EnemyStrategicContextBuilder)
                        .GetMethod("BuildBaseThreat", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(newGroup, new object[] { station });
                    Assert.That(hiddenTarget, Is.Null, "Hidden attackers supply no live target.");
                    Assert.That(pressure.HasPressure, Is.True, "A new group keeps player-wide pressure after contact loss.");
                    Assert.That(typeof(EnemyStrategicContextBuilder)
                        .GetField("_stationPressure", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(newGroup),
                        Is.SameAs(pressure));
                }
                else Assert.That(target, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(attackerView);
                UnityEngine.Object.DestroyImmediate(stationView);
            }
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void OwnedSiteDefense_StrategicUsesKnownHostilesAndSquadronUsesLivePresence(bool known, bool present)
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            var sites = new SiteSource(new CaptureSiteStub(Vector3.zero) { Owner = TestPlayers.Enemy })
            {
                HostilePresent = present
            };
            IEnemyCaptureObjectiveSource source = SiteObjectives(sites, roster)[0];
            var intel = new HostileIntelModel();
            if (known)
                intel.Report(new HostileSighting(1, UnitTypeId.Ship(ShipType.Venator), TestPlayers.Human,
                    default, 100f, 0f, 1f, Time.time));
            var strategic = new List<EnemyCaptureObjective>();
            var squadrons = new EnemySquadronCaptureAssignments();

            source.AddStrategicObjectives(strategic, intel);
            source.AddSquadronTargets(squadrons);

            Assert.That(strategic.Count, Is.EqualTo(known ? 1 : 0));
            if (known) Assert.That(strategic[0].IsThreatened, Is.True);
            Assert.That(squadrons.HasTargets, Is.EqualTo(present));
        }

        [TestCase(CaptureSiteState.Constructing)]
        [TestCase(CaptureSiteState.Operational)]
        public void BuiltHostileMine_RemainsAnObjectiveAfterVisionIsLost(CaptureSiteState state)
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            var site = new CaptureSiteStub(Vector3.zero) { Owner = TestPlayers.Human, FacilityState = state };
            IEnemyCaptureObjectiveSource source = SiteObjectives(new SiteSource(site), roster)[0];
            var objectives = new List<EnemyCaptureObjective>();
            var intel = new HostileIntelModel();

            source.AddStrategicObjectives(objectives, intel);
            Assert.That(objectives, Is.Empty);
            intel.ReportMine(new HostileSighting(2, default, TestPlayers.Human, default, 100f, 0f, 1f, 0f));
            intel.ForgetExpired(1000f);
            source.AddStrategicObjectives(objectives, intel);
            Assert.That(objectives.Single().BlockingFacilityId, Is.EqualTo(2));

            objectives.Clear();
            intel.Forget(2);
            site.Owner = PlayerId.None;
            site.FacilityState = null;
            source.AddStrategicObjectives(objectives, intel);
            Assert.That(objectives.Single().Id, Is.EqualTo("site:0"));
            Assert.That(objectives.Single().BlockingFacilityId, Is.Null);
        }

        [Test]
        public void CaptureSelection_SelectsCloserMine()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            ZoneSource zones = new ZoneSource(roster, Vector3.right * 100f);
            SiteSource sites = new SiteSource(new CaptureSiteStub(Vector3.right * 10f));
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, zones, sites, new TestHazards());
            EnemyCaptureObjective objective = SelectCapture(builder);

            Assert.That(objective.Kind, Is.EqualTo(EnemyCaptureObjectiveType.Facility));
            Assert.That(objective.Position, Is.EqualTo(Vector3.right * 10f));
        }

        [TestCase(1f)]
        [TestCase(11f)]
        public void CaptureSelection_AttackedOwnedSitePreemptsCloserCaptureImmediately(float now)
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            ZoneSource zones = new ZoneSource(roster, Vector3.right * 100f, Vector3.right * 1000f);
            zones.Zones[1].Restore(TestPlayers.Enemy, PlayerId.None, 0f, false);
            var defense = new MutableCaptureObjectiveSource();
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, zones, new SiteSource(),
                new TestHazards(), sources: new IEnemyCaptureObjectiveSource[] { defense });
            EnemyCaptureObjective initial = SelectCapture(builder, 0f);
            Assert.That(initial.Id, Is.EqualTo("zone:0"));
            defense.Objective = new EnemyCaptureObjective("owned-mine", EnemyCaptureObjectiveType.Facility,
                TestPlayers.Enemy, Vector3.right * 1000f, 10f, 0f, true);

            EnemyCaptureObjective selected = SelectCapture(builder, now);

            Assert.That(selected.Id, Is.EqualTo("owned-mine"),
                "Safe defense preempts both the minimum hold and a worse travel score.");
            Assert.That(selected.IsThreatened, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CaptureSelection_DefensePreemptionStillRespectsReservationAndStall(bool reserved)
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            ZoneSource zones = new ZoneSource(roster, Vector3.right * 100f, Vector3.right * 1000f);
            zones.Zones[1].Restore(TestPlayers.Enemy, PlayerId.None, 0f, false);
            var defense = new MutableCaptureObjectiveSource();
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, zones, new SiteSource(),
                new TestHazards(), sources: new IEnemyCaptureObjectiveSource[] { defense });
            EnemyCaptureObjective initial = SelectCapture(builder, 0f);
            defense.Objective = new EnemyCaptureObjective("owned-mine", EnemyCaptureObjectiveType.Facility,
                TestPlayers.Enemy, Vector3.right * 1000f, 10f, 0f, true);
            if (reserved)
                typeof(EnemyStrategicContextBuilder).GetField("_reservedCaptureObjectives",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(builder, new HashSet<string> { "owned-mine" });
            else
            {
                typeof(EnemyStrategicContextBuilder).GetField("_rejectedCaptureId",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(builder, "owned-mine");
                typeof(EnemyStrategicContextBuilder).GetField("_captureRetryAt",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(builder, 20f);
            }

            Assert.That(SelectCapture(builder, 1f).Id, Is.EqualTo(initial.Id));
        }

        [Test]
        public void CaptureSelection_SkipsObjectiveReservedByAlliedAi()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            ZoneSource zones = new ZoneSource(roster, Vector3.right * 100f, Vector3.right * 1000f);
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, zones, new SiteSource(), new TestHazards());
            builder.TeamCooperation.PublishReservations(TestPlayers.SecondEnemy, new[] { "zone:0" });

            Assert.That(SelectCapture(builder).Id, Is.EqualTo("zone:1"));
        }

        [Test]
        public void CaptureSelection_KeepsHeldObjectiveAgainstHigherIndexAllyClaim()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            ZoneSource zones = new ZoneSource(roster, Vector3.right * 100f, Vector3.right * 1000f);
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, zones, new SiteSource(), new TestHazards());
            builder.TeamCooperation.PublishReservations(TestPlayers.Enemy, new[] { "zone:0" });
            builder.TeamCooperation.AddReservations(TestPlayers.SecondEnemy, new[] { "zone:0" });

            Assert.That(SelectCapture(builder).Id, Is.EqualTo("zone:0"));
        }

        [Test]
        public void CaptureSelection_PrefersCloserZoneOverFartherMine()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            ZoneSource zones = new ZoneSource(roster, Vector3.right * 10f);
            SiteSource sites = new SiteSource(new CaptureSiteStub(Vector3.right * 100f));
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, zones, sites, new TestHazards());
            EnemyCaptureObjective objective = SelectCapture(builder);

            Assert.That(objective.Kind, Is.EqualTo(EnemyCaptureObjectiveType.ReinforcementZone));
            Assert.That(objective.Position, Is.EqualTo(Vector3.right * 10f));
        }

        [Test]
        public void MainForceCaptureSelection_PicksNearbyMineOverDistantCentralRelay()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            Vector3 relay = Vector3.right * 800f;
            var zones = new ZoneSource(roster, relay);
            var sites = new SiteSource(new CaptureSiteStub(Vector3.right * 100f));
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, zones, sites, new TestHazards());
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(EnemyStrategicContextBuilder).GetField("_preferStrategicObjectives", flags).SetValue(builder, true);
            typeof(EnemyStrategicContextBuilder).GetField("_strategicCenter", flags).SetValue(builder, relay);

            Assert.That(SelectCapture(builder).Id, Is.EqualTo("site:0"));
        }

        [Test]
        public void CaptureSelection_WithoutZonesStillSelectsMine()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            SiteSource sites = new SiteSource(new CaptureSiteStub(Vector3.right * 10f));
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, new ZoneSource(roster), sites,
                new TestHazards());
            EnemyCaptureObjective objective = SelectCapture(builder);

            Assert.That(objective.Kind, Is.EqualTo(EnemyCaptureObjectiveType.Facility));
            Assert.That(objective.Position, Is.EqualTo(Vector3.right * 10f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CaptureSelection_UsesFullRouteForceWhenLocalMatchupFails(bool hasRouteFighter)
        {
            PlayerRoster roster = TestPlayers.CreateDuel(EnemyAiDifficulty.UltraHard);
            PlayerSlot owner = roster.Get(TestPlayers.Enemy);
            Vector3 destination = Vector3.right * 3000f;
            var zones = new ZoneSource(roster, destination);
            var catalog = new UnitCombatProfileCatalog(null, null, null, null);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var profiles = (Dictionary<UnitTypeId, UnitCombatProfile>)typeof(UnitCombatProfileCatalog)
                .GetField("_profiles", flags).GetValue(catalog);
            var capitalDamage = new float[ForceComposition.ClassSlots];
            capitalDamage[(int)ShipClass.Capital] = 1f;
            var noDamage = new float[ForceComposition.ClassSlots];
            var noHangar = Array.Empty<UnitCombatProfile>();
            UnitTypeId capital = UnitTypeId.Ship(ShipType.Venator);
            UnitTypeId frigate = UnitTypeId.Ship(ShipType.Arquitens);
            UnitTypeId fighter = UnitTypeId.Squadron(SquadronType.Vulture);
            profiles.Add(capital, new UnitCombatProfile(ShipClass.Capital, 1f, 0f, capitalDamage, noDamage, noDamage, noHangar));
            profiles.Add(frigate, new UnitCombatProfile(ShipClass.Frigate, 1000f, 0f, noDamage, noDamage, noDamage, noHangar));
            profiles.Add(fighter, new UnitCombatProfile(ShipClass.Fighter, 1f, 0f, noDamage, noDamage, noDamage, noHangar));
            var registry = new TeamIntelRegistry();
            HostileIntelModel intel = registry.Get(owner.Team);
            intel.Report(new HostileSighting(1, capital, TestPlayers.Human, new FormationPoint(3000f, 0f), 1f, 0f, 1f, 0f));
            intel.Report(new HostileSighting(2, frigate, TestPlayers.Human, new FormationPoint(3000f, 0f), 1000f, 0f, 1f, 0f));
            if (hasRouteFighter)
                intel.Report(new HostileSighting(3, fighter, TestPlayers.Human, new FormationPoint(1500f, 0f), 1f, 0f, 1f, 0f));

            var builder = CreateContextBuilder(null, null, Array.Empty<IEnemyCaptureObjectiveSource>(),
                new EntityLocator(), null, roster, catalog, registry, new VisionService(roster, fogEnabled: false),
                owner, null, new TestHazards(), zones, null, new EnemyStationPressureTracker(), CreateRecovery(roster, zones));
            var ships = (List<IShipEntity>)typeof(EnemyStrategicContextBuilder).GetField("_ownShips", flags).GetValue(builder);
            ships.Add(new FakeShip(destination, 4));
            var ownForce = (ForceComposition)typeof(EnemyStrategicContextBuilder).GetField("_localOwnForce", flags).GetValue(builder);
            ownForce.Add(CombatForce(50000f, 0.01f, fighterDps: 10f));

            // The route fighter increases the killable hull share, raising advantage from 0.71 to 1.00.
            EnemyCaptureObjective selected = SelectCapture(builder);
            if (hasRouteFighter) Assert.That(selected?.Id, Is.EqualTo("zone:0"));
            else Assert.That(selected, Is.Null);
        }

        [Test]
        public void CaptureSelection_WithoutZonesSelectsSatelliteAfterOwningAllLocalMines()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            SiteSource sites = new SiteSource(
                new CaptureSiteStub(Vector3.right * 20f) { Owner = TestPlayers.Enemy },
                new CaptureSiteStub(Vector3.right * 30f) { Owner = TestPlayers.Enemy },
                new CaptureSiteStub(Vector3.right * 10f) { Kind = CaptureSiteKind.Satellite });
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, new ZoneSource(roster), sites,
                new TestHazards());

            EnemyCaptureObjective objective = SelectCapture(builder);

            Assert.That(objective.Id, Is.EqualTo("site:2"));
            Assert.That(objective.Position, Is.EqualTo(Vector3.right * 10f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CaptureSelection_SatellitePadIsCapturableBeforeItsPointMinesAreOwned(bool hasUnownedLocalMine)
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            CaptureSiteStub mine = new CaptureSiteStub(Vector3.right * 100f)
            {
                Owner = hasUnownedLocalMine ? PlayerId.None : TestPlayers.Enemy,
                PointId = hasUnownedLocalMine ? 0 : 1
            };
            SiteSource sites = new SiteSource(mine,
                new CaptureSiteStub(Vector3.right * 10f) { Kind = CaptureSiteKind.Satellite });
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, new ZoneSource(roster), sites,
                new TestHazards());

            EnemyCaptureObjective objective = SelectCapture(builder);

            // Pads come first at every point: satellites hold the point, mines follow. Owning the mines is not required.
            Assert.That(objective.Id, Is.EqualTo("site:1"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CaptureSelection_UnsafeNearestObjectiveAllowsSaferAlternative(bool nearestIsMine)
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            ZoneSource zones = nearestIsMine
                ? new ZoneSource(roster, Vector3.right * 200f)
                : new ZoneSource(roster, Vector3.right * 100f, Vector3.right * 200f);
            SiteSource sites = nearestIsMine
                ? new SiteSource(new CaptureSiteStub(Vector3.right * 100f))
                : new SiteSource();
            SpaceHazardData data = ScriptableObject.CreateInstance<SpaceHazardData>();
            try
            {
                SerializedObject serialized = new SerializedObject(data);
                serialized.FindProperty("<DisablesShields>k__BackingField").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                TestHazards hazards = new TestHazards
                {
                    Sources = new IHazardSource[]
                    {
                        TestHazards.CreateEllipse(data, Vector3.right * 100f, Vector3.right, 20f, 20f)
                    }
                };
                EnemyStrategicContextBuilder builder = CaptureBuilder(roster, zones, sites, hazards);

                EnemyCaptureObjective objective = SelectCapture(builder);

                Assert.That(objective.Kind, Is.EqualTo(EnemyCaptureObjectiveType.ReinforcementZone));
                Assert.That(objective.Position, Is.EqualTo(Vector3.right * 200f));
            }
            finally { UnityEngine.Object.DestroyImmediate(data); }
        }

        [Test]
        public void CaptureSelection_OwnershipChangeInvalidatesCommittedZone()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            ZoneSource zones = new ZoneSource(roster, Vector3.right * 100f, Vector3.right * 200f);
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, zones, new SiteSource(), new TestHazards());
            EnemyCaptureObjective first = SelectCapture(builder);
            Assert.That(first.Position, Is.EqualTo(Vector3.right * 100f));
            zones.Zones[0].Restore(TestPlayers.Enemy, PlayerId.None, 0f, false);

            EnemyCaptureObjective replacement = SelectCapture(builder);

            Assert.That(replacement.Id, Is.Not.EqualTo(first.Id));
            Assert.That(replacement.Position, Is.EqualTo(Vector3.right * 200f));
        }

        [Test]
        public void RetreatAnchor_RemainsSafeAfterRetreatReservesItsDestination()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            Vector3 anchor = Vector3.right * 100f;
            ZoneSource zones = new ZoneSource(roster, anchor);
            zones.Zones[0].Restore(TestPlayers.Enemy, PlayerId.None, 0f, false);
            TestHazards hazards = new TestHazards();
            RetreatObstacleProvider obstacles = new RetreatObstacleProvider();
            ShipNavigationService navigation = new ShipNavigationService(obstacles, hazards, new OfflineSession(PlayerId.None));
            UnitOrderSettings settings = ScriptableObject.CreateInstance<UnitOrderSettings>();
            SpaceHazardData hazardData = ScriptableObject.CreateInstance<SpaceHazardData>();
            GameObject view = new GameObject("Retreat reservation test");
            try
            {
                EnemyStrategicContextBuilder builder = CaptureBuilder(roster, zones, new SiteSource(), hazards, navigation);
                List<IShipEntity> ships = (List<IShipEntity>)typeof(EnemyStrategicContextBuilder)
                    .GetField("_ownShips", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(builder);
                ((FakeShip)ships[0]).WorldPosition = Vector3.zero;
                MethodInfo findAnchor = typeof(EnemyStrategicContextBuilder)
                    .GetMethod("FindRetreatAnchor", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(findAnchor.Invoke(builder, new object[] { Vector3.zero, null }), Is.EqualTo(anchor));

                Vector2Range range = new Vector2Range();
                Type rangeType = range.GetType().BaseType;
                rangeType.GetField("<Min>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(range, Vector2.one * -250f);
                rangeType.GetField("<Max>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(range, Vector2.one * 250f);
                NavigationRetreatFacade retreat = new NavigationRetreatFacade(navigation, range);
                FakeEntity receiver = new FakeEntity(new FakeHealthModel(view.transform), TestPlayers.Enemy, 1)
                    { RetreatCommand = retreat };
                UnitOrderService orders = new UnitOrderService(new EntityLocator(), null, settings);

                orders.IssueRetreat(new IEntity[] { receiver }, anchor);

                Assert.That(retreat.Destination, Is.EqualTo(anchor));
                Assert.That(navigation.IsPositionClear(anchor, retreat.NavigationRadius), Is.False,
                    "The real retreat route must reserve its destination before the next strategic evaluation.");
                Assert.That(findAnchor.Invoke(builder, new object[] { Vector3.zero, null }), Is.EqualTo(anchor));

                obstacles.Contacts.Add(new RadarContact(anchor, 30f, false));
                Assert.That(findAnchor.Invoke(builder, new object[] { Vector3.zero, null }), Is.Null,
                    "Ignoring destination reservations must still reject static obstacles.");
                obstacles.Contacts.Clear();
                SerializedObject serialized = new SerializedObject(hazardData);
                serialized.FindProperty("<DisablesShields>k__BackingField").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                hazards.Sources = new IHazardSource[]
                    { TestHazards.CreateEllipse(hazardData, anchor, Vector3.right, 20f, 20f) };
                Assert.That(findAnchor.Invoke(builder, new object[] { Vector3.zero, null }), Is.Null,
                    "Ignoring destination reservations must still reject increased ion risk.");
            }
            finally
            {
                navigation.Dispose();
                UnityEngine.Object.DestroyImmediate(settings);
                UnityEngine.Object.DestroyImmediate(hazardData);
                UnityEngine.Object.DestroyImmediate(view);
            }
        }

        [Test]
        public void CaptureSelection_InactiveCandidateDoesNotStallBeforeCaptureStarts()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            ZoneSource zones = new ZoneSource(roster, Vector3.right * 100f);
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, zones, new SiteSource(), new TestHazards());
            EnemyCaptureObjective first = SelectCapture(builder, 0f);
            builder.ReportCaptureExecution(null, 0f);

            EnemyCaptureObjective afterDefense = SelectCapture(builder, 30f);
            Assert.That(afterDefense.Id, Is.EqualTo(first.Id));
            builder.ReportCaptureExecution(null, 30f);

            EnemyCaptureObjective afterRetreat = SelectCapture(builder, 60f);
            Assert.That(afterRetreat.Id, Is.EqualTo(first.Id));
            builder.ReportCaptureExecution(afterRetreat, 60f);

            Assert.That(SelectCapture(builder, 79f).Id, Is.EqualTo(first.Id));
            Assert.That(SelectCapture(builder, 80f), Is.Null);
        }

        [Test]
        public void CaptureSelection_InterruptedCapturePausesItsStallBudget()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            ZoneSource zones = new ZoneSource(roster, Vector3.right * 100f);
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, zones, new SiteSource(), new TestHazards());
            EnemyCaptureObjective first = SelectCapture(builder, 0f);
            builder.ReportCaptureExecution(first, 0f);
            Assert.That(SelectCapture(builder, 8f).Id, Is.EqualTo(first.Id));
            builder.ReportCaptureExecution(null, 8f);

            EnemyCaptureObjective afterDefense = SelectCapture(builder, 40f);
            Assert.That(afterDefense.Id, Is.EqualTo(first.Id));
            builder.ReportCaptureExecution(null, 40f);
            EnemyCaptureObjective afterRetreat = SelectCapture(builder, 70f);
            Assert.That(afterRetreat.Id, Is.EqualTo(first.Id));
            builder.ReportCaptureExecution(afterRetreat, 70f);

            Assert.That(SelectCapture(builder, 81f).Id, Is.EqualTo(first.Id));
            Assert.That(SelectCapture(builder, 82f), Is.Null);
        }

        [Test]
        public void CaptureSelection_ExecutedStallRejectsObjectiveForTwentySeconds()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            ZoneSource zones = new ZoneSource(roster, Vector3.right * 100f, Vector3.right * 200f);
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, zones, new SiteSource(), new TestHazards());
            EnemyCaptureObjective first = SelectCapture(builder, 0f);
            builder.ReportCaptureExecution(first, 0f);

            EnemyCaptureObjective alternative = SelectCapture(builder, 20f);
            Assert.That(alternative.Id, Is.Not.EqualTo(first.Id));
            builder.ReportCaptureExecution(null, 20f);
            zones.Zones[1].Restore(TestPlayers.Enemy, PlayerId.None, 0f, false);

            Assert.That(SelectCapture(builder, 39f), Is.Null);
            Assert.That(SelectCapture(builder, 40f).Id, Is.EqualTo(first.Id));
        }

        [Test]
        public void TaskGroups_CaptureProbeSelectsFastestSafeShipInSameDecision()
        {
            var (builder, weak, strong, slower) = CaptureProbeBuilder();
            var orders = new FakeOrderService();
            var planner = new EnemyShipTaskGroupPlanner(builder, new EnemyStrategicDecisionModel(),
                new EnemyTaskForceExecutor(orders), orders);
            IShipEntity[] ships = { slower, weak, strong };
            planner.Reconcile(ships);

            planner.TryCreateCaptureGroup(ships, new HashSet<string>(), false);

            EnemyShipTaskGroup capture = planner.Groups.Single(group => group.IsCaptureDetachment);
            Assert.That(capture.Ships, Is.EqualTo(new[] { strong }),
                "A rejected faster ship must not postpone the next viable candidate to another decision.");
            Assert.That(planner.Groups.Where(group => !group.IsCaptureDetachment).SelectMany(group => group.Ships),
                Is.EquivalentTo(new IShipEntity[] { weak, slower }));
            Assert.That(capture.Context, Is.Null, "Only the normal group evaluation commits a context.");
            Assert.That(orders.Calls, Is.Empty, "Detachment selection itself must not issue orders.");
            var smoother = (EnemyAdvantageSmoother)typeof(EnemyStrategicContextBuilder)
                .GetField("_advantageSmoother", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(capture.ContextBuilder);
            Assert.That(smoother.Local, Is.Zero, "Rejected candidates must not seed the chosen group's smoothing.");
        }

        [Test]
        public void CaptureProbe_UsesRawCandidateStrengthWithoutAdvancingGroupHistory()
        {
            var (builder, weak, strong, _) = CaptureProbeBuilder();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var smoother = (EnemyAdvantageSmoother)typeof(EnemyStrategicContextBuilder)
                .GetField("_advantageSmoother", flags).GetValue(builder);
            smoother.Sample(0.01f, 0.02f, 1f);
            var commitment = (EnemyTargetCommitment)typeof(EnemyStrategicContextBuilder)
                .GetField("_captureCommitment", flags).GetValue(builder);
            commitment.Select("previous", 5f, null, 0f, false, 1f);
            var previous = new EnemyCaptureObjective("previous", EnemyCaptureObjectiveType.ReinforcementZone,
                PlayerId.None, Vector3.zero, 10f, 0f);
            typeof(EnemyStrategicContextBuilder).GetField("_captureObjective", flags).SetValue(builder, previous);
            typeof(EnemyStrategicContextBuilder).GetField("_captureStallSeconds", flags).SetValue(builder, 9f);
            typeof(EnemyStrategicContextBuilder).GetField("_captureUnsafeSince", flags).SetValue(builder, 3f);
            typeof(EnemyStrategicContextBuilder).GetField("_captureEvaluatedAt", flags).SetValue(builder, 7f);

            builder.BeginCaptureProbe(new HashSet<string>());
            Assert.That(builder.CanDetachForCapture(weak), Is.False);
            Assert.That(builder.CanDetachForCapture(strong), Is.True,
                "The same-time weak probe must not smooth down a later strong candidate.");
            Assert.That(builder.CanDetachForCapture(weak), Is.False,
                "The preceding strong probe must not lend strength to a weak candidate.");

            Assert.That(smoother.Local, Is.EqualTo(0.01f));
            Assert.That(smoother.Fleet, Is.EqualTo(0.02f));
            Assert.That(commitment.LastToId, Is.EqualTo("previous"));
            Assert.That(commitment.HeldSeconds(5f), Is.EqualTo(4f));
            Assert.That(typeof(EnemyStrategicContextBuilder).GetField("_captureObjective", flags).GetValue(builder),
                Is.SameAs(previous));
            Assert.That(typeof(EnemyStrategicContextBuilder).GetField("_captureStallSeconds", flags).GetValue(builder), Is.EqualTo(9f));
            Assert.That(typeof(EnemyStrategicContextBuilder).GetField("_captureUnsafeSince", flags).GetValue(builder), Is.EqualTo(3f));
            Assert.That(typeof(EnemyStrategicContextBuilder).GetField("_captureEvaluatedAt", flags).GetValue(builder), Is.EqualTo(7f));
        }

        [Test]
        public void CaptureProbe_ReservedObjectiveCannotCreateDetachment()
        {
            var (builder, _, strong, _) = CaptureProbeBuilder();
            builder.BeginCaptureProbe(new HashSet<string> { "zone:0" });
            Assert.That(builder.CanDetachForCapture(strong), Is.False);
            builder.BeginCaptureProbe(new HashSet<string>());
            Assert.That(builder.CanDetachForCapture(strong), Is.True);
        }

        [TestCase(EnemyOpeningFocus.Neighbour, 0)]
        [TestCase(EnemyOpeningFocus.Centre, 1)]
        [TestCase(EnemyOpeningFocus.Outer, 2)]
        public void OpeningFocus_OnlySelectsObjectivesInItsAssignedLane(EnemyOpeningFocus focus, int index)
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            Vector3[] positions = { Vector3.right * 100f, Vector3.forward * 200f, Vector3.left * 300f };
            var zones = new ZoneSource(roster, 1, positions);
            var builder = CaptureBuilder(roster, zones, new SiteSource(), new TestHazards());
            Assert.That(builder.SetOpeningFocus(focus, Vector3.zero), Is.True);

            Assert.That(SelectCapture(builder).Position, Is.EqualTo(positions[index]));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CaptureSelection_KeepsTheCentreAgainstDefendersThatRejectAnOrdinaryRelay(bool isCentre)
        {
            var (builder, weak, _, _) = CaptureProbeBuilder(isCentre: isCentre);
            builder.BeginCaptureProbe(new HashSet<string>());
            Assert.That(builder.CanDetachForCapture(weak), Is.False, "A weak ship must not detach from a live fight.");

            EnemyCaptureObjective objective = SelectCapture(builder, Time.time);

            Assert.That(objective != null, Is.EqualTo(isCentre));
        }

        [Test]
        public void CaptureProbe_LoneShipCanDetachForSafeMine()
        {
            var (builder, _, strong, _) = CaptureProbeBuilder(withMine: true);
            var ships = new IShipEntity[] { strong };
            var orders = new FakeOrderService();
            var planner = new EnemyShipTaskGroupPlanner(builder, new EnemyStrategicDecisionModel(),
                new EnemyTaskForceExecutor(orders), orders);
            planner.Reconcile(ships);
            planner.TryCreateCaptureGroup(ships, new HashSet<string>(), stationPressure: false);
            planner.Reconcile(ships);

            Assert.That(planner.Groups.Single().IsCaptureDetachment, Is.True);
            Assert.That(planner.Groups.Single().Ships.Single(), Is.SameAs(strong));
        }

        private static (EnemyStrategicContextBuilder Builder, FakeShip Weak, FakeShip Strong, FakeShip Slower)
            CaptureProbeBuilder(bool withMine = false, bool isCentre = false)
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            var locator = new EntityLocator();
            var catalog = new UnitCombatProfileCatalog(null, null, null, null);
            var damage = new float[ForceComposition.ClassSlots];
            damage[(int)ShipClass.Capital] = 10f;
            var profiles = (Dictionary<UnitTypeId, UnitCombatProfile>)typeof(UnitCombatProfileCatalog)
                .GetField("_profiles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(catalog);
            UnitTypeId type = UnitTypeId.Ship(ShipType.Venator);
            profiles.Add(type, new UnitCombatProfile(ShipClass.Capital, 100f, 0f, damage, damage,
                new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>()));
            locator.AddEntity(new FakeEntity(new FakeHealthModel(null) { Hull = 1f, Shields = 0f }, TestPlayers.Enemy, 1));
            locator.AddEntity(new FakeEntity(new FakeHealthModel(null) { Hull = 10000f, Shields = 0f }, TestPlayers.Enemy, 2));
            locator.AddEntity(new FakeEntity(new FakeHealthModel(null) { Hull = 10000f, Shields = 0f }, TestPlayers.Enemy, 3));
            var intel = new TeamIntelRegistry();
            intel.Get(roster.Get(TestPlayers.Enemy).Team).Report(new HostileSighting(4, type, TestPlayers.Human,
                new FormationPoint(0f, 0f), 100f, 0f, 1f, Time.time));
            // Candidates are already inside the zone; no navigation, station, raid or retreat query is needed.
            var builder = CreateContextBuilder(null, null, withMine
                    ? SiteObjectives(new SiteSource(new CaptureSiteStub(Vector3.zero)), roster)
                    : Array.Empty<IEnemyCaptureObjectiveSource>(),
                locator, null, roster, catalog, intel, null,
                roster.Get(TestPlayers.Enemy), null, new TestHazards(),
                new ZoneSource(roster, isCentre ? 0 : -1, Vector3.zero),
                null, new EnemyStationPressureTracker(), null);
            return (builder, new FakeShip(Vector3.zero, 1) { NavigationSpeed = 3f },
                new FakeShip(Vector3.zero, 2) { NavigationSpeed = 2f },
                new FakeShip(Vector3.zero, 3) { NavigationSpeed = 1f });
        }

        [Test]
        public void TaskGroups_SeparatedShipsRetreatAndCaptureIndependently()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, new ZoneSource(roster),
                new SiteSource(), new TestHazards());
            FakeOrderService orders = new FakeOrderService();
            EnemyShipTaskGroupPlanner planner = new EnemyShipTaskGroupPlanner(builder,
                new EnemyStrategicDecisionModel(), new EnemyTaskForceExecutor(orders), orders);
            FakeShip threatened = new FakeShip(Vector3.zero, 1) { CurrentOrder = UnitOrderType.Attack };
            FakeShip distant = new FakeShip(Vector3.right * 5000f, 2) { CurrentOrder = UnitOrderType.Attack };

            planner.Reconcile(new IShipEntity[] { threatened, distant });
            Assert.That(planner.Groups.Count, Is.EqualTo(2));
            EnemyShipTaskGroup first = planner.Groups[0];
            EnemyShipTaskGroup second = planner.Groups[1];
            first.Executor.Execute(new EnemyStrategicDecision("test", EnemyStrategicState.RetreatValue, 1),
                Context(first.Ships));
            second.Executor.Execute(new EnemyStrategicDecision("test", EnemyStrategicState.CaptureZone, 1),
                Context(second.Ships));

            Assert.That(orders.Calls.Single(call => call.Action == UnitActionId.Retreat).Receivers
                .Select(receiver => receiver.Id), Is.EqualTo(new[] { threatened.EntityId }));
            Assert.That(orders.Calls.Single(call => call.Action == UnitActionId.AttackMove).Receivers
                .Select(receiver => receiver.Id), Is.EqualTo(new[] { distant.EntityId }));
            planner.Reconcile(new IShipEntity[] { threatened, distant });
            Assert.That(planner.Groups[0], Is.SameAs(first));
            Assert.That(planner.Groups[1], Is.SameAs(second));
        }

        [Test]
        public void TaskGroups_MergedIdleGroupDoesNotRestoreDestroyedShips()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, new ZoneSource(roster),
                new SiteSource(), new TestHazards());
            FakeOrderService orders = new FakeOrderService();
            var planner = new EnemyShipTaskGroupPlanner(builder,
                new EnemyStrategicDecisionModel(), new EnemyTaskForceExecutor(orders), orders);
            EnemyShipTaskGroup idle = planner.Groups.Single();
            IShipEntity[] ships = {
                new FakeShip(Vector3.zero, 1),
                new FakeShip(Vector3.right * 5000f, 2),
                new FakeShip(Vector3.right * 5010f, 3)
            };

            planner.Reconcile(ships, stationPressure: true);

            Assert.That(planner.Groups.Single(), Is.Not.SameAs(idle));
            Assert.That(planner.Groups.Single().Ships, Is.EquivalentTo(ships));
            Assert.That(idle.Ships, Is.Empty);

            planner.Reconcile(Array.Empty<IShipEntity>());

            Assert.That(planner.Groups.Single(), Is.SameAs(idle));
            Assert.That(planner.Groups.Single().Ships, Is.Empty);
            IShipEntity replacement = new FakeShip(Vector3.zero, 4);
            planner.Reconcile(new[] { replacement });
            Assert.That(planner.Groups.SelectMany(group => group.Ships), Is.EqualTo(new[] { replacement }));
        }

        [Test]
        public void TaskGroups_NearbyMergeReleasesDonorMembership()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, new ZoneSource(roster),
                new SiteSource(), new TestHazards());
            FakeOrderService orders = new FakeOrderService();
            var planner = new EnemyShipTaskGroupPlanner(builder,
                new EnemyStrategicDecisionModel(), new EnemyTaskForceExecutor(orders), orders);
            FakeShip first = new FakeShip(Vector3.zero, 1);
            FakeShip second = new FakeShip(Vector3.right * 5000f, 2);
            IShipEntity[] ships = { first, second };
            planner.Reconcile(ships);
            EnemyShipTaskGroup donor = planner.Groups[1];

            second.WorldPosition = Vector3.right;
            planner.Reconcile(ships);

            Assert.That(planner.Groups.Single().Ships, Is.EquivalentTo(ships));
            Assert.That(donor.Ships, Is.Empty);
        }

        [Test]
        public void TaskGroups_DestroyedMainForceDoesNotPromoteReplacementShip()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, new ZoneSource(roster),
                new SiteSource(), new TestHazards());
            FakeOrderService orders = new FakeOrderService();
            var planner = new EnemyShipTaskGroupPlanner(builder,
                new EnemyStrategicDecisionModel(), new EnemyTaskForceExecutor(orders), orders);
            planner.Reconcile(new IShipEntity[] { new FakeShip(Vector3.zero, 1) }, stationPressure: true);
            EnemyShipTaskGroup main = planner.Groups.Single();
            Assert.That(main.IsMainForce, Is.True);

            planner.Reconcile(Array.Empty<IShipEntity>());

            Assert.That(main.IsMainForce, Is.False);
            planner.Reconcile(new IShipEntity[] { new FakeShip(Vector3.zero, 2) });
            Assert.That(planner.Groups.Single().IsMainForce, Is.False);
        }

        [Test]
        public void TaskGroups_MainForceRecruitsDistantShipsAndKeepsMembershipWhileRallying()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, new ZoneSource(roster),
                new SiteSource(), new TestHazards());
            FakeOrderService orders = new FakeOrderService();
            var planner = new EnemyShipTaskGroupPlanner(builder,
                new EnemyStrategicDecisionModel(), new EnemyTaskForceExecutor(orders), orders);
            var ships = Enumerable.Range(1, 6).Select(id => (IShipEntity)new FakeShip(
                Vector3.right * id * 2000f, id) { CurrentOrder = UnitOrderType.Attack }).ToList();

            planner.Reconcile(ships);
            Assert.That(planner.Groups.Count, Is.EqualTo(1));
            EnemyShipTaskGroup main = planner.Groups[0];
            Assert.That(main.IsMainForce, Is.True);
            Assert.That(main.Ships, Is.EquivalentTo(ships));
            ships.Add(new FakeShip(Vector3.left * 10000f, 7) { CurrentOrder = UnitOrderType.Attack });
            planner.Reconcile(ships);
            Assert.That(planner.Groups.Single(), Is.SameAs(main));
            Assert.That(main.Ships, Is.EquivalentTo(ships));
        }

        [Test]
        public void TaskGroups_StationPressureConcentratesEvenSmallSeparatedForces()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, new ZoneSource(roster),
                new SiteSource(), new TestHazards());
            FakeOrderService orders = new FakeOrderService();
            var planner = new EnemyShipTaskGroupPlanner(builder,
                new EnemyStrategicDecisionModel(), new EnemyTaskForceExecutor(orders), orders);
            IShipEntity[] ships = {
                new FakeShip(Vector3.zero, 1) { CurrentOrder = UnitOrderType.Attack },
                new FakeShip(Vector3.right * 5000f, 2) { CurrentOrder = UnitOrderType.Attack }
            };
            planner.Reconcile(ships);
            Assert.That(planner.Groups.Count, Is.EqualTo(2));
            planner.Reconcile(ships, stationPressure: true);
            Assert.That(planner.Groups.Count, Is.EqualTo(1));
            Assert.That(planner.Groups[0].Ships, Is.EquivalentTo(ships));
        }

        [Test]
        public void CaptureSelection_SkipsAnotherGroupsReservedObjective()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            ZoneSource zones = new ZoneSource(roster, Vector3.right * 10f, Vector3.right * 100f);
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, zones, new SiteSource(), new TestHazards());
            EnemyCaptureObjective first = SelectCapture(builder);
            typeof(EnemyStrategicContextBuilder).GetField("_reservedCaptureObjectives",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(builder, new HashSet<string> { first.Id });

            Assert.That(SelectCapture(builder).Position, Is.EqualTo(Vector3.right * 100f));
        }

        [Test]
        public void CaptureSelection_ReservedObjectiveRemainsDistinctFromNoViableObjective()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            ZoneSource zones = new ZoneSource(roster, Vector3.right * 10f);
            EnemyStrategicContextBuilder builder = CaptureBuilder(roster, zones, new SiteSource(), new TestHazards());
            string objectiveId = SelectCapture(builder).Id;
            typeof(EnemyStrategicContextBuilder).GetField("_reservedCaptureObjectives",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(builder, new HashSet<string> { objectiveId });

            for (int i = 0; i < 10; i++)
            {
                Assert.That(SelectCapture(builder, i), Is.Null);
                Assert.That(builder.CaptureReservedByOtherGroup, Is.True);
            }
            zones.Zones[0].Restore(TestPlayers.Enemy, PlayerId.None, 0f, false);
            Assert.That(SelectCapture(builder, 10f), Is.Null);
            Assert.That(builder.CaptureReservedByOtherGroup, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CaptureZone_CapitalCapturesUnlessItsClearanceFails(bool blocked)
        {
            var scout = new FakeShip(Vector3.zero, 1) { NavigationRadius = 5f };
            var capital = new FakeShip(Vector3.zero, 2) { NavigationRadius = 100f };
            var objective = new EnemyCaptureObjective("centre", EnemyCaptureObjectiveType.ReinforcementZone,
                PlayerId.None, Vector3.right * 500f, 50f, 0f);
            var orders = new FakeOrderService();
            var executor = new EnemyTaskForceExecutor(orders);
            executor.Execute(new EnemyStrategicDecision("test", EnemyStrategicState.CaptureZone, 2),
                Context(new IShipEntity[] { capital, scout }, captureObjective: objective,
                    isPointClear: (point, radius) => !blocked || radius < 100f));

            OrderCall capture = orders.Calls.Single(call => call.Receivers.Any(ship => ship.Id == scout.EntityId));
            OrderCall escort = orders.Calls.Single(call => call.Receivers.Any(ship => ship.Id == capital.EntityId));
            Assert.That(capture.Point, Is.EqualTo(objective.Position));
            Assert.That(Vector3.Distance(escort.Point, objective.Position), Is.EqualTo(blocked ? 150f : 0f));
            Assert.That(orders.Calls.Count, Is.EqualTo(blocked ? 2 : 1));
            Assert.That(capture.Receivers.Count, Is.EqualTo(blocked ? 1 : 2));
        }

[Test]
        public void CaptureZone_CapitalUsesClearInteriorSampleAndRejoinsAfterPerimeterOrder()
        {
            var scout = new FakeShip(Vector3.zero, 1) { NavigationRadius = 5f };
            var capital = new FakeShip(Vector3.zero, 2) { NavigationRadius = 100f };
            var objective = new EnemyCaptureObjective("centre", EnemyCaptureObjectiveType.ReinforcementZone,
                PlayerId.None, Vector3.right * 500f, 50f, 0f);
            var orders = new FakeOrderService();
            var executor = new EnemyTaskForceExecutor(orders);
            var decision = new EnemyStrategicDecision("test", EnemyStrategicState.CaptureZone, 2);
            bool blocked = true;
            var context = Context(new IShipEntity[] { capital, scout }, captureObjective: objective,
                isPointClear: (point, radius) => radius < 100f ||
                    !blocked && point.x > objective.Position.x);
            executor.Execute(decision, context);
            capital.CurrentOrder = UnitOrderType.AttackMove;
            scout.CurrentOrder = UnitOrderType.AttackMove;
            orders.Calls.Clear();
            blocked = false;

            executor.Execute(decision, context);

            Assert.That(orders.Calls.Single().Receivers.Single().Id, Is.EqualTo(capital.EntityId));
            Assert.That(orders.Calls.Single().Point, Is.EqualTo(objective.Position + Vector3.right * 25f));
        }

        [Test]
        public void CaptureSelection_CapitalClearanceDoesNotBlockSmallCapturer()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            Vector3 center = Vector3.right * 100f;
            var obstacles = new RetreatObstacleProvider();
            obstacles.Contacts.Add(new RadarContact(center + Vector3.forward * 30f, 5f, false));
            var hazards = new TestHazards();
            var navigation = new ShipNavigationService(obstacles, hazards, new OfflineSession(PlayerId.None));
            try
            {
                var builder = CaptureBuilder(roster, new ZoneSource(roster, center), new SiteSource(), hazards, navigation);
                var ships = (List<IShipEntity>)typeof(EnemyStrategicContextBuilder)
                    .GetField("_ownShips", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(builder);
                ships.Clear();
                ships.Add(new FakeShip(Vector3.zero, 1) { NavigationRadius = 5f });
                ships.Add(new FakeShip(Vector3.zero, 2) { NavigationRadius = 100f });
                Assert.That(navigation.IsPositionClear(center, 100f, false), Is.False);
                Assert.That(SelectCapture(builder).Position, Is.EqualTo(center));
                obstacles.Contacts.Add(new RadarContact(center, 30f, false));
                Assert.That(SelectCapture(builder), Is.Null, "Static obstacles still block the small capturer.");
            }
            finally { navigation.Dispose(); }
        }

        [Test]
        public void CaptureSelection_IgnoresOwnDestinationReservationButKeepsObstacleRejection()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            Vector3 center = Vector3.right * 100f;
            var obstacles = new RetreatObstacleProvider();
            var hazards = new TestHazards();
            var navigation = new ShipNavigationService(obstacles, hazards, new OfflineSession(PlayerId.None));
            try
            {
                var builder = CaptureBuilder(roster, new ZoneSource(roster, center), new SiteSource(), hazards, navigation);
                var ships = (List<IShipEntity>)typeof(EnemyStrategicContextBuilder)
                    .GetField("_ownShips", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(builder);
                ((FakeShip)ships[0]).WorldPosition = Vector3.zero;
                var range = new Vector2Range();
                Type rangeType = range.GetType().BaseType;
                rangeType.GetField("<Min>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(range, Vector2.one * -250f);
                rangeType.GetField("<Max>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(range, Vector2.one * 250f);
                var traveler = new NavigationRetreatFacade(navigation, range);
                traveler.Retreat(center);

                Assert.That(navigation.IsPositionClear(center, 5f), Is.False);
                Assert.That(SelectCapture(builder).Position, Is.EqualTo(center));
                obstacles.Contacts.Add(new RadarContact(center, 30f, false));
                Assert.That(SelectCapture(builder), Is.Null);
            }
            finally { navigation.Dispose(); }
        }

        private static EnemyStrategicContextBuilder CaptureBuilder(PlayerRoster roster,
            ZoneSource zones, SiteSource sites, TestHazards hazards, IShipNavigationService navigation = null,
            IEnemyCaptureObjectiveSource[] sources = null)
        {
            EntityLocator locator = new EntityLocator();
            EnemyStrategicContextBuilder builder = CreateContextBuilder(
                null, null, sources ?? SiteObjectives(sites, roster), locator, null, roster, null,
                new TeamIntelRegistry(), new VisionService(roster, fogEnabled: false),
                roster.Get(TestPlayers.Enemy), null, hazards, zones,
                navigation, new EnemyStationPressureTracker(), CreateRecovery(roster, zones));
            List<IShipEntity> ships = (List<IShipEntity>)typeof(EnemyStrategicContextBuilder)
                .GetField("_ownShips", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(builder);
            foreach (ReinforcementZonePresenter zone in zones.Zones)
                ships.Add(new FakeShip(zone.Center, ships.Count + 1));
            foreach (ICaptureSite site in sites.Sites)
                ships.Add(new FakeShip(site.Center, ships.Count + 1));
            return builder;
        }

        private static EnemyStrategicContextBuilder CreateContextBuilder(
            IShipService ships, IReinforcementZonesSystem reinforcementZones,
            IReadOnlyList<IEnemyCaptureObjectiveSource> captureSources, IEntityLocator entities, IGameModelObserver game,
            PlayerRoster roster, UnitCombatProfileCatalog catalog, TeamIntelRegistry intel, IVisionService vision,
            PlayerSlot owner, IProductionAccess production, IHazardService hazards, IReinforcementZoneSource zones,
            IShipNavigationService navigation, EnemyStationPressureTracker pressure, EnemyRecoveryAnchor recovery,
            AiWorldSnapshot snapshot = null)
        {
            snapshot ??= new AiWorldSnapshot(entities);
            catalog ??= new UnitCombatProfileCatalog(null, null, null, null);
            zones ??= new ZoneSource(roster);
            return new EnemyStrategicContextBuilder(ships, reinforcementZones, captureSources, entities, game, roster,
                new ForceCompositionBuilder(entities, catalog, snapshot), intel, vision, owner, production, hazards, zones,
                navigation, pressure, recovery, snapshot, new TeamAiPictureRegistry(intel, roster, zones, catalog, snapshot));
        }

        private static EnemyRecoveryAnchor CreateRecovery(PlayerRoster roster, ZoneSource zones = null)
        {
            var locator = new EntityLocator();
            return new EnemyRecoveryAnchor(null, zones ?? new ZoneSource(roster), roster, locator, null, null,
                new TeamIntelRegistry(), roster.Get(TestPlayers.Enemy), null, new EnemyStationPressureTracker(),
                new AiWorldSnapshot(locator));
        }

        private static EnemyCaptureObjective SelectCapture(EnemyStrategicContextBuilder builder, float now = 0f)
        {
            RefreshTeamPicture(builder, now);
            return (EnemyCaptureObjective)typeof(EnemyStrategicContextBuilder)
                .GetMethod("SelectCaptureObjective", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(builder, new object[] { Vector3.zero, now, false });
        }

        private static void RefreshTeamPicture(EnemyStrategicContextBuilder builder, float now)
        {
            var picture = (TeamAiPicture)typeof(EnemyStrategicContextBuilder)
                .GetField("_teamPicture", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(builder);
            picture.Refresh(now);
        }

        private static ForceComposition CombatForce(float hull, float capitalDps, float fighterDps = 0f,
            ShipClass shipClass = ShipClass.Capital)
        {
            var damage = new float[ForceComposition.ClassSlots];
            damage[(int)ShipClass.Capital] = capitalDps;
            damage[(int)ShipClass.Fighter] = fighterDps;
            var force = new ForceComposition();
            force.AddNew(new UnitCombatProfile(shipClass, hull, 0f, damage, damage,
                new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>()), false);
            return force;
        }

        private static EnemyStrategicContext Context(IReadOnlyList<IShipEntity> ships,
            IEntity fleetTarget = null, IEntity ownBase = null, IEntity defenseTarget = null,
            EnemyCaptureObjective captureObjective = null, Vector3? retreatAnchor = null,
            bool hasSafeRetreat = true,
            IReadOnlyDictionary<IShipEntity, ForceComposition> shipForces = null,
            IReadOnlyDictionary<IEntity, ForceComposition> targetForces = null,
            IEntity localThreatTarget = null,
            EnemyStationPressureTracker stationPressure = null,
            Func<Vector3, float, bool> isPointClear = null, IEntity captureBlockingFacility = null,
            Vector3? guardPosition = null)
        {
            if (ownBase != null && stationPressure == null)
            {
                stationPressure = new EnemyStationPressureTracker();
                stationPressure.Update(ownBase.Id, 100f, 100f, Time.time, defenseTarget == null ? 0 : defenseTarget.Id);
                if (defenseTarget != null)
                {
                    Vector3 threat = defenseTarget.GetFacade<IEntityTransformFacade>().Transform.position;
                    stationPressure.ObserveThreatPosition(threat.x, threat.y, threat.z);
                }
            }
            Dictionary<IShipEntity, IEntity> receivers = new Dictionary<IShipEntity, IEntity>();
            var defaultShipForces = new Dictionary<IShipEntity, ForceComposition>();
            foreach (IShipEntity ship in ships)
            {
                receivers.Add(ship, new FakeEntity(id: ship.EntityId, side: TestPlayers.Enemy,
                    health: new FakeHealthModel(null)));
                defaultShipForces.Add(ship, CombatForce(100f, 10f));
            }
            var defaultTargetForces = new Dictionary<IEntity, ForceComposition>();
            foreach (IEntity target in new[] { fleetTarget, defenseTarget, localThreatTarget })
                if (target != null) defaultTargetForces[target] = new ForceComposition();
            EnemyStrategicSnapshot snapshot = new EnemyStrategicSnapshot(
                victoryCondition: BattleVictoryCondition.DestroyEnemyFleet, difficulty: EnemyAiDifficulty.UltraHard,
                ownShipCount: ships.Count, enemyShipCount: 0, ownedCapturableZoneCount: 0,
                fleetAdvantage: 1f, baseThreatRatio: 0f, hasCaptureTarget: true,
                hasEnemyBaseTarget: false, hasOwnBase: ownBase != null,
                hasSafeRetreat: hasSafeRetreat);
            EnemyCaptureObjective objective = captureObjective ?? new EnemyCaptureObjective(
                "relay-1", EnemyCaptureObjectiveType.ReinforcementZone, PlayerId.None,
                new Vector3(55f, 0f, -55f), 10f, 0f);
            return new EnemyStrategicContext(snapshot: snapshot, ships: ships,
                captureTarget: objective.Position, enemyFleetTarget: fleetTarget, enemyBaseTarget: null,
                ownBase: ownBase, receivers: receivers, captureObjective: objective,
                defenseTarget: defenseTarget, retreatAnchor: retreatAnchor,
                shipForces: shipForces ?? defaultShipForces, targetForces: targetForces ?? defaultTargetForces,
                localThreatTarget: localThreatTarget, stationPressure: stationPressure,
                isDefensePointClear: isPointClear ?? ((point, radius) => true),
                captureBlockingFacility: captureBlockingFacility, guardPosition: guardPosition);
        }

        private sealed class ZoneSource : IReinforcementZoneSource
        {
            public IReadOnlyList<ReinforcementZonePresenter> Zones { get; }

            public ZoneSource(PlayerRoster roster, params Vector3[] centers) : this(roster, -1, centers) { }

            public ZoneSource(PlayerRoster roster, int mainIndex, params Vector3[] centers)
            {
                List<ReinforcementZonePresenter> zones = new List<ReinforcementZonePresenter>();
                for (int i = 0; i < centers.Length; i++)
                    zones.Add(new ReinforcementZonePresenter(new ZoneView(centers[i]),
                        TestPlayers.CreateLocalPlayer(roster),
                        new ReinforcementZoneModel(roster, PlayerId.None, 10f, 1f, true), isMain: i == mainIndex));
                Zones = zones;
            }
        }

        private sealed class ZoneView : IReinforcementZoneView
        {
            public Vector3 Center { get; }
            public float Radius => 10f;
            public PlayerId StartingOwner => PlayerId.None;
            public bool IsCapturable => true;
            public float CaptureDuration => 10f;

            public ZoneView(Vector3 center) { Center = center; }

            public void SetVisibility(bool isVisible, bool showCaptureUi) { }

            public void Render(OwnerRelation owner, OwnerRelation capturer, float captureProgress, bool isContested,
                OwnerRelation contestingFirst = OwnerRelation.Neutral,
                OwnerRelation contestingSecond = OwnerRelation.Neutral) { }
        }

        private sealed class CaptureSiteStub : ICaptureSite
        {
            public CaptureSiteKind Kind { get; set; } = CaptureSiteKind.Mining;
            public int PointId { get; set; }
            public PlayerId Owner { get; set; } = PlayerId.None;
            public CaptureSiteState? FacilityState { get; set; }
            public CaptureSiteState State => FacilityState ??
                (Owner == PlayerId.None ? CaptureSiteState.Neutral : CaptureSiteState.Owned);
            public SiteFacilityType FacilityType => SiteFacilityType.Mining;
            public float CaptureProgress => 0f;
            public float ConstructionProgress => 0f;
            public Vector3 Center { get; }
            public Vector3 FacilityPosition => Center;
            public bool IsRevealed => true;
            public float PointerHitDistance(Ray ray) => float.PositiveInfinity;
            public bool IsOperational => false;

            public CaptureSiteStub(Vector3 center) { Center = center; }

            public bool Contains(Vector3 position, float clearance = 0f) =>
                (position - Center).sqrMagnitude <= (10f + clearance) * (10f + clearance);
        }

        private static IEnemyCaptureObjectiveSource[] SiteObjectives(SiteSource sites, PlayerRoster roster)
        {
            CaptureSiteData data = AssetDatabase.LoadAssetAtPath<CaptureSiteData>(
                "Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset");
            return new IEnemyCaptureObjectiveSource[] {
                new EnemySiteObjectives(sites, sites, data, roster, roster.Get(TestPlayers.Enemy))
            };
        }

        private sealed class MutableCaptureObjectiveSource : IEnemyCaptureObjectiveSource
        {
            public EnemyCaptureObjective Objective { get; set; }

            public void AddStrategicObjectives(List<EnemyCaptureObjective> objectives, HostileIntelModel intel)
            {
                if (Objective != null) objectives.Add(Objective);
            }

            public void AddSquadronTargets(EnemySquadronCaptureAssignments assignments) { }
        }

        private sealed class SiteSource : ICaptureSiteDirectory, ICaptureSitePresence
        {
            public event Action SitesChanged { add { } remove { } }
            public bool HostilePresent { get; set; }
            public IReadOnlyList<ICaptureSite> Sites { get; }

            public SiteSource(params ICaptureSite[] sites) { Sites = sites; }

            public bool HasHostileUnits(ICaptureSite site, PlayerId owner) => HostilePresent;
        }

        private sealed class FakeShip : IShipEntity
        {
            public PlayerId Owner => TestPlayers.Enemy;
            public Vector3 WorldPosition { get; set; }
            public float NavigationRadius { get; set; } = 5f;
            public float NavigationSpeed { get; set; } = 1f;
            public long EntityId { get; }
            public UnitOrderType CurrentOrder { get; set; }

            public FakeShip(Vector3 position, long id) { EntityId = id; WorldPosition = position; }
        }

        private sealed class FakeOrderService : IUnitOrderService
        {
            public event Action<UnitOrder> OrderIssued { add { } remove { } }

            public List<OrderCall> Calls { get; } = new List<OrderCall>();

            public void IssueMove(IReadOnlyList<IEntity> receivers, Vector3 point) =>
                Record(UnitActionId.Move, receivers, point);

            public void IssueMove(IReadOnlyList<IEntity> receivers,
                IReadOnlyList<Vector3> destinations) =>
                Record(UnitActionId.Move, receivers,
                    destinations.Count > 0 ? destinations[0] : default);

            public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target) =>
                Record(UnitActionId.Attack, receivers, target: target);

            public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target,
                IReadOnlyList<Vector3> offsets) =>
                Record(UnitActionId.Attack, receivers, target: target, offsets: offsets);

            public void IssueHardPointAttack(IReadOnlyList<IEntity> receivers, IEntity target,
                int hardPointId) =>
                Record(UnitActionId.Attack, receivers, target: target);

            public void IssueAttackMove(IReadOnlyList<IEntity> receivers, Vector3 point) =>
                Record(UnitActionId.AttackMove, receivers, point);

            public void IssueAttackMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> destinations) =>
                Record(UnitActionId.AttackMove, receivers, destinations[0], destinations: destinations);

            public void IssueStop(IReadOnlyList<IEntity> receivers) =>
                Record(UnitActionId.Stop, receivers);

            public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly) =>
                Record(UnitActionId.Guard, receivers, target: friendly);

            public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly,
                IReadOnlyList<Vector3> offsets) =>
                Record(UnitActionId.Guard, receivers, target: friendly, offsets: offsets);

            public void IssueWaypointMove(IReadOnlyList<IEntity> receivers,
                IReadOnlyList<Vector3> waypoints) =>
                Record(UnitActionId.WaypointMove, receivers);

            public void IssueHunt(IReadOnlyList<IEntity> receivers) =>
                Record(UnitActionId.Hunt, receivers);

            public void IssueRetreat(IReadOnlyList<IEntity> receivers) =>
                Record(UnitActionId.Retreat, receivers);

            public void IssueRetreat(IReadOnlyList<IEntity> receivers, Vector3 point) =>
                Record(UnitActionId.Retreat, receivers, point);

            private void Record(UnitActionId action, IReadOnlyList<IEntity> receivers,
                Vector3 point = default, IEntity target = null,
                IReadOnlyList<Vector3> offsets = null, IReadOnlyList<Vector3> destinations = null) =>
                Calls.Add(new OrderCall(action: action, receivers: receivers, point: point, target: target,
                    offsets: offsets, destinations: destinations));
        }

        private sealed class OrderCall
        {
            public UnitActionId Action { get; }
            public IReadOnlyList<IEntity> Receivers { get; }
            public Vector3 Point { get; }
            public IEntity Target { get; }
            public IReadOnlyList<Vector3> Offsets { get; }
            public IReadOnlyList<Vector3> Destinations { get; }

            public OrderCall(IReadOnlyList<IEntity> receivers, IEntity target,
                IReadOnlyList<Vector3> offsets, UnitActionId action, Vector3 point,
                IReadOnlyList<Vector3> destinations = null)
            {
                Action = action;
                Receivers = receivers;
                Point = point;
                Target = target;
                Offsets = offsets;
                Destinations = destinations;
            }
        }

        private sealed class NavigationRetreatFacade : IRetreatFacade, IShipNavigationAgent
        {
            private readonly ShipNavigationService _navigation;
            private readonly Vector2Range _range;
            public Vector3 WorldPosition => Vector3.zero;
            public Vector3 NavigationPosition => WorldPosition;
            public float NavigationHeight => 0f;
            public ShipHullSpan NavigationHullSpan => ShipHullSpan.Unbounded;
            public float NavigationRadius => 5f;
            public float NavigationSpeed => 10f;
            public float NavigationCurrentSpeed => 0f;
            public float NavigationRotationSpeed => 90f;
            public bool IgnoresMapObstacles => false;
            public Vector3 Destination { get; private set; }

            public NavigationRetreatFacade(ShipNavigationService navigation, Vector2Range range)
            {
                _navigation = navigation;
                _range = range;
                _navigation.Register(this, WorldPosition);
            }

            public void Retreat(Vector3 destination)
            {
                Destination = _navigation.Plan(this, Vector3.right, destination, Array.Empty<RadarContact>(),
                    0.5f, NavigationRadius, _range).Destination;
            }
        }

        private sealed class RetreatObstacleProvider : IMapObstacleContactProvider
        {
            public string Id => nameof(RetreatObstacleProvider);
            public List<RadarContact> Contacts { get; } = new List<RadarContact>();

            public void CopyContacts(List<RadarContact> destination)
            {
                destination.Clear();
                destination.AddRange(Contacts);
            }
        }

        private sealed class WithdrawalProduction : IProductionAccess
        {
            public bool CanProduce => true;
            public bool HasHostBase => true;
            public PlayerId ProductionHost => TestPlayers.Enemy;
            public bool TryGetProductionBase(out IEntity productionBase) { productionBase = null; return false; }
        }

        private sealed class WithdrawalZones : IReinforcementZonesSystem
        {
            public event Action OwnershipChanged { add { } remove { } }
            public void CopyOwnedCapturableZoneBounds(PlayerId owner, List<Bounds> destination) => destination.Clear();
            public int GetOwnedCapturableZoneCount(PlayerId owner) => 0;
            public bool TryGetDefaultZoneCenter(PlayerId owner, out Vector3 position) { position = default; return false; }
            public bool TryGetDefaultZoneExitPosition(PlayerId owner, Vector3 shipPosition, float shipRadius,
                out Vector3 position) { position = default; return false; }
            public bool TryGetCaptureTarget(PlayerId owner, Vector3 origin, out Vector3 position)
            { position = default; return false; }
        }

        private sealed class FakeEntity : IEntity, ICombatModifiersFacade, IUnitTypeFacade, IMiningFacilityFacade,
            INavigationFootprintFacade, IUnitCombatObserverFacade, IStationDurabilityFacade, IPlayerBaseFacade, ISecondStationFacade
        {
            public bool IsUnit { get; set; } = true;
            public bool IsMine { get; set; }
            public bool IsBase { get; set; }
            public bool IsSecondStation { get; set; }
            public IRetreatFacade RetreatCommand { get; set; }
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public UnitTypeId UnitTypeId { get; set; } = UnitTypeId.Ship(ShipType.Venator);
            public long Id { get; }
            public IHealthModelObserver HealthModel { get; }
            public PlayerId Owner { get; }
            public float NavigationRadius { get; set; } = 5f;
            public float MaximumDurability { get; set; }
            public float WeaponRange { get; set; }
            public float RadarRange => WeaponRange;
            public IEntity Target { get; set; }
            public IEntity TrackedWeaponTarget { get; set; }

            public bool IsAttacking(IEntity target) => Target == target || TrackedWeaponTarget == target;

            public FakeEntity(IHealthModelObserver health, PlayerId side, long id)
            { Id = id; Owner = side; HealthModel = health; MaximumDurability = health.Hull + health.Shields; }

            public TCommand GetFacade<TCommand>() where TCommand : IEntityFacade
            { TryGetFacade(out TCommand facade); return facade; }

            public bool TryGetFacade<TCommand>(out TCommand command)
                where TCommand : IEntityFacade
            {
                if (!IsUnit && typeof(TCommand) == typeof(IUnitTypeFacade) ||
                    !IsMine && typeof(TCommand) == typeof(IMiningFacilityFacade) ||
                    !IsBase && typeof(TCommand) == typeof(IPlayerBaseFacade) ||
                    !IsSecondStation && typeof(TCommand) == typeof(ISecondStationFacade))
                { command = default; return false; }
                if (this is TCommand self) { command = self; return true; }
                if (RetreatCommand is TCommand retreat) { command = retreat; return true; }
                if (HealthModel is IEntityTransformFacade { Transform: not null } transformFacade &&
                    transformFacade is TCommand typedTransform)
                { command = typedTransform; return true; }
                command = default;
                return false;
            }
        }

        private sealed class FakeHealthModel : IHealthModelObserver, IEntityTransformFacade
        {
            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged { add { } remove { } }

            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull { get; set; } = 1f;
            public ShipClass ShipClass { get; set; } = ShipClass.Capital;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields { get; set; } = 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed { get; set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerId Owner => TestPlayers.Human;
            public Transform Transform { get; }
            public bool HasShields => true;

            public FakeHealthModel(Transform transform) { Transform = transform; }

            public IHardPointModel[] GetShipUnits(HardPointType type) =>
                Array.Empty<IHardPointModel>();
        }
    }
}
