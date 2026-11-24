using System.Collections.Generic;
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
    /// <summary>
    /// Follows a friendly and fights the best threat (<see cref="ShipEngagement.SelectThreat"/>) its guns can reach
    /// from the guard post. The leash is the post plus the chase distance, the own hull radius and the weapon
    /// range, so a big hull or a long-range battery is not pulled home before it can fire.
    /// </summary>
    public sealed class GuardState : IBaseState
    {
        // Seconds between target searches; each one ranks every contact near the post.
        private const float SEARCH_INTERVAL = 0.25f;
        // Team-vision searches while under fire.
        private const float TEAM_SEARCH_INTERVAL = 0.5f;

        private readonly IShipMovement _movement;
        private readonly IWeaponComponent _weapon;
        private readonly IRadarComponent _radar;
        private readonly IAttackDataFactory _attackDataFactory;
        private readonly IShipThreatSense _threats;
        private readonly List<IEntity> _candidates = new List<IEntity>();
        private readonly List<IEntity> _teamHostiles = new List<IEntity>();
        private IEntity _friendly;
        private IEntity _engagementTarget;

        private readonly UnitOrderSettings _settings;
        private Transform _friendlyTransform;

        private Vector3 _offset;
        private Vector3 _followDestination;

        private bool _isReturning;
        private bool _isFollowing;
        private bool _isHolding;
        // The target came from team vision, not this ship's radar.
        private bool _isTeamTarget;
        private float _nextSearchTime;
        private float _nextTeamSearchTime;

        public bool IsComplete => _friendly.HealthModel.IsDestroyed;

        // How far from its post the ship may stray while fighting.
        private float ChaseLimit => _settings.GuardChaseDistance + _movement.NavigationRadius + _weapon.AttackDistance;

        public GuardState(IShipMovement movement, IWeaponComponent weapon,
            IRadarComponent radar, IAttackDataFactory attackDataFactory,
            UnitOrderSettings settings, IShipThreatSense threats)
        {
            _movement = movement;
            _weapon = weapon;
            _radar = radar;
            _attackDataFactory = attackDataFactory;
            _settings = settings;
            _threats = threats;
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
            _isHolding = false;
            _nextSearchTime = float.NegativeInfinity;
            _nextTeamSearchTime = float.NegativeInfinity;
            Follow();
        }

        public void Tick(float deltaTime)
        {
            if (IsComplete) return;
            Vector3 home = _friendlyTransform.position + _offset;
            if (PlanarGeometry.Distance(_movement.CurrentPosition, home) > ChaseLimit)
                _isReturning = true;
            if (_engagementTarget != null &&
                (_isReturning || _engagementTarget.HealthModel.IsDestroyed || _engagementTarget.IsCloaked() ||
                 !IsStillContact(_engagementTarget)))
            {
                _movement.CancelCombatHold();
                _weapon.ResetTarget();
                _engagementTarget = null;
                _isHolding = false;
            }

            if (_isReturning && PlanarGeometry.Distance(_movement.CurrentPosition, home) <=
                _settings.GuardFollowRadius) _isReturning = false;

            // A held target gives way to a preferred enemy, or to an attacker while this ship is under fire.
            if (!_isReturning && Time.time >= _nextSearchTime &&
                (_engagementTarget == null || !ShipEngagement.IsPreferred(_engagementTarget, _weapon) ||
                 _threats.WasDamagedRecently && ShipEngagement.ThreatRank(_engagementTarget, _threats) == 0))
            {
                _nextSearchTime = Time.time + SEARCH_INTERVAL;
                IEntity enemy = FindTarget(home);
                if (enemy != null && enemy != _engagementTarget &&
                    (_engagementTarget == null || ShipEngagement.Outranks(enemy, _engagementTarget, _weapon, _threats)))
                {
                    _movement.CancelCombatHold();
                    _isHolding = false;
                    _engagementTarget = enemy;
                    _isTeamTarget = !_radar.Enemies.Contains(enemy);
                    _weapon.AddTarget(_attackDataFactory.ConstructData(enemy), AttackType.MainTarget);
                    if (!ShipEngagement.CanEngage(enemy, _movement, _weapon))
                        ShipEngagement.Pursue(_movement, _weapon, enemy, newTarget: true);
                }
            }

            if (_engagementTarget != null)
            {
                // Any chase move replaces the follow course, so the next Follow must re-path.
                _isFollowing = false;
                if (ShipEngagement.CanHoldOn(_engagementTarget, _movement, _weapon, _isHolding))
                {
                    _isHolding = true;
                    _movement.HoldPosition(_engagementTarget);
                    _movement.LookAtTarget(_engagementTarget);
                }
                else
                {
                    _isHolding = false;
                    ShipEngagement.Pursue(_movement, _weapon, _engagementTarget);
                }
            }
            else if (PlanarGeometry.Distance(_movement.CurrentPosition, home) >
                     _settings.GuardFollowRadius) Follow();
        }

        public void Exit()
        {
            _movement.CancelCombatHold();
            _engagementTarget = null;
            _isReturning = false;
            _isHolding = false;
            _weapon.ResetTarget();
        }

        // A radar target stays while on radar; a team-vision target only while the guns still reach it.
        private bool IsStillContact(IEntity enemy) =>
            _radar.Enemies.Contains(enemy) ||
            _isTeamTarget && ShipEngagement.CanEngage(enemy, _movement, _weapon, _isHolding);

        // The best threat the guns can reach from within the leash, or, under fire, any team-visible one in reach.
        private IEntity FindTarget(Vector3 home)
        {
            _candidates.Clear();
            // Indexed: ObservableList boxes its enumerator.
            for (int i = 0; i < _radar.Enemies.Count; i++)
            {
                IEntity enemy = _radar.Enemies[i];
                if (enemy.HealthModel.IsDestroyed || !enemy.HealthModel.HasUnits || enemy.IsCloaked() ||
                    PlanarGeometry.Distance(enemy.GetFacade<IEntityTransformFacade>().Transform.position, home) >
                    _settings.GuardChaseDistance + ShipEngagement.ContactDistance(_movement, enemy) +
                    _weapon.GetEngagementRange(enemy.HealthModel.ShipClass)) continue;
                _candidates.Add(enemy);
            }

            if (_threats.WasDamagedRecently && Time.time >= _nextTeamSearchTime)
            {
                _nextTeamSearchTime = Time.time + TEAM_SEARCH_INTERVAL;
                _teamHostiles.Clear();
                _threats.FillTeamVisibleHostiles(_movement.CurrentPosition, ShipEngagement.TEAM_THREAT_RADIUS,
                    _teamHostiles);
                for (int i = 0; i < _teamHostiles.Count; i++)
                {
                    IEntity enemy = _teamHostiles[i];
                    if (!_candidates.Contains(enemy) && ShipEngagement.CanEngage(enemy, _movement, _weapon))
                        _candidates.Add(enemy);
                }
            }

            return ShipEngagement.SelectThreat(_candidates, _movement, _weapon, _threats);
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
