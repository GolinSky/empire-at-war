using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.EngineGlow;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.BaseEntity;
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
        private ShipEngineGlowSync _sync;

        [SetUp]
        public void SetUp()
        {
            _movement = new FakeMovement();
            _modifiers = new CombatModifiers();
            _glow = new Glow();
            _sync = new ShipEngineGlowSync(_movement, _modifiers, _glow);
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
        public void LateDispose_StopsFollowingMovementAndModifiers()
        {
            _sync.Initialize();
            _sync.LateDispose();
            int writes = _glow.States.Count;

            _movement.SetMoving(true);
            _modifiers.SetIonDisabled(true);

            Assert.That(_glow.States, Has.Count.EqualTo(writes));
        }

        private sealed class Glow : IShipEngineGlowComponent
        {
            public readonly List<EngineGlowState> States = new List<EngineGlowState>();
            public EngineGlowState Last => States[States.Count - 1];
            public void SetState(EngineGlowState state) => States.Add(state);
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
