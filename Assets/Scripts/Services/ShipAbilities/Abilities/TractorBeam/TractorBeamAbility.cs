using System.Linq;
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
    public sealed class TractorBeamAbility : IPhasedShipAbility
    {
        private readonly TractorBeamSettings _settings;
        private readonly CombatStatModifier _movementPenalty;
        private IShipAbilityFacade _caster;
        private IEntity _target;
        private IHardPointModel _hardPoint;
        private CombatModifiers _targetModifiers;
        private Transform _targetTransform;
        private BeamShot _view;
        private float _range;

        public bool IsComplete => _hardPoint.IsDestroyed || _target.HealthModel.IsDestroyed || _targetTransform == null ||
            _caster.Modifiers.IsIonDisabled ||
            Vector3.Distance(_caster.WorldPosition, _targetTransform.position) > _range;
        public bool SurvivesCasterDeath => false;

        public TractorBeamAbility(TractorBeamSettings settings)
        {
            _settings = settings;
            _movementPenalty = new CombatStatModifier(1f, 1f, settings.SpeedMultiplier, 1f, 1f);
        }

        public bool CanStart(IShipAbilityFacade caster, IEntity target) =>
            target.TryGetFacade(out ICombatModifiersFacade _) &&
            caster.Health.GetShipUnits(HardPointType.TractorBeam).Any(point => !point.IsDestroyed);

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            _caster = caster;
            _target = target;
            _range = definition.Range;
            _hardPoint = caster.Health.GetShipUnits(HardPointType.TractorBeam).First(point => !point.IsDestroyed);
            _targetTransform = target.GetFacade<IEntityTransformFacade>().Transform;
            _targetModifiers = target.GetFacade<ICombatModifiersFacade>().Modifiers;
            _targetModifiers.Add(_movementPenalty);
            _view = (BeamShot)Object.Instantiate(_settings.Beam.ShotPrefab);
            _view.PlayBeam(_hardPoint.Transform, _targetTransform, definition.Duration, _settings.Beam);
        }

        public void Advance(float deltaTime) { }

        public void Stop()
        {
            _targetModifiers.Remove(_movementPenalty);
            if (_view != null)
            {
                _view.StopBeam();
                Object.Destroy(_view.gameObject);
            }
        }
    }
}
