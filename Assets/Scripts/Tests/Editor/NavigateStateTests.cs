using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.Ship.StateMachine;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class NavigateStateTests
    {
        [Test]
        public void Reenter_AfterDestinationWasQueuedBeforeExit_UsesNewDestination()
        {
            FakeShipMoveComponent moveComponent = new FakeShipMoveComponent();
            NavigateState state = new NavigateState(moveComponent);
            StateMachine1 stateMachine = new StateMachine1();
            Vector3 firstDestination = new Vector3(10f, 0f, 20f);
            Vector3 secondDestination = new Vector3(30f, 0f, 40f);

            state.SetWorldDestination(firstDestination);
            stateMachine.SetState(state);
            state.SetWorldDestination(secondDestination);
            stateMachine.SetState(state);

            Assert.That(moveComponent.LastWorldDestination, Is.EqualTo(secondDestination));
            Assert.That(moveComponent.WorldMoveCount, Is.EqualTo(2));
        }

        [Test]
        public void Reenter_WithoutNewDestination_Throws()
        {
            NavigateState state = new NavigateState(new FakeShipMoveComponent());
            state.SetWorldDestination(Vector3.one);
            state.Enter();
            state.Exit();

            Assert.Throws<InvalidOperationException>(state.Enter);
        }

        private sealed class FakeShipMoveComponent : IShipMoveComponent
        {
            public string Id => nameof(FakeShipMoveComponent);
            public Vector3 CurrentPosition => Vector3.zero;
            public Transform ViewTransform => null;
            public bool IsMoving => false;
            public float HyperSpaceDuration => 0f;
            public Vector3 LastWorldDestination { get; private set; }
            public int WorldMoveCount { get; private set; }

            public event Action<Vector3> TargetPositionChanged;
            public event Action<Vector3> LookAtTargetChanged;
            public event Action Stopped;

            public float MoveAround()
            {
                return 0f;
            }

            public Vector3 CalculateLookDirection(Vector3 targetPosition)
            {
                return Vector3.zero;
            }

            public void MoveToPosition(Vector3 targetPosition)
            {
                LastWorldDestination = targetPosition;
                WorldMoveCount++;
                TargetPositionChanged?.Invoke(targetPosition);
            }

            public void MoveToPositionOnScreen(Vector2 targetPosition)
            {
            }

            public void LookAtTarget(Vector3 targetPosition)
            {
                LookAtTargetChanged?.Invoke(targetPosition);
            }

            public float GetRange(Vector3 targetPosition)
            {
                return 0f;
            }

            public void Stop()
            {
                Stopped?.Invoke();
            }

            public void ApplyMoveCoefficient(float coefficient)
            {
            }

            public void HandleSelection(bool isSelected)
            {
            }

            public void HandleRadarContacts(IReadOnlyList<RadarContact> contacts)
            {
            }
        }
    }
}
