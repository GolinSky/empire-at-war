using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.Repair;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Health")]
    public sealed class FieldRepairServiceTests
    {
        private const float TICK = 1f;

        private EntityLocator _entities;
        private PlayerRegistry _players;
        private ResearchStub _research;
        private FieldRepairService _service;
        private long _nextId;

        [SetUp]
        public void SetUp()
        {
            _entities = new EntityLocator();
            _players = new PlayerRegistry();
            _research = new ResearchStub();
            _players.RegisterResearch(TestPlayers.Human, _research);
            _nextId = 1;
        }

        [TearDown]
        public void TearDown()
        {
            _service?.LateDispose();
            _service = null;
        }

        [Test]
        public void DoesNothingBeforeResearchIsComplete()
        {
            CreateService(new OfflineSession(TestPlayers.Human));
            FakeShip ship = AddShip(TestPlayers.Human, 0.5f);

            RunSeconds(FieldRepairService.COMBAT_COOLDOWN_SECONDS * 2f);

            Assert.That(ship.Repairable.RepairCount, Is.Zero);
        }

        [Test]
        public void RepairsOnePercentPerSecondOnlyAfterTheCombatCooldown()
        {
            _research.CompletedTiers = 1;
            CreateService(new OfflineSession(TestPlayers.Human));
            FakeShip ship = AddShip(TestPlayers.Human, 0.5f);

            RunSeconds(FieldRepairService.COMBAT_COOLDOWN_SECONDS - TICK);
            Assert.That(ship.Repairable.RepairCount, Is.Zero);

            RunSeconds(TICK);
            Assert.That(ship.Repairable.RepairCount, Is.EqualTo(1));
            Assert.That(ship.Repairable.HullPercentage,
                Is.EqualTo(0.5f + FieldRepairService.PERCENT_PER_SECOND / 100f).Within(0.0001f));
        }

        [Test]
        public void TakingDamageRestartsTheCooldown()
        {
            _research.CompletedTiers = 1;
            CreateService(new OfflineSession(TestPlayers.Human));
            FakeShip ship = AddShip(TestPlayers.Human, 0.5f);

            RunSeconds(FieldRepairService.COMBAT_COOLDOWN_SECONDS - TICK);
            ship.Health.Damage(10f);
            RunSeconds(FieldRepairService.COMBAT_COOLDOWN_SECONDS - TICK);

            Assert.That(ship.Repairable.RepairCount, Is.Zero);
            RunSeconds(TICK);
            Assert.That(ship.Repairable.RepairCount, Is.EqualTo(1));
        }

        [Test]
        public void HavingATargetBlocksRepairUntilTheCooldownPassesAfterIt()
        {
            _research.CompletedTiers = 1;
            CreateService(new OfflineSession(TestPlayers.Human));
            FakeShip ship = AddShip(TestPlayers.Human, 0.5f);
            ship.Combat.Target = AddShip(TestPlayers.Enemy, 1f);

            RunSeconds(FieldRepairService.COMBAT_COOLDOWN_SECONDS * 2f);
            Assert.That(ship.Repairable.RepairCount, Is.Zero);

            ship.Combat.Target = null;
            RunSeconds(FieldRepairService.COMBAT_COOLDOWN_SECONDS - TICK);
            Assert.That(ship.Repairable.RepairCount, Is.Zero);
            RunSeconds(TICK);
            Assert.That(ship.Repairable.RepairCount, Is.EqualTo(1));
        }

        [Test]
        public void SkipsOtherFactionsNeutralsUnitsWithoutCombatObserverAndClients()
        {
            _research.CompletedTiers = 1;
            _players.RegisterResearch(TestPlayers.Enemy, new ResearchStub());
            CreateService(new OfflineSession(TestPlayers.Human));
            FakeShip enemy = AddShip(TestPlayers.Enemy, 0.5f);
            FakeShip neutral = AddShip(PlayerId.None, 0.5f);
            FakeShip structure = AddShip(TestPlayers.Human, 0.5f, hasCombatObserver: false);

            RunSeconds(FieldRepairService.COMBAT_COOLDOWN_SECONDS * 2f);

            foreach (FakeShip untouched in new[] { enemy, neutral, structure })
                Assert.That(untouched.Repairable.RepairCount, Is.Zero);

            _service.LateDispose();
            CreateService(new OnlineSession(false, TestPlayers.Human));
            FakeShip clientShip = AddShip(TestPlayers.Human, 0.5f);
            RunSeconds(FieldRepairService.COMBAT_COOLDOWN_SECONDS * 2f);
            Assert.That(clientShip.Repairable.RepairCount, Is.Zero);
        }

        private void CreateService(IMatchSession session)
        {
            _service = new FieldRepairService(_entities, _players, session);
            _service.Initialize();
        }

        private void RunSeconds(float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds - 0.001f; elapsed += TICK) _service.Advance(TICK);
        }

        private FakeShip AddShip(PlayerId owner, float hullPercentage, bool hasCombatObserver = true)
        {
            FakeShip ship = new FakeShip(_nextId++, owner, hullPercentage, hasCombatObserver);
            _entities.AddEntity(ship);
            return ship;
        }

        private sealed class FakeShip : IEntity
        {
            private readonly IEntityFacade[] _facades;

            public FakeShip(long id, PlayerId owner, float hullPercentage, bool hasCombatObserver)
            {
                Id = id;
                Owner = owner;
                Repairable = new FakeRepairable(hullPercentage);
                Combat = new FakeCombat();
                _facades = hasCombatObserver
                    ? new IEntityFacade[] { Repairable, Combat }
                    : new IEntityFacade[] { Repairable };
            }

            public long Id { get; }
            public PlayerId Owner { get; }
            public FakeHealth Health { get; } = new FakeHealth();
            public IHealthModelObserver HealthModel => Health;
            public FakeRepairable Repairable { get; }
            public FakeCombat Combat { get; }

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade
            {
                if (!TryGetFacade(out TFacade facade)) throw new InvalidOperationException(typeof(TFacade).Name);
                return facade;
            }

            public bool TryGetFacade<TFacade>(out TFacade facade) where TFacade : IEntityFacade
            {
                foreach (IEntityFacade candidate in _facades)
                {
                    if (candidate is not TFacade match) continue;
                    facade = match;
                    return true;
                }
                facade = default;
                return false;
            }
        }

        private sealed class FakeRepairable : IRepairableFacade
        {
            public FakeRepairable(float hullPercentage) => HullPercentage = hullPercentage;

            public int RepairCount { get; private set; }
            public float HullPercentage { get; private set; }
            public bool NeedsRepair => HullPercentage < 1f;

            public void Repair(float fraction)
            {
                RepairCount++;
                HullPercentage = Math.Min(1f, HullPercentage + fraction);
            }
        }

        private sealed class FakeCombat : IUnitCombatObserverFacade
        {
            public float WeaponRange => 100f;
            public float RadarRange => 200f;
            public IEntity Target { get; set; }

            public bool IsAttacking(IEntity target) => ReferenceEquals(Target, target);
        }

        private sealed class FakeHealth : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged;

            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull { get; private set; } = 100f;
            public ShipClass ShipClass => ShipClass.Capital;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerId Owner => TestPlayers.Human;
            public bool HasShields => false;

            public void Damage(float amount)
            {
                Hull -= amount;
                OnValueChanged?.Invoke();
            }

            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
        }

        private sealed class ResearchStub : IFactionResearchModelObserver
        {
            public event Action<ResearchType> OnResearchCompleted { add { } remove { } }

            public int CompletedTiers { get; set; }
            public IReadOnlyCollection<ResearchType> ResearchTypes => Array.Empty<ResearchType>();
            public float IncomeMultiplier => 1f;
            public float StationIncomeMultiplier => 1f;

            public bool TryGetNextTier(ResearchType type, out ResearchTierData tier)
            {
                tier = null;
                return false;
            }

            public int GetCompletedTiers(ResearchType type) => type == ResearchType.FieldRepairs ? CompletedTiers : 0;
            public float GetMultiplier(ResearchStat stat, ShipClass shipClass) => 1f;
        }
    }
}
