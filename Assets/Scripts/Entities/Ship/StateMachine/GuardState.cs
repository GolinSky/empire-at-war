using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Services.UnitOrders;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public sealed class GuardState : IBaseState
    {
        private readonly IShipMoveComponent _movement;
        private readonly IWeaponComponent _weapon;
        private readonly IRadarComponent _radar;
        private readonly IAttackDataFactory _attackDataFactory;
        private readonly UnitOrderSettings _settings;
        private IEntity _friendly;
        private IEntity _engagementTarget;
        private Vector3 _offset;
        private bool _isReturning;
        private Vector3 _pursuitDestination;

        public GuardState(IShipMoveComponent movement, IWeaponComponent weapon,
            IRadarComponent radar, IAttackDataFactory attackDataFactory,
            UnitOrderSettings settings)
        {
            _movement = movement;
            _weapon = weapon;
            _radar = radar;
            _attackDataFactory = attackDataFactory;
            _settings = settings;
        }

        public bool IsComplete => _friendly.HealthModel.IsDestroyed;

        public void SetData(IEntity friendly, Vector3 offset)
        {
            _friendly = friendly;
            _offset = offset;
            _isReturning = false;
        }

        public void Enter() => Follow();

        public void Update()
        {
            if (IsComplete) return;
            Vector3 home = _friendly.HealthModel.Transform.position + _offset;
            if (Vector3.Distance(_movement.CurrentPosition,
                    _friendly.HealthModel.Transform.position) > _settings.GuardChaseDistance)
                _isReturning = true;
            if (_engagementTarget != null &&
                (_isReturning || _engagementTarget.HealthModel.IsDestroyed ||
                 !_radar.Enemies.Contains(_engagementTarget)))
            {
                _weapon.ResetTarget();
                _engagementTarget = null;
            }

            if (_isReturning && Vector3.Distance(_movement.CurrentPosition, home) <=
                _settings.GuardFollowRadius) _isReturning = false;

            if (_engagementTarget == null && !_isReturning)
            {
                foreach (IEntity enemy in _radar.Enemies)
                {
                    if (enemy.HealthModel.IsDestroyed || !enemy.HealthModel.HasUnits ||
                        Vector3.Distance(enemy.HealthModel.Transform.position, home) >
                        _settings.GuardChaseDistance) continue;
                    _engagementTarget = enemy;
                    _weapon.AddTarget(_attackDataFactory.ConstructData(enemy), AttackType.MainTarget);
                    _pursuitDestination = enemy.HealthModel.Transform.position;
                    _movement.MoveToPosition(_pursuitDestination, preserveCourse: true);
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
                else _pursuitDestination = ShipEngagement.Pursue(_movement, _weapon, target,
                    _pursuitDestination);
            }
            else if (Vector3.Distance(_movement.CurrentPosition, home) >
                     _settings.GuardFollowRadius) Follow();
        }

        public void Exit()
        {
            _engagementTarget = null;
            _isReturning = false;
            _weapon.ResetTarget();
        }

        private void Follow() => _movement.MoveToPosition(
            _friendly.HealthModel.Transform.position + _offset);
    }
}
