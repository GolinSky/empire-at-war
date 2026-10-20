using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public class IdleState : IBaseState
    {
        // Seconds between searches for a new target. Each search checks every radar contact; hundreds of idle
        // ships searching every frame cost up to ~20 ms. A held target is still followed every frame.
        private const float SEARCH_INTERVAL = 0.25f;

        private readonly IShipMovement _shipMoveComponent;
        private readonly IWeaponComponent _weaponComponent;
        private readonly IRadarComponent _radarComponent;
        private IEntity _engagementTarget;
        private float _nextSearchTime;

        public bool IsComplete => false;

        public IdleState(
            IShipMovement shipMoveComponent,
            IWeaponComponent weaponComponent,
            IRadarComponent radarComponent)
        {
            _shipMoveComponent = shipMoveComponent;
            _weaponComponent = weaponComponent;
            _radarComponent = radarComponent;
        }

        public void Enter()
        {
            if (_shipMoveComponent.IsMoving)
            {
                _shipMoveComponent.Stop();
            }

            _weaponComponent.ResetTarget();
            // Random phase: ships stopping together do not all search on the same frames.
            _nextSearchTime = Time.time + Random.value * SEARCH_INTERVAL;
        }

        public void Tick(float deltaTime)
        {
            if (_engagementTarget != null && (_engagementTarget.IsCloaked() || !ShipEngagement.CanEngage(
                    _engagementTarget, _shipMoveComponent, _weaponComponent)))
            {
                _engagementTarget = null;
            }

            // A held target the loadout is not built for gives way once a preferred enemy comes into range.
            bool isSearching = _engagementTarget == null ||
                               !ShipEngagement.IsPreferred(_engagementTarget, _weaponComponent);
            if (isSearching && Time.time >= _nextSearchTime)
            {
                _nextSearchTime = Time.time + SEARCH_INTERVAL;
                bool hasPreferred = false;
                float nearestDistance = float.PositiveInfinity;
                // Indexed: ObservableList boxes its enumerator.
                for (int i = 0; i < _radarComponent.Enemies.Count; i++)
                {
                    IEntity enemy = _radarComponent.Enemies[i];
                    if (enemy.IsCloaked() || !ShipEngagement.CanEngage(enemy, _shipMoveComponent, _weaponComponent))
                    {
                        continue;
                    }

                    // Nearest preferred enemy; the nearest other one only while no preferred enemy is in range.
                    bool isPreferred = ShipEngagement.IsPreferred(enemy, _weaponComponent);
                    if (hasPreferred && !isPreferred) continue;
                    float distance = _shipMoveComponent.GetRange(enemy.GetFacade<IEntityTransformFacade>().Transform.position);
                    if (distance < nearestDistance || isPreferred && !hasPreferred)
                    {
                        hasPreferred = isPreferred;
                        nearestDistance = distance;
                        _engagementTarget = enemy;
                    }
                }
            }

            if (_engagementTarget != null)
            {
                _shipMoveComponent.LookAtTarget(_engagementTarget.GetFacade<IEntityTransformFacade>().Transform.position);
            }
        }

        public void Exit()
        {
            _engagementTarget = null;
        }

    }
}
