using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.Ships;
using EmpireAtWar.Ship;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("GameFlow")]
    public sealed class BattleVictoryServiceTests
    {
        [Test]
        public void Collection_EighthPlayerBaseAndFleetKeepItsTeamAlive()
        {
            var slots = new PlayerSlot[8];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = new PlayerSlot(new PlayerId(i), new TeamId(i == 0 ? 0 : 1), FactionType.Republic,
                    i == 0 ? PlayerController.Human : PlayerController.Ai, EnemyAiDifficulty.Medium, i);
            var roster = new PlayerRoster(slots);
            var entities = new EntityLocator();
            var ships = new ShipService();
            var last = new PlayerId(7);
            var home = new Unit(1, new PlayerId(0), true);
            var enemyBase = new Unit(2, last, true);
            var enemyShip = new Unit(3, last, false);
            entities.AddEntity(home);
            entities.AddEntity(enemyBase);
            entities.AddEntity(enemyShip);
            ships.Add(enemyShip);
            var victory = new BattleVictoryModel();
            victory.RestoreObservedBases(Array.ConvertAll(slots, slot => slot.Id));
            var service = new BattleVictoryService(null, ships, entities, roster,
                TestPlayers.CreateLocalPlayer(roster), new PlayerRegistry(), null, victory,
                Array.Empty<IBattleSurvivalSource>(), new BattleClock(new SkirmishStateModel()), null);

            var states = Collect(service);
            Assert.That(states.Count, Is.EqualTo(8));
            Assert.That(states[7].UnitCount, Is.EqualTo(1));
            Assert.That(states[7].IsBaseAlive, Is.True);
            Assert.That(victory.Evaluate(BattleVictoryCondition.DestroyEnemyFleet, states, new TeamId(0)),
                Is.EqualTo(BattleOutcome.None));

            enemyBase.Health.IsDestroyed = true;
            enemyShip.Health.IsDestroyed = true;
            states = Collect(service);
            Assert.That(states[7].UnitCount, Is.Zero);
            Assert.That(states[7].IsBaseAlive, Is.False);
            Assert.That(victory.Evaluate(BattleVictoryCondition.DestroyEnemyFleet, states, new TeamId(0)),
                Is.EqualTo(BattleOutcome.PlayerVictory));
        }

        [Test]
        public void Collection_SeparatesBasesAndUnits_ExcludesDestroyedEntities_AndRefreshes()
        {
            var roster = TestPlayers.CreateDuel();
            var entities = new EntityLocator();
            var registry = new PlayerRegistry();
            var service = new BattleVictoryService(null, new ShipService(), entities, roster,
                TestPlayers.CreateLocalPlayer(roster), registry, null, new BattleVictoryModel(),
                Array.Empty<IBattleSurvivalSource>(), new BattleClock(new SkirmishStateModel()), null);
            var humanBase = new Unit(1, TestPlayers.Human, true);
            var enemyBase = new Unit(2, TestPlayers.Enemy, true);
            var humanUnit = new Unit(3, TestPlayers.Human, false);
            var wreck = new Unit(4, TestPlayers.Enemy, false);
            wreck.Health.IsDestroyed = true;
            foreach (var unit in new[] { humanBase, enemyBase, humanUnit, wreck }) entities.AddEntity(unit);

            List<PlayerBattleState> states = Collect(service);
            Assert.That(states.Find(value => value.Player == TestPlayers.Human).UnitCount, Is.EqualTo(1));
            Assert.That(states.Find(value => value.Player == TestPlayers.Human).IsBaseAlive, Is.True);
            Assert.That(states.Find(value => value.Player == TestPlayers.Enemy).UnitCount, Is.Zero);
            Assert.That(states.Find(value => value.Player == TestPlayers.Enemy).IsBaseAlive, Is.True);

            enemyBase.Health.HasUnits = false;
            entities.RemoveEntity(humanUnit);
            states = Collect(service);
            Assert.That(states.Find(value => value.Player == TestPlayers.Human).UnitCount, Is.Zero);
            Assert.That(states.Find(value => value.Player == TestPlayers.Enemy).IsBaseAlive, Is.False);
        }

        [Test]
        public void FleetCondition_EndsWithOnlyEnemySquadronsRemaining()
        {
            var roster = TestPlayers.CreateDuel();
            var entities = new EntityLocator();
            var victory = new BattleVictoryModel();
            var service = new BattleVictoryService(null, new ShipService(), entities, roster,
                TestPlayers.CreateLocalPlayer(roster), new PlayerRegistry(), null, victory,
                Array.Empty<IBattleSurvivalSource>(), new BattleClock(new SkirmishStateModel()), null);
            entities.AddEntity(new Unit(1, TestPlayers.Human, true));
            var enemyBase = new Unit(2, TestPlayers.Enemy, true);
            entities.AddEntity(enemyBase);
            entities.AddEntity(new Entity(new Health(), entities,
                new[] { new UnitTypeFacade(UnitTypeId.Squadron(SquadronType.Delta7)) }, TestPlayers.Enemy, 3,
                Array.Empty<IPooledEntityEvents>(), Array.Empty<IHangarLaunchEvents>()));

            Assert.That(victory.Evaluate(BattleVictoryCondition.DestroyEnemyFleet, Collect(service), new TeamId(0)),
                Is.EqualTo(BattleOutcome.None));
            enemyBase.Health.IsDestroyed = true;
            Assert.That(victory.Evaluate(BattleVictoryCondition.DestroyEnemyFleet, Collect(service), new TeamId(0)),
                Is.EqualTo(BattleOutcome.PlayerVictory));
        }

        [Test]
        public void FleetCondition_EndsWithOnlyEnemyDefensePlatformsRemaining()
        {
            var roster = TestPlayers.CreateDuel();
            var entities = new EntityLocator();
            var victory = new BattleVictoryModel();
            var service = new BattleVictoryService(null, new ShipService(), entities, roster,
                TestPlayers.CreateLocalPlayer(roster), new PlayerRegistry(), null, victory,
                Array.Empty<IBattleSurvivalSource>(), new BattleClock(new SkirmishStateModel()), null);
            entities.AddEntity(new Unit(1, TestPlayers.Human, true));
            var enemyBase = new Unit(2, TestPlayers.Enemy, true);
            entities.AddEntity(enemyBase);
            entities.AddEntity(new Unit(3, TestPlayers.Enemy, false, isShip: false));

            Assert.That(Collect(service).Find(value => value.Player == TestPlayers.Enemy).UnitCount, Is.Zero);
            Assert.That(victory.Evaluate(BattleVictoryCondition.DestroyEnemyFleet, Collect(service), new TeamId(0)),
                Is.EqualTo(BattleOutcome.None));
            enemyBase.Health.IsDestroyed = true;
            Assert.That(victory.Evaluate(BattleVictoryCondition.DestroyEnemyFleet, Collect(service), new TeamId(0)),
                Is.EqualTo(BattleOutcome.PlayerVictory));
        }

        [Test]
        public void Collection_TracksNonSquadronReserveUntilConsumed()
        {
            var roster = TestPlayers.CreateDuel();
            var registry = new PlayerRegistry();
            var reserve = new ReinforcementModel(null);
            var request = new ShipUnitRequest(new FactionData(), ShipType.Venator);
            registry.RegisterReinforcementReserve(TestPlayers.Enemy, reserve);
            reserve.AddReinforcement(new SquadronUnitRequest(new FactionData(), SquadronType.Delta7));
            reserve.AddReinforcement(request);
            var service = new BattleVictoryService(null, new ShipService(), new EntityLocator(), roster,
                TestPlayers.CreateLocalPlayer(roster), registry, null, new BattleVictoryModel(),
                Array.Empty<IBattleSurvivalSource>(), new BattleClock(new SkirmishStateModel()), null);
            Assert.That(Collect(service).Find(value => value.Player == TestPlayers.Enemy).HasPendingReinforcement, Is.True);
            Assert.That(Collect(service).Find(value => value.Player == TestPlayers.Human).HasPendingReinforcement, Is.False);
            reserve.ConsumeReinforcement(request);
            Assert.That(Collect(service).Find(value => value.Player == TestPlayers.Enemy).HasPendingReinforcement, Is.False);
        }

        [TestCase(100)]
        [TestCase(500)]
        [TestCase(1000)]
        public void Profile_Collection(int entityCount)
        {
            var allocationRecorder = UnityEngine.Profiling.Recorder.Get("GC.Alloc");
            allocationRecorder.FilterToCurrentThread();
            allocationRecorder.enabled = false;
            allocationRecorder.enabled = true;
            var allocationControl = new byte[1024];
            allocationRecorder.enabled = false;
            int allocationControlCount = allocationRecorder.sampleBlockCount;
            GC.KeepAlive(allocationControl);
            Assert.That(allocationControlCount, Is.GreaterThan(0), "Allocation recorder must detect a known allocation.");
            const int WARMUP_COUNT = 1000;
            const int ITERATION_COUNT = 5000;
            const int SAMPLE_COUNT = 5;
            var roster = TestPlayers.CreateDuel();
            var entities = new EntityLocator();
            var ships = new ShipService();
            for (int i = 0; i < entityCount; i++)
            {
                var unit = new Unit(i, i % 2 == 0 ? TestPlayers.Human : TestPlayers.Enemy, i < 2);
                entities.AddEntity(unit);
                if (i >= 2) ships.Add(unit);
            }
            var service = new BattleVictoryService(null, ships, entities, roster,
                TestPlayers.CreateLocalPlayer(roster), new PlayerRegistry(), null,
                new BattleVictoryModel(), Array.Empty<IBattleSurvivalSource>(), new BattleClock(new SkirmishStateModel()),
                null);
            // Bind once so reflection and harness allocations are outside the measured region.
            var collect = (Action)Delegate.CreateDelegate(typeof(Action), service,
                typeof(BattleVictoryService).GetMethod("CollectStates", BindingFlags.Instance | BindingFlags.NonPublic));
            for (int i = 0; i < WARMUP_COUNT; i++) collect();
            var samples = new double[SAMPLE_COUNT];
            for (int sample = 0; sample < SAMPLE_COUNT; sample++)
            {
                long start = System.Diagnostics.Stopwatch.GetTimestamp();
                for (int i = 0; i < ITERATION_COUNT; i++) collect();
                long elapsed = System.Diagnostics.Stopwatch.GetTimestamp() - start;
                samples[sample] = elapsed * 1000000.0 / System.Diagnostics.Stopwatch.Frequency / ITERATION_COUNT;
            }
            allocationRecorder.enabled = true;
            try
            {
                for (int i = 0; i < ITERATION_COUNT; i++) collect();
            }
            finally
            {
                allocationRecorder.enabled = false;
            }
            int allocationCount = allocationRecorder.sampleBlockCount;
            Array.Sort(samples);
            UnityEngine.Debug.Log($"VictoryCollection entities={entityCount}, ships={entityCount - 2}, median_us={samples[SAMPLE_COUNT / 2]:F3}, min_us={samples[0]:F3}, max_us={samples[SAMPLE_COUNT - 1]:F3}, allocation_events_per_call={(double)allocationCount / ITERATION_COUNT:F3}, control_allocation_events={allocationControlCount}");
            Assert.That(allocationCount, Is.Zero, "Warmed victory collection must not allocate.");
            var states = Collect(service);
            Assert.That(states.Find(value => value.Player == TestPlayers.Human).UnitCount, Is.EqualTo(entityCount / 2 - 1));
            Assert.That(states.Find(value => value.Player == TestPlayers.Enemy).UnitCount, Is.EqualTo(entityCount / 2 - 1));
            Assert.That(states.TrueForAll(value => value.IsBaseAlive), Is.True);
        }

        [Test]
        public void Losses_GroupAlliesAndOpponents_AndCountEachDestroyedSquadronLife()
        {
            var entities = new EntityLocator();
            var state = new SkirmishStateModel();
            var tracker = new BattleLossTracker(entities, TestPlayers.CreateLocalPlayer(TestPlayers.CreateTeamGame()), state);
            var venator = new UnitTypeFacade(UnitTypeId.Ship(ShipType.Venator));
            var local = CreateEntity(entities, 1, TestPlayers.Human, venator);
            local.Initialize();
            tracker.Initialize();
            var beforeBattle = CreateEntity(entities, 2, TestPlayers.Human, venator);
            beforeBattle.Initialize();
            ((Health)beforeBattle.HealthModel).Destroy();

            state.SetState(BattleState.Running);
            var ally = CreateEntity(entities, 3, TestPlayers.Ally, venator);
            var opposingFleet = CreateEntity(entities, 4, TestPlayers.SecondEnemy, venator);
            var squadron = CreateEntity(entities, 5, TestPlayers.Enemy,
                new UnitTypeFacade(UnitTypeId.Squadron(SquadronType.Delta7)));
            var neutral = CreateEntity(entities, 6, PlayerId.None, venator);
            foreach (Entity entity in new[] { ally, opposingFleet, squadron, neutral })
                entity.Initialize();
            entities.RemoveEntity(squadron); // Stowing a live squadron is not a loss.
            Assert.That(tracker.GetEnemyLosses(), Is.Empty);
            entities.AddEntity(squadron);
            foreach (Entity entity in new[] { local, ally, opposingFleet, squadron, neutral })
                ((Health)entity.HealthModel).Destroy();

            entities.RemoveEntity(squadron);
            ((Health)squadron.HealthModel).IsDestroyed = false;
            entities.AddEntity(squadron);
            BattleUnitLoss[] snapshot = tracker.GetEnemyLosses();
            ((Health)squadron.HealthModel).Destroy();
            state.SetState(BattleState.Ended);
            var late = CreateEntity(entities, 7, TestPlayers.Enemy, venator);
            late.Initialize();
            ((Health)late.HealthModel).Destroy();

            BattleUnitLoss playerLoss = tracker.GetPlayerLosses().Single();
            Assert.That(playerLoss.UnitType, Is.EqualTo(UnitTypeId.Ship(ShipType.Venator)));
            Assert.That(playerLoss.Count, Is.EqualTo(2));
            Assert.That(tracker.GetEnemyLosses().Single(loss => loss.Kind == BattleLossKind.Ship).Count, Is.EqualTo(1));
            Assert.That(tracker.GetEnemyLosses().Single(loss => loss.Kind == BattleLossKind.Squadron).Count, Is.EqualTo(2));
            Assert.That(snapshot.Single(loss => loss.Kind == BattleLossKind.Squadron).Count, Is.EqualTo(1));
            tracker.LateDispose();
        }

        [Test]
        public void Losses_RejoiningClientSortsHostLossesFromItsOwnSide_AndKeepsHigherLocalCounts()
        {
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            var hostEntities = new EntityLocator();
            var hostState = new SkirmishStateModel();
            var host = new BattleLossTracker(hostEntities, TestPlayers.CreateLocalPlayer(roster), hostState);
            host.Initialize();
            hostState.SetState(BattleState.Running);
            var venator = new UnitTypeFacade(UnitTypeId.Ship(ShipType.Venator));
            long id = 1;
            foreach (PlayerId owner in new[] { TestPlayers.Human, TestPlayers.Ally, TestPlayers.Enemy })
            {
                Entity ship = CreateEntity(hostEntities, id++, owner, venator);
                ship.Initialize();
                ((Health)ship.HealthModel).Destroy();
            }

            var clientEntities = new EntityLocator();
            var clientState = new SkirmishStateModel();
            var client = new BattleLossTracker(clientEntities,
                new LocalPlayer(roster, new OnlineSession(false, TestPlayers.SecondEnemy)), clientState);
            client.Initialize();
            clientState.SetState(BattleState.Running);
            // Two enemy-team deaths reached this client before the host's copy did.
            for (int i = 0; i < 2; i++)
            {
                Entity ship = CreateEntity(clientEntities, id++, TestPlayers.Enemy, venator);
                ship.Initialize();
                ((Health)ship.HealthModel).Destroy();
            }

            client.MergeHostLosses(host.GetLossesByOwner());

            Assert.That(client.GetPlayerLosses().Single().Count, Is.EqualTo(2), "Own side keeps the higher local count.");
            Assert.That(client.GetEnemyLosses().Single().Count, Is.EqualTo(2), "Human and ally are this client's enemies.");
            host.LateDispose();
            client.LateDispose();
        }

        [Test]
        public void Losses_RestoreStructureTypesAndStationLevel_AndAccumulateAfterLoading()
        {
            var entities = new EntityLocator();
            var state = new SkirmishStateModel();
            var tracker = new BattleLossTracker(entities, TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()), state);
            tracker.Initialize();
            var level = new SecondStationLevelModel();
            var station = CreateEntity(entities, 1, TestPlayers.Human,
                new StructureSaveFacade(1, TestPlayers.Human, StructureKind.SecondStation, (int)FactionType.Republic,
                    null, null), new StationLevelFacade(level));
            var platform = CreateEntity(entities, 2, TestPlayers.Enemy,
                new StructureSaveFacade(2, TestPlayers.Enemy, StructureKind.DefendPlatform, (int)DefendPlatformType.GolanIII,
                    null, null));
            station.Initialize();
            platform.Initialize();
            state.SetState(BattleState.Running);
            level.CurrentLevel = 3;
            ((Health)station.HealthModel).Destroy();
            ((Health)platform.HealthModel).Destroy();

            BattleSave save = BattleSave.Create(null, null, default, Array.Empty<UnitSave>(), Array.Empty<StructureSave>());
            tracker.Capture(save);
            tracker.RestoreBeforeUnits(BattleSave.FromJson(save.ToJson()));

            BattleUnitLoss stationLoss = tracker.GetPlayerLosses().Single();
            Assert.That(stationLoss.Kind, Is.EqualTo(BattleLossKind.SpaceStation));
            Assert.That(stationLoss.StationFaction, Is.EqualTo(FactionType.Republic));
            Assert.That(stationLoss.StationLevel, Is.EqualTo(3));
            Assert.That(stationLoss.Count, Is.EqualTo(1));
            var rebuiltPlatform = CreateEntity(entities, 3, TestPlayers.Enemy,
                new StructureSaveFacade(3, TestPlayers.Enemy, StructureKind.DefendPlatform, (int)DefendPlatformType.GolanIII,
                    null, null));
            rebuiltPlatform.Initialize();
            ((Health)rebuiltPlatform.HealthModel).Destroy();
            BattleUnitLoss platformLoss = tracker.GetEnemyLosses().Single();
            Assert.That(platformLoss.Kind, Is.EqualTo(BattleLossKind.DefendPlatform));
            Assert.That(platformLoss.DefendPlatformType, Is.EqualTo(DefendPlatformType.GolanIII));
            Assert.That(platformLoss.Count, Is.EqualTo(2));

            tracker.RestoreBeforeUnits(BattleSave.FromJson("{}"));
            Assert.That(tracker.GetPlayerLosses(), Is.Empty);
            Assert.That(tracker.GetEnemyLosses(), Is.Empty);
            tracker.LateDispose();
        }

        private static Entity CreateEntity(EntityLocator entities, long id, PlayerId owner, params IEntityFacade[] facades) =>
            new Entity(new Health(), entities, facades, owner, id,
                Array.Empty<IPooledEntityEvents>(), Array.Empty<IHangarLaunchEvents>());

        private static List<PlayerBattleState> Collect(BattleVictoryService service)
        {
            typeof(BattleVictoryService).GetMethod("CollectStates", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(service, null);
            return (List<PlayerBattleState>)typeof(BattleVictoryService).GetField("_states", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service);
        }

        private sealed class Unit : IEntity, IPlayerBaseFacade, IShipEntity
        {
            private readonly bool _isBase;
            private readonly UnitTypeFacade _shipType;
            public long Id { get; }
            public PlayerId Owner { get; }
            public UnityEngine.Vector3 WorldPosition => default;
            public float NavigationRadius => 1;
            public float NavigationSpeed => 1;
            public long EntityId => Id;
            public UnitOrderType CurrentOrder => default;
            public Health Health { get; } = new Health();
            public IHealthModelObserver HealthModel => Health;
            public Unit(long id, PlayerId owner, bool isBase, bool isShip = true)
            {
                Id = id;
                Owner = owner;
                _isBase = isBase;
                _shipType = !isBase && isShip ? new UnitTypeFacade(UnitTypeId.Ship(ShipType.Venator)) : null;
            }
            public T GetFacade<T>() where T : IEntityFacade => throw new NotSupportedException();
            public bool TryGetFacade<T>(out T facade) where T : IEntityFacade
            {
                if (_isBase && typeof(T) == typeof(IPlayerBaseFacade)) { facade = (T)(object)this; return true; }
                if (_shipType != null && typeof(T) == typeof(IUnitTypeFacade)) { facade = (T)(object)_shipType; return true; }
                facade = default;
                return false;
            }
        }

        private sealed class Health : IHealthModelObserver
        {
            public event Action OnDestroy;
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass => ShipClass.Capital;
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 100;
            public float HullPercentage => 1;
            public float Shields => 0;
            public float ShieldPercentage => 0;
            public bool IsDestroyed { get; set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits { get; set; } = true;
            public bool HasLiveHardPoints => false;
            public bool HasShields => false;
            public PlayerId Owner => default;
            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();

            public void Destroy()
            {
                IsDestroyed = true;
                OnDestroy?.Invoke();
            }
        }
    }
}
