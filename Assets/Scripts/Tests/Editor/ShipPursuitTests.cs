using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Ship.Orders;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.Ship;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Movement
{
    public sealed class ShipPursuitTests
    {
        private GameObject _targetObject;
        private Target _target;
        private Movement _movement;
        private Weapon _weapon;
        private UnitOrderSettings _orderSettings;

        [SetUp]
        public void SetUp()
        {
            _targetObject = new GameObject("Pursuit target");
            _targetObject.transform.position = new Vector3(500f, 0f, 0f);
            _target = new Target(_targetObject.transform);
            _movement = new Movement();
            _weapon = new Weapon();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_targetObject);
            if (_orderSettings != null) Object.DestroyImmediate(_orderSettings);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Attack_CommitsLegDespiteTargetMotionAndOwnDetour(bool formation)
        {
            AttackTargetState state = new AttackTargetState(null, _weapon, _movement);
            state.SetData(_target, formation ? Vector3.forward * 20f : Vector3.zero,
                EmpireAtWar.Entities.BaseEntity.Orders.UnitOrderModel.NO_HARD_POINT);
            state.Tick(0.1f);
            Vector3 committed = _movement.Destination;

            _movement.CurrentPosition = new Vector3(100f, 0f, 200f);
            state.Tick(0.1f);
            _target.Transform.position = new Vector3(900f, 0f, 0f);
            for (int i = 0; i < 20; i++) state.Tick(0.1f);

            Assert.That(_movement.MoveCount, Is.EqualTo(1));
            Assert.That(_movement.Destination, Is.EqualTo(committed));
            _movement.FinishLeg();
            state.Tick(0.1f);
            Assert.That(_movement.MoveCount, Is.EqualTo(2));
            Assert.That(Vector3.Distance(_movement.Destination, _target.Transform.position),
                Is.EqualTo(65f).Within(0.001f));
        }

        [Test]
        public void Attack_TargetClosesRange_StopsWithoutAnotherPlan()
        {
            AttackTargetState state = new AttackTargetState(null, _weapon, _movement);
            state.SetData(_target);
            state.Tick(0.1f);
            _target.Transform.position = new Vector3(40f, 0f, 0f);
            state.Tick(0.1f);

            Assert.That(_movement.IsMoving, Is.False);
            Assert.That(_movement.MoveCount, Is.EqualTo(1));
        }

        [Test]
        public void SharedPursuit_NewTargetReplacesCourse_ThenWaitsForArrival()
        {
            _movement.MoveToPosition(Vector3.back * 100f);
            ShipEngagement.Pursue(_movement, _weapon, _target, newTarget: true);
            Assert.That(_movement.Destination.x, Is.EqualTo(435f));
            _target.Transform.position = new Vector3(900f, 0f, 0f);
            for (int i = 0; i < 20; i++) ShipEngagement.Pursue(_movement, _weapon, _target);
            Assert.That(_movement.MoveCount, Is.EqualTo(2));

            _movement.FinishLeg();
            ShipEngagement.Pursue(_movement, _weapon, _target);
            Assert.That(_movement.MoveCount, Is.EqualTo(3));
            Assert.That(_movement.Destination.x, Is.EqualTo(835f));
        }

        [Test]
        public void SharedPursuit_StandoffRespectsTargetHull()
        {
            _target.NavigationRadius = 100f;
            ShipEngagement.Pursue(_movement, _weapon, _target);
            Assert.That(Vector3.Distance(_movement.Destination, _target.Transform.position),
                Is.EqualTo(105f));
        }

        [Test]
        public void Guard_MovingFriendlyDoesNotReplaceFollowLeg()
        {
            UnitOrderSettings settings = ScriptableObject.CreateInstance<UnitOrderSettings>();
            try
            {
                GuardState state = new GuardState(_movement, _weapon, new Radar(), null, settings);
                state.SetData(_target, Vector3.zero);
                state.Enter();
                _target.Transform.position = new Vector3(900f, 0f, 0f);
                for (int i = 0; i < 20; i++) state.Tick(0.1f);
                Assert.That(_movement.MoveCount, Is.EqualTo(1));

                _movement.FinishLeg();
                state.Tick(0.1f);
                Assert.That(_movement.MoveCount, Is.EqualTo(2));
                Assert.That(_movement.Destination, Is.EqualTo(_target.Transform.position));
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void AbilityApproach_MovingTargetDoesNotReplaceLeg()
        {
            AbilityApproachState state = new AbilityApproachState(_movement);
            state.SetData(_target, 100f);
            state.Enter();
            _target.Transform.position = new Vector3(900f, 0f, 0f);
            for (int i = 0; i < 20; i++) state.Tick(0.1f);
            Assert.That(_movement.MoveCount, Is.EqualTo(1));

            _movement.FinishLeg();
            state.Tick(0.1f);
            Assert.That(_movement.MoveCount, Is.EqualTo(2));
        }

        [Test]
        public void AbilityApproach_UsesRangeStandoff()
        {
            AbilityApproachState state = new AbilityApproachState(_movement);
            state.SetData(_target, 100f);
            state.Enter();
            Assert.That(_movement.Destination.x, Is.EqualTo(410f));
            _movement.FinishLeg();
            Assert.That(state.IsInRange, Is.True);
            Assert.That(state.IsComplete, Is.True);
        }

        [Test]
        public void AbilityApproach_FailedPathRetriesThenCompletesWithoutCasting()
        {
            AbilityApproachState state = new AbilityApproachState(_movement);
            state.SetData(_target, 100f);
            state.Enter();
            for (int i = 0; i < 3; i++)
            {
                _movement.Stop();
                state.Tick(0.5f);
            }
            Assert.That(_movement.MoveCount, Is.EqualTo(3));
            Assert.That(state.IsComplete, Is.True);
            Assert.That(state.IsInRange, Is.False);
        }

        [Test]
        public void AbilityApproach_HullOutsideCastRangeCompletesAfterContact()
        {
            _target.NavigationRadius = 120f;
            AbilityApproachState state = new AbilityApproachState(_movement);
            state.SetData(_target, 100f);
            state.Enter();
            for (int i = 0; i < 4; i++)
            {
                _movement.FinishLeg();
                state.Tick(0.5f);
            }
            Assert.That(state.IsComplete, Is.True);
            Assert.That(state.IsInRange, Is.False);
            Assert.That(Vector3.Distance(_movement.Destination, _target.Transform.position), Is.EqualTo(125f));
        }

        [Test]
        public void AbilityApproach_FailureResumesRemainingWaypoints()
        {
            UnitOrderModel orders = new UnitOrderModel();
            var first = new EmpireAtWar.Components.Movement.Formation.FormationPoint(10f, 20f);
            var second = new EmpireAtWar.Components.Movement.Formation.FormationPoint(30f, 40f);
            orders.Replace(UnitOrderType.WaypointMove, first, waypoints: new[] { first, second });
            orders.AdvanceWaypoint(out _);
            ShipStateMachine machine = new ShipStateMachine();
            IdleState idle = new IdleState(_movement, _weapon, new Radar());
            machine.SetState(idle);
            NavigateState navigate = new NavigateState(_movement);
            ShipOrderRunner runner = new ShipOrderRunner(_movement, _weapon, null, orders, machine,
                idle, navigate, null, null, null, null, new AbilityApproachState(_movement), null, null);

            runner.CastAbility(ShipAbilityId.TractorBeam, _target, 100f);
            Assert.That(runner.CurrentOrder, Is.EqualTo(UnitOrderType.UseAbility));
            for (int i = 0; i < 3; i++)
            {
                _movement.Stop();
                runner.Tick(0.5f);
            }

            Assert.That(runner.CurrentOrder, Is.EqualTo(UnitOrderType.WaypointMove));
            Assert.That(orders.WaypointIndex, Is.EqualTo(1));
            Assert.That(machine.CurrentState, Is.SameAs(navigate));
            Assert.That(_movement.Destination, Is.EqualTo(new Vector3(30f, 0f, 40f)));
        }

        [Test]
        public void AttackMove_ArrivedShip_DoesNotFinishBeforeItsFirstTargetSearch()
        {
            // Seeded so the random first delay is above zero; edit-mode time does not advance between ticks.
            UnityEngine.Random.InitState(7);
            AttackMoveState state = new AttackMoveState(_movement, _weapon, new Radar(), null);
            state.SetData(Vector3.zero, new EmpireAtWar.Entities.BaseEntity.Orders.AttackMoveEngagement());
            state.Enter();
            _movement.FinishLeg();

            state.Tick(0.1f);

            Assert.That(state.IsComplete, Is.False);
            state.Exit();
        }

        [Test]
        public void Hunt_NoVisibleEnemiesRemainsActiveAndStopsPreviousMovement()
        {
            UnitOrderSettings settings = ScriptableObject.CreateInstance<UnitOrderSettings>();
            try
            {
                var players = EmpireAtWar.Tests.Editor.TestPlayers.CreateDuel();
                HuntState state = new HuntState(_movement, _weapon, null, new EntityLocator(), null,
                    EmpireAtWar.Tests.Editor.TestPlayers.CreateLocalPlayer(players), settings,
                    EmpireAtWar.Tests.Editor.TestPlayers.Human);
                _movement.MoveToPosition(Vector3.forward * 100f);
                state.Enter();
                state.Tick(0.1f);
                Assert.That(state.IsComplete, Is.False);
                Assert.That(_movement.IsMoving, Is.False);
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void AbilityApproach_SuccessfulCastResumesGuard()
        {
            AbilityCaster caster = new AbilityCaster(_target, _movement, ShipAbilityId.BoostEnginePower);
            RecordingAbility ability = new RecordingAbility();
            ShipAbilityService abilities = new ShipAbilityService(new AbilityFactory(ability),
                EmpireAtWar.Tests.Editor.TestPlayers.CreateDuel());
            UnitOrderModel orders = new UnitOrderModel();
            ShipStateMachine machine = new ShipStateMachine();
            _orderSettings = ScriptableObject.CreateInstance<UnitOrderSettings>();
            GuardState guard = new GuardState(_movement, _weapon, new Radar(), null, _orderSettings);
            IdleState idle = new IdleState(_movement, _weapon, new Radar());
            ShipOrderRunner runner = new ShipOrderRunner(_movement, _weapon, null, orders, machine,
                idle, null, null, null, guard, null, new AbilityApproachState(_movement), abilities, caster);
            runner.Start();
            runner.Guard(_target, Vector3.forward * 20f);
            runner.CastAbility(ShipAbilityId.BoostEnginePower, _target, 100f);
            _movement.FinishLeg();

            runner.Tick(0.1f);

            Assert.That(ability.StartCount, Is.EqualTo(1));
            Assert.That(runner.CurrentOrder, Is.EqualTo(UnitOrderType.Guard));
            Assert.That(machine.CurrentState, Is.SameAs(guard));
            Assert.That(_movement.Destination, Is.EqualTo(_target.Transform.position + Vector3.forward * 20f));
            abilities.Clear();
        }

        [Test]
        public void Guard_RepeatedExplicitOrderCancelsCloak()
        {
            AbilityCaster caster = new AbilityCaster(_target, _movement, ShipAbilityId.Cloak);
            ShipAbilityService abilities = new ShipAbilityService(new AbilityFactory(new CloakAbility()),
                EmpireAtWar.Tests.Editor.TestPlayers.CreateDuel());
            UnitOrderModel orders = new UnitOrderModel();
            ShipStateMachine machine = new ShipStateMachine();
            _orderSettings = ScriptableObject.CreateInstance<UnitOrderSettings>();
            GuardState guard = new GuardState(_movement, _weapon, new Radar(), null, _orderSettings);
            ShipOrderRunner runner = new ShipOrderRunner(_movement, _weapon, null, orders, machine,
                new IdleState(_movement, _weapon, new Radar()), null, null, null, guard, null, null, abilities, caster);
            runner.Start();
            runner.Guard(_target, Vector3.forward * 20f);
            Assert.That(abilities.TryActivate(caster, ShipAbilityId.Cloak, null), Is.True);
            Assert.That(caster.Modifiers.IsCloaked, Is.True);

            runner.Guard(_target, Vector3.forward * 20f);

            Assert.That(caster.Modifiers.IsCloaked, Is.False);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
            Assert.That(_movement.MoveCount, Is.EqualTo(1));
            abilities.Clear();
        }

        [Test]
        public void AttackMove_SameDestinationNewEngagementReplacesMembership()
        {
            AbilityCaster caster = new AbilityCaster(_target, _movement, ShipAbilityId.BoostEnginePower);
            ShipAbilityService abilities = new ShipAbilityService(new AbilityFactory(new RecordingAbility()),
                EmpireAtWar.Tests.Editor.TestPlayers.CreateDuel());
            Radar radar = new Radar();
            AttackMoveState attackMove = new AttackMoveState(_movement, _weapon, radar, null);
            ShipOrderRunner runner = new ShipOrderRunner(_movement, _weapon, null, new UnitOrderModel(),
                new ShipStateMachine(), new IdleState(_movement, _weapon, radar), null, null, attackMove,
                null, null, null, abilities, caster);
            runner.Start();
            AttackMoveEngagement first = new AttackMoveEngagement();
            AttackMoveEngagement second = new AttackMoveEngagement();
            runner.AttackMoveTo(Vector3.forward * 100f, first);
            first.Report(0, new[] { _target });

            runner.AttackMoveTo(Vector3.forward * 100f, second);

            Assert.That(first.Contains(_target), Is.False);
            Assert.DoesNotThrow(() => second.Report(0, new[] { _target }));
            Assert.That(second.Contains(_target), Is.True);
            Assert.That(_movement.MoveCount, Is.EqualTo(2));
            runner.Release();
        }

        private sealed class AbilityCaster : IShipAbilityFacade
        {
            private readonly Movement _movement;
            public IReadOnlyList<ShipAbilitySlot> Slots { get; }
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public Vector3 WorldPosition => _movement.CurrentPosition;
            public IEntity Entity { get; }
            public IHealthModelObserver Health => Entity.HealthModel;
            public float RadarRange => 100f;
            public AbilityCaster(IEntity entity, Movement movement, ShipAbilityId id)
            {
                Entity = entity;
                _movement = movement;
                ShipAbilityDefinition definition = new ShipAbilityDefinition();
                typeof(ShipAbilityDefinition).GetField("canCancel", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(definition, true);
                Slots = new[] { new ShipAbilitySlot(this, definition, id) };
            }
        }

        private sealed class AbilityFactory : IShipAbilityFactory
        {
            private readonly IShipAbility _ability;
            public AbilityFactory(IShipAbility ability) => _ability = ability;
            public IShipAbility Create(ShipAbilityDefinition definition) => _ability;
        }

        private sealed class RecordingAbility : IShipAbility
        {
            public int StartCount { get; private set; }
            public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target) => StartCount++;
            public void Stop() { }
        }

        private sealed class Movement : IShipMovement
        {
            public Vector3 CurrentPosition { get; set; }
            public Vector3 Destination { get; private set; }
            public bool IsMoving { get; private set; }
            public float NavigationRadius => 5f;
            public int MoveCount { get; private set; }
            public void MoveToPosition(Vector3 position, bool preserveCourse = false)
            { Destination = position; IsMoving = true; MoveCount++; }
            public void FinishLeg() { CurrentPosition = Destination; IsMoving = false; }
            public void LookAtTarget(Vector3 position) { }
            public void LookInDirection(Vector3 direction) { }
            public float GetRange(Vector3 position) => Vector3.Distance(CurrentPosition, position);
            public void Stop() => IsMoving = false;
        }

        private sealed class Weapon : IWeaponComponent
        {
            public string Id => nameof(Weapon);
            public float AttackDistance => 100f;
            public TargetPreference PreferredTarget => TargetPreference.Ships;
            public float GetEngagementRange(ShipClass targetClass) => 100f;
            public bool HasEnoughRange(float distance, float contactDistance, ShipClass targetClass) =>
                distance <= Mathf.Max(80f, contactDistance * 1.15f);
            public void AddTarget(AttackData data, AttackType type) { }
            public bool HasTarget(IHealthFacade healthFacade) => false;
            public void ResetTarget() { }
            public void Release() { }
        }

        private sealed class Radar : IRadarComponent
        {
            public string Id => nameof(Radar);
            public ObservableList<IEntity> Enemies { get; } = new ObservableList<IEntity>();
        }

        private sealed class Target : IEntity, IEntityTransformFacade, INavigationFootprintFacade
        {
            public long Id => 1;
            public IHealthModelObserver HealthModel { get; } = new Health();
            public PlayerId Owner => EmpireAtWar.Tests.Editor.TestPlayers.Enemy;
            public Transform Transform { get; }
            public float NavigationRadius { get; set; } = 5f;
            public Target(Transform transform) => Transform = transform;
            public T GetFacade<T>() where T : IEntityFacade =>
                this is T facade ? facade : throw new InvalidOperationException(typeof(T).Name);
            public bool TryGetFacade<T>(out T facade) where T : IEntityFacade
            {
                if (this is T found) { facade = found; return true; }
                facade = default;
                return false;
            }
        }

        private sealed class Health : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public ShipClass ShipClass => ShipClass.Capital;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerId Owner => EmpireAtWar.Tests.Editor.TestPlayers.Enemy;
            public bool HasShields => true;
            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
        }
    }
}
