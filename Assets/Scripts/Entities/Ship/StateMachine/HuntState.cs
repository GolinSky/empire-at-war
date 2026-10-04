using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Services.UnitOrders;
using UnityEngine;
using ViewComponents;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public sealed class HuntState : IBaseState
    {
        private readonly IShipMovement _movement;
        private readonly IWeaponComponent _weapon;
        private readonly IAttackDataFactory _attackDataFactory;
        private readonly IEntityLocator _locator;
        private readonly IFogOfWarSystem _fogOfWarSystem;
        private readonly IPlayerRelations _relations;
        private IEntity _target;

        private readonly UnitOrderSettings _settings;

        private readonly PlayerId _side;
        private Vector3 _pursuitDestination;

        private float _retargetTimer;

        private readonly bool _respectsFog;

        public bool IsComplete => _target == null;

        public HuntState(IShipMovement movement, IWeaponComponent weapon,
            IAttackDataFactory attackDataFactory, IEntityLocator locator,
            IFogOfWarSystem fogOfWarSystem, IPlayerRelations relations, ILocalPlayer localPlayer,
            UnitOrderSettings settings, PlayerId side)
        {
            _relations = relations;
            // Only the human's ships are limited to what the fog of war reveals.
            _respectsFog = localPlayer.IsLocal(side);
            _movement = movement;
            _weapon = weapon;
            _attackDataFactory = attackDataFactory;
            _locator = locator;
            _fogOfWarSystem = fogOfWarSystem;
            _settings = settings;
            _side = side;
        }

        public void Enter()
        {
            _retargetTimer = 0f;
            FindTarget();
        }

        public void Tick(float deltaTime)
        {
            _retargetTimer -= deltaTime;
            if (_retargetTimer <= 0f || _target == null ||
                _target.HealthModel.IsDestroyed)
            {
                FindTarget();
                _retargetTimer = _settings.HuntRetargetInterval;
            }

            if (_target == null) return;
            Vector3 target = _target.GetFacade<IEntityTransformFacade>().Transform.position;
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
                if (!_relations.IsHostile(_side, entity.Owner) || entity.HealthModel.IsDestroyed ||
                    !entity.HealthModel.HasUnits) continue;
                Vector3 position = entity.GetFacade<IEntityTransformFacade>().Transform.position;
                if (_respectsFog && _fogOfWarSystem.GetVisibilityAtPosition(position) < 0.5f)
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
                _pursuitDestination = _target.GetFacade<IEntityTransformFacade>().Transform.position;
                _movement.MoveToPosition(_pursuitDestination);
            }
        }
    }
}
