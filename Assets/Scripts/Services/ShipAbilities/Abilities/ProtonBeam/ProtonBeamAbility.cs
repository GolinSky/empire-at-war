using System;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Weapon;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class ProtonBeamAbility : IShipAbility
    {
        private readonly ProtonBeamSettings _settings;
        private readonly ImpactEffectPresenter _impactPresenter;
        private BeamShot _view;

        public ProtonBeamAbility(ProtonBeamSettings settings,
            ImpactEffectPresenter impactPresenter)
        {
            _settings = settings;
            _impactPresenter = impactPresenter;
        }

        public void Start(IShipAbilityCommand caster, ShipAbilityDefinition definition, IEntity target)
        {
            if (!target.TryGetCommand(out IHealthCommand health))
                throw new InvalidOperationException($"{nameof(ProtonBeamAbility)} requires an {nameof(IHealthCommand)} on the target.");

            _view = Object.Instantiate(_settings.ViewPrefab);
            _view.PrepareImpact(_impactPresenter, target.HealthModel, _settings.DamageType, 1.5f, true);
            _view.PlayBeam(caster.Health.Transform, target.HealthModel.Transform, definition.Duration);
            HardPointModel[] hardPoints = target.HealthModel.HardPointModels;
            for (int i = 0; i < hardPoints.Length; i++)
            {
                if (hardPoints[i].IsDestroyed) continue;
                health.ApplyDamage(_settings.Damage, _settings.DamageType, i);
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
