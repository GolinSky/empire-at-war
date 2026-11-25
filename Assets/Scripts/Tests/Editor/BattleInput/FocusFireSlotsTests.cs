using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.UnitOrders;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
    public sealed class FocusFireSlotsTests
    {
        private const int SHIP_CAP = 2;
        private const int SQUADRON_CAP = 3;

        private EntityLocator _locator;
        private FixedCaps _caps;
        private FocusFireSlots _slots;
        private long _nextId;

        [SetUp]
        public void SetUp()
        {
            _locator = new EntityLocator();
            _caps = new FixedCaps { Ships = SHIP_CAP, Squadrons = SQUADRON_CAP };
            _slots = new FocusFireSlots(_locator, TestPlayers.CreateTeamGame(), _caps);
        }

        [TearDown]
        public void TearDown() => _slots.Dispose();

        [Test]
        public void TryClaim_FillsTheCapThenRefuses()
        {
            UnitStub target = Unit(TestPlayers.Enemy);

            for (int i = 0; i < SHIP_CAP; i++)
                Assert.That(_slots.TryClaim(Unit(TestPlayers.Human), target), Is.True);
            UnitStub late = Unit(TestPlayers.Human);

            Assert.That(_slots.TryClaim(late, target), Is.False);
            Assert.That(_slots.Holds(late, target), Is.False);
            Assert.That(_slots.FreeSlots(TestPlayers.Human, target, isSquadron: false), Is.Zero);
            Assert.That(_slots.HasSlotFor(late, target), Is.False);
        }

        [Test]
        public void TryClaim_HolderKeepsItsSlotOnRepeat()
        {
            UnitStub target = Unit(TestPlayers.Enemy);
            UnitStub holder = Unit(TestPlayers.Human);
            _slots.TryClaim(holder, target);
            for (int i = 1; i < SHIP_CAP; i++) _slots.TryClaim(Unit(TestPlayers.Human), target);

            Assert.That(_slots.TryClaim(holder, target), Is.True);
            Assert.That(_slots.HasSlotFor(holder, target), Is.True, "A full target still has room for its holder.");
            Assert.That(_slots.CountHolders(TestPlayers.Human, target, isSquadron: false), Is.EqualTo(SHIP_CAP));
        }

        [Test]
        public void Allies_ShareOnePool_EnemiesOfEachOtherDoNot()
        {
            UnitStub target = Unit(TestPlayers.Enemy);
            for (int i = 0; i < SHIP_CAP; i++) _slots.TryClaim(Unit(TestPlayers.Human), target);

            Assert.That(_slots.TryClaim(Unit(TestPlayers.Ally), target), Is.False, "Allies share the human's cap.");

            UnitStub otherTeamTarget = Unit(TestPlayers.Human);
            _slots.TryClaim(Unit(TestPlayers.Enemy), otherTeamTarget);
            Assert.That(_slots.FreeSlots(TestPlayers.Human, otherTeamTarget, isSquadron: false), Is.EqualTo(SHIP_CAP),
                "Enemy holders do not use the human team's slots.");
        }

        [Test]
        public void ShipsAndSquadrons_HoldSeparatePools()
        {
            UnitStub target = Unit(TestPlayers.Enemy);
            for (int i = 0; i < SHIP_CAP; i++) _slots.TryClaim(Unit(TestPlayers.Human), target);

            for (int i = 0; i < SQUADRON_CAP; i++)
                Assert.That(_slots.TryClaim(Unit(TestPlayers.Human, isSquadron: true), target), Is.True);
            Assert.That(_slots.TryClaim(Unit(TestPlayers.Human, isSquadron: true), target), Is.False);
        }

        [Test]
        public void Release_FreesTheSlotAndRaisesChanged()
        {
            UnitStub target = Unit(TestPlayers.Enemy);
            UnitStub holder = Unit(TestPlayers.Human);
            _slots.TryClaim(holder, target);
            var changed = new List<IEntity>();
            _slots.Changed += changed.Add;

            _slots.Release(holder.Id);

            Assert.That(_slots.CountHolders(TestPlayers.Human, target, isSquadron: false), Is.Zero);
            Assert.That(changed, Is.EqualTo(new IEntity[] { target }));
        }

        [Test]
        public void ClaimOnANewTarget_ReleasesTheOldOne()
        {
            UnitStub first = Unit(TestPlayers.Enemy);
            UnitStub second = Unit(TestPlayers.Enemy);
            UnitStub holder = Unit(TestPlayers.Human);
            _slots.TryClaim(holder, first);

            _slots.TryClaim(holder, second);

            Assert.That(_slots.Holds(holder, first), Is.False);
            Assert.That(_slots.CountHolders(TestPlayers.Human, first, isSquadron: false), Is.Zero);
        }

        [Test]
        public void DestroyedHolder_StopsCounting()
        {
            UnitStub target = Unit(TestPlayers.Enemy);
            UnitStub holder = Unit(TestPlayers.Human);
            _slots.TryClaim(holder, target);

            holder.Health.IsDestroyed = true;

            Assert.That(_slots.CountHolders(TestPlayers.Human, target, isSquadron: false), Is.Zero);
        }

        [Test]
        public void RemovedTargetOrAttacker_ReleasesItsClaims()
        {
            UnitStub target = Unit(TestPlayers.Enemy);
            UnitStub holder = Unit(TestPlayers.Human);
            UnitStub leaving = Unit(TestPlayers.Human);
            UnitStub other = Unit(TestPlayers.Enemy);
            _slots.TryClaim(holder, target);
            _slots.TryClaim(leaving, other);

            _locator.RemoveEntity(target);
            _locator.RemoveEntity(leaving);

            Assert.That(_slots.Holds(holder, target), Is.False);
            Assert.That(_slots.CountHolders(TestPlayers.Human, other, isSquadron: false), Is.Zero);
        }

        [Test]
        public void UnlimitedCap_NeverRefuses()
        {
            _caps.Ships = FocusFireCaps.UNLIMITED;
            UnitStub target = Unit(TestPlayers.Enemy);

            for (int i = 0; i < 10; i++) Assert.That(_slots.TryClaim(Unit(TestPlayers.Human), target), Is.True);
            Assert.That(_slots.FreeSlots(TestPlayers.Human, target, isSquadron: false), Is.EqualTo(int.MaxValue));
        }

        private UnitStub Unit(PlayerId owner, bool isSquadron = false)
        {
            UnitStub unit = new UnitStub(++_nextId, owner, isSquadron);
            _locator.AddEntity(unit);
            return unit;
        }

        private sealed class FixedCaps : IFocusFireCaps
        {
            public int Ships { get; set; }
            public int Squadrons { get; set; }

            public int GetCap(IEntity target, bool isSquadron) => isSquadron ? Squadrons : Ships;
        }

        private sealed class UnitStub : IEntity, IUnitTypeFacade
        {
            public long Id { get; }
            public PlayerId Owner { get; }
            public HealthStub Health { get; }
            public IHealthModelObserver HealthModel => Health;
            public UnitTypeId UnitTypeId { get; }

            public UnitStub(long id, PlayerId owner, bool isSquadron)
            {
                Id = id;
                Owner = owner;
                Health = new HealthStub(owner);
                UnitTypeId = isSquadron ? UnitTypeId.Squadron(default(SquadronType)) : UnitTypeId.Ship(default);
            }

            public bool TryGetFacade<TFacade>(out TFacade entityFacade) where TFacade : IEntityFacade
            {
                if (this is TFacade facade)
                {
                    entityFacade = facade;
                    return true;
                }

                entityFacade = default;
                return false;
            }

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade
            {
                if (TryGetFacade(out TFacade facade)) return facade;
                throw new InvalidOperationException(typeof(TFacade).Name);
            }
        }

        private sealed class HealthStub : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }

            public HealthStub(PlayerId owner) => Owner = owner;

            public ShipClass ShipClass => ShipClass.Frigate;
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<IReadOnlyHardPointModel>();
            public float Hull => 1f;
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed { get; set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => false;
            public PlayerId Owner { get; }

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) => Array.Empty<IHardPointModel>();
        }
    }
}
