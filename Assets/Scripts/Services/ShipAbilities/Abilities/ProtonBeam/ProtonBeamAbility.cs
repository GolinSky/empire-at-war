using System;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Health;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class ProtonBeamAbility : IShipAbility
    {
        private readonly ProtonBeamSettings _settings;
        private ProtonBeamView _view;

        public ProtonBeamAbility(ProtonBeamSettings settings) { _settings = settings; }

        public void Start(IShipAbilityCommand caster, ShipAbilityDefinition definition, IEntity target)
        {
            if (!target.TryGetCommand(out IHealthCommand health))
                throw new InvalidOperationException($"{nameof(ProtonBeamAbility)} requires an {nameof(IHealthCommand)} on the target.");

            _view = Object.Instantiate(_settings.ViewPrefab);
            _view.Play(caster.WorldPosition, target.HealthModel.Transform, definition.Duration);
            HardPointModel[] hardPoints = target.HealthModel.HardPointModels;
            for (int i = 0; i < hardPoints.Length; i++)
            {
                if (hardPoints[i].IsDestroyed) continue;
                health.ApplyDamage(_settings.Damage, _settings.WeaponType, i);
                break;
            }
        }

        public void Stop()
        {
            // Scene unload can destroy the view before the service stops the ability.
            if (_view != null) Object.Destroy(_view.gameObject);
        }
    }
}
