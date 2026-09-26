using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Entities.Ship.Orders;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Patterns.StateMachine;
using NUnit.Framework;
using UnityEngine;
using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipStopTests
    {
        [TestCase(UnitOrderType.WaypointMove)]
        [TestCase(UnitOrderType.Retreat)]
        [TestCase(UnitOrderType.Attack)]
        public void Stop_ClearsOrderAndMainTargetAndEntersIdle(UnitOrderType order)
        {
            UnitOrderModel model = new UnitOrderModel();
            FormationPoint destination = new FormationPoint(10f, 20f);
            if (order == UnitOrderType.WaypointMove)
                model.Replace(order, destination,
                    waypoints: new[] { destination, new FormationPoint(30f, 40f) });
            else if (order == UnitOrderType.Retreat)
                model.Replace(order, destination);
            else model.Replace(order);
            FakeMovement movement = new FakeMovement();
            FakeWeapon weapon = new FakeWeapon();
            IdleState idle = new IdleState(movement, weapon, null);
            ShipStateMachine stateMachine = new ShipStateMachine();
            stateMachine.SetState(new PassiveState());
            ShipAIBrain brain = new ShipAIBrain(null, null, movement, null, null, model);
            ShipOrderRunner runner = new ShipOrderRunner(model, stateMachine, brain, movement, weapon,
                null, idle, null, null, null, null, null, null, PlayerType.Player);

            runner.Stop();

            Assert.That(model.Current, Is.EqualTo(UnitOrderType.None));
            Assert.That(stateMachine.CurrentState, Is.SameAs(idle));
            Assert.That(weapon.ResetCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(movement.StopCount, Is.EqualTo(1));
            Assert.That(model.AdvanceWaypoint(out _), Is.False);
        }

        private sealed class PassiveState : IBaseState
        {
            public bool IsComplete => false;
            public void Enter() { }
            public void Tick(float deltaTime) { }
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

        private sealed class FakeMovement : IShipMovement
        {
            public Vector3 CurrentPosition => Vector3.zero;
            public bool IsMoving => false;
            public bool IsBlocked => false;
            public float NavigationRadius => 1f;
            public int StopCount { get; private set; }
            public void MoveToPosition(Vector3 position, bool preserveCourse = false) { }
            public void LookAtTarget(Vector3 position) { }
            public float GetRange(Vector3 position) => 0f;
            public void Stop() => StopCount++;
        }
    }
}
