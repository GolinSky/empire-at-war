using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Weapon;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class CompositeBeamAbility : IPhasedShipAbility
    {
        private const float DAMAGE_TICK_INTERVAL = 0.2f;

        private readonly CompositeBeamSettings _settings;
        private readonly ImpactEffectPresenter _impactPresenter;
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

        public CompositeBeamAbility(CompositeBeamSettings settings,
            ImpactEffectPresenter impactPresenter, WeaponsData weaponsData)
        {
            _settings = settings;
            _impactPresenter = impactPresenter;
            _beamProfile = weaponsData.GetProfile(WeaponType.CompositeBeam);
        }

        public bool CanStart(IShipAbilityFacade caster, IEntity target) =>
            target.HealthModel.ShipClass == ShipClass.Frigate ||
            target.HealthModel.ShipClass == ShipClass.Cruiser ||
            target.HealthModel.ShipClass == ShipClass.Capital ||
            target.HealthModel.ShipClass == ShipClass.HeavyCapital ||
            target.IsPlayerBase();

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            _health = target.GetFacade<IHealthFacade>();
            _caster = caster;
            _target = target;
            _timeLeft = definition.Duration;
            _damagePerSecond = _settings.Damage / definition.Duration;
            _pendingTime = 0f;
            _view = (BeamShot)Object.Instantiate(_beamProfile.ShotPrefab);
            _view.PrepareImpact(_impactPresenter, target.HealthModel, DamageType.CompositeBeam, _settings.Damage, 1.5f, true);
            _view.PlayBeam(caster.Entity.GetFacade<ICompositeBeamFacade>().Muzzle,
                target.GetFacade<IEntityTransformFacade>().Transform, definition.Duration, _beamProfile);
        }

        public void Advance(float deltaTime)
        {
            float step = Math.Min(deltaTime, _timeLeft);
            _timeLeft = Math.Max(0f, _timeLeft - deltaTime);
            _pendingTime += step;
            if (_pendingTime < DAMAGE_TICK_INTERVAL && _timeLeft > 0f) return;

            HardPointModel[] hardPoints = _target.HealthModel.HardPointModels;
            if (hardPoints.Length == 0)
                _health.ApplyDamage(_damagePerSecond * _pendingTime, DamageType.CompositeBeam, 0);
            for (int i = 0; i < hardPoints.Length; i++)
            {
                if (hardPoints[i].IsDestroyed) continue;
                _health.ApplyDamage(_damagePerSecond * _pendingTime, DamageType.CompositeBeam, i);
                break;
            }
            _pendingTime = 0f;
        }

        public void Stop()
        {
            if (_view != null)
            {
                _view.StopBeam();
                Object.Destroy(_view.gameObject);
            }
        }
    }
}
