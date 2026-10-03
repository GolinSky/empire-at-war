using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Patterns.StateMachine;
using UnityEngine;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    /// <summary>
    /// Moves toward the destination and fights every enemy the attack-move group detects on the way.
    /// Targets come from the shared <see cref="AttackMoveEngagement"/> so the group spreads its fire;
    /// once the engagement set is clear the ship resumes its course.
    /// </summary>
    public sealed class AttackMoveState : IBaseState
    {
        private readonly IShipMovement _movement;
        private readonly IWeaponComponent _weapon;
        private readonly IRadarComponent _radar;
        private readonly IAttackDataFactory _attackDataFactory;
        private IEntity _engagementTarget;

        private readonly Func<IEntity, float> _rangeTo;
        private AttackMoveEngagement _nextEngagement;
        private AttackMoveEngagement _engagement;

        private Vector3 _destination;
        private Vector3 _pursuitDestination;

        private int _member;

        public bool IsComplete => _engagementTarget == null && !_movement.IsMoving;

        public AttackMoveState(IShipMovement movement, IWeaponComponent weapon,
            IRadarComponent radar, IAttackDataFactory attackDataFactory)
        {
            _movement = movement;
            _weapon = weapon;
            _radar = radar;
            _attackDataFactory = attackDataFactory;
            _rangeTo = enemy => _movement.GetRange(enemy.GetFacade<IEntityTransformFacade>().Transform.position);
        }

        public void SetData(Vector3 destination, AttackMoveEngagement engagement)
        {
            _destination = destination;
            // Applied on Enter: SetData runs before Exit, which must leave the engagement it joined.
            _nextEngagement = engagement;
        }

        public void Enter()
        {
            _engagement = _nextEngagement;
            _engagementTarget = null;
            _member = _engagement.Join();
            _movement.MoveToPosition(_destination);
        }

        public void Tick(float deltaTime)
        {
            _engagement.Report(_member, _radar.Enemies);
            if (_engagementTarget != null && !_engagement.Contains(_engagementTarget))
            {
                _weapon.ResetTarget();
                _engagementTarget = null;
                _movement.MoveToPosition(_destination);
            }

            if (_engagementTarget == null)
            {
                _engagementTarget = _engagement.SelectTarget(_member, _rangeTo);
                if (_engagementTarget != null)
                {
                    _weapon.AddTarget(_attackDataFactory.ConstructData(_engagementTarget), AttackType.MainTarget);
                    // A new target always breaks off the course to the attack-move point.
                    _pursuitDestination = _engagementTarget.GetFacade<IEntityTransformFacade>().Transform.position;
                    if (!ShipEngagement.CanEngage(_engagementTarget, _movement, _weapon))
                        _movement.MoveToPosition(_pursuitDestination, preserveCourse: true);
                }
            }

            if (_engagementTarget != null)
            {
                Vector3 target = _engagementTarget.GetFacade<IEntityTransformFacade>().Transform.position;
                if (ShipEngagement.CanEngage(_engagementTarget, _movement, _weapon))
                {
                    if (_movement.IsMoving) _movement.Stop();
                    _movement.LookAtTarget(target);
                }
                else _pursuitDestination = ShipEngagement.Pursue(_movement, _weapon, target,
                    _pursuitDestination);
            }
        }

        public void Exit()
        {
            _engagement.Leave(_member);
            _engagementTarget = null;
            _weapon.ResetTarget();
        }
    }
}
