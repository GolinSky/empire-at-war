using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Patterns.StateMachine;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    /// <summary>
    /// No order (also a player's Stop): the ship never moves, but turns to and marks as main target the best threat
    /// its guns already reach, own radar or team vision (<see cref="ShipEngagement.SelectThreat"/>).
    /// </summary>
    public class IdleState : IBaseState
    {
        // Seconds between searches for a new target. Each search checks every radar contact; hundreds of idle
        // ships searching every frame cost up to ~20 ms. A held target is still followed every frame.
        private const float SEARCH_INTERVAL = 0.25f;
        // Team-vision searches while not under fire: catches hostiles beyond own radar but inside weapon reach.
        private const float TEAM_SEARCH_INTERVAL = 1f;

        private readonly IShipMovement _shipMoveComponent;
        private readonly IWeaponComponent _weaponComponent;
        private readonly IRadarComponent _radarComponent;
        private readonly IAttackDataFactory _attackDataFactory;
        private readonly IShipThreatSense _threats;
        private readonly List<IEntity> _candidates = new List<IEntity>();
        private readonly List<IEntity> _teamHostiles = new List<IEntity>();
        private IEntity _engagementTarget;
        private bool _hasMainTarget;
        private float _nextSearchTime;
        private float _nextTeamSearchTime;

        public bool IsComplete => false;

        public IdleState(
            IShipMovement shipMoveComponent,
            IWeaponComponent weaponComponent,
            IRadarComponent radarComponent,
            IAttackDataFactory attackDataFactory,
            IShipThreatSense threats)
        {
            _shipMoveComponent = shipMoveComponent;
            _weaponComponent = weaponComponent;
            _radarComponent = radarComponent;
            _attackDataFactory = attackDataFactory;
            _threats = threats;
        }

        public void Enter()
        {
            if (_shipMoveComponent.IsMoving)
            {
                _shipMoveComponent.Stop();
            }

            _weaponComponent.ResetTarget();
            _hasMainTarget = false;
            // Random phase: ships stopping together do not all search on the same frames.
            _nextSearchTime = Time.time + Random.value * SEARCH_INTERVAL;
            _nextTeamSearchTime = _nextSearchTime;
        }

        public void Tick(float deltaTime)
        {
            if (_engagementTarget != null && (_engagementTarget.IsCloaked() || !ShipEngagement.CanEngage(
                    _engagementTarget, _shipMoveComponent, _weaponComponent, isHolding: true)))
            {
                DropTarget();
            }

            // A held target gives way to a preferred enemy, or to an attacker while this ship is under fire.
            bool isSearching = _engagementTarget == null ||
                               !ShipEngagement.IsPreferred(_engagementTarget, _weaponComponent) ||
                               _threats.WasDamagedRecently && ShipEngagement.ThreatRank(_engagementTarget, _threats) == 0;
            if (isSearching && Time.time >= _nextSearchTime)
            {
                _nextSearchTime = Time.time + SEARCH_INTERVAL;
                IEntity found = FindThreat();
                if (found != null && found != _engagementTarget && (_engagementTarget == null ||
                        ShipEngagement.Outranks(found, _engagementTarget, _weaponComponent, _threats)))
                    SetTarget(found);
            }

            if (_engagementTarget != null)
            {
                _shipMoveComponent.LookAtTarget(_engagementTarget);
            }
        }

        public void Exit()
        {
            DropTarget();
        }

        private IEntity FindThreat()
        {
            _candidates.Clear();
            // Indexed: ObservableList boxes its enumerator.
            for (int i = 0; i < _radarComponent.Enemies.Count; i++)
            {
                IEntity enemy = _radarComponent.Enemies[i];
                if (ShipEngagement.CanEngage(enemy, _shipMoveComponent, _weaponComponent)) _candidates.Add(enemy);
            }

            // A ship out-sighted by a bigger hull is shot from beyond its own radar; team vision still lists it.
            if (_threats.WasDamagedRecently || Time.time >= _nextTeamSearchTime)
            {
                _nextTeamSearchTime = Time.time + TEAM_SEARCH_INTERVAL;
                _teamHostiles.Clear();
                _threats.FillTeamVisibleHostiles(_shipMoveComponent.CurrentPosition,
                    ShipEngagement.TEAM_THREAT_RADIUS, _teamHostiles);
                for (int i = 0; i < _teamHostiles.Count; i++)
                {
                    IEntity enemy = _teamHostiles[i];
                    if (!_candidates.Contains(enemy) &&
                        ShipEngagement.CanEngage(enemy, _shipMoveComponent, _weaponComponent))
                        _candidates.Add(enemy);
                }
            }

            return ShipEngagement.SelectThreat(_candidates, _shipMoveComponent, _weaponComponent, _threats);
        }

        private void SetTarget(IEntity enemy)
        {
            _engagementTarget = enemy;
            _weaponComponent.AddTarget(_attackDataFactory.ConstructData(enemy), AttackType.MainTarget);
            _hasMainTarget = true;
        }

        private void DropTarget()
        {
            _engagementTarget = null;
            if (!_hasMainTarget) return;
            _hasMainTarget = false;
            _weaponComponent.ResetTarget();
        }
    }
}
