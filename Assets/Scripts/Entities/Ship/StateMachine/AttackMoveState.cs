using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
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
    /// The destination marks an area: the enemies within the ship's reach of the point. The ship flies toward it
    /// without breaking off for fights outside it (weapons still fire at what passes in range). As soon as the
    /// attack-move group detects an enemy inside the area, the ship stops flying to the point and engages from
    /// firing range; it never chases beyond that leash. With no enemy there it flies to the point and fights
    /// what arrives.
    /// The one exception en route: a warship (corvette or bigger) that hit this ship in the last
    /// <see cref="ShipThreatSense.RECENT_DAMAGE_SECONDS"/> and is in weapon reach. The ship stops, fights it without
    /// chasing, then continues to the destination (<see cref="IsEngagingEnRoute"/>).
    /// Targets come from the shared <see cref="AttackMoveEngagement"/> so the group spreads its fire; targets in
    /// weapon reach and attackers of a team station or of this ship rank first. A ship that loses its target looks
    /// for the next one first and returns to the destination only when none is left.
    /// </summary>
    public sealed class AttackMoveState : IBaseState
    {
        // Seconds between target searches while the ship has no target, with a random first delay. A whole
        // attack-move group otherwise searches, claims and arms its weapons on the order's frame (~28 ms for
        // 178 ships) and again on the frame their shared targets die.
        private const float SEARCH_INTERVAL = 0.25f;
        // Engagement priority of a target already in weapon reach; above every threat rank.
        private const int IN_REACH_PRIORITY = 3;

        private readonly IShipMovement _movement;
        private readonly IWeaponComponent _weapon;
        private readonly IRadarComponent _radar;
        private readonly IAttackDataFactory _attackDataFactory;
        private readonly IShipThreatSense _threats;
        private IEntity _engagementTarget;
        // A warship that hit this ship on the way; fought before the flight continues.
        private IEntity _enRouteTarget;

        private readonly Func<IEntity, float> _rangeTo;
        private readonly Func<IEntity, bool> _isPreferred;
        private readonly Func<IEntity, bool> _isLeashed;
        private readonly Func<IEntity, bool> _isLeashedPreferred;
        private readonly Func<IEntity, int> _priority;
        private AttackMoveEngagement _nextEngagement;
        private AttackMoveEngagement _engagement;

        private Vector3 _destination;

        private int _member;
        private float _nextSearchTime;
        private float _nextEnRouteSearchTime;
        // A search found nothing since the last target was dropped; until then the order is not finished.
        private bool _hasSearched;
        // Set once the ship reaches the destination or the group detects an enemy in its area; until then it claims no enemies.
        private bool _hasArrived;
        // A target was lost: return to the destination unless the next search finds another.
        private bool _isReturnPending;
        // Holding on the current target; enables the leave margin.
        private bool _isHolding;

        public bool IsComplete => _engagementTarget == null && _enRouteTarget == null && _hasSearched &&
                                  !_movement.IsMoving;
        public bool IsEngaging => _engagementTarget != null || _enRouteTarget != null;
        /// <summary>Stopped on the way to fight a warship that hit it; the flight resumes once that fight ends.</summary>
        public bool IsEngagingEnRoute => _enRouteTarget != null;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal IEntity EngagementTarget => _engagementTarget ?? _enRouteTarget;
#endif

        public AttackMoveState(IShipMovement movement, IWeaponComponent weapon,
            IRadarComponent radar, IAttackDataFactory attackDataFactory, IShipThreatSense threats)
        {
            _movement = movement;
            _weapon = weapon;
            _radar = radar;
            _attackDataFactory = attackDataFactory;
            _threats = threats;
            _rangeTo = enemy => _movement.GetRange(enemy.GetFacade<IEntityTransformFacade>().Transform.position);
            _isPreferred = enemy => ShipEngagement.IsPreferred(enemy, _weapon);
            _isLeashed = enemy => ShipEngagement.IsInReachFrom(_destination, _movement, _weapon, enemy);
            _isLeashedPreferred = enemy => _isLeashed(enemy) && _isPreferred(enemy);
            _priority = enemy => (ShipEngagement.CanEngage(enemy, _movement, _weapon) ? IN_REACH_PRIORITY : 0) +
                                 ShipEngagement.ThreatRank(enemy, _threats);
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
            _enRouteTarget = null;
            _hasArrived = false;
            _isReturnPending = false;
            _isHolding = false;
            _nextEnRouteSearchTime = float.NegativeInfinity;
            _member = _engagement.Join();
            _movement.MoveToPosition(_destination);
            DelaySearch();
        }

        public void Tick(float deltaTime)
        {
            _engagement.Report(_member, _radar.Enemies);
            if (!_hasArrived)
            {
                if (!IsAreaContactFound() && TickEnRoute()) return;
                _hasArrived = true;
            }

            if (_engagementTarget != null &&
                (!_engagement.Contains(_engagementTarget) || !_isLeashed(_engagementTarget)))
            {
                // Hold course until the next search: heading back first and then turning for the next target
                // made the ship zigzag between the point and the fight.
                _weapon.ResetTarget();
                _engagementTarget = null;
                _isHolding = false;
                _isReturnPending = true;
                DelaySearch();
            }

            // A held target the loadout is not built for gives way once a preferred enemy is in the set.
            bool isSearching = _engagementTarget == null || !_isPreferred(_engagementTarget);
            if (isSearching && Time.time >= _nextSearchTime)
            {
                _nextSearchTime = Time.time + SEARCH_INTERVAL;
                _hasSearched = true;
                if (_engagementTarget == null || _engagement.HasPreferred(_isLeashedPreferred))
                {
                    IEntity selectedTarget = _engagement.SelectTarget(_member, _rangeTo, _isPreferred, _isLeashed,
                        _priority);
                    if (selectedTarget != _engagementTarget)
                    {
                        _movement.CancelCombatHold();
                        _isHolding = false;
                    }
                    _engagementTarget = selectedTarget;
                    if (_engagementTarget != null)
                    {
                        _isReturnPending = false;
                        _weapon.AddTarget(_attackDataFactory.ConstructData(_engagementTarget), AttackType.MainTarget);
                        // A new target always breaks off the course to the attack-move point.
                        if (!ShipEngagement.CanEngage(_engagementTarget, _movement, _weapon))
                            ShipEngagement.Pursue(_movement, _weapon, _engagementTarget, newTarget: true);
                    }
                }
            }

            if (_engagementTarget == null)
            {
                if (_isReturnPending && _hasSearched)
                {
                    _isReturnPending = false;
                    // A ship already on the point stays put: a re-plan this short only loops around it.
                    if (_movement.GetRange(_destination) > _movement.NavigationRadius)
                        _movement.MoveToPosition(_destination);
                }
            }
            else
            {
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
        }

        // The point marks an area, not a stop: once the group detects an enemy inside it, the ship stops flying to the
        // point and engages from firing range. Not while it fights a warship that hit it on the way.
        private bool IsAreaContactFound() =>
            _enRouteTarget == null && Time.time >= _nextSearchTime && _engagement.HasPreferred(_isLeashed);

        /// <summary>
        /// Flight to the point: true while the ship is still on its way (flying or fighting an attacker), false once
        /// it has arrived.
        /// </summary>
        private bool TickEnRoute()
        {
            if (_enRouteTarget != null)
            {
                if (!_enRouteTarget.IsCloaked() &&
                    ShipEngagement.CanEngage(_enRouteTarget, _movement, _weapon, isHolding: true))
                {
                    _movement.HoldPosition(_enRouteTarget);
                    _movement.LookAtTarget(_enRouteTarget);
                    return true;
                }

                // Look for the next attacker before turning back onto the course.
                IEntity next = _threats.WasDamagedRecently ? FindEnRouteAttacker() : null;
                if (next != null)
                {
                    BeginEnRouteFight(next);
                    return true;
                }

                _enRouteTarget = null;
                _weapon.ResetTarget();
                _movement.CancelCombatHold();
                if (_movement.GetRange(_destination) <= _movement.NavigationRadius) return false;
                _movement.MoveToPosition(_destination);
                return true;
            }

            if (!_movement.IsMoving) return false;
            if (!_threats.WasDamagedRecently || Time.time < _nextEnRouteSearchTime) return true;
            _nextEnRouteSearchTime = Time.time + SEARCH_INTERVAL;
            IEntity attacker = FindEnRouteAttacker();
            if (attacker != null) BeginEnRouteFight(attacker);
            return true;
        }

        // The biggest, then nearest, warship on own radar that attacks this ship from inside its weapon reach.
        private IEntity FindEnRouteAttacker()
        {
            IEntity best = null;
            int bestTier = -1;
            float bestDistance = float.PositiveInfinity;
            // Indexed: ObservableList boxes its enumerator.
            for (int i = 0; i < _radar.Enemies.Count; i++)
            {
                IEntity enemy = _radar.Enemies[i];
                if (enemy.IsCloaked() || !ShipEngagement.IsWarship(enemy) ||
                    !ShipEngagement.CanEngage(enemy, _movement, _weapon) || !_threats.IsAttackingSelf(enemy)) continue;
                int tier = enemy.HealthModel.ShipClass.HullTier();
                float distance = _rangeTo(enemy);
                if (tier < bestTier || tier == bestTier && distance >= bestDistance) continue;
                best = enemy;
                bestTier = tier;
                bestDistance = distance;
            }

            return best;
        }

        private void BeginEnRouteFight(IEntity attacker)
        {
            _enRouteTarget = attacker;
            _weapon.AddTarget(_attackDataFactory.ConstructData(attacker), AttackType.MainTarget);
            _movement.HoldPosition(attacker);
            _movement.LookAtTarget(attacker);
        }

        private void DelaySearch()
        {
            _hasSearched = false;
            _nextSearchTime = Time.time + UnityEngine.Random.value * SEARCH_INTERVAL;
        }

        public void Exit()
        {
            _movement.CancelCombatHold();
            _engagement.Leave(_member);
            _engagementTarget = null;
            _enRouteTarget = null;
            _isHolding = false;
            _weapon.ResetTarget();
        }
    }
}
