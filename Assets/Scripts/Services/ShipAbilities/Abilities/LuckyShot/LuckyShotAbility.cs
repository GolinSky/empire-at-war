using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class LuckyShotAbility : IAdvancingShipAbility, ITargetedShipAbility,
        IShipAbilityInterruptionPolicy, IShipAbilityRemainingTime
    {
        private readonly LuckyShotSettings _settings;
        private readonly IEntityLocator _entities;
        private readonly BoltFlightBatch _boltFlights;
        private readonly ImpactEffectPresenter _impacts;
        private readonly IVisionService _vision;
        private readonly IMatchSession _session;
        private IShipAbilityFacade _caster;
        private IEntity _target;
        private IHardPointModel _leader;
        private IUnitOrderObserverFacade _orders;
        private ShotEffect _shot;
        private float _remaining;
        private bool _launched;
        private bool _hit;

        public IEntity Target => _target;
        public bool IsComplete { get; private set; }
        public bool SurvivesCasterDeath => _launched;
        public bool SurvivesHazardSuppression => _launched;
        public float RemainingTime
        {
            get
            {
                if (IsComplete || _target.HealthModel.IsDestroyed) return 0f;
                if (_launched) return Mathf.Max(0f, _remaining);
                Vector3 offset = _target.GetFacade<IEntityTransformFacade>().Transform.position - _leader.Position;
                return Mathf.Max(0f, _remaining) +
                    new Vector2(offset.x, offset.z).magnitude / _settings.Projectile.ProjectileSpeed;
            }
        }

        public LuckyShotAbility(LuckyShotSettings settings, IEntityLocator entities, BoltFlightBatch boltFlights,
            ImpactEffectPresenter impacts, IVisionService vision, IMatchSession session)
        {
            _settings = settings;
            _entities = entities;
            _boltFlights = boltFlights;
            _impacts = impacts;
            _vision = vision;
            _session = session;
        }

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            _caster = caster;
            _target = target;
            _leader = caster.Health.GetShipUnits(HardPointType.Any)[0];
            // A live host event can reach a replica after Luke's death was replicated.
            IsComplete = _leader.Id != 0 || _leader.IsDestroyed;
            if (IsComplete) return;
            _remaining = _settings.Windup;
            if (_session.IsAuthority)
            {
                _orders = caster.Entity.GetFacade<IUnitOrderObserverFacade>();
                if (_orders.AttackTarget != target)
                    caster.Entity.GetFacade<IAttackFacade>().Attack(target, Vector3.zero);
            }
        }

        public void Advance(float deltaTime)
        {
            if (IsComplete) return;
            if (!_entities.TryGetEntity(_target.Id, out IEntity current) || current != _target ||
                _target.HealthModel.IsDestroyed || (!_launched &&
                    (_target.IsCloaked() || _leader.IsDestroyed || _caster.Modifiers.AbilitiesSuppressed ||
                        (_session.IsAuthority && _orders.CurrentOrder != UnitOrderType.Attack))))
            {
                IsComplete = true;
                return;
            }

            _remaining -= deltaTime;
            if (_remaining > 0f) return;
            if (!_launched)
            {
                Transform target = _target.GetFacade<IEntityTransformFacade>().Transform;
                _shot = Object.Instantiate(_settings.Projectile.ShotPrefab);
                _shot.PrepareImpact(_impacts, _target.HealthModel, DamageType.PrecisionStrike,
                    _settings.Damage, _settings.Projectile.Size.x, true);
                _remaining = _shot.Fire(_leader.Transform, target, target, Vector3.zero,
                    _settings.Projectile, _boltFlights);
                _launched = true;
                if (_session.IsAuthority)
                    _vision.Reveal(_target.Owner, _caster.Entity.Id, _vision.FiringRevealSeconds);
                return;
            }

            if (_session.IsAuthority)
                _target.GetFacade<IHealthFacade>().ApplyDamage(_settings.Damage, DamageType.PrecisionStrike, 0);
            _hit = true;
            IsComplete = true;
        }

        public void Stop()
        {
            IsComplete = true;
            if (_shot == null) return;
            if (_hit) _shot.RetireAfterCompletion();
            else _shot.CancelAndDestroy();
            _shot = null;
        }
    }
}
