using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.Ships;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BattleVictoryServiceTests
    {
        [Test]
        public void Collection_SeparatesBasesAndUnits_ExcludesDestroyedEntities_AndRefreshes()
        {
            var roster = TestPlayers.CreateDuel();
            var entities = new EntityLocator();
            var registry = new PlayerRegistry();
            var service = new BattleVictoryService(null, new ShipService(), entities, roster,
                TestPlayers.CreateLocalPlayer(roster), registry, null, new BattleVictoryModel(), new CaptureSitesSystem());
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
        public void Collection_TracksPendingReserveUntilConsumed()
        {
            var roster = TestPlayers.CreateDuel();
            var registry = new PlayerRegistry();
            var reserve = new ReinforcementModel(null);
            var request = new ShipUnitRequest(new FactionData(), ShipType.Venator);
            registry.RegisterReinforcementReserve(TestPlayers.Enemy, reserve);
            reserve.AddReinforcement(request);
            var service = new BattleVictoryService(null, new ShipService(), new EntityLocator(), roster,
                TestPlayers.CreateLocalPlayer(roster), registry, null, new BattleVictoryModel(), new CaptureSitesSystem());
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
                new BattleVictoryModel(), new CaptureSitesSystem());
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

        private static List<PlayerBattleState> Collect(BattleVictoryService service)
        {
            typeof(BattleVictoryService).GetMethod("CollectStates", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(service, null);
            return (List<PlayerBattleState>)typeof(BattleVictoryService).GetField("_states", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service);
        }

        private sealed class Unit : IEntity, IPlayerBaseFacade, EmpireAtWar.Ship.IShipEntity
        {
            private readonly bool _isBase;
            public long Id { get; }
            public PlayerId Owner { get; }
            public UnityEngine.Vector3 WorldPosition => default;
            public float NavigationRadius => 1;
            public float NavigationSpeed => 1;
            public long EntityId => Id;
            public EmpireAtWar.Entities.BaseEntity.Orders.UnitOrderType CurrentOrder => default;
            public Health Health { get; } = new Health();
            public IHealthModelObserver HealthModel => Health;
            public Unit(long id, PlayerId owner, bool isBase) { Id = id; Owner = owner; _isBase = isBase; }
            public T GetFacade<T>() where T : IEntityFacade => throw new NotSupportedException();
            public bool TryGetFacade<T>(out T facade) where T : IEntityFacade
            {
                if (_isBase && typeof(T) == typeof(IPlayerBaseFacade)) { facade = (T)(object)this; return true; }
                facade = default;
                return false;
            }
        }

        private sealed class Health : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass => ShipClass.Capital;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
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
        }
    }
}
