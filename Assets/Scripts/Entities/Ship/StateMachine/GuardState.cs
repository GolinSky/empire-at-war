using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Services.UnitOrders;
using UnityEngine;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Utils;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public sealed class GuardState : IBaseState
    {
        private readonly IShipMovement _movement;
        private readonly IWeaponComponent _weapon;
        private readonly IRadarComponent _radar;
        private readonly IAttackDataFactory _attackDataFactory;
        private IEntity _friendly;
        private IEntity _engagementTarget;

        private readonly UnitOrderSettings _settings;
        private Transform _friendlyTransform;

        private Vector3 _offset;
        private Vector3 _followDestination;

        private bool _isReturning;
        private bool _isFollowing;

        public bool IsComplete => _friendly.HealthModel.IsDestroyed;

        public GuardState(IShipMovement movement, IWeaponComponent weapon,
            IRadarComponent radar, IAttackDataFactory attackDataFactory,
            UnitOrderSettings settings)
        {
            _movement = movement;
            _weapon = weapon;
            _radar = radar;
            _attackDataFactory = attackDataFactory;
            _settings = settings;
        }

        public void SetData(IEntity friendly, Vector3 offset)
        {
            _friendly = friendly;
            _friendlyTransform = friendly.GetFacade<IEntityTransformFacade>().Transform;
            _offset = offset;
            _isReturning = false;
        }

        // A fresh guard order always moves, even if the ship is still on an older course.
        public void Enter()
        {
            _isFollowing = false;
            Follow();
        }

        public void Tick(float deltaTime)
        {
            if (IsComplete) return;
            Vector3 home = _friendlyTransform.position + _offset;
            if (PlanarGeometry.Distance(_movement.CurrentPosition,
                    _friendlyTransform.position) > _settings.GuardChaseDistance)
                _isReturning = true;
            if (_engagementTarget != null &&
                (_isReturning || _engagementTarget.HealthModel.IsDestroyed || _engagementTarget.IsCloaked() ||
                 !_radar.Enemies.Contains(_engagementTarget)))
            {
                _weapon.ResetTarget();
                _engagementTarget = null;
            }

            if (_isReturning && PlanarGeometry.Distance(_movement.CurrentPosition, home) <=
                _settings.GuardFollowRadius) _isReturning = false;

            if (_engagementTarget == null && !_isReturning)
            {
                // Indexed: ObservableList boxes its enumerator.
                for (int i = 0; i < _radar.Enemies.Count; i++)
                {
                    IEntity enemy = _radar.Enemies[i];
                    if (enemy.HealthModel.IsDestroyed || !enemy.HealthModel.HasUnits || enemy.IsCloaked() ||
                        PlanarGeometry.Distance(enemy.GetFacade<IEntityTransformFacade>().Transform.position, home) >
                        _settings.GuardChaseDistance) continue;
                    _engagementTarget = enemy;
                    _weapon.AddTarget(_attackDataFactory.ConstructData(enemy), AttackType.MainTarget);
                    if (!ShipEngagement.CanEngage(enemy, _movement, _weapon))
                        ShipEngagement.Pursue(_movement, _weapon, enemy, newTarget: true);
                    break;
                }
            }

            if (_engagementTarget != null)
            {
                // Any chase move replaces the follow course, so the next Follow must re-path.
                _isFollowing = false;
                Vector3 target = _engagementTarget.GetFacade<IEntityTransformFacade>().Transform.position;
                if (ShipEngagement.CanEngage(_engagementTarget, _movement, _weapon))
                {
                    if (_movement.IsMoving) _movement.Stop();
                    _movement.LookAtTarget(target);
                }
                else ShipEngagement.Pursue(_movement, _weapon, _engagementTarget);
            }
            else if (PlanarGeometry.Distance(_movement.CurrentPosition, home) >
                     _settings.GuardFollowRadius) Follow();
        }

        public void Exit()
        {
            _engagementTarget = null;
            _isReturning = false;
            _weapon.ResetTarget();
        }

        // Finish the current follow leg before considering the friendly's new position.
        private void Follow()
        {
            Vector3 destination = _friendlyTransform.position + _offset;
            float updateDistance = Mathf.Max(_movement.NavigationRadius,
                _settings.GuardFollowRadius * 0.5f);
            if (_isFollowing && (_movement.IsMoving ||
                (destination - _followDestination).sqrMagnitude < updateDistance * updateDistance))
                return;
            bool preserveCourse = _isFollowing;
            _isFollowing = true;
            _followDestination = destination;
            _movement.MoveToPosition(destination, preserveCourse);
        }
    }
}
