using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Patterns.StateMachine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public class IdleState : IBaseState
    {
        private readonly IShipMoveComponent _shipMoveComponent;
        private readonly IWeaponComponent _weaponComponent;
        private readonly IRadarComponent _radarComponent;
        private IEntity _engagementTarget;

        public IdleState(
            IShipMoveComponent shipMoveComponent,
            IWeaponComponent weaponComponent,
            IRadarComponent radarComponent)
        {
            _shipMoveComponent = shipMoveComponent;
            _weaponComponent = weaponComponent;
            _radarComponent = radarComponent;
        }

        public void Enter()
        {
            if (_shipMoveComponent.IsMoving || _shipMoveComponent.IsBlocked)
            {
                _shipMoveComponent.Stop();
            }

            _weaponComponent.ResetTarget();
        }

        public void Update()
        {
            if (_engagementTarget != null && !ShipEngagement.CanEngage(
                    _engagementTarget, _shipMoveComponent, _weaponComponent))
            {
                _engagementTarget = null;
            }

            if (_engagementTarget == null)
            {
                float nearestDistance = float.PositiveInfinity;
                foreach (IEntity enemy in _radarComponent.Enemies)
                {
                    if (!ShipEngagement.CanEngage(enemy, _shipMoveComponent, _weaponComponent))
                    {
                        continue;
                    }

                    float distance = _shipMoveComponent.GetRange(enemy.HealthModel.Transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        _engagementTarget = enemy;
                    }
                }
            }

            if (_engagementTarget != null)
            {
                _shipMoveComponent.LookAtTarget(_engagementTarget.HealthModel.Transform.position);
            }
        }

        public void Exit()
        {
            _engagementTarget = null;
        }

    }
}
