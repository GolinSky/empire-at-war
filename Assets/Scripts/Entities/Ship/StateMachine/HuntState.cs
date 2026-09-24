using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Services.UnitOrders;
using UnityEngine;
using ViewComponents;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public sealed class HuntState : IBaseState
    {
        private readonly IShipMoveComponent _movement;
        private readonly IWeaponComponent _weapon;
        private readonly IAttackDataFactory _attackDataFactory;
        private readonly IEntityLocator _locator;
        private readonly FogOfWarSystem _fog;
        private readonly UnitOrderSettings _settings;
        private readonly PlayerType _side;
        private IEntity _target;
        private float _retargetTimer;
        private Vector3 _pursuitDestination;

        public HuntState(IShipMoveComponent movement, IWeaponComponent weapon,
            IAttackDataFactory attackDataFactory, IEntityLocator locator,
            FogOfWarSystem fog, UnitOrderSettings settings, PlayerType side)
        {
            _movement = movement;
            _weapon = weapon;
            _attackDataFactory = attackDataFactory;
            _locator = locator;
            _fog = fog;
            _settings = settings;
            _side = side;
        }

        public bool IsComplete => _target == null;

        public void Enter()
        {
            _retargetTimer = 0f;
            FindTarget();
        }

        public void Update()
        {
            _retargetTimer -= Time.deltaTime;
            if (_retargetTimer <= 0f || _target == null ||
                _target.HealthModel.IsDestroyed)
            {
                FindTarget();
                _retargetTimer = _settings.HuntRetargetInterval;
            }

            if (_target == null) return;
            Vector3 target = _target.HealthModel.Transform.position;
            if (ShipEngagement.CanEngage(_target, _movement, _weapon))
            {
                if (_movement.IsMoving) _movement.Stop();
                _movement.LookAtTarget(target);
            }
            else _pursuitDestination = ShipEngagement.Pursue(_movement, _weapon, target,
                _pursuitDestination);
        }

        public void Exit()
        {
            _target = null;
            _weapon.ResetTarget();
        }

        private void FindTarget()
        {
            IEntity closest = null;
            float nearest = float.PositiveInfinity;
            foreach (IEntity entity in _locator.Entities)
            {
                if (entity.PlayerType == _side || entity.HealthModel.IsDestroyed ||
                    !entity.HealthModel.HasUnits) continue;
                Vector3 position = entity.HealthModel.Transform.position;
                if (_side == PlayerType.Player && _fog.GetVisibilityAtPosition(position) < 0.5f)
                    continue;
                float distance = (position - _movement.CurrentPosition).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance;
                closest = entity;
            }

            if (closest == _target) return;
            _target = closest;
            _weapon.ResetTarget();
            if (_target != null)
            {
                _weapon.AddTarget(_attackDataFactory.ConstructData(_target),
                    AttackType.MainTarget);
                _pursuitDestination = _target.HealthModel.Transform.position;
                _movement.MoveToPosition(_pursuitDestination);
            }
        }
    }
}
