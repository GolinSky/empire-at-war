using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.Services.Vision;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>
    /// Malevolence Ion Pulse auto target: a live attack-order target only (wait when out of range); without one,
    /// station, then Dreadnought > BattleCruiser > Capital, in range only.
    /// </summary>
    [Category("Abilities")]
    public sealed class IonPulseTargetPriorityTests
    {
        private const float RANGE = 100f;
        private readonly List<GameObject> _views = new List<GameObject>();
        private ShipAbilityCatalog _catalog;
        private EntityLocator _entities;
        private FakeCaster _caster;
        private ShipAbilityDefinition _definition;
        private long _nextId = 2;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<ShipAbilityCatalog>();
            _entities = new EntityLocator();
            _definition = new ShipAbilityDefinition();
            Type type = typeof(ShipAbilityDefinition);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("settings", flags).SetValue(_definition, new IonPulseSettings());
            type.GetField("requiresEnemyTarget", flags).SetValue(_definition, true);
            type.GetField("range", flags).SetValue(_definition, RANGE);
            _caster = new FakeCaster(_definition, CreateView(0f));
            _entities.AddEntity(_caster.Entity);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject view in _views) UnityEngine.Object.DestroyImmediate(view);
            _views.Clear();
            UnityEngine.Object.DestroyImmediate(_catalog);
        }

        [Test]
        public void AttackTargetInRange_BeatsStationAndDreadnought()
        {
            AddStation(RANGE * 0.9f);
            AddShip(ShipClass.Dreadnought, RANGE * 0.2f);
            IEntity ordered = AddShip(ShipClass.Capital, RANGE * 0.1f);
            _caster.AttackTarget = ordered;

            Assert.That(FindTarget(), Is.SameAs(ordered));
        }

        [Test]
        public void WithoutOrder_StationInRange_BeatsDreadnought()
        {
            IEntity station = AddStation(RANGE * 0.9f);
            AddShip(ShipClass.Dreadnought, RANGE * 0.2f);
            AddShip(ShipClass.Capital, RANGE * 0.1f);

            Assert.That(FindTarget(), Is.SameAs(station));
        }

        [Test]
        public void WithoutOrderOrStation_BiggestClassWins()
        {
            IEntity dreadnought = AddShip(ShipClass.Dreadnought, RANGE * 0.5f);
            AddShip(ShipClass.BattleCruiser, RANGE * 0.2f);
            AddShip(ShipClass.Capital, RANGE * 0.1f);
            AddShip(ShipClass.Frigate, RANGE * 0.05f);

            Assert.That(FindTarget(), Is.SameAs(dreadnought));
        }

        [Test]
        public void AttackTargetOutOfRange_WaitsDespiteAHigherPriorityTargetInRange()
        {
            AddStation(RANGE * 0.5f);
            AddShip(ShipClass.Dreadnought, RANGE * 0.2f);
            _caster.AttackTarget = AddShip(ShipClass.Capital, RANGE * 1.5f);

            Assert.That(FindTarget(), Is.Null);
        }

        [Test]
        public void AttackTargetTheAbilityCannotHit_SkipsDespiteAValidTargetInRange()
        {
            AddShip(ShipClass.Dreadnought, RANGE * 0.2f);
            _caster.AttackTarget = AddShip(ShipClass.Fighter, RANGE * 0.1f);

            Assert.That(FindTarget(), Is.Null);
        }

        [Test]
        public void AbilityWithoutRanking_AttackTargetOutOfRange_WaitsDespiteANearerTarget()
        {
            var unranked = new ShipAbilityDefinition();
            Type type = typeof(ShipAbilityDefinition);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("requiresEnemyTarget", flags).SetValue(unranked, true);
            type.GetField("range", flags).SetValue(unranked, RANGE);
            AddShip(ShipClass.Frigate, RANGE * 0.2f);
            IEntity ordered = AddShip(ShipClass.Frigate, RANGE * 1.5f);
            _caster.AttackTarget = ordered;

            Assert.That(FindTarget(unranked), Is.Null);

            ordered.GetFacade<IEntityTransformFacade>().Transform.position = Vector3.right * (RANGE * 0.8f);
            Assert.That(FindTarget(unranked), Is.SameAs(ordered));
        }

        [Test]
        public void OutOfRangeTargets_AreSkipped()
        {
            AddStation(RANGE * 1.5f);
            AddShip(ShipClass.Dreadnought, RANGE * 1.5f);
            IEntity battleCruiser = AddShip(ShipClass.BattleCruiser, RANGE * 0.6f);
            AddShip(ShipClass.Capital, RANGE * 0.1f);

            Assert.That(FindTarget(), Is.SameAs(battleCruiser));
        }

        [Test]
        public void SameClass_AttackTargetBeatsNearer()
        {
            AddShip(ShipClass.Frigate, RANGE * 0.1f);
            IEntity ordered = AddShip(ShipClass.Frigate, RANGE * 0.8f);
            _caster.AttackTarget = ordered;

            Assert.That(FindTarget(), Is.SameAs(ordered));
        }

        [Test]
        public void Superlaser_AttackOrderOnACorvette_StaysSilentDespiteACapitalInRange()
        {
            var superlaser = new ShipAbilityDefinition();
            Type type = typeof(ShipAbilityDefinition);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("settings", flags).SetValue(superlaser, new SuperlaserSettings());
            type.GetField("requiresEnemyTarget", flags).SetValue(superlaser, true);
            type.GetField("range", flags).SetValue(superlaser, RANGE);
            AddShip(ShipClass.Capital, RANGE * 0.5f);
            _caster.AttackTarget = AddShip(ShipClass.Corvette, RANGE * 0.2f);

            Assert.That(FindTarget(superlaser), Is.Null);
        }

        private IEntity FindTarget(ShipAbilityDefinition definition = null)
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            var service = new ShipAbilityService(new NoAbilityFactory(), roster, new VisionService(roster, fogEnabled: false));
            var combat = new AutoPowerCombatSnapshot(_entities, roster, service, null, null, _catalog);
            combat.Capture();
            return combat.FindTarget(_caster, definition ?? _definition);
        }

        private IEntity AddShip(ShipClass shipClass, float distance)
        {
            var entity = new FakeEntity(_nextId++, TestPlayers.Human, new FakeHealth(CreateView(distance), shipClass),
                new UnitTypeFacade(UnitTypeId.Ship(ShipType.CorellianCorvette)));
            _entities.AddEntity(entity);
            return entity;
        }

        private IEntity AddStation(float distance)
        {
            var entity = new FakeEntity(_nextId++, TestPlayers.Human,
                new FakeHealth(CreateView(distance), ShipClass.Structure), new PlayerBaseFacade());
            _entities.AddEntity(entity);
            return entity;
        }

        private Transform CreateView(float distance)
        {
            var view = new GameObject("Ion pulse target");
            view.transform.position = Vector3.right * distance;
            _views.Add(view);
            return view.transform;
        }

        private sealed class NoAbilityFactory : IShipAbilityFactory
        {
            public IShipAbility Create(ShipAbilityDefinition definition) => throw new InvalidOperationException();
        }

        private sealed class FakeCaster : IShipAbilityFacade, IUnitOrderObserverFacade
        {
            private IEntity _attackTarget;

            public IReadOnlyList<ShipAbilitySlot> Slots { get; }
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public Vector3 WorldPosition => Vector3.zero;
            public IEntity Entity { get; }
            public IHealthModelObserver Health { get; }
            public float RadarRange => RANGE * 2f;
            public UnitOrderType CurrentOrder { get; private set; }

            public IEntity AttackTarget
            {
                get => _attackTarget;
                set
                {
                    _attackTarget = value;
                    CurrentOrder = UnitOrderType.Attack;
                }
            }

            public FakeCaster(ShipAbilityDefinition definition, Transform transform)
            {
                Health = new FakeHealth(transform, ShipClass.Dreadnought);
                Entity = new FakeEntity(1, TestPlayers.Enemy, (FakeHealth)Health, this);
                Slots = new[] { new ShipAbilitySlot(id: ShipAbilityId.IonPulse, definition: definition, owner: this) };
            }
        }

        private sealed class FakeEntity : IEntity
        {
            private readonly IEntityFacade[] _facades;

            public long Id { get; }
            public IHealthModelObserver HealthModel { get; }
            public PlayerId Owner { get; }

            public FakeEntity(long id, PlayerId owner, FakeHealth health, params IEntityFacade[] facades)
            {
                Id = id;
                Owner = owner;
                HealthModel = health;
                _facades = new IEntityFacade[facades.Length + 1];
                _facades[0] = health;
                facades.CopyTo(_facades, 1);
            }

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade
            {
                TryGetFacade(out TFacade facade);
                return facade;
            }

            public bool TryGetFacade<TFacade>(out TFacade facade) where TFacade : IEntityFacade
            {
                foreach (IEntityFacade candidate in _facades)
                    if (candidate is TFacade matching)
                    {
                        facade = matching;
                        return true;
                    }
                facade = default;
                return false;
            }
        }

        private sealed class FakeHealth : IHealthModelObserver, IEntityTransformFacade, INavigationFootprintFacade
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }

            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<IReadOnlyHardPointModel>();
            public float Hull => 1f;
            public ShipClass ShipClass { get; }
            public float NavigationRadius => 0f;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerId Owner => TestPlayers.Human;
            public Transform Transform { get; }
            public bool HasShields => true;

            public FakeHealth(Transform transform, ShipClass shipClass)
            {
                Transform = transform;
                ShipClass = shipClass;
            }

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) => Array.Empty<IHardPointModel>();
        }
    }
}
