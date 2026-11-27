using System;
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
    /// The ship flies to its slot in the group's formation (the destination) and never leaves it to chase. Its guns
    /// fire at whatever they reach on the way and on the slot, spread over the enemies
    /// (<see cref="IWeaponComponent.SetSpreadFire"/>): no enemy is made the main target. Once stopped on the slot
    /// it holds there and turns toward the enemy the shared <see cref="AttackMoveEngagement"/> gives it: the
    /// loadout's preferred enemies first, then enemies in weapon reach, then threat, then the one fewest of the
    /// group face. The order ends once the group reports no enemy.
    /// </summary>
    public sealed class AttackMoveState : IBaseState
    {
        // Seconds between facing-target searches, with a random first delay. A whole attack-move group otherwise
        // searches and claims on the order's frame (~28 ms for 178 ships) and again when their shared targets die.
        private const float SEARCH_INTERVAL = 0.25f;
        // Facing priority of an enemy already in weapon reach; above every threat rank.
        private const int IN_REACH_PRIORITY = 3;

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
        private int _member;
        private float _nextSearchTime;
        // A search ran since the ship stopped on its slot; until then the order is not finished.
        private bool _hasSearched;
        private bool _hasArrived;

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
            _hasSearched = false;
            _member = _engagement.Join();
            _weapon.SetSpreadFire(true);
            // A ship stopped on its slot stays put: a re-plan this short only loops around it.
            if (_movement.IsMoving || _movement.GetRange(_destination) > _movement.NavigationRadius)
                _movement.MoveToPosition(_destination);
            _nextSearchTime = Time.time + UnityEngine.Random.value * SEARCH_INTERVAL;
        }

        public void Tick(float deltaTime)
        {
            _engagement.Report(_member, _radar.Enemies);
            if (!_hasArrived)
            {
                // Flying with the group: guns fire on the way, the course holds.
                if (_movement.IsMoving) return;
                _hasArrived = true;
            }

            if (Time.time >= _nextSearchTime)
            {
                _nextSearchTime = Time.time + SEARCH_INTERVAL;
                _hasSearched = true;
                IEntity next = _engagement.SelectTarget(_member, _rangeTo, _isPreferred, priority: _priority);
                if (next == null && _facingTarget != null) _movement.CancelCombatHold();
                _facingTarget = next;
            }

            if (_facingTarget == null) return;
            _movement.HoldPosition(_facingTarget);
            _movement.LookAtTarget(_facingTarget);
        }

        public void Exit()
        {
            _movement.CancelCombatHold();
            _engagement.Leave(_member);
            _facingTarget = null;
            _weapon.SetSpreadFire(false);
        }
    }
}
