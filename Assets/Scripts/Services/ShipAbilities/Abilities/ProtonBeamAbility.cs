using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class ProtonBeamAbility : IShipAbility
    {
        private ProtonBeamView _view;

        public void Start(IShipAbilityCommand caster, ShipAbilityDefinition definition, IEntity target)
        {
            _view = Object.Instantiate(definition.BeamViewPrefab);
            _view.Play(caster.WorldPosition, target.HealthModel.Transform, definition.Duration);
            target.TryGetCommand(out IHealthCommand health);
            HardPointModel[] hardPoints = target.HealthModel.HardPointModels;
            for (int i = 0; i < hardPoints.Length; i++)
            {
                if (hardPoints[i].IsDestroyed) continue;
                health.ApplyDamage(definition.BeamDamage, definition.BeamWeaponType, i);
                break;
            }
        }

        public void Stop() => Object.Destroy(_view.gameObject);
    }
}
