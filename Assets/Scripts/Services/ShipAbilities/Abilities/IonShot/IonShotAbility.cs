using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class IonShotAbility : IPhasedShipAbility
    {
        private readonly IonShotSettings _settings;
        private readonly CombatStatModifier _disable = new CombatStatModifier(1f, 1f, 0f, 1f, 1f);
        private IEntity _target;
        private CombatModifiers _targetModifiers;
        private float _remaining;
        private bool _hasHit;
        private bool _disabled;
        private bool _complete;

        public bool IsComplete => _complete;
        public bool SurvivesCasterDeath => true;

        public IonShotAbility(IonShotSettings settings) => _settings = settings;

        public bool CanStart(IShipAbilityFacade caster, IEntity target)
        {
            if (!target.TryGetFacade(out ICombatModifiersFacade combat)) return false;
            foreach (IHardPointModel member in caster.Health.GetShipUnits(HardPointType.Any))
                if (!member.IsDestroyed) return true;
            return false;
        }

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            _target = target;
            _targetModifiers = target.GetFacade<ICombatModifiersFacade>().Modifiers;
            IHardPointModel targetUnit = target.HealthModel.GetShipUnits(HardPointType.Any)[0];
            foreach (IHardPointModel member in caster.Health.GetShipUnits(HardPointType.Any))
            {
                if (member.IsDestroyed) continue;
                ShotEffect shot = Object.Instantiate(_settings.Projectile.ShotPrefab);
                float travelTime = shot.Fire(member.Transform, targetUnit.Transform, targetUnit.Pivot,
                    Vector3.zero, _settings.Projectile);
                shot.RetireAfterCompletion();
                _remaining = Mathf.Max(_remaining, travelTime);
            }
        }

        public void Advance(float deltaTime)
        {
            if (_complete) return;
            if (_target.HealthModel.IsDestroyed)
            {
                _complete = true;
                return;
            }

            _remaining -= deltaTime;
            if (_remaining > 0f) return;
            if (_hasHit)
            {
                _complete = true;
                return;
            }

            _hasHit = true;
            _targetModifiers.Add(_disable);
            _targetModifiers.AddIonPulseDisable();
            _disabled = true;
            _remaining += _settings.DisableDuration;
        }

        public void Stop()
        {
            if (!_disabled) return;
            _targetModifiers.Remove(_disable);
            _targetModifiers.RemoveIonPulseDisable();
            _disabled = false;
        }
    }
}
