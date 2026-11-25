using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.EngineGlow;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Ship;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class ShipEngineGlowSyncTests
    {
        private FakeMovement _movement;
        private CombatModifiers _modifiers;
        private Glow _glow;
        private FakeHealth _health;
        private ShipEngineGlowSync _sync;

        [SetUp]
        public void SetUp()
        {
            _movement = new FakeMovement();
            _modifiers = new CombatModifiers();
            _glow = new Glow();
            // Hardpoint 1 is a weapon: only engines drive glows.
            _health = new FakeHealth(new FakeHardPoint(0, HardPointType.Engines), new FakeHardPoint(1, HardPointType.Weapon),
                new FakeHardPoint(2, HardPointType.Engines));
            _sync = new ShipEngineGlowSync(_movement, _modifiers, _health, _glow);
        }

        [Test]
        public void Initialize_StoppedShipIdles()
        {
            _sync.Initialize();

            Assert.That(_glow.Last, Is.EqualTo(EngineGlowState.Idle));
        }

        [Test]
        public void Movement_SwitchesBetweenThrustAndIdle()
        {
            _sync.Initialize();

            _movement.SetMoving(true);
            Assert.That(_glow.Last, Is.EqualTo(EngineGlowState.Thrust));

            _movement.SetMoving(false);
            Assert.That(_glow.Last, Is.EqualTo(EngineGlowState.Idle));
        }

        [Test]
        public void IonDisable_TurnsGlowOffUntilItEnds()
        {
            _sync.Initialize();
            _movement.SetMoving(true);

            _modifiers.SetIonDisabled(true);
            Assert.That(_glow.Last, Is.EqualTo(EngineGlowState.Off));

            _movement.SetMoving(false);
            Assert.That(_glow.Last, Is.EqualTo(EngineGlowState.Off), "Stopping while stunned keeps the engines off.");

            _modifiers.SetIonDisabled(false);
            Assert.That(_glow.Last, Is.EqualTo(EngineGlowState.Idle));
        }

        [Test]
        public void Initialize_ReportsEveryEngineHardpointOnce()
        {
            _sync.Initialize();

            Assert.That(_glow.Engines, Is.EquivalentTo(new[] { (0, false), (2, false) }));
        }

        [Test]
        public void DestroyedEngine_SwitchesOffOnlyItsOwnGlows()
        {
            _sync.Initialize();
            _glow.Engines.Clear();

            _health.HardPoints[2].Destroy();

            Assert.That(_glow.Engines, Is.EqualTo(new[] { (2, true) }));
        }

        [Test]
        public void WeaponHardpointDamage_LeavesGlowsAlone()
        {
            _sync.Initialize();
            _glow.Engines.Clear();

            _health.HardPoints[1].Destroy();

            Assert.That(_glow.Engines, Is.Empty);
        }

        [Test]
        public void LateDispose_StopsFollowingMovementAndModifiers()
        {
            _sync.Initialize();
            _sync.LateDispose();
            int writes = _glow.States.Count;

            _movement.SetMoving(true);
            _modifiers.SetIonDisabled(true);
            _health.HardPoints[0].Destroy();

            Assert.That(_glow.States, Has.Count.EqualTo(writes));
            Assert.That(_glow.Engines.Count(engine => engine.Item2), Is.Zero);
        }

        private sealed class Glow : IShipEngineGlowComponent
        {
            public readonly List<EngineGlowState> States = new List<EngineGlowState>();
            public readonly List<(int, bool)> Engines = new List<(int, bool)>();
            public EngineGlowState Last => States[States.Count - 1];
            public void SetState(EngineGlowState state) => States.Add(state);
            public void SetEngineDestroyed(int engineHardPointIndex, bool destroyed) => Engines.Add((engineHardPointIndex, destroyed));
        }

        private sealed class FakeHardPoint : IReadOnlyHardPointModel
        {
            public FakeHardPoint(int id, HardPointType type)
            {
                Id = id;
                HardPointType = type;
            }

            public event Action OnHardPointHealthChanged;
            public event Action OnDestroyedByDamage { add { } remove { } }
            public int Id { get; }
            public HardPointType HardPointType { get; }
            public bool IsInstalled => true;
            public bool IsDestroyed { get; private set; }
            public float HealthPercentage => IsDestroyed ? 0f : 1f;
            public float HullDamageMultiplier => 1f;

            public void Destroy()
            {
                IsDestroyed = true;
                OnHardPointHealthChanged?.Invoke();
            }
        }

        private sealed class FakeHealth : IHealthComponent, IHealthModelObserver
        {
            public FakeHealth(params FakeHardPoint[] hardPoints) => HardPoints = hardPoints;

            public FakeHardPoint[] HardPoints { get; }
            public string Id => "health";
            public bool Destroyed => false;
            public IHealthModelObserver HealthModelObserver => this;
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass => default;
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => HardPoints;
            public float Hull => 1f;
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => false;
            public PlayerId Owner => default;
            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) => Array.Empty<IHardPointModel>();
            public void ApplyDamage(float damage, DamageType damageType, int shipUnitId) { }
            public bool Equal(IHealthModelObserver modelObserver) => modelObserver == this;
        }

        private sealed class FakeMovement : IShipMoveComponent
        {
            public string Id => "movement";
            public Vector3 CurrentPosition => Vector3.zero;
            public bool IsMoving { get; private set; }
            public bool IsArriving => false;
            public float NavigationRadius => 1f;
            public float CurrentSpeed => 0f;
            public float PaceSpeed => 1f;
            public float UndamagedPaceSpeed => 1f;
            public float NavigationSpeed => 1f;

            public event Action<Vector3> DestinationChanged { add { } remove { } }
            public event Action<Vector3> LookingAt { add { } remove { } }
            public event Action HyperSpaceCompleted { add { } remove { } }
            public event Action<bool> MovingChanged;

            public void SetMoving(bool isMoving)
            {
                IsMoving = isMoving;
                MovingChanged?.Invoke(isMoving);
            }

            public void MoveToPosition(Vector3 targetPosition, bool preserveCourse = false) { }
            public void LookAtTarget(IEntity target) { }
            public void LookInDirection(Vector3 direction) { }
            public float GetRange(Vector3 targetPosition) => 0f;
            public void Stop() { }
            public void HoldPosition(IEntity target) { }
            public void CancelCombatHold() { }
            public void SetSpeedCap(float cap) { }
            public void ApplyMoveCoefficient(float coefficient) { }
            public void HandleSelection(bool isSelected) { }
        }
    }
}
