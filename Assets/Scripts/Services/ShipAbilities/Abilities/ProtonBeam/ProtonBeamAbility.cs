using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Weapon;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    /// <summary>Holds a beam on the target and spreads the damage evenly over the ability's duration.</summary>
    public sealed class ProtonBeamAbility : IPhasedShipAbility
    {
        private const float DAMAGE_TICK_INTERVAL = 0.2f;

        private readonly ProtonBeamSettings _settings;
        private readonly ImpactEffectPresenter _impactPresenter;
        // Reuses the Laser Beam hardpoint's prefab, color and width.
        private readonly WeaponProfile _beamProfile;
        private BeamShot _view;
        private IShipAbilityFacade _caster;
        private IEntity _target;
        private IHealthFacade _health;

        private float _damagePerSecond;
        private float _timeLeft;
        private float _pendingTime;

        public bool IsComplete => _timeLeft <= 0f || _target.HealthModel.IsDestroyed || _caster.Modifiers.IsIonDisabled;
        public bool SurvivesCasterDeath => false;

        public ProtonBeamAbility(ProtonBeamSettings settings,
            ImpactEffectPresenter impactPresenter, WeaponsData weaponsData)
        {
            _settings = settings;
            _impactPresenter = impactPresenter;
            _beamProfile = weaponsData.GetProfile(WeaponType.LaserBeam);
        }

        public bool CanStart(IShipAbilityFacade caster, IEntity target) => true;

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            if (!target.TryGetFacade(out _health))
                throw new InvalidOperationException($"{nameof(ProtonBeamAbility)} requires an {nameof(IHealthFacade)} on the target.");

            _caster = caster;
            _target = target;
            _timeLeft = definition.Duration;
            _damagePerSecond = _settings.Damage / definition.Duration;
            _pendingTime = 0f;

            _view = (BeamShot)Object.Instantiate(_beamProfile.ShotPrefab);
            _view.PrepareImpact(_impactPresenter, target.HealthModel, _settings.DamageType, _settings.Damage, 1.5f, true);
            _view.PlayBeam(caster.Entity.GetFacade<IEntityTransformFacade>().Transform, target.GetFacade<IEntityTransformFacade>().Transform, definition.Duration, _beamProfile);
        }

        public void Advance(float deltaTime)
        {
            // Same arithmetic as the slot timer, so the last tick lands on the frame the slot ends.
            float step = Math.Min(deltaTime, _timeLeft);
            _timeLeft = Math.Max(0f, _timeLeft - deltaTime);
            _pendingTime += step;
            if (_pendingTime < DAMAGE_TICK_INTERVAL && _timeLeft > 0f) return;

            ApplyDamage(_damagePerSecond * _pendingTime);
            _pendingTime = 0f;
        }

        private void ApplyDamage(float damage)
        {
            HardPointModel[] hardPoints = _target.HealthModel.HardPointModels;
            for (int i = 0; i < hardPoints.Length; i++)
            {
                if (hardPoints[i].IsDestroyed) continue;
                _health.ApplyDamage(damage, _settings.DamageType, i);
                break;
            }
        }

        public void Stop()
        {
            // Scene unload can destroy the view before the service stops the ability.
            if (_view != null)
            {
                _view.StopBeam();
                Object.Destroy(_view.gameObject);
            }
        }
    }
}
