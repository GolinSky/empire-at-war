using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Utils;
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
        private readonly IFocusFireClaim _focusFire;
        private IEntity _target;

        private readonly UnitOrderSettings _settings;

        private readonly PlayerId _side;

        private float _retargetTimer;
        private bool _isHolding;

        private readonly IVisionService _visionService;

        public bool IsComplete => false;

        public HuntState(IShipMovement movement, IWeaponComponent weapon,
            IAttackDataFactory attackDataFactory, IEntityLocator locator,
            IPlayerRelations relations, IVisionService visionService,
            UnitOrderSettings settings, PlayerId side, IFocusFireClaim focusFire)
        {
            _focusFire = focusFire;
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
            _isHolding = false;
            FindTarget();
        }

        public void Tick(float deltaTime)
        {
            _retargetTimer -= deltaTime;
            bool isLost = _target != null && (_target.HealthModel.IsDestroyed || _target.IsCloaked() ||
                                              !_visionService.IsVisible(_side, _target));
            // A target the guns still reach is kept; a nearer enemy is no reason to break off and turn.
            if (isLost || _retargetTimer <= 0f &&
                (_target == null || !ShipEngagement.CanEngage(_target, _movement, _weapon, _isHolding)))
            {
                FindTarget();
                _retargetTimer = _settings.HuntRetargetInterval;
            }
            else if (_retargetTimer <= 0f) _retargetTimer = _settings.HuntRetargetInterval;

            if (_target == null)
            {
                if (_movement.IsMoving) _movement.Stop();
                return;
            }
            if (ShipEngagement.CanHoldOn(_target, _movement, _weapon, _isHolding))
            {
                _isHolding = true;
                _movement.HoldPosition(_target);
                _movement.LookAtTarget(_target);
            }
            else
            {
                _isHolding = false;
                ShipEngagement.Pursue(_movement, _weapon, _target);
            }
        }

        public void Exit()
        {
            _movement.CancelCombatHold();
            _isHolding = false;
            _target = null;
            _focusFire.Release();
            _weapon.ResetTarget();
        }

        private void FindTarget()
        {
            // An enemy with a free focus fire slot first; then the closest preferred enemy, the closest other one only
            // when no preferred enemy is left.
            IEntity closest = null;
            bool hasOpen = false;
            bool hasPreferred = false;
            float nearest = float.PositiveInfinity;
            foreach (IEntity entity in _locator.Entities)
            {
                if (!_relations.IsHostile(_side, entity.Owner) || entity.HealthModel.IsDestroyed ||
                    !entity.HealthModel.HasUnits || entity.IsCloaked()) continue;
                if (!_visionService.IsVisible(_side, entity)) continue;
                bool isOpen = _focusFire.HasSlot(entity);
                if (hasOpen && !isOpen) continue;
                bool isPreferred = ShipEngagement.IsPreferred(entity, _weapon);
                bool isBetterTier = isOpen && !hasOpen || isPreferred && !hasPreferred;
                if (!isBetterTier && hasPreferred && !isPreferred) continue;
                Vector3 position = entity.GetFacade<IEntityTransformFacade>().Transform.position;
                float distance = PlanarGeometry.DistanceSquared(position, _movement.CurrentPosition);
                if (!isBetterTier && distance >= nearest) continue;
                hasOpen = isOpen;
                hasPreferred = isPreferred;
                nearest = distance;
                closest = entity;
            }

            if (closest == _target) return;
            _movement.CancelCombatHold();
            _isHolding = false;
            _target = closest;
            _weapon.ResetTarget();
            if (_target == null) _focusFire.Release();
            else _focusFire.TryClaim(_target);
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
