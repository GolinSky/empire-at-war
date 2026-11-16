using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public abstract class SustainedBeamAbility : IAdvancingShipAbility, ITargetedShipAbility, IReplicaPersistentShipAbility
    {
        private const float DAMAGE_TICK_INTERVAL = 0.2f;
        private readonly ImpactEffectPresenter _impactPresenter;
        private readonly WeaponProfile _profile;
        private readonly DamageType _damageType;
        private readonly float _damage;
        private readonly IVisionService _vision;
        private readonly IMatchSession _session;
        private BeamShot _view;
        private IShipAbilityFacade _caster;
        private IEntity _target;
        public IEntity Target => _target;
        private IHealthFacade _health;
        private float _damagePerSecond;
        private float _timeLeft;
        private float _pendingTime;

        protected SustainedBeamAbility(ImpactEffectPresenter impactPresenter, WeaponProfile profile,
            DamageType damageType, float damage, IVisionService vision, IMatchSession session)
        {
            _impactPresenter = impactPresenter;
            _profile = profile;
            _damageType = damageType;
            _damage = damage;
            _vision = vision;
            _session = session;
        }

        public bool IsComplete => _timeLeft <= 0f || _target.HealthModel.IsDestroyed ||
            _target.IsCloaked() || _caster.Modifiers.AbilitiesSuppressed;

        protected abstract Transform GetMuzzle(IShipAbilityFacade caster);

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            _caster = caster;
            _target = target;
            _health = target.GetFacade<IHealthFacade>();
            _timeLeft = definition.Duration;
            _damagePerSecond = _damage / definition.Duration;
            _pendingTime = 0f;
            _view = Object.Instantiate(_profile.BeamPrefab);
            _view.PrepareImpact(_impactPresenter, target.HealthModel, _damageType, _damage, 1.5f, true);
            _view.PlayBeam(GetMuzzle(caster), target.GetFacade<IEntityTransformFacade>().Transform,
                definition.Duration, _profile);
            if (_session.IsAuthority)
                _vision.Reveal(target.Owner, caster.Entity.Id, _vision.FiringRevealSeconds);
        }

        public void Advance(float deltaTime)
        {
            _pendingTime += Math.Min(deltaTime, _timeLeft);
            _timeLeft = Math.Max(0f, _timeLeft - deltaTime);
            if (_pendingTime < DAMAGE_TICK_INTERVAL && _timeLeft > 0f) return;

            // The first live hardpoint takes the hit. Index zero routes it to the hull on a wreck.
            IReadOnlyList<IReadOnlyHardPointModel> hardPoints = _target.HealthModel.HardPointModels;
            int hardPointId = 0;
            for (int i = 0; i < hardPoints.Count; i++)
            {
                if (hardPoints[i].IsDestroyed) continue;
                hardPointId = i;
                break;
            }
            _health.ApplyDamage(_damagePerSecond * _pendingTime * _caster.Modifiers.GetDamageMultiplier(_target),
                _damageType, hardPointId);
            if (_session.IsAuthority && _pendingTime > 0f)
                _vision.Reveal(_target.Owner, _caster.Entity.Id, _vision.FiringRevealSeconds);
            _pendingTime = 0f;
        }

        public void Stop()
        {
            if (_view == null) return;
            _view.StopBeam();
            Object.Destroy(_view.gameObject);
        }
    }
}
