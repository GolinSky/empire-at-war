using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Entities.Ship.Orders;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Patterns.StateMachine;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipStopTests
    {
        [TestCase(ShipOrderType.WaypointMove)]
        [TestCase(ShipOrderType.Retreat)]
        [TestCase(ShipOrderType.Attack)]
        public void Stop_ClearsOrderAndMainTargetAndEntersIdle(ShipOrderType order)
        {
            GameObject gameObject = new GameObject("ShipStopTest");
            EmpireAtWar.Ship.Ship ship = gameObject.AddComponent<EmpireAtWar.Ship.Ship>();
            ShipOrderModel model = new ShipOrderModel();
            FormationPoint destination = new FormationPoint(10f, 20f);
            if (order == ShipOrderType.WaypointMove)
                model.Replace(order, destination,
                    waypoints: new[] { destination, new FormationPoint(30f, 40f) });
            else if (order == ShipOrderType.Retreat)
                model.Replace(order, destination, retreatDelay: 5f);
            else model.Replace(order);
            FakeMovement movement = new FakeMovement();
            FakeWeapon weapon = new FakeWeapon();
            IdleState idle = new IdleState(movement, weapon, null);
            StateMachine1 stateMachine = new StateMachine1();
            stateMachine.SetState(new PassiveState());
            Set(ship, "_orderModel", model);
            Set(ship, "_shipMoveComponent", movement);
            Set(ship, "_weaponComponent", weapon);
            Set(ship, "_idleState", idle);
            Set(ship, "_stateMachine", stateMachine);
            Set(ship, "_playerType", PlayerType.Player);
            Set(ship, "_shipAIBrain", new ShipAIBrain(null, null, null,
                null, null, null, null, null, null));

            try
            {
                ship.Stop();

                Assert.That(model.Current, Is.EqualTo(ShipOrderType.None));
                Assert.That(stateMachine.CurrentState, Is.SameAs(idle));
                Assert.That(weapon.ResetCount, Is.GreaterThanOrEqualTo(1));
                Assert.That(movement.StopCount, Is.EqualTo(1));
                Assert.That(model.AdvanceWaypoint(out _), Is.False);
                Assert.That(model.AdvanceRetreat(10f), Is.False);
            }
            finally { Object.DestroyImmediate(gameObject); }
        }

        private static void Set(object target, string name, object value) =>
            typeof(EmpireAtWar.Ship.Ship).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private sealed class PassiveState : IBaseState
        {
            public void Enter() { }
            public void Update() { }
            public void Exit() { }
        }

        private sealed class FakeWeapon : IWeaponComponent
        {
            public string Id => nameof(FakeWeapon);
            public int ResetCount { get; private set; }
            public float AttackDistance => 100f;
            public void AddTarget(AttackData data, AttackType type) { }
            public bool HasEnoughRange(float distance) => true;
            public void ResetTarget() => ResetCount++;
            public void Release() { }
        }

        private sealed class FakeMovement : IShipMoveComponent
        {
            public string Id => nameof(FakeMovement);
            public Vector3 CurrentPosition => Vector3.zero;
            public bool IsMoving => false;
            public bool IsBlocked => false;
            public float NavigationRadius => 1f;
            public float NavigationSpeed => 1f;
            public float HyperSpaceDuration => 0f;
            public int StopCount { get; private set; }
            public void MoveToPosition(Vector3 position, bool preserveCourse = false) { }
            public void MoveToPositionOnScreen(Vector2 position) { }
            public void LookAtTarget(Vector3 position) { }
            public float GetRange(Vector3 position) => 0f;
            public void Stop() => StopCount++;
            public void ApplyMoveCoefficient(float coefficient) { }
            public void HandleSelection(bool selected) { }
            public void HandleRadarContacts(IReadOnlyList<RadarContact> contacts) { }
            public void SetMediator(IShipMovementMediator mediator) { }
        }
    }
}
