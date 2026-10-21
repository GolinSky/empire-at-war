using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Services.Vision;
using UnityEngine;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public sealed class HuntState : IBaseState
    {
        private readonly IShipMovement _movement;
        private readonly IWeaponComponent _weapon;
        private readonly IAttackDataFactory _attackDataFactory;
        private readonly IEntityLocator _locator;
        private readonly IPlayerRelations _relations;
        private IEntity _target;

        private readonly UnitOrderSettings _settings;

        private readonly PlayerId _side;

        private float _retargetTimer;

        private readonly IVisionService _visionService;

        public bool IsComplete => false;

        public HuntState(IShipMovement movement, IWeaponComponent weapon,
            IAttackDataFactory attackDataFactory, IEntityLocator locator,
            IPlayerRelations relations, IVisionService visionService,
            UnitOrderSettings settings, PlayerId side)
        {
            _relations = relations;
            _visionService = visionService;
            _movement = movement;
            _weapon = weapon;
            _attackDataFactory = attackDataFactory;
            _locator = locator;
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
            if (_retargetTimer <= 0f || _target != null &&
                (_target.HealthModel.IsDestroyed || _target.IsCloaked() ||
                 !_visionService.IsVisible(_side, _target.GetFacade<IEntityTransformFacade>().Transform.position)))
            {
                FindTarget();
                _retargetTimer = _settings.HuntRetargetInterval;
            }

            if (_target == null)
            {
                if (_movement.IsMoving) _movement.Stop();
                return;
            }
            Vector3 target = _target.GetFacade<IEntityTransformFacade>().Transform.position;
            if (ShipEngagement.CanEngage(_target, _movement, _weapon))
            {
                if (_movement.IsMoving) _movement.Stop();
                _movement.LookAtTarget(target);
            }
            else ShipEngagement.Pursue(_movement, _weapon, _target);
        }

        public void Exit()
        {
            _target = null;
            _weapon.ResetTarget();
        }

        private void FindTarget()
        {
            // The closest preferred enemy; the closest other one only when no preferred enemy is left.
            IEntity closest = null;
            bool hasPreferred = false;
            float nearest = float.PositiveInfinity;
            foreach (IEntity entity in _locator.Entities)
            {
                if (!_relations.IsHostile(_side, entity.Owner) || entity.HealthModel.IsDestroyed ||
                    !entity.HealthModel.HasUnits || entity.IsCloaked()) continue;
                bool isPreferred = ShipEngagement.IsPreferred(entity, _weapon);
                if (hasPreferred && !isPreferred) continue;
                Vector3 position = entity.GetFacade<IEntityTransformFacade>().Transform.position;
                if (!_visionService.IsVisible(_side, position)) continue;
                float distance = (position - _movement.CurrentPosition).sqrMagnitude;
                if (distance >= nearest && (hasPreferred || !isPreferred)) continue;
                hasPreferred = isPreferred;
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
                if (!ShipEngagement.CanEngage(_target, _movement, _weapon))
                    ShipEngagement.Pursue(_movement, _weapon, _target, newTarget: true);
            }
        }
    }
}
