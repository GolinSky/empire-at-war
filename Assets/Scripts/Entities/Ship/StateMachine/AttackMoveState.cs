using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Patterns.StateMachine;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public sealed class AttackMoveState : IBaseState
    {
        private readonly IShipMoveComponent _movement;
        private readonly IWeaponComponent _weapon;
        private readonly IRadarComponent _radar;
        private readonly IAttackDataFactory _attackDataFactory;
        private IEntity _engagementTarget;
        private Vector3 _destination;

        public AttackMoveState(IShipMoveComponent movement, IWeaponComponent weapon,
            IRadarComponent radar, IAttackDataFactory attackDataFactory)
        {
            _movement = movement;
            _weapon = weapon;
            _radar = radar;
            _attackDataFactory = attackDataFactory;
        }

        public bool IsEngaging => _engagementTarget != null;
        public void SetDestination(Vector3 destination) => _destination = destination;

        public void Enter()
        {
            _engagementTarget = null;
            _movement.MoveToPosition(_destination);
        }

        public void Update()
        {
            if (_engagementTarget != null &&
                (!_radar.Enemies.Contains(_engagementTarget) ||
                 _engagementTarget.HealthModel.IsDestroyed ||
                 !_engagementTarget.HealthModel.HasUnits))
            {
                _weapon.ResetTarget();
                _engagementTarget = null;
                _movement.MoveToPosition(_destination);
            }

            if (_engagementTarget == null)
            {
                foreach (IEntity enemy in _radar.Enemies)
                {
                    if (enemy.HealthModel.IsDestroyed || !enemy.HealthModel.HasUnits) continue;
                    _engagementTarget = enemy;
                    _weapon.AddTarget(_attackDataFactory.ConstructData(enemy), AttackType.MainTarget);
                    break;
                }
            }

            if (_engagementTarget != null)
            {
                Vector3 target = _engagementTarget.HealthModel.Transform.position;
                if (ShipEngagement.CanEngage(_engagementTarget, _movement, _weapon))
                {
                    if (_movement.IsMoving) _movement.Stop();
                    _movement.LookAtTarget(target);
                }
                else _movement.MoveToPosition(target, preserveCourse: true);
            }
        }

        public void Exit()
        {
            _engagementTarget = null;
            _weapon.ResetTarget();
        }
    }
}
