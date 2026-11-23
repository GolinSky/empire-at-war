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
    /// Flies to the destination without breaking off for fights on the way (weapons still fire at what passes in
    /// range), then fights the enemies the arrived attack-move group detects within the ship's reach of the
    /// destination; it never chases beyond that leash.
    /// Targets come from the shared <see cref="AttackMoveEngagement"/> so the group spreads its fire. A ship that
    /// loses its target looks for the next one first and returns to the destination only when none is left.
    /// </summary>
    public sealed class AttackMoveState : IBaseState
    {
        // Seconds between target searches while the ship has no target, with a random first delay. A whole
        // attack-move group otherwise searches, claims and arms its weapons on the order's frame (~28 ms for
        // 178 ships) and again on the frame their shared targets die.
        private const float SEARCH_INTERVAL = 0.25f;

        private readonly IShipMovement _movement;
        private readonly IWeaponComponent _weapon;
        private readonly IRadarComponent _radar;
        private readonly IAttackDataFactory _attackDataFactory;
        private IEntity _engagementTarget;

        private readonly Func<IEntity, float> _rangeTo;
        private readonly Func<IEntity, bool> _isPreferred;
        private readonly Func<IEntity, bool> _isLeashed;
        private readonly Func<IEntity, bool> _isLeashedPreferred;
        private AttackMoveEngagement _nextEngagement;
        private AttackMoveEngagement _engagement;

        private Vector3 _destination;

        private int _member;
        private float _nextSearchTime;
        // A search found nothing since the last target was dropped; until then the order is not finished.
        private bool _hasSearched;
        // Set once the ship first reaches the destination; until then it neither reports nor claims enemies.
        private bool _hasArrived;
        // A target was lost: return to the destination unless the next search finds another.
        private bool _isReturnPending;

        public bool IsComplete => _engagementTarget == null && _hasSearched && !_movement.IsMoving;
        public bool IsEngaging => _engagementTarget != null;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal IEntity EngagementTarget => _engagementTarget;
#endif

        public AttackMoveState(IShipMovement movement, IWeaponComponent weapon,
            IRadarComponent radar, IAttackDataFactory attackDataFactory)
        {
            _movement = movement;
            _weapon = weapon;
            _radar = radar;
            _attackDataFactory = attackDataFactory;
            _rangeTo = enemy => _movement.GetRange(enemy.GetFacade<IEntityTransformFacade>().Transform.position);
            _isPreferred = enemy => ShipEngagement.IsPreferred(enemy, _weapon);
            _isLeashed = enemy => ShipEngagement.IsInReachFrom(_destination, _movement, _weapon, enemy);
            _isLeashedPreferred = enemy => _isLeashed(enemy) && _isPreferred(enemy);
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
            _hasArrived = false;
            _isReturnPending = false;
            _member = _engagement.Join();
            _movement.MoveToPosition(_destination);
            DelaySearch();
        }

        public void Tick(float deltaTime)
        {
            if (!_hasArrived)
            {
                if (_movement.IsMoving) return;
                _hasArrived = true;
            }

            _engagement.Report(_member, _radar.Enemies);
            if (_engagementTarget != null &&
                (!_engagement.Contains(_engagementTarget) || !_isLeashed(_engagementTarget)))
            {
                // Hold course until the next search: heading back first and then turning for the next target
                // made the ship zigzag between the point and the fight.
                _weapon.ResetTarget();
                _engagementTarget = null;
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
                    IEntity selectedTarget = _engagement.SelectTarget(_member, _rangeTo, _isPreferred, _isLeashed);
                    if (selectedTarget != _engagementTarget) _movement.CancelCombatHold();
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
                if (ShipEngagement.CanEngage(_engagementTarget, _movement, _weapon))
                {
                    _movement.HoldPosition(_engagementTarget);
                    _movement.LookAtTarget(_engagementTarget);
                }
                else ShipEngagement.Pursue(_movement, _weapon, _engagementTarget);
            }
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
            _weapon.ResetTarget();
        }
    }
}
