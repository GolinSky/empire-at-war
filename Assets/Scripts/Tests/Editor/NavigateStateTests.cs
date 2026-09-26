using System;
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
            FakeShipMovement movement = new FakeShipMovement();
            NavigateState state = new NavigateState(movement);
            ShipStateMachine stateMachine = new ShipStateMachine();
            Vector3 firstDestination = new Vector3(10f, 0f, 20f);
            Vector3 secondDestination = new Vector3(30f, 0f, 40f);

            state.SetWorldDestination(firstDestination);
            stateMachine.SetState(state);
            state.SetWorldDestination(secondDestination);
            stateMachine.SetState(state);

            Assert.That(movement.LastWorldDestination, Is.EqualTo(secondDestination));
            Assert.That(movement.WorldMoveCount, Is.EqualTo(2));
        }

        [Test]
        public void Reenter_WithoutNewDestination_Throws()
        {
            NavigateState state = new NavigateState(new FakeShipMovement());
            state.SetWorldDestination(Vector3.one);
            state.Enter();
            state.Exit();

            Assert.Throws<InvalidOperationException>(state.Enter);
        }

        private sealed class FakeShipMovement : IShipMovement
        {
            public Vector3 CurrentPosition => Vector3.zero;
            public bool IsMoving => false;
            public bool IsBlocked => false;
            public float NavigationRadius => 1f;
            public Vector3 LastWorldDestination { get; private set; }
            public int WorldMoveCount { get; private set; }

            public void MoveToPosition(Vector3 targetPosition, bool preserveCourse = false)
            {
                LastWorldDestination = targetPosition;
                WorldMoveCount++;
            }

            public void LookAtTarget(Vector3 targetPosition)
            {
            }

            public float GetRange(Vector3 targetPosition)
            {
                return 0f;
            }

            public void Stop()
            {
            }
        }
    }
}
