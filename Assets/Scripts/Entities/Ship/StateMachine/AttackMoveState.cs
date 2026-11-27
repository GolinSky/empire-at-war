using System;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Utils;
using UnityEngine;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    /// <summary>
    /// The destination marks the enemy area, not a place to fly into. The ship flies toward its slot in the group's
    /// formation and stops as soon as an enemy (not strikecraft) is in its own weapon reach; it fights from there and
    /// never chases. Front rows stop first, rear rows close until they reach range. When nothing stays in reach while
    /// the group still sees enemies, it flies on toward the slot. Its guns are spread over the enemies
    /// (<see cref="IWeaponComponent.SetSpreadFire"/>): no enemy is made the main target. Stopped, it holds and turns
    /// toward the enemy the shared <see cref="AttackMoveEngagement"/> gives it: the loadout's preferred enemies first,
    /// then enemies in weapon reach, then threat, then the one fewest of the group face. The order ends once the
    /// group reports no enemy.
    /// </summary>
    public sealed class AttackMoveState : IBaseState
    {
        // Seconds between facing-target searches, with a random first delay. A whole attack-move group otherwise
        // searches and claims on the order's frame (~28 ms for 178 ships) and again when their shared targets die.
        private const float SEARCH_INTERVAL = 0.25f;
        // Facing priority of an enemy already in weapon reach; above every threat rank.
        private const int IN_REACH_PRIORITY = 3;
        // A stop within this many navigation radii of the slot is the arrival: the planner moves an occupied slot
        // up to two candidate rings (2 radii apart) away. Farther, the plan failed or fell back near the start
        // (battle 2026-11-27: a VenatorII held 1,292 away, a fresh Rothana 894 away).
        private const float ARRIVAL_RADIUS_FACTOR = 5f;
        // Seconds between new tries to reach a slot the last plan did not reach.
        private const float MOVE_RETRY_INTERVAL = 2f;

        private readonly IShipMovement _movement;
        private readonly IWeaponComponent _weapon;
        private readonly IRadarComponent _radar;
        private readonly Func<IEntity, float> _rangeTo;
        private readonly Func<IEntity, bool> _isPreferred;
        private readonly Func<IEntity, int> _priority;
        private AttackMoveEngagement _nextEngagement;
        private AttackMoveEngagement _engagement;
        // The enemy the ship turns to on its slot; its guns pick their own targets.
        private IEntity _facingTarget;

        private Vector3 _destination;
        // Where the ship heads now: its slot, moved back when a group member ahead stopped short.
        private Vector3 _goal;
        private int _member;
        private float _nextSearchTime;
        private float _nextMoveRetryTime;
        // A search ran since the ship stopped on its slot; until then the order is not finished.
        private bool _hasSearched;
        private bool _hasArrived;
        // Stopped before the slot because an enemy came into reach; flies on once none stays in reach.
        private bool _isStoppedShort;

        public bool IsComplete => _hasArrived && _hasSearched && _facingTarget == null && !_movement.IsMoving;
        public bool IsEngaging => _facingTarget != null;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal IEntity EngagementTarget => _facingTarget;
#endif

        public AttackMoveState(IShipMovement movement, IWeaponComponent weapon, IRadarComponent radar,
            IShipThreatSense threats)
        {
            _movement = movement;
            _weapon = weapon;
            _radar = radar;
            _rangeTo = enemy => _movement.GetRange(enemy.GetFacade<IEntityTransformFacade>().Transform.position);
            _isPreferred = enemy => ShipEngagement.IsPreferred(enemy, _weapon);
            _priority = enemy => (ShipEngagement.CanEngage(enemy, _movement, _weapon) ? IN_REACH_PRIORITY : 0) +
                                 ShipEngagement.ThreatRank(enemy, threats);
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
            _facingTarget = null;
            _hasArrived = false;
            _isStoppedShort = false;
            _hasSearched = false;
            _member = _engagement.Join();
            _weapon.SetSpreadFire(true);
            _goal = GetGoal();
            if (HasEnemyInHeldReach())
            {
                // Already in reach of an enemy: it fights from here, even when its slot is behind it (a new order
                // turned whole groups around to fly back to their rows). No pull-back: it may stand past its slot.
                if (_movement.IsMoving) _movement.Stop();
                _hasArrived = true;
                _isStoppedShort = !IsOnGoal();
            }
            // A ship stopped on its slot stays put: a re-plan this short only loops around it.
            else if (_movement.IsMoving || _movement.GetRange(_goal) > _movement.NavigationRadius)
                _movement.MoveToPosition(_goal);
            _nextMoveRetryTime = Time.time + MOVE_RETRY_INTERVAL;
            _nextSearchTime = Time.time + UnityEngine.Random.value * SEARCH_INTERVAL;
        }

        public void Tick(float deltaTime)
        {
            _engagement.Report(_member, _radar.Enemies);
            bool isSearchDue = Time.time >= _nextSearchTime;
            if (!_isStoppedShort && isSearchDue && FollowPullBack()) return;
            if (!_hasArrived)
            {
                if (_movement.IsMoving)
                {
                    // Flying with the group until an enemy is in this ship's own firing range: it fights from there.
                    if (!isSearchDue || !HasEnemyInRange()) return;
                    _movement.Stop();
                    StopShort();
                }
                else if (IsOnGoal()) _hasArrived = true;
                else if (HasEnemyInRange()) StopShort();
                else if (Time.time >= _nextMoveRetryTime)
                {
                    // Stopped short of the slot: try again instead of holding where the failed plan left it.
                    _nextMoveRetryTime = Time.time + MOVE_RETRY_INTERVAL;
                    _movement.MoveToPosition(_goal);
                    return;
                }
            }
            else if (_isStoppedShort && isSearchDue && !HasEnemyInHeldReach())
            {
                // The enemies in range are gone or out of it: close in toward the slot again, never past it.
                _isStoppedShort = false;
                _engagement.ClearPullBack(_member);
                _goal = GetGoal();
                if (!IsOnGoal())
                {
                    _hasArrived = false;
                    _facingTarget = null;
                    _movement.MoveToPosition(_goal);
                    return;
                }
            }

            if (isSearchDue)
            {
                _nextSearchTime = Time.time + SEARCH_INTERVAL;
                _hasSearched = true;
                IEntity next = _engagement.SelectTarget(_member, _rangeTo, _isPreferred, priority: _priority);
                if (next == null && _facingTarget != null) _movement.CancelCombatHold();
                _facingTarget = next;
            }

            if (_facingTarget == null) return;
            // Short of the slot the ship only turns: a hold would stop the next try.
            if (_hasArrived) _movement.HoldPosition(_facingTarget);
            // Guns aim the hull only at an enemy they reach; one beyond reach is faced bow-on, so a waiting
            // formation does not swing broadside to targets it cannot hit.
            if (ShipEngagement.CanEngage(_facingTarget, _movement, _weapon, isHolding: true))
                _movement.LookAtTarget(_facingTarget);
            else
                _movement.LookInDirection(
                    _facingTarget.GetFacade<IEntityTransformFacade>().Transform.position - _movement.CurrentPosition);
        }

        private void StopShort()
        {
            _hasArrived = true;
            _isStoppedShort = true;
            Vector3 position = _movement.CurrentPosition;
            _engagement.SetPullBack(_member, _destination.x, _destination.z, position.y,
                _destination.x - position.x, _destination.z - position.z);
        }

        // A ship ahead stopped short: this slot moves back by the same amount, so rows keep their spacing.
        private bool FollowPullBack()
        {
            Vector3 goal = GetGoal();
            if (PlanarGeometry.Distance(goal, _goal) <= _movement.NavigationRadius) return false;
            _goal = goal;
            if (_hasArrived)
            {
                _hasArrived = false;
                _movement.CancelCombatHold();
            }

            _movement.MoveToPosition(_goal);
            return true;
        }

        private bool IsOnGoal() =>
            _movement.GetRange(_goal) <= _movement.NavigationRadius * ARRIVAL_RADIUS_FACTOR;

        private Vector3 GetGoal() =>
            _engagement.TryGetPullBack(_member, _destination.x, _destination.z, _movement.CurrentPosition.y,
                out float pullX, out float pullZ)
                ? new Vector3(_destination.x - pullX, _destination.y, _destination.z - pullZ)
                : _destination;

        // An enemy (not strikecraft) inside firing range with the main batteries reaching it bow-on: most guns fire.
        private bool HasEnemyInRange()
        {
            var enemies = _radar.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                IEntity enemy = enemies[i];
                // Strikecraft swarm everywhere; they never stop a warship's approach.
                if (enemy.HealthModel.ShipClass.IsStrikecraft() || enemy.HealthModel.IsDestroyed ||
                    !enemy.HealthModel.HasUnits) continue;
                if (ShipEngagement.IsInRange(_movement, _weapon, enemy,
                        enemy.GetFacade<IEntityTransformFacade>().Transform.position) &&
                    _weapon.MainBatteryReaches(enemy, ShipEngagement.ContactDistance(_movement, enemy), 1f, false))
                    return true;
            }

            return false;
        }

        // Reach with the leave margin: the ship keeps its stop while any enemy stays this close.
        private bool HasEnemyInHeldReach()
        {
            var enemies = _radar.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                IEntity enemy = enemies[i];
                if (!enemy.HealthModel.ShipClass.IsStrikecraft() &&
                    ShipEngagement.CanEngage(enemy, _movement, _weapon, isHolding: true)) return true;
            }

            return false;
        }

        public void Exit()
        {
            _movement.CancelCombatHold();
            _isStoppedShort = false;
            _engagement.Leave(_member);
            _facingTarget = null;
            _weapon.SetSpreadFire(false);
        }
    }
}
