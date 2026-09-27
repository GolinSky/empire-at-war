using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Camera;
using UnityEngine;
using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Entities.Ship.Orders
{
    /// <summary>
    /// Turns the ship's current order into a state and is the only place that changes ship state.
    /// The AI brain can override the order with fleeing; once it stops fleeing the order restarts.
    /// </summary>
    public sealed class ShipOrderRunner
    {
        private readonly UnitOrderModel _orders;
        private readonly ShipStateMachine _stateMachine;
        private readonly ShipAIBrain _brain;
        private readonly IShipMovement _movement;
        private readonly IWeaponComponent _weapon;
        private readonly ICameraService _cameraService;
        private readonly IdleState _idleState;
        private readonly NavigateState _navigateState;
        private readonly AttackTargetState _attackTargetState;
        private readonly AttackMoveState _attackMoveState;
        private readonly GuardState _guardState;
        private readonly HuntState _huntState;
        private readonly FleeState _fleeState;
        private readonly bool _isAiControlled;

        public UnitOrderType CurrentOrder => _orders.Current;

        public ShipOrderRunner(UnitOrderModel orders, ShipStateMachine stateMachine, ShipAIBrain brain,
            IShipMovement movement, IWeaponComponent weapon, ICameraService cameraService,
            IdleState idleState, NavigateState navigateState, AttackTargetState attackTargetState,
            AttackMoveState attackMoveState, GuardState guardState, HuntState huntState,
            FleeState fleeState, PlayerType playerType)
        {
            _orders = orders;
            _stateMachine = stateMachine;
            _brain = brain;
            _movement = movement;
            _weapon = weapon;
            _cameraService = cameraService;
            _idleState = idleState;
            _navigateState = navigateState;
            _attackTargetState = attackTargetState;
            _attackMoveState = attackMoveState;
            _guardState = guardState;
            _huntState = huntState;
            _fleeState = fleeState;
            _isAiControlled = playerType == PlayerType.Opponent;
        }

        public void Start() => _stateMachine.SetState(_idleState);

        public void Tick(float deltaTime)
        {
            _brain.Tick(deltaTime);
            if (_brain.IsFleeing)
            {
                if (_stateMachine.CurrentState != _fleeState) _stateMachine.SetState(_fleeState);
            }
            else if (_stateMachine.CurrentState == _fleeState)
            {
                StartOrder();
            }

            _stateMachine.Tick(deltaTime);
            if (_stateMachine.CurrentState.IsComplete) CompleteOrder();
        }

        public void Release()
        {
            _orders.Clear();
            _brain.Enable(false);
        }

        public void MoveTo(Vector2 screenPosition) =>
            MoveTo(_cameraService.GetWorldPoint(screenPosition, _movement.CurrentPosition));

        public void MoveTo(Vector3 worldPosition)
        {
            FormationPoint destination = ToPoint(worldPosition);
            if (_orders.Matches(UnitOrderType.Move, destination)) return;
            Issue(UnitOrderType.Move, destination);
        }

        public void Attack(IEntity target, Vector3 formationOffset)
        {
            FormationPoint offset = ToPoint(formationOffset);
            if (_orders.Matches(UnitOrderType.Attack, target: target, offset: offset)) return;
            Issue(UnitOrderType.Attack, target: target, offset: offset);
        }

        public void AttackHardPoint(IEntity target, int hardPointId, Vector3 formationOffset)
        {
            FormationPoint offset = ToPoint(formationOffset);
            if (_orders.Matches(UnitOrderType.Attack, target: target, offset: offset,
                    targetHardPointId: hardPointId)) return;
            Issue(UnitOrderType.Attack, target: target, offset: offset, targetHardPointId: hardPointId);
        }

        public void AttackMoveTo(Vector3 destination)
        {
            FormationPoint point = ToPoint(destination);
            if (_orders.Matches(UnitOrderType.AttackMove, point)) return;
            Issue(UnitOrderType.AttackMove, point);
        }

        public void Guard(IEntity friendly, Vector3 offset)
        {
            FormationPoint formationOffset = ToPoint(offset);
            if (_orders.Matches(UnitOrderType.Guard, target: friendly, offset: formationOffset)) return;
            Issue(UnitOrderType.Guard, target: friendly, offset: formationOffset);
        }

        public void MoveAlong(IReadOnlyList<Vector3> waypoints)
        {
            if (waypoints.Count == 0) return;
            List<FormationPoint> points = new List<FormationPoint>(waypoints.Count);
            foreach (Vector3 waypoint in waypoints) points.Add(ToPoint(waypoint));
            if (_orders.MatchesWaypoints(points)) return;
            Issue(UnitOrderType.WaypointMove, points[0], waypoints: points);
        }

        public void Hunt()
        {
            if (_orders.Current == UnitOrderType.Hunt) return;
            Issue(UnitOrderType.Hunt);
        }

        public void Retreat(Vector3 destination)
        {
            FormationPoint point = ToPoint(destination);
            if (_orders.Matches(UnitOrderType.Retreat, point)) return;
            _weapon.ResetTarget();
            Issue(UnitOrderType.Retreat, point);
        }

        public void Stop()
        {
            _weapon.ResetTarget();
            Issue(UnitOrderType.None);
        }

        private void Issue(UnitOrderType type, FormationPoint destination = default,
            IEntity target = null, FormationPoint offset = default,
            IReadOnlyList<FormationPoint> waypoints = null, int targetHardPointId = UnitOrderModel.NO_HARD_POINT)
        {
            _orders.Replace(type, destination, target, offset, waypoints, targetHardPointId);
            _brain.Enable(_isAiControlled);
            StartOrder();
        }

        private void StartOrder()
        {
            // Fleeing overrides every order; Tick restarts the order once the brain stops fleeing.
            if (_brain.IsFleeing) return;
            switch (_orders.Current)
            {
                case UnitOrderType.Move:
                case UnitOrderType.WaypointMove:
                case UnitOrderType.Retreat:
                    _navigateState.SetWorldDestination(ToVector(_orders.Destination));
                    _stateMachine.SetState(_navigateState);
                    break;
                case UnitOrderType.Attack:
                    if (_orders.Target.HealthModel.IsDestroyed)
                    {
                        Stop();
                        break;
                    }

                    _attackTargetState.SetData(_orders.Target, ToVector(_orders.Offset),
                        _orders.TargetHardPointId);
                    _stateMachine.SetState(_attackTargetState);
                    break;
                case UnitOrderType.AttackMove:
                    _attackMoveState.SetDestination(ToVector(_orders.Destination));
                    _stateMachine.SetState(_attackMoveState);
                    break;
                case UnitOrderType.Guard:
                    if (_orders.Target.HealthModel.IsDestroyed)
                    {
                        Stop();
                        break;
                    }

                    _guardState.SetData(_orders.Target, ToVector(_orders.Offset));
                    _stateMachine.SetState(_guardState);
                    break;
                case UnitOrderType.Hunt:
                    _stateMachine.SetState(_huntState);
                    break;
                default:
                    _movement.Stop();
                    _stateMachine.SetState(_idleState);
                    break;
            }
        }

        private void CompleteOrder()
        {
            if (_orders.Current == UnitOrderType.WaypointMove && _orders.AdvanceWaypoint(out FormationPoint next))
            {
                _navigateState.SetWorldDestination(ToVector(next));
                _stateMachine.SetState(_navigateState);
                return;
            }

            _orders.Clear();
            _stateMachine.SetState(_idleState);
        }

        private static FormationPoint ToPoint(Vector3 value) => new FormationPoint(value.x, value.z);

        private static Vector3 ToVector(FormationPoint value) => new Vector3(value.X, 0f, value.Z);
    }
}
